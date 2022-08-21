namespace codecrafters_sqlite.Utils;


public class FileHelper
{
    public static byte[] ReadBytes(string path, long offset, int count)
    {
        var res = new byte[count];
        using var reader = new BinaryReader(new FileStream(path, FileMode.Open));
        reader.BaseStream.Seek(offset, SeekOrigin.Begin);
        reader.Read(res, 0, count);

        return res;
    }
}