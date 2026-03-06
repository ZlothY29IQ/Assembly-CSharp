using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GorillaNetworking;
using GorillaTag.Audio;
using Newtonsoft.Json;
using Photon.Voice.PUN;
using PlayFab;
using PlayFab.CloudScriptModels;
using UnityEngine;

[RequireComponent(typeof(VRRig), typeof(VRRigReliableState))]
public class RigContainer : MonoBehaviour
{
	[SerializeField]
	private VRRig vrrig;

	[SerializeField]
	private VRRigReliableState reliableState;

	[SerializeField]
	private Transform speakerHead;

	[SerializeField]
	private AudioSource replacementVoiceSource;

	private List<LoudSpeakerNetwork> loudSpeakerNetworks;

	[SerializeField]
	private LCKSocialCameraFollower m_lckCococamFollower;

	[SerializeField]
	private LCKSocialCameraFollower m_lckTablet;

	private PhotonVoiceView voiceView;

	private int m_cachedNetViewID;

	private bool enableVoice = true;

	private bool forceMute;

	[SerializeField]
	private SphereCollider headCollider;

	[SerializeField]
	private CapsuleCollider bodyCollider;

	[SerializeField]
	private VRRigEvents rigEvents;

	public bool hasManualMute;

	private bool bPlayerAutoMuted;

	public int playerChatQuality = 2;

	private static List<NetPlayer> playersToCheckAutomute = new List<NetPlayer>();

	private static bool automuteQueued = false;

	private static List<NetPlayer> requestedAutomutePlayers;

	private static bool waitingForAutomuteCallback = false;

	private static RigContainer staticTempRC;

	public bool Initialized { get; private set; }

	public VRRig Rig => vrrig;

	public VRRigReliableState ReliableState => reliableState;

	public Transform SpeakerHead => speakerHead;

	public AudioSource ReplacementVoiceSource => replacementVoiceSource;

	public List<LoudSpeakerNetwork> LoudSpeakerNetworks => loudSpeakerNetworks;

	public LCKSocialCameraFollower LckCococamFollower => m_lckCococamFollower;

	public LCKSocialCameraFollower LCKTabletFollower => m_lckTablet;

	public PhotonVoiceView Voice
	{
		get
		{
			return voiceView;
		}
		set
		{
			if (!(value == voiceView))
			{
				if (voiceView != null)
				{
					voiceView.SpeakerInUse.enabled = false;
				}
				voiceView = value;
				RefreshVoiceChat();
			}
		}
	}

	public NetworkView netView => vrrig.netView;

	public int CachedNetViewID => m_cachedNetViewID;

	public bool Muted
	{
		get
		{
			return !enableVoice;
		}
		set
		{
			enableVoice = !value;
			RefreshVoiceChat();
		}
	}

	public NetPlayer Creator
	{
		get
		{
			return vrrig.creator;
		}
		set
		{
			if (!vrrig.isOfflineVRRig && (vrrig.creator == null || !vrrig.creator.InRoom))
			{
				vrrig.creator = value;
			}
		}
	}

	public bool ForceMute
	{
		get
		{
			return forceMute;
		}
		set
		{
			forceMute = value;
			RefreshVoiceChat();
		}
	}

	public SphereCollider HeadCollider => headCollider;

	public CapsuleCollider BodyCollider => bodyCollider;

	public VRRigEvents RigEvents => rigEvents;

	public bool GetIsPlayerAutoMuted()
	{
		return bPlayerAutoMuted;
	}

	public void UpdateAutomuteLevel(string autoMuteLevel)
	{
		if (autoMuteLevel.Equals("LOW", StringComparison.OrdinalIgnoreCase))
		{
			playerChatQuality = 1;
		}
		else if (autoMuteLevel.Equals("HIGH", StringComparison.OrdinalIgnoreCase))
		{
			playerChatQuality = 0;
		}
		else if (autoMuteLevel.Equals("ERROR", StringComparison.OrdinalIgnoreCase))
		{
			playerChatQuality = 2;
		}
		else
		{
			playerChatQuality = 2;
		}
		RefreshVoiceChat();
	}

	private void Awake()
	{
		loudSpeakerNetworks = new List<LoudSpeakerNetwork>();
	}

