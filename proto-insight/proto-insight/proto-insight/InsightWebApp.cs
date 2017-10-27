using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using proto_contract;
using proto_domain;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace proto_insight
{
    public class InsightWebApp
    {
        public InsightWebApp()
        {
        }

        public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
        {
            var serverAddressesFeature = app.ServerFeatures.Get<IServerAddressesFeature>();

            app.UseWebSockets();

            var nodeEvents = new BlockingCollection<NodeInput>();
            var line = new ProtoCoreLine();

            app.Use(async (context, next) =>
            {
                if (context.Request.Path == "/ws")
                {
                    if (context.WebSockets.IsWebSocketRequest)
                    {
                        WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
                        await UiSocket(context, webSocket, line);
                    }
                    else
                    {
                        context.Response.StatusCode = 400;
                    }
                }
                else
                {
                    await next();
                }
            });

            app.Use(async (context, next) =>
            {
                if (context.Request.Path == "/event" && context.Request.Method == "PUT")
                {
                    using (var sr = new StreamReader(context.Request.Body))
                    {
                        var evt = sr.ReadToEnd();
                        var body = JsonConvert.DeserializeObject<NodeInputUpload>(evt, new JsonSerializerSettings
                        {
                            ContractResolver = new NodeInputUploadContractResolver()
                        });

                        body.Input.Timestamp = DateTimeOffset.Now.AddMilliseconds(body.EventOffset);
                        Console.WriteLine(body.Input.ToString());
                        line.HandleNodeInput(body.Input);

                    }

                    using (var sw = new StreamWriter(context.Response.Body))
                    {
                        sw.WriteLine(DateTimeOffset.UtcNow.Ticks);
                    }
                }
                else
                {
                    await next();
                }
            });

            app.Run(async (context) =>
            {
                context.Response.ContentType = "text/html";

                await context.Response.WriteAsync($"<p>Request URL: {context.Request.GetDisplayUrl()}<p>");
            });
        }

        private async Task UiSocket(HttpContext context, WebSocket webSocket, ProtoCoreLine line)
        {
            EventHandler<ProtoCoreLine> handler = async (s, e) =>
            {
                if (webSocket.State == WebSocketState.Open)
                {
                    var json = JsonConvert.SerializeObject(e);
                    var buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(json));
                    await webSocket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
                }
            };

            line.StateChange += handler;

            if (webSocket.State == WebSocketState.Open)
            {
                var json = JsonConvert.SerializeObject(line);
                var buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(json));
                await webSocket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
            }

            while (webSocket.State == WebSocketState.Open)
            {
                await Task.Delay(1000);
            }

            line.StateChange -= handler;
        }

        //private async Task EchoSocket(HttpContext context, WebSocket webSocket, BlockingCollection<string> nodeEvents)
        //{
        //    var buffer = new byte[1024 * 4];
        //    WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
        //    while (!result.CloseStatus.HasValue)
        //    {
        //        await webSocket.SendAsync(new ArraySegment<byte>(buffer, 0, result.Count), result.MessageType, result.EndOfMessage, CancellationToken.None);

        //        result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
        //    }

        //    await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
        //}
    }
}