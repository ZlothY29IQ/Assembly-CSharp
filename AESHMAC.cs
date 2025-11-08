using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

public static class AESHMAC
{
	private static readonly RandomNumberGenerator gRNG = RandomNumberGenerator.Create();

	public const int BlockBitSize = 128;

	public const int KeyBitSize = 256;

	public const int SaltBitSize = 64;

	public const int Iterations = 10000;

	public const int MinPasswordLength = 12;

	public static byte[] NewKey()
	{
		byte[] array = new byte[32];
		gRNG.GetBytes(array);
		return array;
	}

	public static string SimpleEncrypt(string plaintext, byte[] key, byte[] auth, byte[] salt = null)
	{
		if (string.IsNullOrEmpty(plaintext))
		{
			throw new ArgumentNullException("plaintext");
		}
		return Convert.ToBase64String(SimpleEncrypt(Encoding.UTF8.GetBytes(plaintext), key, auth, salt));
	}

	public static string SimpleDecrypt(string ciphertext, byte[] key, byte[] auth, int saltLength = 0)
	{
		if (string.IsNullOrWhiteSpace(ciphertext))
		{
			throw new ArgumentNullException("ciphertext");
		}
		byte[] array = SimpleDecrypt(Convert.FromBase64String(ciphertext), key, auth, saltLength);
		if (array != null)
		{
			return Encoding.UTF8.GetString(array);
		}
		return null;
	}

	public static string SimpleEncryptWithKey(string plaintext, string key, byte[] salt = null)
	{
		if (string.IsNullOrEmpty(plaintext))
		{
			throw new ArgumentNullException("plaintext");
		}
		return Convert.ToBase64String(SimpleEncryptWithKey(Encoding.UTF8.GetBytes(plaintext), key, salt));
	}

	public static string SimpleDecryptWithKey(string ciphertext, string key, int saltLength = 0)
	{
		if (string.IsNullOrWhiteSpace(ciphertext))
		{
			throw new ArgumentNullException("ciphertext");
		}
		byte[] array = SimpleDecryptWithKey(Convert.FromBase64String(ciphertext), key, saltLength);
		if (array != null)
		{
			return Encoding.UTF8.GetString(array);
		}
		return null;
	}

	public static byte[] SimpleEncrypt(byte[] plaintext, byte[] key, byte[] auth, byte[] salt = null)
	{
		if (key == null || key.Length != 32)
		{
			throw new ArgumentException($"Key must be {256} bits", "key");
		}
		if (auth == null || auth.Length != 32)
		{
			throw new ArgumentException($"Auth must be {256} bits", "auth");
		}
		if (plaintext == null || plaintext.Length < 1)
		{
			throw new ArgumentNullException("plaintext");
		}
		if (salt == null)
		{
			salt = new byte[0];
		}
		byte[] iV;
		byte[] buffer;
		using (AesManaged aesManaged = new AesManaged
		{
			KeySize = 256,
			BlockSize = 128,
			Mode = CipherMode.CBC,
			Padding = PaddingMode.PKCS7
		})
		{
			aesManaged.GenerateIV();
			iV = aesManaged.IV;
			using ICryptoTransform transform = aesManaged.CreateEncryptor(key, iV);
			using MemoryStream memoryStream = new MemoryStream();
			using (CryptoStream output = new CryptoStream(memoryStream, transform, CryptoStreamMode.Write))
			{
				using BinaryWriter binaryWriter = new BinaryWriter(output);
				binaryWriter.Write(plaintext);
			}
			buffer = memoryStream.ToArray();
		}
		using HMACSHA256 hMACSHA = new HMACSHA256(auth);
		using MemoryStream memoryStream2 = new MemoryStream();
		using (BinaryWriter binaryWriter2 = new BinaryWriter(memoryStream2))
		{
			binaryWriter2.Write(salt);
			binaryWriter2.Write(iV);
			binaryWriter2.Write(buffer);
			binaryWriter2.Flush();
			byte[] buffer2 = hMACSHA.ComputeHash(memoryStream2.ToArray());
			binaryWriter2.Write(buffer2);
		}
		return memoryStream2.ToArray();
	}