	private void Start()
	{
		if (Rig.isOfflineVRRig)
		{
			vrrig.creator = NetworkSystem.Instance.LocalPlayer;
			RoomSystem.JoinedRoomEvent += new Action(OnMultiPlayerStarted);
			RoomSystem.LeftRoomEvent += new Action(OnReturnedToSinglePlayer);
		}
		else
		{
			rigEvents.enableEvent += new Action<RigContainer>(RigPostEnable);
		}
		Rig.rigContainer = this;
	}

	private void RigPostEnable(RigContainer _)
	{
		vrrig.UpdateName();
	}

	private void OnMultiPlayerStarted()
	{
		if (Rig.isOfflineVRRig)
		{
			vrrig.creator = NetworkSystem.Instance.GetLocalPlayer();
		}
	}

	private void OnReturnedToSinglePlayer()
	{
		if (Rig.isOfflineVRRig)
		{
			CancelAutomuteRequest();
		}
	}

	private void OnDisable()
	{
		Initialized = false;
		enableVoice = true;
		voiceView = null;
		base.gameObject.transform.localPosition = Vector3.zero;
		base.gameObject.transform.localRotation = Quaternion.identity;
		vrrig.syncPos = base.gameObject.transform.position;
		vrrig.syncRotation = base.gameObject.transform.rotation;
		forceMute = false;
	}

	internal void InitializeNetwork(NetworkView netView, PhotonVoiceView voiceView, VRRigSerializer vrRigSerializer)
	{
		if ((bool)netView && (bool)voiceView)
		{
			InitializeNetwork_Shared(netView, vrRigSerializer);
			Voice = voiceView;
			vrrig.voiceAudio = voiceView.SpeakerInUse.GetComponent<AudioSource>();
		}
	}

	private void InitializeNetwork_Shared(NetworkView netView, VRRigSerializer vrRigSerializer)
	{
		if ((bool)vrrig.netView)
		{
			MonkeAgent.instance.SendReport("inappropriate tag data being sent creating multiple vrrigs", Creator.UserId, Creator.NickName);
			if (vrrig.netView.IsMine)
			{
				NetworkSystem.Instance.NetDestroy(vrrig.gameObject);
			}
			else
			{
				vrrig.netView.gameObject.SetActive(value: false);
			}
		}
		vrrig.netView = netView;
		vrrig.rigSerializer = vrRigSerializer;
		vrrig.OwningNetPlayer = NetworkSystem.Instance.GetPlayer(NetworkSystem.Instance.GetOwningPlayerID(vrRigSerializer.gameObject));
		m_cachedNetViewID = netView.ViewID;
		if (!Initialized)
		{
			vrrig.NetInitialize();
			if (GorillaGameManager.instance != null && NetworkSystem.Instance.IsMasterClient)
			{
				int owningPlayerID = NetworkSystem.Instance.GetOwningPlayerID(vrRigSerializer.gameObject);
				bool playerTutorialCompletion = NetworkSystem.Instance.GetPlayerTutorialCompletion(owningPlayerID);
				GorillaGameManager.instance.NewVRRig(netView.Owner, netView.ViewID, playerTutorialCompletion);
			}
			_ = vrrig.OwningNetPlayer.IsLocal;
			if (!vrrig.isOfflineVRRig && vrrig.InitializedCosmetics)
			{
				netView.SendRPC("RPC_RequestCosmetics", netView.Owner);
			}
		}
		Initialized = true;
		if (!vrrig.isOfflineVRRig)
		{
			StartCoroutine(QueueAutomute(Creator));
		}
	}

	private static IEnumerator QueueAutomute(NetPlayer player)
	{
		playersToCheckAutomute.Add(player);
		if (!automuteQueued)
		{
			automuteQueued = true;
			yield return new WaitForSecondsRealtime(1f);
			while (waitingForAutomuteCallback)
			{
				yield return null;
			}
			automuteQueued = false;
			RequestAutomuteSettings();
		}
	}

