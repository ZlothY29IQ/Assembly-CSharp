using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NexusSDK;
using UnityEngine;
using UnityEngine.Networking;

public class NexusManager : MonoBehaviour
{
	public enum Environment
	{
		PRODUCTION,
		SANDBOX
	}

	[Serializable]
	public class MemberCode
	{
		public string memberCode { get; set; }

		public NexusGroupId groupId { get; set; }
	}

	[Serializable]
	public struct GetMembersRequest
	{
		public int page { get; set; }

		public int pageSize { get; set; }
	}

	private const string ENV_PRODUCTION = "production";

	private const string ENV_SANDBOX = "sandbox";

	private const string ENV_PRODUCTION_API_KEY = "nexus_pk_4c18dcb1531846c7abad4cb00c5242bb";

	private const string ENV_SANDBOX_API_KEY = "nexus_pk_ba155a8c229740489d214f024e25f25c";

	private Environment environment = Environment.SANDBOX;

	public static NexusManager instance;

	private Member[] validatedMembers;

	public Environment CurrentEnvironment => environment;

	private void Awake()
	{
		if (instance == null)
		{
			environment = Environment.PRODUCTION;
			instance = this;
		}
		else
		{
			UnityEngine.Object.Destroy(this);
		}
	}

	private void Start()
	{
		SDKInitializer.Init((environment == Environment.SANDBOX) ? "nexus_pk_ba155a8c229740489d214f024e25f25c" : "nexus_pk_4c18dcb1531846c7abad4cb00c5242bb", (environment == Environment.SANDBOX) ? "sandbox" : "production");
	}

	public async Task<Member> VerifyCreatorCode(string terminalId, string code, NexusGroupId id)
	{
		string text = SDKInitializer.ApiBaseUrl + "/manage/members/{memberCode}";
		text = text.Replace("{memberCode}", code);
		List<string> list = new List<string>();
		list.Add("groupId=" + id.Code);
		text += "?";
		text += string.Join("&", list);
		Debug.Log("CreatorCodeTerminal " + terminalId + " :: GetMemberByCode :: " + text);
		using UnityWebRequest webRequest = UnityWebRequest.Get(text);
		webRequest.SetRequestHeader("x-shared-secret", SDKInitializer.ApiKey);
		await webRequest.SendWebRequest();
		if (webRequest.responseCode == 200)
		{
			Debug.Log("CreatorCodeTerminal " + terminalId + " :: GetMemberByCode :: valid");
			return JsonConvert.DeserializeObject<Member>(webRequest.downloadHandler.text, new JsonSerializerSettings
			{
				NullValueHandling = NullValueHandling.Ignore
			});
		}
		Debug.Log("CreatorCodeTerminal " + terminalId + " :: GetMemberByCode :: invalid");
		return default(Member);
	}

	public async Task<bool> VerifyCreatorCodeJIT(string memberCode, string groupCode)
	{
		string text = SDKInitializer.ApiBaseUrl + "/manage/members/{memberCode}";
		text = text.Replace("{memberCode}", memberCode);
		List<string> list = new List<string>();
		list.Add("groupId=" + groupCode);
		text += "?";
		text += string.Join("&", list);
		using UnityWebRequest webRequest = UnityWebRequest.Get(text);
		webRequest.SetRequestHeader("x-shared-secret", SDKInitializer.ApiKey);
		await webRequest.SendWebRequest();
		if (webRequest.responseCode == 200)
		{
			return true;
		}
		return false;
	}
}
