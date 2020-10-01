# How to use client
- How to start the client?
    ```
    Client.Instance.Start("127.0.0.1", 55600, OnClientMessage);

    public static void OnClientMessage(byte[] message, short messageType)
    {
    }
    ```
- How to send a message to a server?

    Create a message class made out of basic C# types ```long, int, short, float, double, string, byte, byte[], bool```. Mark it with ```[PacketSerializable]``` if it needs to be sent.
    ```
    class PositionMessage
    {
        [PacketSerializable]
        public float X { get; set; } = 1.0f;

        [PacketSerializable]
        public float Y { get; set; } = 1.0f;

        [PacketSerializable]
        public float Z { get; set; } = 1.0f;

        [PacketSerializable]
        public string Message { get; set; } = "Secret box";
    }
    ```
    Define an enum that will contain type of every message. 
    ```
    public enum MessageType
    {
        ClientMove
    }
    ```
    Now you can send the message (if the client is started).
    ```
    Client.Instance.Send(new PositionMessage(), (byte)MessageType.ClientMove);
    ```
- How to receive a message on client?
    ```
    public static void OnClientMessage(byte[] message, short messageType)
    {
        if ((MessageType)messageType == MessageType.ClientMove)
        {
            var pos = Serializer.Deserialize<PositionMessage>(message);
        }
    }
    ```
- How to start a server?
    ```
    Server.Instance.Start(55600, ServerTick);

    private static void ServerTick(long tick)
    {
    }
    ```

- How to receive messge on server?
    ```
    private static void ServerTick(long tick)
    {
        for(int i=0; i < Server.Instance.ReceiveQueue.Count; i++)
        {
            Server.Instance.ReceiveQueue.TryDequeue(out Message msg);
            switch ((MessageType)msg.Type)
            {
                case MessageType.ClientMove:
                    var pos = Serializer.Deserialize<PositionMessage>(msg.Data);
                    break;
            }
        }
    }
    ```

- How to send a message to all clients?
    ```
    Server.Instance.Send(pos, msg.Type);
    ```

- How to get sequence number of a message in the client?
    ```
    Client.Instance._sequenceNumber
    ```

- How to serialize a message?
    ```
    Serializer.Deserialize<PositionMessage>(msg.Data);
    ```