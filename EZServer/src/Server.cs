using EZServer.src;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace EZServer
{
    public class ClientOnServer
    {
        public int LastReceivedMessage { get; set; }
    }

    public class Server
    {
        public static Server Instance { get; } = new Server();

        public ushort MaxClients = 50;
        public ushort TickRate = 128;
        public ushort MaxMessagesInQueue = 1000;

        public ConcurrentQueue<Message> ReceiveQueue { get; } = new ConcurrentQueue<Message>();
        public ConcurrentQueue<Message> SendQueue { get; } = new ConcurrentQueue<Message>();
        public Dictionary<IPEndPoint, ClientOnServer> ConnectedClients { get; } = new Dictionary<IPEndPoint, ClientOnServer>();

        private UdpClient _client;
        private int _tick = 0;
        private Stopwatch _stopWatch = new Stopwatch();

        private Server()
        {
            
        }

        public void Start(int port, Action<long> ServerTick)
        {
            _client = new UdpClient(port);
            _client.BeginReceive(OnReceive, null);
            var timeToWait = TimeSpan.FromSeconds(1.0 / TickRate);

            Task.Run(() =>
            {
                var numTicks = Stopwatch.Frequency / TickRate;
                _stopWatch.Restart();
                while (true)
                {
                    ServerTick.Invoke(_tick);
                    SendAll();
                    _tick++;

                    var timeToSleep = timeToWait - _stopWatch.Elapsed;
                    if(timeToSleep.TotalMilliseconds > 0)
                    {
                        Console.WriteLine($"Can sleep another {timeToSleep.TotalMilliseconds}ms.");
                        Thread.Sleep(timeToSleep);
                    }
                    else
                    {
                        Console.WriteLine($"Too soon! {ReceiveQueue.Count} {SendQueue.Count}");
                    }
                    _stopWatch.Restart();
                }
            });
        }

        public bool Send(object message, byte messageType)
        {
            if(SendQueue.Count >= MaxMessagesInQueue)
            {
                return false;
            }
            var msg = Serializer.Serialize(message).ToArray();
            SendQueue.Enqueue(new Message() { 
                Data = msg,
                Type = messageType,
                SequenceNumber = _tick
            });
            return true;
        }

        private void SendAll()
        {
            for(int i=0;i<SendQueue.Count;i++)
            {
                if(SendQueue.First().SequenceNumber == _tick)
                {
                    SendQueue.TryDequeue(out Message msg);
                    var data = msg.Data.ToList();
                    var header = new MessageHeader { MessageType = msg.Type, SequenceNumber = _tick };
                    var headerSerialized = Serializer.Serialize(header).ToArray();
                    data.InsertRange(0, headerSerialized);
                    foreach (var client in ConnectedClients)
                    {
                        _client.Send(data.ToArray(), data.Count, client.Key);
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

            var header = new byte [MessageHeader.NumBytes];
            Array.Copy(data, 0, header, 0, header.Length);
            var headerDeserialized = Serializer.Deserialize<MessageHeader>(header);

            var body = new byte[data.Length - header.Length];
            Array.Copy(data, header.Length, body, 0, body.Length);

            if(!ConnectedClients.ContainsKey(sender))
            {
                OnConnected(sender);
            }
            if(ConnectedClients[sender].LastReceivedMessage < headerDeserialized.SequenceNumber)
            {
                ConnectedClients[sender].LastReceivedMessage = headerDeserialized.SequenceNumber;
                ReceiveQueue.Enqueue(new Message
                {
                    Data = body,
                    SequenceNumber = headerDeserialized.SequenceNumber,
                    Type = headerDeserialized.MessageType,
                    Sender = sender.ToString()
                });
            }

            _client.BeginReceive(OnReceive, null);
        }

        private void OnConnected(IPEndPoint client)
        {
            if(ConnectedClients.Count < MaxClients)
            {
                ConnectedClients.Add(client, new ClientOnServer());
            }
        }
    }
}
