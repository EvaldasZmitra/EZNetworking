using System;
using System.Net;
using System.Net.Sockets;

namespace EZNetworking
{
    public sealed class Client: IDisposable
    {
        public static Client Instance { get; } = new Client();

        private UdpClient _client;
        private Action<Message> _onReceiveMessage;

        public void Start(string address, int port, Action<Message> onReceive)
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
            _onReceiveMessage.Invoke(Serializer.Deserialize<Message>(data));
            _client.BeginReceive(OnReceive, null);
        }

        public void Send(object message)
        {
            var data = Serializer.Serialize(message);
            _client.Send(data.ToArray(), data.Count);
        }

        private void SendConnectionMessage()
        {
            Send(new ConnectionMessage());
        }

        public void Dispose()
        {
            _client.Dispose();
        }
    }
}
