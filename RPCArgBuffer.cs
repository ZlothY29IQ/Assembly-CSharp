using System.Runtime.InteropServices;

public struct RPCArgBuffer<T> where T : struct
{
	public T Args;

	public byte[] Data;

	public int DataLength;

	public RPCArgBuffer(T argStruct)
	{
		DataLength = Marshal.SizeOf(typeof(T));
		Data = new byte[DataLength];
		Args = argStruct;
	}
}
