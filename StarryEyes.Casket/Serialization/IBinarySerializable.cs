using System.IO;

namespace StarryEyes.Casket.Serialization
{
    public interface IBinarySerializable
    {
        void Serialize(BinaryWriter writer);

        void Deserialize(BinaryReader reader);
    }
}
