using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Devices.Gpio;
using System.Reflection;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using System.Collections.Concurrent;
using System.Net;
using System.Threading;
using System.Net.Http;
using Windows.UI.Input;
using proto_contract;
using Newtonsoft.Json;
using Windows.System.Profile;
using Windows.Devices.WiFi;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace proto_ui
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        const string INSIGHT_HOST = "http://protoapi.coreinsightsystems.com";

        static int[] INPUT_PINS = new[] { 4, 27, 5, 13, 18 };
        static int[] SENSOR_DETECT_PINS = new[] { 17, 25, 6, 19, 23 };


        private GpioController gpio;
        private ConcurrentQueue<Tuple<NodeInput, TimeSpan>> inputEvents;
        private ManualResetEventSlim inputEventSignal;
        private Stopwatch timestampAnchor;
        private GpioPin[] pins;
        private BarcodeReceiver barcode;
        private KeyboardDeliveryInterceptor keyboardInterceptor;
        private string nodeId;
        private DispatcherTimer wifiRescanTimer;

        public MainPage()
        {
            this.InitializeComponent();
            InitTimestampGenerator();
            InitUploader();
            InitGpio();
            InitKeyboard();
            InitWifiScan();

            this.nodeId = "";
            if (Windows.Foundation.Metadata.ApiInformation.IsTypePresent("Windows.System.Profile.HardwareIdentification"))
            {
                var token = HardwareIdentification.GetPackageSpecificToken(null);
                var hardwareId = token.Id;
                var dataReader = Windows.Storage.Streams.DataReader.FromBuffer(hardwareId);

                byte[] bytes = new byte[hardwareId.Length];
                dataReader.ReadBytes(bytes);

                nodeId = BitConverter.ToString(bytes).Replace("-", "");
            }
        }

        private void InitWifiScan()
        {
            this.wifiRescanTimer = new DispatcherTimer();
            wifiRescanTimer.Interval = TimeSpan.FromSeconds(30);
            wifiRescanTimer.Tick += WifiRescanTimer_Tick;
            wifiRescanTimer.Start();
        }

        private async void WifiRescanTimer_Tick(object sender, object e)
        {
            try
            {
                wifiRescanTimer.Stop();
                Debug.WriteLine("Scanning wifi");

                var adapters = await WiFiAdapter.FindAllAdaptersAsync();
                foreach(var a in adapters)
                {
                    Debug.WriteLine($"Found adapter: " + a.NetworkAdapter.NetworkAdapterId);
                    foreach(var net in a.NetworkReport.AvailableNetworks)
                    {
                        Debug.WriteLine($"Found network: " + net.Ssid);
                    }
                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            finally
            {
                wifiRescanTimer.Start();
            }
        }

        private void InitKeyboard()
        {
            this.gpio = GpioController.GetDefault();
            if (gpio == null) { return; }

            //dont set up interceptor if we're not on pi as we want to enable typign int he text box

            this.barcode = new BarcodeReceiver();
            this.barcode.BarcodeRecieved += Barcode_BarcodeRecieved;
            this.keyboardInterceptor = KeyboardDeliveryInterceptor.GetForCurrentView();
            keyboardInterceptor.IsInterceptionEnabledWhenInForeground = true;
            keyboardInterceptor.KeyUp += (s, e) => barcode.HandleKeyRelease(e.VirtualKey);
            keyboardInterceptor.KeyDown += (s, e) => barcode.HandleKeyPress(e.VirtualKey);
        }

        private void Barcode_BarcodeRecieved(object sender, string e)
        {
            NodeInput ni = new BarcodeNodeInput()
            {
                Barcode = e
            };

            inputEvents.Enqueue(Tuple.Create(ni, timestampAnchor.Elapsed));
            inputEventSignal.Set();
        }

        private void InitUploader()
        {
            var insightHost = new Uri(INSIGHT_HOST);
            var insightEventUploadUri = new Uri(insightHost, "event");

            inputEventSignal = new ManualResetEventSlim();
            inputEvents = new ConcurrentQueue<Tuple<NodeInput, TimeSpan>>();
            Task.Run(async () =>
            {
                try
                {
                    while (true)
                    {
                        inputEventSignal.Wait(TimeSpan.FromMilliseconds(100));
                        inputEventSignal.Reset();

                        Tuple<NodeInput, TimeSpan> item;
                        while (inputEvents.TryDequeue(out item))
                        {
                            item.Item1.NodeId = this.nodeId;
                            var uploaded = false;
                            while (!uploaded)
                            {
                                try
                                {
                                    var hc = new HttpClient();

                                    var bodyObj = new
                                    {
                                        //how long ago did this event occur relative to right now
                                        EventOffset = (item.Item2 - timestampAnchor.Elapsed).TotalMilliseconds,
                                        Input = item.Item1
                                    };

                                    var body = JsonConvert.SerializeObject(
                                        bodyObj, 
                                        Formatting.None,
                                        new JsonSerializerSettings
                                        {
                                            NullValueHandling = NullValueHandling.Ignore
                                        });

                                    var response = await hc.PutAsync(insightEventUploadUri, new System.Net.Http.StringContent(body));
                                    var responseText = await response.Content.ReadAsStringAsync();
                                    Debug.WriteLine("Response RCVd: " + responseText);

                                    uploaded = true;
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine(ex.Message);
                                }

                                if (!uploaded)
                                {
                                    Debug.WriteLine("Upload failed");
                                    await Task.Delay(1000);
                                }
                            }
                        }
                    }
                }
                catch(Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
                Application.Current.Exit();
            });
        }

        private void InitTimestampGenerator()
        {
            this.timestampAnchor = new Stopwatch();
            timestampAnchor.Start();
        }

        private void InitGpio()
        {
            this.gpio = GpioController.GetDefault();
            if (gpio == null) { return; }

            this.pins = new GpioPin[INPUT_PINS.Length];
            for(var i = 0; i < INPUT_PINS.Length; i++)
            {
                var ioNum = i;
                var pinNum = INPUT_PINS[i];
                var pin = gpio.OpenPin(pinNum, GpioSharingMode.Exclusive);
                pin.SetDriveMode(GpioPinDriveMode.InputPullUp);
                pin.DebounceTimeout = TimeSpan.FromMilliseconds(50);
                pin.ValueChanged += (evtPin, evt) => Pin_ValueChanged(evtPin, evt, ioNum);
                pins[ioNum] = pin;
            }
        }

        private void Pin_ValueChanged(GpioPin evtPin, GpioPinValueChangedEventArgs evt, int sensorIndex)
        {
            Debug.WriteLine($"PIN:{evtPin.PinNumber}, EDGE:{evt.Edge}, TIME:{DateTimeOffset.Now}");
            NodeInput di = new DigitalNodeInput
            {
                Edge = evt.Edge == GpioPinEdge.FallingEdge ? DigitalNodeInput.EdgeType.Falling : DigitalNodeInput.EdgeType.Rising,
                InputIndex = sensorIndex
            };
            inputEvents.Enqueue(Tuple.Create(di, timestampAnchor.Elapsed));
            inputEventSignal.Set();
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            NodeInput ni = new BarcodeNodeInput()
            {
                Barcode = this.sampleBc.Text
            };

            await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                this.sampleBc.Text = "";
            });

            inputEvents.Enqueue(Tuple.Create(ni, timestampAnchor.Elapsed));
            inputEventSignal.Set();
        }

        private void sampleBc_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }
}
