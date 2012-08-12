using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace StarryEyes.Vanille.Serialization
{
    public static class BinarySerialization
    {
        public static void Serialize(Stream stream, IBinarySerializable item)
        {
            using (var bw = new BinaryWriter(stream))
            {
                item.Serialize(bw);
            }
        }

        public static void SerializeCollection<T>(Stream stream, IEnumerable<T> item)
            where T : IBinarySerializable, new()
        {
            using (var bw = new BinaryWriter(stream))
            {
                bw.Write(item);
            }
        }

        public static T Deserialize<T>(Stream stream)
            where T : IBinarySerializable, new()
        {
            using (var br = new BinaryReader(stream))
            {
                var item = new T();
                item.Deserialize(br);
                return item;
            }
        }

        public static IEnumerable<T> DeserializeCollection<T>(Stream stream)
            where T : IBinarySerializable, new()
        {
            using (var br = new BinaryReader(stream))
            {
                return br.ReadCollection<T>().ToArray();
            }
        }

    }

    public static class SerializationHelper
    {
        public static void Write(this BinaryWriter writer, IBinarySerializable item)
        {
            if (item != null)
            {
                writer.Write(true);
                item.Serialize(writer);
            }
            else
            {
                writer.Write(false);
            }
        }

        public static void Write(this BinaryWriter writer, long? value)
        {
            if (value.HasValue)
            {
                writer.Write(true);
                writer.Write(value.Value);
            }
            else
            {
                writer.Write(false);
            }
        }

        public static void Write(this BinaryWriter writer, double? value)
        {
            if (value.HasValue)
            {
                writer.Write(true);
                writer.Write(value.Value);
            }
            else
            {
                writer.Write(false);
            }
        }

        public static void Write(this BinaryWriter writer, Uri value)
        {
            writer.Write(value.OriginalString);
        }

        public static void Write(this BinaryWriter writer, DateTime value)
        {
            writer.Write(value.ToFileTime());
        }

        public static void Write(this BinaryWriter writer, IEnumerable<long> items)
        {
            var array = items.ToArray();
            writer.Write(array.Length);
            array.ForEach(i => writer.Write(i));
        }

        public static void Write<T>(this BinaryWriter writer, IEnumerable<T> item) where T : IBinarySerializable, new()
        {
            var ia = item.ToArray();
            writer.Write(ia.Length);
            item.ForEach(i => i.Serialize(writer));
        }

        public static IEnumerable<long> ReadIds(this BinaryReader reader)
        {
            int num = reader.ReadInt32();
            for (int i = 0; i < num; i++)
            {
                yield return reader.ReadInt64();
            }
        }

        public static IEnumerable<T> ReadCollection<T>(this BinaryReader reader) where T : IBinarySerializable, new()
        {
            int num = reader.ReadInt32();
            for (int i = 0; i < num; i++)
            {
                var item = new T();
                item.Deserialize(reader);
                yield return item;
            }
        }

        public static long? ReadNullableLong(this BinaryReader reader)
        {
            if (reader.ReadBoolean())
                return reader.ReadInt64();
            else
                return null;
        }

        public static double? ReadNullableDouble(this BinaryReader reader)
        {
            if (reader.ReadBoolean())
                return reader.ReadDouble();
            else
                return null;
        }

        public static Uri ReadUri(this BinaryReader reader)
        {
            var uristr = reader.ReadString();
            return new Uri(uristr);
        }

        public static DateTime ReadDateTime(this BinaryReader reader)
        {
            return DateTime.FromFileTime(reader.ReadInt64());
        }

        public static T ReadObject<T>(this BinaryReader reader) where T : IBinarySerializable, new()
        {
            if (reader.ReadBoolean())
            {
                var item = new T();
                item.Deserialize(reader);
                return item;
            }
            else
            {
                return default(T);
            }
        }
    }
}