using System;
using System.Threading.Tasks;
using GorillaNetworking;
using Newtonsoft.Json;
using PlayFab;
using UnityEngine;
using UnityEngine.Networking;

public class HowManyMonke : MonoBehaviour
{
	private enum State
	{
		READY,
		TD_LOOKUP,
		HMM_LOOKUP
	}

	private class CCUResponse
	{
		public int CCUTotal;

		public string ErrorMessage;
	}

	private const string preLog = "[GT/HowManyMonke]  ";

	private const string preErr = "ERROR!!!  ";

	public static int ThisMany = 12549;

	public static Action<int> OnCheck;

	[SerializeField]
	private string titleDataKey;

	private State state;

	private static int recheckDelay;

	[SerializeField]
	private string CCUEndpoint;

	public static float RecheckDelay => Mathf.Max((float)recheckDelay / 1000f, 1f);

	public async void Start()
	{
		state = State.READY;
		await Task.Delay(1000);
		Debug.Log("[GT/HowManyMonke]  " + $"Checking NetworkSystem.Instance: {NetworkSystem.Instance}");
		while (NetworkSystem.Instance == null)
		{
			await Task.Delay(1000);
			Debug.Log("[GT/HowManyMonke]  " + $"Re-Checking NetworkSystem.Instance: {NetworkSystem.Instance}");
		}
		ThisMany = await FetchThisMany();
		if (OnCheck != null)
		{
			OnCheck(ThisMany);
		}
		Debug.Log("[GT/HowManyMonke]  " + $"Fetch Complete: {ThisMany}");
		await FetchRecheckDelay();
		while (Application.isPlaying && recheckDelay > 0)
		{
			await Task.Delay(recheckDelay);
			if (OnCheck != null)
			{
				ThisMany = await FetchThisMany();
				OnCheck(ThisMany);
				await FetchRecheckDelay();
			}
		}
	}

	private async Task FetchRecheckDelay()
	{
		state = State.TD_LOOKUP;
		PlayFabTitleDataCache.Instance.GetTitleData(titleDataKey, onTD, onTDError);
		while (state != State.READY)
		{
			await Task.Yield();
		}
	}

	private void onTDError(PlayFabError error)
	{
		state = State.READY;
		recheckDelay = 0;
	}

	private void onTD(string obj)
	{
		state = State.READY;
		if (int.TryParse(obj, out recheckDelay))
		{
			recheckDelay *= 1000;
		}
		else
		{
			recheckDelay = 0;
		}
	}

	private async Task<int> FetchThisMany()
	{
		if (recheckDelay < 0)
		{
			return NetworkSystem.Instance.GlobalPlayerCount();
		}
		UnityWebRequest request = new UnityWebRequest(PlayFabAuthenticatorSettings.ModerationApiBaseUrl + CCUEndpoint, "POST")
		{
			downloadHandler = new DownloadHandlerBuffer()
		};
		await request.SendWebRequest();
		if (request.result == UnityWebRequest.Result.Success)
		{
			return JsonConvert.DeserializeObject<CCUResponse>(request.downloadHandler.text).CCUTotal;
		}
		return NetworkSystem.Instance.GlobalPlayerCount();
	}
}
