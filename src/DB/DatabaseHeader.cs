using codecrafters_sqlite.Utils;
using static System.Buffers.Binary.BinaryPrimitives;


namespace codecrafters_sqlite.DB;


public record DatabaseHeader(ushort PageSize)
{
    public static DatabaseHeader Parse(string path)
    {
        var header = FileHelper.ReadBytes(path, 0, 100);
        return new DatabaseHeader(ReadUInt16BigEndian(header[16..18]));
    }
}