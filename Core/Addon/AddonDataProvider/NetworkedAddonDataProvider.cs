using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Core
{
    public sealed class NetworkedAddonDataProvider : IAddonDataProvider
    {
        private ILogger logger;

        //private Color[] FrameColors;
        private Color[] FrameColors = new Color[100];

        private UdpClient udpClient;
        private IPEndPoint RemoteIpEndPoint;

        private readonly int myPort;
        private readonly string connectTo;
        private readonly int connectPort;

        private readonly byte[] welcome = new byte[] { 0 };

        public NetworkedAddonDataProvider(ILogger logger, int myPort, string connectTo, int connectPort)
        {
            this.logger = logger;

            this.myPort = myPort;
            this.connectTo = connectTo;
            this.connectPort = connectPort;

            udpClient = new UdpClient(myPort);
            RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, myPort);

            Connect();

            FrameColors = new Color[200];
        }
        private void Connect()
        {
            udpClient.Connect(connectTo, connectPort);
        }

        public Color GetColor(int index)
        {
            return FrameColors[index];
        }

        public void Update()
        {
            try
            {
                udpClient.Send(welcome, welcome.Length);

                if(udpClient.Available > 0)
                {
                    byte[] bytes = udpClient.Receive(ref RemoteIpEndPoint);

                    //FrameColors = new Color[bytes.Length / 3];
                    int length = bytes.Length / 3;
                    for (int i = 0; i < length; i++)
                    {
                        FrameColors[i] = Color.FromArgb(bytes[3 * i + 0], bytes[3 * i + 1], bytes[3 * i + 2]);
                    }
                }
            }
            catch(SocketException e)
            {
                logger.LogError(e.Message);
                if(e.ErrorCode == 10054)
                {
                    logger.LogInformation("Reconnecting...");
                    Thread.Sleep(1000);
                    Connect();
                }
            }
        }

        public void Dispose()
        {
            udpClient?.Close();
            udpClient?.Dispose();
        }
    }
}
