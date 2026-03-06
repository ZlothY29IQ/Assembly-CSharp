using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

public static class CreatorCodes
{
	public enum CreatorCodeStatus
	{
		Empty,
		Unchecked,
		Validating,
		Valid
	}

	[Serializable]
	private class CreatorCodesData
	{
		public Dictionary<string, string> currentCreatorCode = new Dictionary<string, string>();

		public Dictionary<string, DateTime> codeFirstUsedTime = new Dictionary<string, DateTime>();
	}

	private const int MAX_CODE_LENGTH = 10;

	private const string PLAYER_PREF_KEY = "CreatorCodes_Store";

	private const int DAYS_TO_STORE_CODE = 14;

	private static CreatorCodesData data = new CreatorCodesData();

	private static Dictionary<string, NexusManager.MemberCode> ValidatedCreatorCode;

	private static Dictionary<string, CreatorCodeStatus> creatorCodeStatus;

	public static bool Intialized = false;

	public static Member supportedMember;

	public static event Action<string> OnCreatorCodeChangedEvent;

	public static event Action InitializedEvent;

	public static event Action<string, string, NexusGroupId> OnCreatorCodeValidEvent;

	public static event Action<string> OnCreatorCodeFailureEvent;

	public static string getCurrentCreatorCode(string id)
	{
		if (id.IsNullOrEmpty())
		{
			return string.Empty;
		}
		if (data.currentCreatorCode == null)
		{
			return string.Empty;
		}
		if (!data.currentCreatorCode.ContainsKey(id))
		{
			return string.Empty;
		}
		return data.currentCreatorCode[id];
	}

	public static CreatorCodeStatus getCurrentCreatorCodeStatus(string id)
	{
		if (id == null)
		{
			return CreatorCodeStatus.Empty;
		}
		if (creatorCodeStatus == null)
		{
			return CreatorCodeStatus.Empty;
		}
		if (!creatorCodeStatus.ContainsKey(id))
		{
			return CreatorCodeStatus.Empty;
		}
		return creatorCodeStatus[id];
	}

	public static void Initialize()
	{
		ValidatedCreatorCode = new Dictionary<string, NexusManager.MemberCode>();
		creatorCodeStatus = new Dictionary<string, CreatorCodeStatus>();
		LoadData();
		Intialized = true;
		CreatorCodes.InitializedEvent?.Invoke();
	}

	public static void DeleteCharacter(string id)
	{
		if (data.currentCreatorCode.ContainsKey(id) && data.currentCreatorCode[id].Length > 0)
		{
			data.currentCreatorCode[id] = data.currentCreatorCode[id].Substring(0, data.currentCreatorCode[id].Length - 1);
			ValidatedCreatorCode[id] = null;
			creatorCodeStatus[id] = ((data.currentCreatorCode[id].Length != 0) ? CreatorCodeStatus.Unchecked : CreatorCodeStatus.Empty);
			CreatorCodes.OnCreatorCodeChangedEvent?.Invoke(id);
		}
	}

	public static void AppendKey(string id, string input)
	{
		if (!data.currentCreatorCode.ContainsKey(id))
		{
			data.currentCreatorCode[id] = string.Empty;
		}
		if (data.currentCreatorCode[id].Length < 10)
		{
			data.currentCreatorCode[id] += input;
			ValidatedCreatorCode[id] = null;
			creatorCodeStatus[id] = CreatorCodeStatus.Unchecked;
			CreatorCodes.OnCreatorCodeChangedEvent?.Invoke(id);
		}
	}

	public static void ResetCreatorCode(string id)
	{
		Debug.Log("Resetting creator code");
		data.currentCreatorCode[id] = "";
		creatorCodeStatus[id] = CreatorCodeStatus.Empty;
		supportedMember = default(Member);
		ValidatedCreatorCode[id] = null;
		SaveData();
		CreatorCodes.OnCreatorCodeChangedEvent?.Invoke(id);
	}

	public static async Task<NexusManager.MemberCode> CheckValidationCoroutineJIT(string terminalId, string code, NexusGroupId[] group)
	{
		creatorCodeStatus[terminalId] = CreatorCodeStatus.Validating;
		CreatorCodes.OnCreatorCodeChangedEvent?.Invoke(terminalId);
		for (int i = 0; i < group.Length; i++)
		{
			Member member = await NexusManager.instance.VerifyCreatorCode(terminalId, code, group[i]);
			if (!member.Equals(default(Member)))
			{
				creatorCodeStatus[terminalId] = CreatorCodeStatus.Valid;
				supportedMember = member;
				ValidatedCreatorCode[terminalId] = new NexusManager.MemberCode
				{
					memberCode = code,
					groupId = group[i]
				};
				data.codeFirstUsedTime[terminalId] = DateTime.UtcNow;
				SaveData();
				CreatorCodes.OnCreatorCodeValidEvent?.Invoke(terminalId, code, group[i]);
				return new NexusManager.MemberCode
				{
					memberCode = code,
					groupId = group[i]
				};
			}
		}
		creatorCodeStatus[terminalId] = CreatorCodeStatus.Unchecked;
		CreatorCodes.OnCreatorCodeFailureEvent?.Invoke(terminalId);
		return null;
	}

	private static void SaveData()
	{
		PlayerPrefs.SetString("CreatorCodes_Store", JsonConvert.SerializeObject(data));
	}

	private static void LoadData()
	{
		string text = PlayerPrefs.GetString("CreatorCodes_Store", string.Empty);
		if (text.Length == 0)
		{
			return;
		}
		data = JsonConvert.DeserializeObject<CreatorCodesData>(text);
		foreach (string key in data.currentCreatorCode.Keys)
		{
			if (data.codeFirstUsedTime.ContainsKey(key) && DateTime.UtcNow.Subtract(data.codeFirstUsedTime[key]).Days > 14)
			{
				data.currentCreatorCode[key] = string.Empty;
			}
		}
	}
}