	public static byte[] SimpleDecrypt(byte[] ciphertext, byte[] key, byte[] auth, int saltLength = 0)
	{
		if (key == null || key.Length != 32)
		{
			throw new ArgumentException($"Key must be {256} bits", "key");
		}
		if (auth == null || auth.Length != 32)
		{
			throw new ArgumentException($"Auth must be {256} bits", "auth");
		}
		if (ciphertext == null || ciphertext.Length == 0)
		{
			throw new ArgumentNullException("ciphertext");
		}
		using HMACSHA256 hMACSHA = new HMACSHA256(auth);
		byte[] array = new byte[hMACSHA.HashSize / 8];
		byte[] array2 = hMACSHA.ComputeHash(ciphertext, 0, ciphertext.Length - array.Length);
		int num = 16;
		if (ciphertext.Length < array.Length + saltLength + num)
		{
			return null;
		}
		Array.Copy(ciphertext, ciphertext.Length - array.Length, array, 0, array.Length);
		int num2 = 0;
		for (int i = 0; i < array.Length; i++)
		{
			num2 |= array[i] ^ array2[i];
		}
		if (num2 != 0)
		{
			return null;
		}
		using AesManaged aesManaged = new AesManaged
		{
			KeySize = 256,
			BlockSize = 128,
			Mode = CipherMode.CBC,
			Padding = PaddingMode.PKCS7
		};
		byte[] array3 = new byte[num];
		Array.Copy(ciphertext, saltLength, array3, 0, array3.Length);
		using ICryptoTransform transform = aesManaged.CreateDecryptor(key, array3);
		using MemoryStream memoryStream = new MemoryStream();
		using (CryptoStream output = new CryptoStream(memoryStream, transform, CryptoStreamMode.Write))
		{
			using BinaryWriter binaryWriter = new BinaryWriter(output);
			binaryWriter.Write(ciphertext, saltLength + array3.Length, ciphertext.Length - saltLength - array3.Length - array.Length);
		}
		return memoryStream.ToArray();
	}

	public static byte[] SimpleEncryptWithKey(byte[] plaintext, string key, byte[] salt = null)
	{
		if (salt == null)
		{
			salt = new byte[0];
		}
		if (string.IsNullOrWhiteSpace(key) || key.Length < 12)
		{
			throw new ArgumentException($"Key must be at least {12} characters", "key");
		}
		if (plaintext == null || plaintext.Length == 0)
		{
			throw new ArgumentNullException("plaintext");
		}
		byte[] array = new byte[16 + salt.Length];
		Array.Copy(salt, array, salt.Length);
		int num = salt.Length;
		byte[] bytes;
		using (Rfc2898DeriveBytes rfc2898DeriveBytes = new Rfc2898DeriveBytes(key, 8, 10000))
		{
			byte[] salt2 = rfc2898DeriveBytes.Salt;
			bytes = rfc2898DeriveBytes.GetBytes(32);
			Array.Copy(salt2, 0, array, num, salt2.Length);
			num += salt2.Length;
		}
		byte[] bytes2;
		using (Rfc2898DeriveBytes rfc2898DeriveBytes2 = new Rfc2898DeriveBytes(key, 8, 10000))
		{
			byte[] salt3 = rfc2898DeriveBytes2.Salt;
			bytes2 = rfc2898DeriveBytes2.GetBytes(32);
			Array.Copy(salt3, 0, array, num, salt3.Length);
		}
		return SimpleEncrypt(plaintext, bytes, bytes2, array);
	}

	public static byte[] SimpleDecryptWithKey(byte[] ciphertext, string key, int saltLength = 0)
	{
		if (string.IsNullOrWhiteSpace(key) || key.Length < 12)
		{
			throw new ArgumentException($"Key must be at least {12} characters", "key");
		}
		if (ciphertext == null || ciphertext.Length == 0)
		{
			throw new ArgumentNullException("ciphertext");
		}
		byte[] array = new byte[8];
		byte[] array2 = new byte[8];
		Array.Copy(ciphertext, saltLength, array, 0, array.Length);
		Array.Copy(ciphertext, saltLength + array.Length, array2, 0, array2.Length);
		byte[] key2 = Rfc2898DeriveBytes(key, array, 10000, 32);
		byte[] auth = Rfc2898DeriveBytes(key, array2, 10000, 32);
		return SimpleDecrypt(ciphertext, key2, auth, array.Length + array2.Length + saltLength);
	}

	private static byte[] Rfc2898DeriveBytes(string password, byte[] salt, int iterations, int numBytes)
	{
		using Rfc2898DeriveBytes rfc2898DeriveBytes = new Rfc2898DeriveBytes(password, salt, iterations);
		return rfc2898DeriveBytes.GetBytes(numBytes);
	}
}
