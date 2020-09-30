﻿using System;
using System.Collections.Generic;
using System.Text;

namespace EZServer.src
{
    public class MessageHeader
    {
        public static int NumBytes { 
            get {
                var msg = new MessageHeader();
                var msgSerialized = Serializer.Serialize(msg);
                return msgSerialized.Count; 
            } 
        }

        [PacketSerializable]
        public int SequenceNumber { get; set; }

        [PacketSerializable]
        public byte MessageType { get; set; }
    }
}