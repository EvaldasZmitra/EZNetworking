namespace EZNetworking
{
    public class Message
    {
        public byte[] Data { get; set; }
        public byte Type { get; set; }
        public int SequenceNumber { get; set; }
        public string Sender { get; set; }
    }

    internal class ConnectionMessage 
    {
        [PacketSerializable]
        public string HelloWorld { get; set; }
    }
}
