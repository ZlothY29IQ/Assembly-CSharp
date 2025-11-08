using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using NexusSDK;
using UnityEngine;
using UnityEngine.Networking;

public class NexusManager : MonoBehaviour
{
	[Serializable]
	public struct GetMemberByCodeRequest
	{
		public string memberCode { get; set; }

		public string groupId { get; set; }
	}

	[Serializable]
	public struct GetMembersRequest
	{
		public int page { get; set; }

		public int pageSize { get; set; }
	}

	private string publicApiKey = "nexus_pk_4c18dcb1531846c7abad4cb00c5242bb";

	private string environment = "production";

	public static NexusManager instance;

	private Member[] validatedMembers;

	private void Awake()
	{
		if (instance == null)
		{
			instance = this;
		}
		else
		{
			UnityEngine.Object.Destroy(this);
		}
	}

	private void Start()
	{
		SDKInitializer.Init(publicApiKey, environment);
	}

	public static IEnumerator GetMembers(GetMembersRequest RequestParams, Action<AttributionAPI.GetMembers200Response> onSuccess, Action<string> onFailure)
	{
		string text = SDKInitializer.ApiBaseUrl + "/manage/members";
		List<string> list = new List<string>();
		if (RequestParams.page != 0)
		{
			list.Add("page=" + RequestParams.page);
		}
		if (RequestParams.pageSize != 0)
		{
			list.Add("pageSize=" + RequestParams.pageSize);
		}
		text += "?";
		text += string.Join("&", list);
		using UnityWebRequest webRequest = UnityWebRequest.Get(text);
		webRequest.SetRequestHeader("x-shared-secret", SDKInitializer.ApiKey);
		yield return webRequest.SendWebRequest();
		if (webRequest.responseCode == 200)
		{
			AttributionAPI.GetMembers200Response obj = JsonConvert.DeserializeObject<AttributionAPI.GetMembers200Response>(webRequest.downloadHandler.text, new JsonSerializerSettings
			{
				NullValueHandling = NullValueHandling.Ignore
			});
			onSuccess?.Invoke(obj);
		}
		else
		{
			onFailure?.Invoke(webRequest.error);
		}
	}

	public void VerifyCreatorCode(string code, Action<Member> onSuccess, Action onFailure)
	{
		GetMemberByCodeRequest getMemberByCodeRequest = default(GetMemberByCodeRequest);
		getMemberByCodeRequest.memberCode = code;
		GetMemberByCodeRequest requestParams = getMemberByCodeRequest;
		StartCoroutine(GetMemberByCode(requestParams, onSuccess, onFailure));
	}

	public static IEnumerator GetMemberByCode(GetMemberByCodeRequest RequestParams, Action<Member> onSuccess, Action onFailure)
	{
		string text = SDKInitializer.ApiBaseUrl + "/manage/members/{memberCode}";
		text = text.Replace("{memberCode}", RequestParams.memberCode);
		List<string> values = new List<string>();
		text += "?";
		text += string.Join("&", values);
		using UnityWebRequest webRequest = UnityWebRequest.Get(text);
		webRequest.SetRequestHeader("x-shared-secret", SDKInitializer.ApiKey);
		yield return webRequest.SendWebRequest();
		if (webRequest.responseCode == 200)
		{
			Member obj = JsonConvert.DeserializeObject<Member>(webRequest.downloadHandler.text, new JsonSerializerSettings
			{
				NullValueHandling = NullValueHandling.Ignore
			});
			onSuccess?.Invoke(obj);
		}
		else
		{
			onFailure?.Invoke();
		}
	}
}
