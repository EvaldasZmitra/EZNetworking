using EZServer.src;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace EZServer
{
    public enum MessageType
    {
        ClientMove
    }

    class PositionMessage
    {
        [PacketSerializable]
        public float X { get; set; } = 1.0f;

        [PacketSerializable]
        public float Y { get; set; } = 1.0f;

        [PacketSerializable]
        public float Z { get; set; } = 1.0f;

        [PacketSerializable]
        public string Message { get; set; } = "Noob";

        [PacketSerializable]
        public string Placeholder { get; set; } = "Noob asdhfbdsafbnjdsan jfn jdsfnj fnjsadnfj nsadkjfnja nsdjnf jasdnkjfnsad";

        [PacketSerializable]
        public string Name { get; set; } = "Secret box";
    }

    class Program
    {
        public static void OnClientMessage(byte[] message, short messageType)
        {
            if ((MessageType)messageType == MessageType.ClientMove)
            {
                var pos = Serializer.Deserialize<PositionMessage>(message);
                //Console.WriteLine($"Received message from server, {pos.Name} pos is {pos.X}, {pos.Y}");
            }
        }

        private static void ServerTick(long tick)
        {
            for(int i=0; i < Server.Instance.ReceiveQueue.Count; i++)
            {
                Server.Instance.ReceiveQueue.TryDequeue(out Message msg);
                switch ((MessageType)msg.Type)
                {
                    case MessageType.ClientMove:
                        var pos = Serializer.Deserialize<PositionMessage>(msg.Data);
                        pos.X += 1.0f;
                        Server.Instance.Send(pos, msg.Type);
                        Thread.Sleep(1);
                        break;
                }
            }
        }

        static void Main(string[] args)
        {
            Server.Instance.Start(55600, ServerTick);

            Client.Instance.Start("127.0.0.1", 55600, OnClientMessage);

            Task.Run(() =>
            {
                while(true)
                {
                    Client.Instance.Send(new PositionMessage(), (byte)MessageType.ClientMove);
                    if(Client.Instance._sequenceNumber < 2000)
                    {
                        Thread.Sleep(TimeSpan.FromMilliseconds(1));
                    }
                    else
                    {
                        Thread.Sleep(TimeSpan.FromMilliseconds(7.8));
                    }
                }
            });


            Console.ReadKey();
        }
    }
}
