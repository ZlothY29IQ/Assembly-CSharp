using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using GorillaTag.Scripts.Utilities;
using Newtonsoft.Json;
using UnityEngine;

namespace DefaultNamespace;

public class EvolvingCosmeticSaveData
{
	public readonly Dictionary<string, int> SelectedIndices = new Dictionary<string, int>();

	private static EvolvingCosmeticSaveData? s_instance;

	public const string PlayerPrefsKey = "EvolvingCosmeticSaveData";

	public static EvolvingCosmeticSaveData Instance => s_instance ?? (s_instance = new EvolvingCosmeticSaveData());

	private EvolvingCosmeticSaveData()
	{
		string text = PlayerPrefs.GetString("EvolvingCosmeticSaveData");
		if (text != null)
		{
			ReadFromJson(text);
		}
	}

	public string Write()
	{
		JsonSerializer jsonSerializer = new JsonSerializer();
		using TextWriter textWriter = new StringWriterWithEncoding(Encoding.UTF8);
		using JsonWriter jsonWriter = new JsonTextWriter(textWriter);
		jsonSerializer.Serialize(jsonWriter, this);
		return textWriter.ToString();
	}

	private void ReadFromJson(string json)
	{
		using TextReader reader = new StringReader(json);
		using JsonReader jsonReader = new JsonTextReader(reader);
		while (jsonReader.Read())
		{
			if (jsonReader.TokenType == JsonToken.PropertyName && (string)jsonReader.Value == "SelectedIndices")
			{
				ReadSelectedIndices(jsonReader);
			}
		}
	}

	private void ReadSelectedIndices(JsonReader reader)
	{
		int num = 0;
		string text = null;
		do
		{
			if (!reader.Read())
			{
				throw new Exception("Json read error");
			}
			switch (reader.TokenType)
			{
			case JsonToken.StartObject:
				num++;
				break;
			case JsonToken.EndObject:
				num--;
				break;
			case JsonToken.PropertyName:
				if (text != null)
				{
					throw new Exception("Json read error");
				}
				text = (reader.Value as string) ?? throw new Exception("Json read error");
				break;
			case JsonToken.Integer:
				if (text == null)
				{
					throw new Exception("Json read error");
				}
				if (!(reader.Value is long num2))
				{
					throw new Exception("Json read error");
				}
				SelectedIndices[text] = (int)num2;
				break;
			}
		}
		while (num > 0);
	}
}
