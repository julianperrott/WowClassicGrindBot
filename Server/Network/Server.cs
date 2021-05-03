using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Net.Sockets;

namespace Server.Network
{
    public sealed class Server : IDisposable
    {
        private readonly ILogger logger;
        private readonly int port;

        private readonly IDataProvider provider;

        private UdpClient newsock;
        private IPEndPoint sender;

        public Server(ILogger logger, int port, IDataProvider provider)
        {
            this.logger = logger;
            this.port = port;
            this.provider = provider;

            sender = new IPEndPoint(IPAddress.Any, 0);
        }

        public void ListenServer()
        {
            IPEndPoint ipep = new IPEndPoint(IPAddress.Any, this.port);
            newsock = new UdpClient(ipep);

            logger.LogInformation($"Listening on 0.0.0.0:{port}");

            try
            {
                do
                {
                    while (!Console.KeyAvailable)
                    {
                        var data = new byte[1];
                        data = newsock.Receive(ref sender);

                        if (provider.HasData())
                        {
                            var d = provider.GetData();
                            newsock.Send(d, d.Length, sender);
                        }
                    }
                }
                while (Console.ReadKey(true).Key != ConsoleKey.Escape);
            }
            catch(Exception e)
            {
                logger.LogError(e.Message);
            }
        }

        public void Dispose()
        {
            newsock?.Close();
            newsock?.Dispose();
        }
    }
}
