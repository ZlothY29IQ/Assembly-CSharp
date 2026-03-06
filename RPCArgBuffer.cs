using System.Runtime.InteropServices;

public struct RPCArgBuffer<T>(T argStruct) where T : struct
{
	public T Args = argStruct;

	public byte[] Data = new byte[DataLength];

	public int DataLength = Marshal.SizeOf(typeof(T));
}
