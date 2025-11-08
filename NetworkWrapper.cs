using System;
using GorillaNetworking;
using UnityEngine;
using UnityEngine.UI;

public class NetworkWrapper : MonoBehaviour
{
	[HideInInspector]
	public NetworkSystem activeNetworkSystem;

	public Text titleRef;

	[Header("NetSys settings")]
	public NetworkSystemConfig netSysConfig;

	public string[] networkRegionNames;

	public string[] devNetworkRegionNames;

	[Header("Debug output refs")]
	public Text stateTextRef;

	public Text playerCountTextRef;

	[SerializeField]
	private SO_NetworkVoiceSettings VoiceSettings;

	private const string WrapperResourcePath = "P_NetworkWrapper";

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	public static void AutoInstantiate()
	{
		UnityEngine.Object.DontDestroyOnLoad(UnityEngine.Object.Instantiate(Resources.Load<GameObject>("P_NetworkWrapper")));
	}

	private void Awake()
	{
		if (titleRef != null)
		{
			titleRef.text = "PUN";
		}
		activeNetworkSystem = base.gameObject.AddComponent<NetworkSystemPUN>();
		activeNetworkSystem.AddVoiceSettings(VoiceSettings);
		activeNetworkSystem.config = netSysConfig;
		activeNetworkSystem.regionNames = networkRegionNames;
		activeNetworkSystem.OnPlayerJoined += new Action<NetPlayer>(UpdatePlayerCountWrapper);
		activeNetworkSystem.OnPlayerLeft += new Action<NetPlayer>(UpdatePlayerCountWrapper);
		activeNetworkSystem.OnMultiplayerStarted += new Action(UpdatePlayerCount);
		activeNetworkSystem.OnReturnedToSinglePlayer += new Action(UpdatePlayerCount);
		Debug.Log("<color=green>initialize Network System</color>");
		activeNetworkSystem.Initialise();
	}

	private void UpdatePlayerCountWrapper(NetPlayer player)
	{
		UpdatePlayerCount();
	}

	private void UpdatePlayerCount()
	{
		if (!(playerCountTextRef == null))
		{
			if (!activeNetworkSystem.IsOnline)
			{
				playerCountTextRef.text = $"0/{netSysConfig.MaxPlayerCount}";
				Debug.Log("Player count updated");
			}
			else
			{
				Debug.Log("Player count not updated");
				playerCountTextRef.text = $"{activeNetworkSystem.AllNetPlayers.Length}/{netSysConfig.MaxPlayerCount}";
			}
		}
	}
}
