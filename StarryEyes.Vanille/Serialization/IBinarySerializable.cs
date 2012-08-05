using System.IO;

namespace StarryEyes.Vanille.Serialization
{
    public interface IBinarySerializable 
    {
        void Serialize(BinaryWriter writer);

        void Deserialize(BinaryReader reader);
    }
}
