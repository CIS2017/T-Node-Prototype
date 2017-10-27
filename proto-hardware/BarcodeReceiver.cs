using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.System;

namespace proto_ui
{
    public class BarcodeReceiver
    {
        private object handleKeyLocker = new object();
        private bool shift;
        private StringBuilder currentBarcode;

        public BarcodeReceiver()
        {
            this.shift = false;
            this.currentBarcode = new StringBuilder();
        }

        public event EventHandler<string> BarcodeRecieved;

        public void HandleKeyPress(VirtualKey key)
        {
            lock (handleKeyLocker)
            {
                if (key == VirtualKey.Shift)
                {
                    shift = true;
                }
                else if ( key != VirtualKey.Enter )
                {
                    currentBarcode.Append(GetCharFromKey(key, shift));
                }
            }
        }

        public void HandleKeyRelease(VirtualKey key)
        {
            lock (handleKeyLocker)
            {
                if (key == VirtualKey.Enter)
                {
                    BarcodeRecieved?.Invoke(this, currentBarcode.ToString());
                    currentBarcode.Clear();
                }
                else if (key == VirtualKey.Shift)
                {
                    shift = false;
                }
            }
        }

        private string GetCharFromKey(VirtualKey k, bool isShift)
        {
            if((int)k >= (int)VirtualKey.A && (int)k <= (int)VirtualKey.Z)
            {
                var c = "" + (char)('A' + ((int)k - (int)VirtualKey.A));
                return isShift ? c.ToUpper() : c.ToLower();
            }

            var numKeysShift = ")!@#$%^&*(";
            if ((int)k >= (int)VirtualKey.Number0 && (int)k <= (int)VirtualKey.Number9)
            {
                var digitOffset = (int)k - (int)VirtualKey.Number0;
                return isShift ? numKeysShift[digitOffset].ToString() : digitOffset.ToString();
            }

            if ((int)k >= (int)VirtualKey.NumberPad0 && (int)k <= (int)VirtualKey.NumberPad9)
            {
                var digit = (int)k - (int)VirtualKey.NumberPad0;
                return digit.ToString();
            }

            var otherChars = new Dictionary<VirtualKey, string>()
            {
                { VirtualKey.Space, "  " }
                , {(VirtualKey)188, ",<" }
                , {(VirtualKey)190, ".>" }
                , {(VirtualKey)191, "/?" }
                , {(VirtualKey)192, "`~" }
                , {(VirtualKey)189, "-_" }
                , {(VirtualKey)187, "=+" }
                , {(VirtualKey)186, ";:" }
                , {(VirtualKey)222, "'\"" }
                , {(VirtualKey)219, "[{" }
                , {(VirtualKey)221, "]}" }
                , {(VirtualKey)220, "\\|" }
            };
            string other;
            if(otherChars.TryGetValue(k, out other))
            {
                return "" + (isShift ? other[1] : other[0]);
            }

            return k.ToString();
        }
    }
}
