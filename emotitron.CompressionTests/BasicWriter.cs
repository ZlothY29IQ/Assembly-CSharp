namespace emotitron.CompressionTests;

public class BasicWriter
{
	public static int pos;

	public static void Reset()
	{
		pos = 0;
	}

	public static byte[] BasicWrite(byte[] buffer, byte value)
	{
		buffer[pos] = value;
		pos++;
		return buffer;
	}

	public static byte BasicRead(byte[] buffer)
	{
		byte result = buffer[pos];
		pos++;
		return result;
	}
}
