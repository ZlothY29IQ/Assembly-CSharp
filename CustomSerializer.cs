using System;
using System.IO;
using System.Text;
using UnityEngine;

public static class CustomSerializer
{
	public static byte[] ByteSerialize(this object obj)
	{
		return Serialize(obj);
	}

	public static object ByteDeserialize(this byte[] bytes)
	{
		return Deserialize(bytes);
	}

	public static byte[] Serialize(object obj)
	{
		using MemoryStream memoryStream = new MemoryStream();
		using BinaryWriter writer = new BinaryWriter(memoryStream, Encoding.UTF8);
		SerializeObject(writer, obj);
		return memoryStream.ToArray();
	}

	public static object Deserialize(byte[] data)
	{
		using MemoryStream input = new MemoryStream(data);
		using BinaryReader reader = new BinaryReader(input, Encoding.UTF8);
		return DeserializeObject(reader);
	}

	private static void SerializeObject(BinaryWriter writer, object obj)
	{
		if (!(obj is string value))
		{
			if (!(obj is bool value2))
			{
				if (!(obj is int value3))
				{
					if (!(obj is float value4))
					{
						if (!(obj is double value5))
						{
							if (!(obj is Vector2 vector))
							{
								if (!(obj is Vector3 vector2))
								{
									if (!(obj is object[] objects))
									{
										if (!(obj is byte value6))
										{
											if (!(obj is Enum obj2))
											{
												if (!(obj is NetEventOptions options))
												{
													if (obj is Quaternion quaternion)
													{
														writer.Write((byte)12);
														writer.Write(quaternion.x);
														writer.Write(quaternion.y);
														writer.Write(quaternion.z);
														writer.Write(quaternion.w);
													}
													else
													{
														Debug.LogWarning("<color=blue>type not supported " + obj.GetType().ToString() + "</color>");
													}
												}
												else
												{
													writer.Write((byte)11);
													SerializeNetEventOptions(writer, options);
												}
											}
											else
											{
												writer.Write((byte)10);
												writer.Write(Convert.ToInt32(obj2));
												writer.Write(obj2.GetType().AssemblyQualifiedName);
											}
										}
										else
										{
											writer.Write((byte)9);
											writer.Write(value6);
										}
									}
									else
									{
										writer.Write((byte)8);
										SerializeObjectArray(writer, objects);
									}
								}
								else
								{
									writer.Write((byte)7);
									writer.Write(vector2.x);
									writer.Write(vector2.y);
									writer.Write(vector2.z);
								}
							}
							else
							{
								writer.Write((byte)6);
								writer.Write(vector.x);
								writer.Write(vector.y);
							}
						}
						else
						{
							writer.Write((byte)5);
							writer.Write(value5);
						}
					}
					else
					{
						writer.Write((byte)4);
						writer.Write(value4);
					}
				}
				else
				{
					writer.Write((byte)3);
					writer.Write(value3);
				}
			}
			else
			{
				writer.Write((byte)2);
				writer.Write(value2);
			}
		}
		else
		{
			writer.Write((byte)1);
			writer.Write(value);
		}
	}

	private static void SerializeObjectArray(BinaryWriter writer, object[] objects)
	{
		writer.Write(objects.Length);
		foreach (object obj in objects)
		{
			SerializeObject(writer, obj);
		}
	}

	private static void SerializeNetEventOptions(BinaryWriter writer, NetEventOptions options)
	{
		writer.Write((int)options.Reciever);
		if (options.TargetActors == null)
		{
			writer.Write(0);
		}
		else
		{
			writer.Write(options.TargetActors.Length);
			int[] targetActors = options.TargetActors;
			foreach (int value in targetActors)
			{
				writer.Write(value);
			}
		}
		writer.Write(options.Flags.WebhookFlags);
	}

	private static object DeserializeObject(BinaryReader reader)
	{
		switch (reader.ReadByte())
		{
		case 0:
			return null;
		case 1:
			return reader.ReadString();
		case 2:
			return reader.ReadBoolean();
		case 3:
			return reader.ReadInt32();
		case 4:
			return reader.ReadSingle();
		case 5:
			return reader.ReadDouble();
		case 6:
		{
			float x3 = reader.ReadSingle();
			float y3 = reader.ReadSingle();
			return new Vector2(x3, y3);
		}
		case 7:
		{
			float x2 = reader.ReadSingle();
			float y2 = reader.ReadSingle();
			float z2 = reader.ReadSingle();
			return new Vector3(x2, y2, z2);
		}
		case 8:
			return DeserializeObjectArray(reader);
		case 9:
			return reader.ReadByte();
		case 10:
		{
			int value = reader.ReadInt32();
			return Enum.ToObject(Type.GetType(reader.ReadString()), value);
		}
		case 11:
			return DeserializeNetEventOptions(reader);
		case 12:
		{
			float x = reader.ReadSingle();
			float y = reader.ReadSingle();
			float z = reader.ReadSingle();
			float w = reader.ReadSingle();
			return new Quaternion(x, y, z, w);
		}
		default:
			throw new InvalidOperationException("Unsupported type");
		}
	}

	private static object[] DeserializeObjectArray(BinaryReader reader)
	{
		int num = reader.ReadInt32();
		object[] array = new object[num];
		for (int i = 0; i < num; i++)
		{
			array[i] = DeserializeObject(reader);
		}
		return array;
	}

	private static NetEventOptions DeserializeNetEventOptions(BinaryReader reader)
	{
		int reciever = reader.ReadInt32();
		int num = reader.ReadInt32();
		int[] array = null;
		if (num > 0)
		{
			array = new int[num];
			for (int i = 0; i < num; i++)
			{
				array[i] = reader.ReadInt32();
			}
		}
		byte flags = reader.ReadByte();
		return new NetEventOptions(reciever, array, flags);
	}
}
