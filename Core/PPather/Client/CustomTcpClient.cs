using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Timers;
using Timer = System.Timers.Timer;

namespace Core
{
    public abstract class CustomTcpClient
    {
        private int connectionTimerBusy;

        private readonly ILogger logger;

        public int watchdogPollMs { get; private set; }

        public IPAddress Ip { get; private set; }

        public bool IsConnected { get; private set; }

        public int Port { get; private set; }

        protected Timer ConnectionWatchdog { get; private set; }

        protected BinaryReader? Reader { get; private set; }

        protected NetworkStream? Stream { get; private set; }

        protected TcpClient? TcpClient { get; private set; }

        private int ConnectionFailedCounter { get; set; }


        public CustomTcpClient(ILogger logger, string ip, int port, int watchdogPollMs = 1000)
        {
            this.logger = logger;
            Ip = IPAddress.Parse(ip);
            Port = port;

            this.watchdogPollMs = watchdogPollMs;

            ConnectionWatchdog = new Timer(watchdogPollMs);
            ConnectionWatchdog.Elapsed += ConnectionWatchdogTick;
            ConnectionWatchdog.Start();

            ConnectionFailedCounter = 100;
        }

        public CustomTcpClient(ILogger logger, IPAddress ip, int port, int watchdogPollMs = 1000)
        {
            this.logger = logger;
            Ip = ip;
            Port = port;

            ConnectionWatchdog = new Timer(watchdogPollMs);
            ConnectionWatchdog.Elapsed += ConnectionWatchdogTick;
            ConnectionWatchdog.Start();

            ConnectionFailedCounter = 100;
        }

        public void Disconnect()
        {
            ConnectionWatchdog.Stop();
            TcpClient?.Close();

            Reader?.Close();
            Stream?.Close();
        }

        public unsafe byte[]? SendData<T>(T data, int size) where T : unmanaged
        {
            if (Stream == null)
            {
                return null;
            }

            //var s = BitConverter.GetBytes(size);
            //var span = new Span<byte>(&data, size);
            //logger.LogInformation($"size:{size} s: {s.Length}");
            //logger.LogInformation($"data:{data} s: {span.Length}");

            Stream.Write(BitConverter.GetBytes(size));
            Stream.Write(new Span<byte>(&data, size));
            Stream.Flush();

            if (Reader == null)
                return null;

            int dataSize = BitConverter.ToInt32(Reader.ReadBytes(4), 0);
            return Reader.ReadBytes(dataSize);
        }

        private void ConnectionWatchdogTick(object sender, ElapsedEventArgs e)
        {
            // only start one timer tick at a time
            if (Interlocked.CompareExchange(ref connectionTimerBusy, 1, 0) == 1)
            {
                return;
            }

            //logger.LogInformation($"ConnectionWatchdogTick {connectionTimerBusy}");

            try
            {
                if (TcpClient == null)
                {
                    TcpClient = new TcpClient() { NoDelay = true };
                    TcpClient.ConnectAsync(Ip, Port).Wait();

                    if (TcpClient.Client.Connected)
                    {
                        Stream = TcpClient.GetStream();
                        Stream.ReadTimeout = 1000;
                        Stream.WriteTimeout = 1000;

                        Reader = new BinaryReader(Stream);

                        ConnectionFailedCounter = 0;
                    }
                }
                else
                {
                    IsConnected = Stream != null && Reader != null && SendData(0, 4)?[0] == 1;

                    if (!IsConnected)
                    {
                        ++ConnectionFailedCounter;
                    }
                    else
                    {
                        ConnectionFailedCounter = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                ++ConnectionFailedCounter;
            }
            finally
            {
                if (ConnectionFailedCounter > 3)
                {
                    TcpClient?.Close();
                    TcpClient?.Dispose();
                    TcpClient = null;

                    Reader?.Close();
                    Stream?.Close();

                    IsConnected = false;

                    logger.LogWarning("Connection is closed!");
                }

                connectionTimerBusy = 0;
            }
        }
    }
}
