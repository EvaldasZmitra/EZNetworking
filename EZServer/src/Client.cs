using EZServer.src;
using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace EZServer
{
    public class Client
    {
        public static Client Instance { get; } = new Client();
        private UdpClient _client;
        private Action<byte[], byte> _onReceiveMessage;
        private int _sequenceNumber = 0;
        private int _lastReceivedSequenceNumber = 0;

        private Client()
        {

        }

        public void Start(string address, int port, Action<byte[], byte> onReceive)
        {
            _client = new UdpClient();
            _client.Connect(address, port);
            _onReceiveMessage = onReceive;
            _client.BeginReceive(OnReceive, null);
            SendConnectionMessage();
        }

        private void OnReceive(IAsyncResult s)
        {
            var sender = new IPEndPoint(IPAddress.Any, 0);
            var data = _client.EndReceive(s, ref sender);

            var header = new byte[MessageHeader.NumBytes];
            Array.Copy(data, header, header.Length);
            var headerDeserialized = Serializer.Deserialize<MessageHeader>(header);

            var body = new byte[data.Length - header.Length];
            Array.Copy(data, header.Length, body, 0, body.Length);

            if(headerDeserialized.SequenceNumber > _lastReceivedSequenceNumber)
            {
                _onReceiveMessage.Invoke(body, headerDeserialized.MessageType);
                _lastReceivedSequenceNumber = headerDeserialized.SequenceNumber;
            }
            _client.BeginReceive(OnReceive, null);
        }

        public void Send(object message, byte messageType)
        {
            _sequenceNumber++;
            var data = Serializer.Serialize(new MessageHeader()
            {
                SequenceNumber = _sequenceNumber,
                MessageType = messageType
            });
            data.AddRange(Serializer.Serialize(message));
            _client.Send(data.ToArray(), data.Count);
        }

        private void SendConnectionMessage()
        {
            Send(new ConnectionMessage(), byte.MaxValue);
        }
    }
}
