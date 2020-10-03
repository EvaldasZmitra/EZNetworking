using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace EZNetworking
{
    public sealed class Server : IDisposable
    {
        public static Server Instance { get; } = new Server();

        public ushort MaxClients { get; set; } = 50;
        public ushort TickRate { get; set; } = 128;
        public ushort MaxMessagesInQueue { get; set; } = 1000;

        public ConcurrentQueue<Message> ReceiveQueue { get; } = new ConcurrentQueue<Message>();
        public ConcurrentQueue<Message> SendQueue { get; } = new ConcurrentQueue<Message>();
        public Dictionary<IPEndPoint, int> ConnectedClients { get; } = new Dictionary<IPEndPoint, int>();

        private UdpClient _client;
        private int _tick = 0;
        private readonly Stopwatch _stopWatch = new Stopwatch();

        public void Start(int port, Action<long> ServerTick)
        {
            _client = new UdpClient(port);
            _client.BeginReceive(OnReceive, null);
            var timeToWait = TimeSpan.FromSeconds(1.0 / TickRate);

            Task.Run(() =>
            {
                _stopWatch.Restart();
                while (true)
                {
                    ServerTick.Invoke(_tick);
                    SendAll();
                    _tick++;

                    var timeToSleep = timeToWait - _stopWatch.Elapsed;
                    Thread.Sleep(Math.Max((int)timeToSleep.TotalMilliseconds, 0));
                    _stopWatch.Restart();
                }
            });
        }

        public bool Send(object message)
        {
            if(SendQueue.Count >= MaxMessagesInQueue)
            {
                return false;
            }
            var msg = Serializer.Serialize(message).ToArray();
            
            SendQueue.Enqueue(new Message() { 
                Data = msg,
                SequenceNumber = _tick
            });
            return true;
        }

        private void SendAll()
        {
            for(int i=0;i<SendQueue.Count;i++)
            {
                if(SendQueue.First().SequenceNumber <= _tick)
                {
                    SendQueue.TryDequeue(out Message msg);
                    foreach (var client in ConnectedClients)
                    {
                        _client.Send(msg.Data, msg.Data.Count(), client.Key);
                    }
                }
            }
        }

        private void OnReceive(IAsyncResult s)
        {
            if(ReceiveQueue.Count >= MaxMessagesInQueue)
            {
                return;
            }

            var sender = new IPEndPoint(IPAddress.Any, 0);
            var data = _client.EndReceive(s, ref sender);

            if(!ConnectedClients.ContainsKey(sender))
            {
                OnConnected(sender);
            }
            var msg = Serializer.Deserialize<Message>(data);

            if(ConnectedClients[sender] < msg.SequenceNumber)
            {
                ConnectedClients[sender] = msg.SequenceNumber;
                ReceiveQueue.Enqueue(new Message
                {
                    Data = data,
                    SequenceNumber = msg.SequenceNumber
                });
            }

            _client.BeginReceive(OnReceive, null);
        }

        private void OnConnected(IPEndPoint client)
        {
            if(ConnectedClients.Count < MaxClients)
            {
                ConnectedClients.Add(client, 0);
            }
        }

        public void Dispose()
        {
            _client.Dispose();
        }
    }
}
