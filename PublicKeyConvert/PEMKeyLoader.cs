using System;
using System.IO;
using System.Security.Cryptography;

namespace PublicKeyConvert;

public class PEMKeyLoader
{
	private static byte[] SeqOID = new byte[15]
	{
		48, 13, 6, 9, 42, 134, 72, 134, 247, 13,
		1, 1, 1, 5, 0
	};

	private static bool CompareBytearrays(byte[] a, byte[] b)
	{
		if (a.Length != b.Length)
		{
			return false;
		}
		int num = 0;
		for (int i = 0; i < a.Length; i++)
		{
			if (a[i] != b[num])
			{
				return false;
			}
			num++;
		}
		return true;
	}

	public static RSACryptoServiceProvider CryptoServiceProviderFromPublicKeyInfo(byte[] x509key)
	{
		_ = new byte[15];
		if (x509key == null || x509key.Length == 0)
		{
			return null;
		}
		BinaryReader binaryReader = new BinaryReader(new MemoryStream(x509key));
		ushort num = 0;
		try
		{
			switch (binaryReader.ReadUInt16())
			{
			case 33072:
				binaryReader.ReadByte();
				break;
			case 33328:
				binaryReader.ReadInt16();
				break;
			default:
				return null;
			}
			if (!CompareBytearrays(binaryReader.ReadBytes(15), SeqOID))
			{
				return null;
			}
			switch (binaryReader.ReadUInt16())
			{
			case 33027:
				binaryReader.ReadByte();
				break;
			case 33283:
				binaryReader.ReadInt16();
				break;
			default:
				return null;
			}
			if (binaryReader.ReadByte() != 0)
			{
				return null;
			}
			switch (binaryReader.ReadUInt16())
			{
			case 33072:
				binaryReader.ReadByte();
				break;
			case 33328:
				binaryReader.ReadInt16();
				break;
			default:
				return null;
			}
			num = binaryReader.ReadUInt16();
			byte b = 0;
			byte b2 = 0;
			switch (num)
			{
			case 33026:
				b = binaryReader.ReadByte();
				break;
			case 33282:
				b2 = binaryReader.ReadByte();
				b = binaryReader.ReadByte();
				break;
			default:
				return null;
			}
			int num2 = BitConverter.ToInt32(new byte[4] { b, b2, 0, 0 }, 0);
			if (binaryReader.PeekChar() == 0)
			{
				binaryReader.ReadByte();
				num2--;
			}
			byte[] modulus = binaryReader.ReadBytes(num2);
			if (binaryReader.ReadByte() != 2)
			{
				return null;
			}
			int count = binaryReader.ReadByte();
			byte[] exponent = binaryReader.ReadBytes(count);
			RSACryptoServiceProvider rSACryptoServiceProvider = new RSACryptoServiceProvider();
			rSACryptoServiceProvider.ImportParameters(new RSAParameters
			{
				Modulus = modulus,
				Exponent = exponent
			});
			return rSACryptoServiceProvider;
		}
		finally
		{
			binaryReader.Close();
		}
	}

	public static RSACryptoServiceProvider CryptoServiceProviderFromPublicKeyInfo(string base64EncodedKey)
	{
		try
		{
			return CryptoServiceProviderFromPublicKeyInfo(Convert.FromBase64String(base64EncodedKey));
		}
		catch (FormatException)
		{
		}
		return null;
	}
}
