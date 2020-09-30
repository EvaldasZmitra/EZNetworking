namespace EZServer.src
{
    public class Message
    {
        public byte[] Data { get; set; }
        public byte Type { get; set; }
        public int SequenceNumber { get; set; }
        public string Sender { get; set; }
    }
}
