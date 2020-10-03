using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EZNetworking
{
    [AttributeUsage(AttributeTargets.Property)]
    public class PacketSerializable: Attribute
    {

    }

    public static class Serializer
    {
        internal static List<byte> Serialize(object o)
        {
            var bytes = new List<byte>();
            var properties = o.GetType().GetProperties().Where(x => x.GetCustomAttributes(typeof(PacketSerializable), true).Any()).ToList();

           foreach (var propertyInfo in properties)
            {
                switch(propertyInfo.PropertyType.Name)
                {
                    case "Int64":
                        var valueLong = (long)propertyInfo.GetValue(o);
                        bytes.AddRange(BitConverter.GetBytes(valueLong));
                        break;

                    case "Int32":
                        var valueInt = (int)propertyInfo.GetValue(o);
                        bytes.AddRange(BitConverter.GetBytes(valueInt));
                        break;

                    case "Int16":
                        var valueShort = (short)propertyInfo.GetValue(o);
                        bytes.AddRange(BitConverter.GetBytes(valueShort));
                        break;

                    case "Single":
                        var valueFloat = (float)propertyInfo.GetValue(o);
                        bytes.AddRange(BitConverter.GetBytes(valueFloat));
                        break;

                    case "Double":
                        var valueDouble = (double)propertyInfo.GetValue(o);
                        bytes.AddRange(BitConverter.GetBytes(valueDouble));
                        break;

                    case "String":
                        var valueString = (string)propertyInfo.GetValue(o);
                        bytes.AddRange(BitConverter.GetBytes((short)valueString.Length));
                        bytes.AddRange(Encoding.ASCII.GetBytes(valueString));
                        break;

                    case "Byte":
                        var valueByte = (byte)propertyInfo.GetValue(o);
                        bytes.Add(valueByte);
                        break;

                    case "Byte[]":
                        var valueBytes = (byte[])propertyInfo.GetValue(o);
                        bytes.AddRange(BitConverter.GetBytes((short)valueBytes.Length));
                        bytes.AddRange(valueBytes);
                        break;

                    case "Boolean":
                        var valueBool = (bool)propertyInfo.GetValue(o);
                        bytes.AddRange(BitConverter.GetBytes(valueBool));
                        break;

                    default:
                        break;
                }
            }
            return bytes;
        }

        public static T Deserialize<T>(byte[] bytes)
        {
            var rez = Activator.CreateInstance(typeof(T));
            var readPos = 0;
            var properties = typeof(T).GetProperties().Where(x => x.GetCustomAttributes(typeof(PacketSerializable), true).Any()).ToList();

            foreach (var propertyInfo in properties)
            {
                switch (propertyInfo.PropertyType.Name)
                {
                    case "Int64":
                        var valueLong = BitConverter.ToInt64(bytes, readPos);
                        readPos += 8;
                        propertyInfo.SetValue(rez, valueLong);
                        break;

                    case "Int32":
                        var valueInt = BitConverter.ToInt32(bytes, readPos);
                        readPos += 4;
                        propertyInfo.SetValue(rez, valueInt);
                        break;

                    case "Int16":
                        var valueShort = BitConverter.ToInt16(bytes, readPos);
                        readPos += 2;
                        propertyInfo.SetValue(rez, valueShort);
                        break;

                    case "Single":
                        var valueFloat = BitConverter.ToSingle(bytes, readPos);
                        readPos += 4;
                        propertyInfo.SetValue(rez, valueFloat);
                        break;

                    case "Double":
                        var valueDouble = BitConverter.ToDouble(bytes, readPos);
                        readPos += 8;
                        propertyInfo.SetValue(rez, valueDouble);
                        break;

                    case "Boolean":
                        var valueBool = BitConverter.ToBoolean(bytes, readPos);
                        readPos += 1;
                        propertyInfo.SetValue(rez, valueBool);
                        break;

                    case "String":
                        var stringLength = BitConverter.ToInt16(bytes, readPos);
                        readPos += 2;
                        var stringValue = Encoding.ASCII.GetString(bytes, readPos, stringLength);
                        readPos += stringLength;
                        propertyInfo.SetValue(rez, stringValue);
                        break;

                    case "Byte":
                        var valueByte = bytes[readPos];
                        readPos += 1;
                        propertyInfo.SetValue(rez, valueByte);
                        break;

                    case "Byte[]":
                        var bytesLength = BitConverter.ToInt16(bytes, readPos);
                        readPos += 2;
                        var bytesValue = new byte[bytesLength];
                        Array.Copy(bytes, readPos, bytesValue, 0, bytesLength);
                        readPos += bytesLength;
                        propertyInfo.SetValue(rez, bytesValue);
                        break;

                    default:
                        break;
                }
            }
            return (T)rez;
        }
    }
}
