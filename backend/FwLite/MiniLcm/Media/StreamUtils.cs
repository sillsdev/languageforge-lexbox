namespace MiniLcm.Media;

public static class StreamUtils
{
    public static long? SafeLength(this Stream stream)
    {
        try
        {
            return stream.Length;
        }
        catch
        {
            //some streams don't know their length, but CanSeek can be false and it still returns it's length, so we just catch it here
            return null;
        }
    }
}
