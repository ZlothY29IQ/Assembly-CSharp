using System;
using GorillaExtensions;
using Steamworks;
using UnityEngine;

public class MothershipAuthenticator : MonoBehaviour, IGorillaSliceableSimple
{
	public static volatile MothershipAuthenticator Instance;

	public MetaAuthenticator MetaAuthenticator;

	public SteamAuthenticator SteamAuthenticator;

	public string TestNickname;

	public string TestAccountId;

	public bool UseConstantTestAccountId;

	public int MaxMetaLoginAttempts = 5;

	public Action OnLoginSuccess;

	public Action<string, string, string> OnLoginFailure;

	public Action<int> OnLoginAttemptFailure;

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		if (Instance == null)
		{
			Instance = null;
		}
	}

	public void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
		}
		else if (Instance != this)
		{
			UnityEngine.Object.Destroy(base.gameObject);
		}
		if (!MothershipClientApiUnity.IsEnabled())
		{
			Debug.Log("Mothership is not enabled.");
			return;
		}
		if (Instance.SteamAuthenticator == null)
		{
			Instance.SteamAuthenticator = Instance.gameObject.GetOrAddComponent<SteamAuthenticator>();
		}
		MothershipClientApiUnity.SetAuthRefreshedCallback(delegate
		{
			BeginLoginFlow();
		});
	}

	public void BeginLoginFlow()
	{
		Debug.Log("making login call");
		LogInWithSteam();
	}

	private void LogInWithInsecure()
	{
		MothershipClientApiUnity.LogInWithInsecure1(TestNickname, TestAccountId, delegate(LoginResponse LoginResponse)
		{
			Debug.Log("Logged in with Mothership Id " + LoginResponse.MothershipPlayerId);
			MothershipClientApiUnity.OpenNotificationsSocket();
			OnLoginSuccess?.Invoke();
		}, delegate(MothershipError MothershipError, int errorCode)
		{
			Debug.LogError($"Failed to log in, error {MothershipError.Message} trace ID: {MothershipError.TraceId} status: {errorCode} Mothership error code: {MothershipError.MothershipErrorCode}");
			OnLoginAttemptFailure?.Invoke(1);
			OnLoginFailure?.Invoke(MothershipError.Message, MothershipError.MothershipErrorCode, MothershipError.TraceId);
		});
	}

	private void LogInWithSteam()
	{
		MothershipClientApiUnity.StartLoginWithSteam(delegate(PlayerSteamBeginLoginResponse resp)
		{
			string nonce = resp.Nonce;
			SteamAuthTicket ticketHandle = HAuthTicket.Invalid;
			ticketHandle = SteamAuthenticator.GetAuthTicketForWebApi(nonce, delegate(string ticket)
			{
				MothershipClientApiUnity.CompleteLoginWithSteam(nonce, ticket, delegate
				{
					ticketHandle.Dispose();
					Debug.Log("Logged in to Mothership with Steam");
					MothershipClientApiUnity.OpenNotificationsSocket();
					OnLoginSuccess?.Invoke();
				}, delegate(MothershipError MothershipError, int errorCode)
				{
					ticketHandle.Dispose();
					Debug.LogError($"Couldn't log into Mothership with Steam error {MothershipError.Message} trace ID: {MothershipError.TraceId} status: {errorCode} Mothership error code: {MothershipError.MothershipErrorCode}");
					OnLoginAttemptFailure?.Invoke(1);
					OnLoginFailure?.Invoke(MothershipError.Message, MothershipError.MothershipErrorCode, MothershipError.TraceId);
				});
			}, delegate(EResult error)
			{
				string text = $"Couldn't get an auth ticket for logging into Mothership with Steam: {error}";
				Debug.LogError(text);
				OnLoginAttemptFailure?.Invoke(1);
				OnLoginFailure?.Invoke(text, "", "");
			});
		}, delegate(MothershipError MothershipError, int errorCode)
		{
			Debug.LogError($"Couldn't start Mothership auth for Steam error {MothershipError.Message} trace ID: {MothershipError.TraceId} status: {errorCode} Mothership error code: {MothershipError.MothershipErrorCode}");
			OnLoginAttemptFailure?.Invoke(1);
			OnLoginFailure?.Invoke(MothershipError.Message, MothershipError.MothershipErrorCode, MothershipError.TraceId);
		});
	}

	public void OnEnable()
	{
		GorillaSlicerSimpleManager.RegisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.Update);
	}

	public void OnDisable()
	{
		GorillaSlicerSimpleManager.UnregisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.Update);
	}

	public void SliceUpdate()
	{
		if (MothershipClientApiUnity.IsEnabled())
		{
			MothershipClientApiUnity.Tick(Time.deltaTime);
		}
	}
}
