using System;
using System.Collections;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

public class DearLemmingController : MonoBehaviour
{
	[Serializable]
	private class DearLemmingRequest
	{
		public string MothershipId;

		public string MothershipToken;

		public string MothershipTitleId;

		public string MothershipEnvId;

		public string MessageText;
	}

	[Serializable]
	public class DearLemmingResponse
	{
		public bool CanSubmit;

		public DateTime? NextSubmitTimeUtc;

		public double? SecondsUntilNextSubmit;

		public string Error;

		public int StatusCode;
	}

	private int maxRetriesOnFail = 3;

	private int checkRetryCount;

	private int submitRetryCount;

	private bool isChecking;

	private bool isSubmitting;

	public static DearLemmingController instance { get; private set; }

	public event Action<DearLemmingResponse> OnCheckComplete;

	public event Action<DearLemmingResponse> OnSubmitComplete;

	public void Awake()
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

	public async void CheckCanSubmit()
	{
		if (!isChecking)
		{
			isChecking = true;
			await WaitForLogin();
			StartCheck();
		}
	}

	public async void SubmitMessage(string messageText)
	{
		if (!isSubmitting)
		{
			isSubmitting = true;
			await WaitForLogin();
			StartSubmit(messageText);
		}
	}

	private void StartCheck()
	{
		StartCoroutine(DoRequest("/api/CheckDearLemming", null, isCheckRequest: true));
	}

	private void StartSubmit(string messageText)
	{
		StartCoroutine(DoRequest("/api/SubmitDearLemming", messageText, isCheckRequest: false));
	}

	private IEnumerator DoRequest(string endpoint, string messageText, bool isCheckRequest)
	{
		DearLemmingRequest obj = new DearLemmingRequest
		{
			MothershipId = MothershipClientContext.MothershipId,
			MothershipToken = MothershipClientContext.Token,
			MothershipTitleId = MothershipClientApiUnity.TitleId,
			MothershipEnvId = MothershipClientApiUnity.EnvironmentId,
			MessageText = messageText
		};
		UnityWebRequest request = new UnityWebRequest(PlayFabAuthenticatorSettings.VotingApiBaseUrl + endpoint, "POST");
		byte[] bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(obj));
		bool retry = false;
		request.uploadHandler = new UploadHandlerRaw(bytes);
		request.downloadHandler = new DownloadHandlerBuffer();
		request.SetRequestHeader("Content-Type", "application/json");
		yield return request.SendWebRequest();
		if (request.result == UnityWebRequest.Result.Success || request.responseCode == 201)
		{
			HandleResponse(request.downloadHandler.text, isCheckRequest);
		}
		else
		{
			long responseCode = request.responseCode;
			if ((responseCode < 500 || responseCode >= 600) && request.result != UnityWebRequest.Result.ConnectionError)
			{
				HandleResponse(request.downloadHandler.text, isCheckRequest);
				yield break;
			}
			retry = true;
		}
		if (!retry)
		{
			yield break;
		}
		int num = (isCheckRequest ? checkRetryCount : submitRetryCount);
		if (num < maxRetriesOnFail)
		{
			int num2 = (int)Mathf.Pow(2f, num + 1);
			if (isCheckRequest)
			{
				checkRetryCount++;
			}
			else
			{
				submitRetryCount++;
			}
			yield return new WaitForSecondsRealtime(num2);
			if (isCheckRequest)
			{
				StartCheck();
			}
			else
			{
				StartSubmit(messageText);
			}
		}
		else
		{
			if (isCheckRequest)
			{
				checkRetryCount = 0;
			}
			else
			{
				submitRetryCount = 0;
			}
			HandleResponse(null, isCheckRequest);
		}
	}

	private void HandleResponse(string json, bool isCheckRequest)
	{
		DearLemmingResponse obj = null;
		if (!string.IsNullOrEmpty(json))
		{
			try
			{
				obj = JsonConvert.DeserializeObject<DearLemmingResponse>(json);
			}
			catch
			{
			}
		}
		if (isCheckRequest)
		{
			isChecking = false;
			this.OnCheckComplete?.Invoke(obj);
		}
		else
		{
			isSubmitting = false;
			this.OnSubmitComplete?.Invoke(obj);
		}
	}

	private async Task WaitForLogin()
	{
		while (!MothershipClientApiUnity.IsClientLoggedIn())
		{
			await Task.Yield();
			await Task.Delay(1000);
		}
	}
}
