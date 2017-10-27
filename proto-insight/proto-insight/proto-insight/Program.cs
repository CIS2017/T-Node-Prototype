using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using System.Net;

namespace proto_insight
{
    class Program
    {
        static void Main(string[] args)
        {
            var exitTokenSource = new CancellationTokenSource();

            Console.CancelKeyPress += (s, e) => { exitTokenSource.Cancel(); };

            var wa = BuildWebHost();
            wa.RunAsync().Wait(0);
            exitTokenSource.Token.WaitHandle.WaitOne();

            //    var sessionId = Guid.NewGuid().ToString("d");
            //Send(sessionId, exitTokenSource.Token).Wait(0);
            //Consume(sessionId, exitTokenSource.Token).Wait(0);
        }

        public static IWebHost BuildWebHost()
        {
            return WebHost.CreateDefaultBuilder()
                .UseKestrel(options =>
                {
                    options.Listen(IPAddress.Any, 5000);
                })
                .ConfigureLogging((hostingContext, logging) =>
                {
                    //logging.ClearProviders();
                })
                .UseStartup<InsightWebApp>()
                .Build();
        }

        private static async Task Send(string sessionId, CancellationToken cancellationToken)
        {
            ConnectionFactory factory = new ConnectionFactory();
            factory.Uri = new Uri("amqp://guest:guest@localhost:5672/");
            using (IConnection conn = factory.CreateConnection())
            {
                using (var model = conn.CreateModel())
                {
                    model.ExchangeDeclare("test", ExchangeType.Direct);
                    model.QueueDeclare("test-q1", false, false, true, null);
                    model.QueueBind("test-q1", "test", "", new Dictionary<string, object>());

                    while (!cancellationToken.IsCancellationRequested)
                    {
                        model.BasicPublish("test", "", null, Encoding.UTF8.GetBytes($"{sessionId}:{DateTime.Now.Ticks}"));

                        await Task.Delay(100);
                    }
                }
            }
        }

        private static async Task Consume(string sessionId, CancellationToken cancellationToken)
        {
            await Task.Delay(0);

            ConnectionFactory factory = new ConnectionFactory();
            factory.Uri = new Uri("amqp://guest:guest@localhost:5672/");
            using (IConnection conn = factory.CreateConnection())
            {
                using (var model = conn.CreateModel())
                {
                    var consumer = new EventingBasicConsumer(model);
                    consumer.Received += (sender, msg) =>
                    {
                        var msgTxt = Encoding.UTF8.GetString(msg.Body);
                        if (!msgTxt.StartsWith(sessionId)) { return; }
                        long sentAtTicks = long.Parse(msgTxt.Split(":")[1]);
                        var sentAt = new DateTime(sentAtTicks);
                        Console.WriteLine($"Rcvd latency: {DateTime.Now - sentAt}");
                        model.BasicAck(msg.DeliveryTag, false);
                    };

                    string tag = model.BasicConsume(consumer, "test-q1");

                    cancellationToken.WaitHandle.WaitOne();
                }
            }
        }
        
        //private static async Task Send(string sessionId, CancellationToken cancellationToken)
        //{
        //    string brokerList = "localhost:9092";
        //    string topicName = "test";

        //    var config = new Dictionary<string, object> { { "bootstrap.servers", brokerList } };

        //    using (var producer = new Producer<Null, string>(config, null, new StringSerializer(Encoding.UTF8)))
        //    {
        //        while (!cancellationToken.IsCancellationRequested)
        //        {
        //            var deliveryReport = await producer.ProduceAsync(topicName, null, $"{sessionId}:{DateTime.Now.Ticks}");
        //            await Task.Delay(1000, cancellationToken);
        //        }
        //    }
        //}
        //private static async Task Consume(string sessionId, CancellationToken cancellationToken)
        //{
        //    string brokerList = "localhost:9092";
        //    string topicName = "test";

        //    var config = new Dictionary<string, object>
        //    {
        //        { "group.id", "simple-csharp-consumer" },
        //        { "bootstrap.servers", brokerList }
        //    };

        //    using (var consumer = new Consumer<Null, string>(config, null, new StringDeserializer(Encoding.UTF8)))
        //    {
        //        consumer.Assign(new List<TopicPartitionOffset> { new TopicPartitionOffset(topicName, 0, 0) });

        //        consumer.OnMessage += (s, e) =>
        //        {
        //            var msgTxt = e.Value;
        //            if (!msgTxt.StartsWith(sessionId)) { return; }
        //            long sentAtTicks = long.Parse(msgTxt.Split(":")[1]);
        //            var sentAt = new DateTime(sentAtTicks);

        //            Console.WriteLine($"Rcvd latency: {DateTime.Now - sentAt}");
        //        };

        //        while (!cancellationToken.IsCancellationRequested)
        //        {
        //            consumer.Poll(TimeSpan.FromMilliseconds(50));
        //        }
        //    }

        //}

        //private static async Task Send(string sessionId, CancellationToken cancellationToken)
        //{
        //    var options = new KafkaOptions(new Uri("http://localhost:9092"));
        //    var router = new BrokerRouter(options);
        //    using (var client = new Producer(router))
        //    {
        //        while(!cancellationToken.IsCancellationRequested)
        //        {
        //            await client.SendMessageAsync("test", new[] { new Message($"{sessionId}:{DateTime.Now.Ticks}") });
        //            await Task.Delay(1000, cancellationToken);
        //        }
        //    }
        //}

        //private static async Task Consume(string sessionId, CancellationToken cancellationToken)
        //{
        //    await Task.Delay(0);

        //    var options = new KafkaOptions(new Uri("http://localhost:9092"));
        //    var router = new BrokerRouter(options);
        //    var consumerOptions = new ConsumerOptions("test", router);
        //    consumerOptions.MinimumBytes = 0;

        //    using (var client = new Consumer(consumerOptions))
        //    {
        //        foreach(var msg in client.Consume(cancellationToken))
        //        {
        //            var msgTxt = Encoding.UTF8.GetString(msg.Value);
        //            if(!msgTxt.StartsWith(sessionId)) { continue; }
        //            long sentAtTicks = long.Parse(msgTxt.Split(":")[1]);
        //            var sentAt = new DateTime(sentAtTicks);

        //            Console.WriteLine($"Rcvd latency: {DateTime.Now - sentAt}");
        //        }
        //    }
        //}
    }
}