	private static void RequestAutomuteSettings()
	{
		if (playersToCheckAutomute.Count == 0)
		{
			return;
		}
		waitingForAutomuteCallback = true;
		playersToCheckAutomute.RemoveAll((NetPlayer player) => player == null);
		requestedAutomutePlayers = new List<NetPlayer>(playersToCheckAutomute);
		playersToCheckAutomute.Clear();
		string[] value = requestedAutomutePlayers.Select((NetPlayer x) => x.UserId).ToArray();
		foreach (NetPlayer requestedAutomutePlayer in requestedAutomutePlayers)
		{
			_ = requestedAutomutePlayer;
		}
		PlayFabCloudScriptAPI.ExecuteFunction(new ExecuteFunctionRequest
		{
			Entity = new EntityKey
			{
				Id = PlayFabSettings.staticPlayer.EntityId,
				Type = PlayFabSettings.staticPlayer.EntityType
			},
			FunctionName = "ShouldUserAutomutePlayer",
			FunctionParameter = string.Join(",", value)
		}, delegate(ExecuteFunctionResult result)
		{
			Dictionary<string, string> dictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(result.FunctionResult.ToString());
			if (dictionary == null)
			{
				foreach (NetPlayer requestedAutomutePlayer2 in requestedAutomutePlayers)
				{
					if (requestedAutomutePlayer2 != null)
					{
						ReceiveAutomuteSettings(requestedAutomutePlayer2, "none");
					}
				}
			}
			else
			{
				foreach (NetPlayer requestedAutomutePlayer3 in requestedAutomutePlayers)
				{
					if (requestedAutomutePlayer3 != null)
					{
						if (dictionary.TryGetValue(requestedAutomutePlayer3.UserId, out var value2))
						{
							ReceiveAutomuteSettings(requestedAutomutePlayer3, value2);
						}
						else
						{
							ReceiveAutomuteSettings(requestedAutomutePlayer3, "none");
						}
					}
				}
			}
			requestedAutomutePlayers.Clear();
			waitingForAutomuteCallback = false;
		}, delegate
		{
			foreach (NetPlayer requestedAutomutePlayer4 in requestedAutomutePlayers)
			{
				ReceiveAutomuteSettings(requestedAutomutePlayer4, "ERROR");
			}
			requestedAutomutePlayers.Clear();
			waitingForAutomuteCallback = false;
		});
	}

	private static void CancelAutomuteRequest()
	{
		playersToCheckAutomute.Clear();
		automuteQueued = false;
		if (requestedAutomutePlayers != null)
		{
			requestedAutomutePlayers.Clear();
		}
		waitingForAutomuteCallback = false;
	}

	private static void ReceiveAutomuteSettings(NetPlayer player, string score)
	{
		VRRigCache.Instance.TryGetVrrig(player, out var playerRig);
		if (playerRig != null)
		{
			playerRig.UpdateAutomuteLevel(score);
		}
	}

	private void ProcessAutomute()
	{
		int num = PlayerPrefs.GetInt("autoMute", 1);
		bPlayerAutoMuted = !hasManualMute && playerChatQuality < num;
	}

	public void RefreshVoiceChat()
	{
		if (!(Voice == null))
		{
			ProcessAutomute();
			Voice.SpeakerInUse.enabled = !forceMute && enableVoice && !bPlayerAutoMuted && GorillaComputer.instance.voiceChatOn == "TRUE";
			replacementVoiceSource.mute = forceMute || !enableVoice || bPlayerAutoMuted || GorillaComputer.instance.voiceChatOn == "OFF";
		}
	}

	public void AddLoudSpeakerNetwork(LoudSpeakerNetwork network)
	{
		if (!loudSpeakerNetworks.Contains(network))
		{
			loudSpeakerNetworks.Add(network);
		}
	}

	public void RemoveLoudSpeakerNetwork(LoudSpeakerNetwork network)
	{
		loudSpeakerNetworks.Remove(network);
	}

	public static void RefreshAllRigVoices()
	{
		staticTempRC = null;
		if (!NetworkSystem.Instance.InRoom || VRRigCache.Instance == null)
		{
			return;
		}
		NetPlayer[] allNetPlayers = NetworkSystem.Instance.AllNetPlayers;
		foreach (NetPlayer targetPlayer in allNetPlayers)
		{
			if (VRRigCache.Instance.TryGetVrrig(targetPlayer, out staticTempRC))
			{
				staticTempRC.RefreshVoiceChat();
			}
		}
	}
}
