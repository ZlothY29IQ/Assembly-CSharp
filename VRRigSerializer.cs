using System;
using Fusion;
using GorillaExtensions;
using GorillaGameModes;
using GorillaLocomotion;
using GorillaTag;
using GorillaTag.Audio;
using GorillaTagScripts;
using Photon.Pun;
using Photon.Voice.PUN;
using Photon.Voice.Unity;
using UnityEngine;

[NetworkBehaviourWeaved(35)]
internal class VRRigSerializer : GorillaWrappedSerializer, IFXContextParems<HandTapArgs>, IFXContextParems<GeoSoundArg>
{
	[WeaverGenerated]
	[SerializeField]
	[DefaultForProperty("nickName", 0, 17)]
	[DrawIf("IsEditorWritable", true, CompareOperator.Equal, DrawIfMode.ReadOnly)]
	private NetworkString<_16> _nickName;

	[WeaverGenerated]
	[SerializeField]
	[DefaultForProperty("defaultName", 17, 17)]
	[DrawIf("IsEditorWritable", true, CompareOperator.Equal, DrawIfMode.ReadOnly)]
	private NetworkString<_16> _defaultName;

	[WeaverGenerated]
	[SerializeField]
	[DefaultForProperty("tutorialComplete", 34, 1)]
	[DrawIf("IsEditorWritable", true, CompareOperator.Equal, DrawIfMode.ReadOnly)]
	private bool _tutorialComplete;

	[SerializeField]
	private PhotonVoiceView voiceView;

	public Transform networkSpeaker;

	[SerializeField]
	private VRRig vrrig;

	private RigContainer rigContainer;

	private HandTapArgs handTapArgs = new HandTapArgs();

	private GeoSoundArg geoSoundArg = new GeoSoundArg();

	[Networked]
	[NetworkedWeaved(0, 17)]
	public unsafe NetworkString<_16> nickName
	{
		get
		{
			if (((NetworkBehaviour)this).Ptr == null)
			{
				throw new InvalidOperationException("Error when accessing VRRigSerializer.nickName. Networked properties can only be accessed when Spawned() has been called.");
			}
			return *(NetworkString<_16>*)((byte*)((NetworkBehaviour)this).Ptr + 0);
		}
		set
		{
			if (((NetworkBehaviour)this).Ptr == null)
			{
				throw new InvalidOperationException("Error when accessing VRRigSerializer.nickName. Networked properties can only be accessed when Spawned() has been called.");
			}
			*(NetworkString<_16>*)((byte*)((NetworkBehaviour)this).Ptr + 0) = value;
		}
	}

	[Networked]
	[NetworkedWeaved(17, 17)]
	public unsafe NetworkString<_16> defaultName
	{
		get
		{
			if (((NetworkBehaviour)this).Ptr == null)
			{
				throw new InvalidOperationException("Error when accessing VRRigSerializer.defaultName. Networked properties can only be accessed when Spawned() has been called.");
			}
			return *(NetworkString<_16>*)(((NetworkBehaviour)this).Ptr + 17);
		}
		set
		{
			if (((NetworkBehaviour)this).Ptr == null)
			{
				throw new InvalidOperationException("Error when accessing VRRigSerializer.defaultName. Networked properties can only be accessed when Spawned() has been called.");
			}
			*(NetworkString<_16>*)(((NetworkBehaviour)this).Ptr + 17) = value;
		}
	}

	[Networked]
	[NetworkedWeaved(34, 1)]
	public unsafe bool tutorialComplete
	{
		get
		{
			if (((NetworkBehaviour)this).Ptr == null)
			{
				throw new InvalidOperationException("Error when accessing VRRigSerializer.tutorialComplete. Networked properties can only be accessed when Spawned() has been called.");
			}
			return ReadWriteUtilsForWeaver.ReadBoolean(((NetworkBehaviour)this).Ptr + 34);
		}
		set
		{
			if (((NetworkBehaviour)this).Ptr == null)
			{
				throw new InvalidOperationException("Error when accessing VRRigSerializer.tutorialComplete. Networked properties can only be accessed when Spawned() has been called.");
			}
			ReadWriteUtilsForWeaver.WriteBoolean(((NetworkBehaviour)this).Ptr + 34, value);
		}
	}

	private PhotonVoiceView Voice => voiceView;

	public VRRig VRRig => vrrig;

	public FXSystemSettings settings => vrrig.fxSettings;

	public InDelegateListProcessor<RigContainer, PhotonMessageInfoWrapped> SuccesfullSpawnEvent { get; private set; } = new InDelegateListProcessor<RigContainer, PhotonMessageInfoWrapped>(2);

	protected override bool OnSpawnSetupCheck(PhotonMessageInfoWrapped wrappedInfo, out GameObject outTargetObject, out Type outTargetType)
	{
		outTargetObject = null;
		outTargetType = null;
		NetPlayer player = NetworkSystem.Instance.GetPlayer(wrappedInfo.senderID);
		if (netView.IsRoomView)
		{
			if (player != null)
			{
				MonkeAgent.instance.SendReport("creating rigs as room objects", player.UserId, player.NickName);
			}
			return false;
		}
		if (NetworkSystem.Instance.IsObjectRoomObject(base.gameObject))
		{
			NetPlayer player2 = NetworkSystem.Instance.GetPlayer(wrappedInfo.senderID);
			if (player2 != null)
			{
				MonkeAgent.instance.SendReport("creating rigs as room objects", player2.UserId, player2.NickName);
			}
			return false;
		}
		if (player != netView.Owner)
		{
			MonkeAgent.instance.SendReport("creating rigs for someone else", player.UserId, player.NickName);
			return false;
		}
		if (VRRigCache.Instance.TryGetVrrig(player, out rigContainer))
		{
			outTargetObject = rigContainer.gameObject;
			outTargetType = typeof(VRRig);
			vrrig = rigContainer.Rig;
			return true;
		}
		return false;
	}

	protected override void OnSuccesfullySpawned(PhotonMessageInfoWrapped info)
	{
		bool initialized = rigContainer.Initialized;
		rigContainer.InitializeNetwork(netView, Voice, this);
		networkSpeaker.SetParent(rigContainer.SpeakerHead, worldPositionStays: false);
		base.transform.SetParent(VRRigCache.Instance.NetworkParent, worldPositionStays: true);
		SetupLoudSpeakerNetwork(rigContainer);
		netView.GetView.AddCallbackTarget(this);
		if (!initialized)
		{
			object[] instantiationData = info.punInfo.photonView.InstantiationData;
			float red = 0f;
			float green = 0f;
			float blue = 0f;
			if (instantiationData != null && instantiationData.Length == 3 && instantiationData[0] is float value && instantiationData[1] is float value2 && instantiationData[2] is float value3)
			{
				red = value.ClampSafe(0f, 1f);
				green = value2.ClampSafe(0f, 1f);
				blue = value3.ClampSafe(0f, 1f);
			}
			vrrig.InitializeNoobMaterialLocal(red, green, blue);
		}
		SuccesfullSpawnEvent.InvokeSafe(in rigContainer, in info);
		NetworkSystem.Instance.IsObjectLocallyOwned(base.gameObject);
		if (VRRigCache.isInitialized)
		{
			VRRigCache.Instance.OnVrrigSerializerSuccesfullySpawned();
		}
	}

	protected override void OnFailedSpawn()
	{
	}

	protected override void OnBeforeDespawn()
	{
		CleanUp(netDestroy: true);
	}

	private void CleanUp(bool netDestroy)
	{
		if (!successfullInstantiate)
		{
			return;
		}
		successfullInstantiate = false;
		if (vrrig != null)
		{
			if (!NetworkSystem.Instance.InRoom)
			{
				if (vrrig.isOfflineVRRig)
				{
					vrrig.ChangeMaterialLocal(0);
				}
			}
			else if (vrrig.isOfflineVRRig)
			{
				NetworkSystem.Instance.NetDestroy(base.gameObject);
			}
			if ((object)vrrig.netView == netView)
			{
				vrrig.netView = null;
			}
			if ((object)vrrig.rigSerializer == this)
			{
				vrrig.rigSerializer = null;
			}
		}
		if (networkSpeaker != null)
		{
			CleanupLoudSpeakerNetwork();
			if (netDestroy)
			{
				networkSpeaker.SetParent(base.transform, worldPositionStays: false);
			}
			else
			{
				networkSpeaker.SetParent(null);
			}
			networkSpeaker.gameObject.SetActive(value: false);
		}
		vrrig = null;
	}

	private void OnDisable()
	{
		NetworkBehaviourUtils.InternalOnDisable(this);
		CleanUp(netDestroy: false);
	}

	private void OnDestroy()
	{
		NetworkBehaviourUtils.InternalOnDestroy(this);
		if (networkSpeaker != null && networkSpeaker.parent != base.transform)
		{
			UnityEngine.Object.Destroy(networkSpeaker.gameObject);
		}
	}

	[PunRPC]
	public void RPC_InitializeNoobMaterial(float red, float green, float blue, PhotonMessageInfo info)
	{
		InitializeNoobMaterialShared(red, green, blue, info);
	}

	[PunRPC]
	public void RPC_RequestCosmetics(PhotonMessageInfo info)
	{
		RequestCosmeticsShared(info);
	}

	[PunRPC]
	public void RPC_PlayDrum(int drumIndex, float drumVolume, PhotonMessageInfo info)
	{
		PlayDrumShared(drumIndex, drumVolume, info);
	}

	[PunRPC]
	public void RPC_PlaySelfOnlyInstrument(int selfOnlyIndex, int noteIndex, float instrumentVol, PhotonMessageInfo info)
	{
		PlaySelfOnlyInstrumentShared(selfOnlyIndex, noteIndex, instrumentVol, info);
	}

	[PunRPC]
	public void RPC_PlayHandTap(int soundIndex, bool isLeftHand, float tapVolume, PhotonMessageInfo info = default(PhotonMessageInfo))
	{
		PlayHandTapShared(soundIndex, isLeftHand, tapVolume, info);
	}

	public void RPC_UpdateNativeSize(float value, PhotonMessageInfo info = default(PhotonMessageInfo))
	{
		UpdateNativeSizeShared(value, info);
	}

	public void RPC_UpdateCosmetics(string[] currentItems, PhotonMessageInfo info)
	{
	}

	public void RPC_UpdateCosmeticsWithTryon(string[] currentItems, string[] tryOnItems, PhotonMessageInfo info)
	{
	}

	[PunRPC]
	public void RPC_UpdateCosmeticsWithTryonPacked(int[] currentItemsPacked, int[] tryOnItemsPacked, bool playfx, PhotonMessageInfo info)
	{
		UpdateCosmeticsWithTryonShared(currentItemsPacked, tryOnItemsPacked, playfx, info);
	}

	[PunRPC]
	public void RPC_HideAllCosmetics(PhotonMessageInfo info)
	{
		vrrig?.HideAllCosmetics(info);
	}

	[PunRPC]
	public void RPC_PlaySplashEffect(Vector3 splashPosition, Quaternion splashRotation, float splashScale, float boundingRadius, bool bigSplash, bool enteringWater, PhotonMessageInfo info)
	{
		PlaySplashEffectShared(splashPosition, splashRotation, splashScale, boundingRadius, bigSplash, enteringWater, info);
	}

	[PunRPC]
	public void RPC_PlayGeodeEffect(Vector3 hitPosition, PhotonMessageInfo info)
	{
		PlayGeodeEffectShared(hitPosition, info);
	}

	[PunRPC]
	public void EnableNonCosmeticHandItemRPC(bool enable, bool isLeftHand, PhotonMessageInfo info)
	{
		EnableNonCosmeticHandItemShared(enable, isLeftHand, info);
	}

	[PunRPC]
	public void OnHandTapRPC(int audioClipIndex, bool isDownTap, bool isLeftHand, StiltID stiltID, float handTapSpeed, long packedDirFromHitToHand, PhotonMessageInfo info)
	{
		OnHandTapRPCShared(audioClipIndex, isDownTap, isLeftHand, stiltID, handTapSpeed, packedDirFromHitToHand, info);
	}

	[PunRPC]
	public void RPC_UpdateQuestScore(int score, PhotonMessageInfo info)
	{
		UpdateQuestScore(score, info);
	}

	[PunRPC]
	public void RPC_UpdateRankedInfo(float elo, int questRank, int PCRank, PhotonMessageInfo info)
	{
		UpdateRankedInfo(elo, questRank, PCRank, info);
	}

	private void SetupLoudSpeakerNetwork(RigContainer rigContainer)
	{
		if (networkSpeaker == null)
		{
			return;
		}
		Speaker component = networkSpeaker.GetComponent<Speaker>();
		if (component == null)
		{
			return;
		}
		foreach (LoudSpeakerNetwork loudSpeakerNetwork in rigContainer.LoudSpeakerNetworks)
		{
			loudSpeakerNetwork.AddSpeaker(component);
		}
	}

	private void CleanupLoudSpeakerNetwork()
	{
		if (networkSpeaker == null)
		{
			return;
		}
		Speaker component = networkSpeaker.GetComponent<Speaker>();
		if (component == null)
		{
			return;
		}
		foreach (LoudSpeakerNetwork loudSpeakerNetwork in rigContainer.LoudSpeakerNetworks)
		{
			loudSpeakerNetwork.RemoveSpeaker(component);
		}
	}

	public void BroadcastLoudSpeakerNetwork(bool toggleBroadcast, int actorNumber)
	{
		if (VRRigCache.Instance.TryGetVrrig(NetworkSystem.Instance.GetPlayer(actorNumber), out var playerRig))
		{
			bool isLocal = actorNumber == NetworkSystem.Instance.LocalPlayer.ActorNumber;
			BroadcastLoudSpeakerNetworkShared(toggleBroadcast, playerRig, actorNumber, isLocal);
		}
	}

	private void BroadcastLoudSpeakerNetworkShared(bool toggleBroadcast, RigContainer rigContainer, int actorNumber, bool isLocal)
	{
		SetupLoudSpeakerNetwork(rigContainer);
		foreach (LoudSpeakerNetwork loudSpeakerNetwork in rigContainer.LoudSpeakerNetworks)
		{
			if (toggleBroadcast)
			{
				loudSpeakerNetwork.BroadcastLoudSpeakerNetwork(actorNumber, isLocal);
			}
			else
			{
				loudSpeakerNetwork.StopBroadcastLoudSpeakerNetwork(actorNumber, isLocal);
			}
		}
	}

	[PunRPC]
	public void GrabbedByPlayer(bool grabbedBody, bool grabbedLeftHand, bool grabbedWithLeftHand, PhotonMessageInfo info)
	{
		if (GorillaGameModes.GameMode.ActiveGameMode is GorillaGuardianManager gorillaGuardianManager && gorillaGuardianManager.IsPlayerGuardian(info.Sender) && VRRigCache.Instance.TryGetVrrig(info.Sender, out var playerRig))
		{
			vrrig.GrabbedByPlayer(playerRig.Rig, grabbedBody, grabbedLeftHand, grabbedWithLeftHand);
		}
	}

	[PunRPC]
	public void DroppedByPlayer(Vector3 throwVelocity, PhotonMessageInfo info)
	{
		MonkeAgent.IncrementRPCCall(info, "DroppedByPlayer");
		if (vrrig.isOfflineVRRig && VRRigCache.Instance.TryGetVrrig(info.Sender, out var playerRig) && throwVelocity.IsValid(10000f))
		{
			vrrig.DroppedByPlayer(playerRig.Rig, throwVelocity);
		}
	}

	void IFXContextParems<HandTapArgs>.OnPlayFX(HandTapArgs parems)
	{
		vrrig.PlayHandTapLocal(parems.soundIndex, parems.isLeftHand, parems.tapVolume);
	}

	void IFXContextParems<GeoSoundArg>.OnPlayFX(GeoSoundArg parems)
	{
		vrrig?.PlayGeodeEffect(parems.position);
	}

	private void OnHandTapRPCShared(int audioClipIndex, bool isDownTap, bool isLeftHand, StiltID stiltID, float handTapSpeed, long packedDirFromHitToHand, PhotonMessageInfoWrapped info)
	{
		MonkeAgent.IncrementRPCCall(info, "OnHandTapRPCShared");
		if (info.Sender != netView.Owner || audioClipIndex < 0 || audioClipIndex >= GTPlayer.Instance.materialData.Count)
		{
			return;
		}
		TakeMyHand_HandLink takeMyHand_HandLink = (isLeftHand ? vrrig.rightHandLink : vrrig.leftHandLink);
		NetPlayer grabbedPlayer = takeMyHand_HandLink.grabbedPlayer;
		if (grabbedPlayer != null && grabbedPlayer.IsLocal)
		{
			(takeMyHand_HandLink.grabbedHandIsLeft ? VRRig.LocalRig.leftHandLink : VRRig.LocalRig.rightHandLink).PlayVicariousTapHaptic();
		}
		Vector3 tapDir = Utils.UnpackVector3FromLong(packedDirFromHitToHand);
		if (!Mathf.Approximately(tapDir.sqrMagnitude, 1f))
		{
			tapDir.Normalize();
		}
		float max = GorillaTagger.Instance.DefaultHandTapVolume;
		if (GorillaGameModes.GameMode.ActiveGameMode is GorillaAmbushManager gorillaAmbushManager && gorillaAmbushManager.IsInfected(rigContainer.Creator))
		{
			max = gorillaAmbushManager.crawlingSpeedForMaxVolume;
		}
		OnHandTapFX onHandTapFX = new OnHandTapFX
		{
			rig = vrrig,
			surfaceIndex = audioClipIndex,
			isDownTap = isDownTap,
			isLeftHand = isLeftHand,
			stiltID = stiltID,
			volume = handTapSpeed.ClampSafe(0f, max),
			speed = handTapSpeed,
			tapDir = tapDir
		};
		if (CrittersManager.instance.IsNotNull() && CrittersManager.instance.LocalAuthority() && CrittersManager.instance.rigSetupByRig[vrrig].IsNotNull())
		{
			CrittersLoudNoise crittersLoudNoise = (CrittersLoudNoise)CrittersManager.instance.rigSetupByRig[vrrig].rigActors[(!isLeftHand) ? 2 : 0].actorSet;
			if (crittersLoudNoise.IsNotNull())
			{
				crittersLoudNoise.PlayHandTapRemote(info.SentServerTime, isLeftHand);
			}
		}
		GameEntityManager managerForZone = GameEntityManager.GetManagerForZone(GTZone.ghostReactor);
		if (managerForZone != null && managerForZone.ghostReactorManager != null)
		{
			Vector3 tapPos = (isLeftHand ? vrrig.leftHand.rigTarget.position : vrrig.rightHand.rigTarget.position);
			managerForZone.ghostReactorManager.OnSharedTap(vrrig, tapPos, handTapSpeed);
		}
		FXSystem.PlayFXForRig(FXType.OnHandTap, onHandTapFX, info);
	}

	private void PlayHandTapShared(int soundIndex, bool isLeftHand, float tapVolume, PhotonMessageInfoWrapped info = default(PhotonMessageInfoWrapped))
	{
		MonkeAgent.IncrementRPCCall(info, "PlayHandTapShared");
		NetPlayer sender = info.Sender;
		if (info.Sender == netView.Owner && float.IsFinite(tapVolume))
		{
			handTapArgs.soundIndex = soundIndex;
			handTapArgs.isLeftHand = isLeftHand;
			handTapArgs.tapVolume = Mathf.Clamp(tapVolume, 0f, 0.1f);
			FXSystem.PlayFX(FXType.PlayHandTap, this, handTapArgs, info);
		}
		else
		{
			MonkeAgent.instance.SendReport("inappropriate tag data being sent hand tap", sender.UserId, sender.NickName);
		}
	}

	private void UpdateNativeSizeShared(float value, PhotonMessageInfoWrapped info = default(PhotonMessageInfoWrapped))
	{
		MonkeAgent.IncrementRPCCall(info, "UpdateNativeSizeShared");
		NetPlayer sender = info.Sender;
		if (info.Sender == netView.Owner && RPCUtil.SafeValue(value, 0.1f, 10f) && RPCUtil.NotSpam("UpdateNativeSizeShared", info, 1f))
		{
			if (vrrig != null)
			{
				vrrig.NativeScale = value;
			}
		}
		else
		{
			MonkeAgent.instance.SendReport("inappropriate tag data being sent native size", sender.UserId, sender.NickName);
		}
	}

	private void PlayGeodeEffectShared(Vector3 hitPosition, PhotonMessageInfoWrapped info)
	{
		MonkeAgent.IncrementRPCCall(info, "PlayGeodeEffectShared");
		if (info.Sender == netView.Owner && hitPosition.IsValid(10000f))
		{
			geoSoundArg.position = hitPosition;
			FXSystem.PlayFX(FXType.PlayHandTap, this, geoSoundArg, info);
		}
		else
		{
			MonkeAgent.instance.SendReport("inappropriate tag data being sent geode effect", info.Sender.UserId, info.Sender.NickName);
		}
	}

	private void InitializeNoobMaterialShared(float red, float green, float blue, PhotonMessageInfoWrapped info)
	{
		vrrig?.InitializeNoobMaterial(red, green, blue, info);
	}

	private void RequestMaterialColorShared(int askingPlayerID, PhotonMessageInfoWrapped info)
	{
		vrrig?.RequestMaterialColor(askingPlayerID, info);
	}

	private void RequestCosmeticsShared(PhotonMessageInfoWrapped info)
	{
		MonkeAgent.IncrementRPCCall(info, "RequestCosmetics");
		if (VRRigCache.Instance.TryGetVrrig(info.Sender, out var playerRig) && playerRig.Rig.fxSettings.callSettings[9].CallLimitSettings.CheckCallTime(Time.unscaledTime))
		{
			vrrig?.RequestCosmetics(info);
		}
	}

	private void PlayDrumShared(int drumIndex, float drumVolume, PhotonMessageInfoWrapped info)
	{
		vrrig?.PlayDrum(drumIndex, drumVolume, info);
	}

	private void PlaySelfOnlyInstrumentShared(int selfOnlyIndex, int noteIndex, float instrumentVol, PhotonMessageInfoWrapped info)
	{
		vrrig?.PlaySelfOnlyInstrument(selfOnlyIndex, noteIndex, instrumentVol, info);
	}

	private void UpdateCosmeticsWithTryonShared(int[] currentItems, int[] tryOnItems, bool playfx, PhotonMessageInfoWrapped info)
	{
		vrrig?.UpdateCosmeticsWithTryon(currentItems, tryOnItems, playfx, info);
	}

	private void PlaySplashEffectShared(Vector3 splashPosition, Quaternion splashRotation, float splashScale, float boundingRadius, bool bigSplash, bool enteringWater, PhotonMessageInfoWrapped info)
	{
		vrrig?.PlaySplashEffect(splashPosition, splashRotation, splashScale, boundingRadius, bigSplash, enteringWater, info);
	}

	private void EnableNonCosmeticHandItemShared(bool enable, bool isLeftHand, PhotonMessageInfoWrapped info)
	{
		vrrig?.EnableNonCosmeticHandItemRPC(enable, isLeftHand, info);
	}

	public void UpdateQuestScore(int score, PhotonMessageInfoWrapped info)
	{
		vrrig?.UpdateQuestScore(score, info);
	}

	public void UpdateRankedInfo(float elo, int questRank, int PCRank, PhotonMessageInfoWrapped info)
	{
		vrrig?.UpdateRankedInfo(elo, questRank, PCRank, info);
	}

	[WeaverGenerated]
	public override void CopyBackingFieldsToState(bool P_0)
	{
		base.CopyBackingFieldsToState(P_0);
		nickName = _nickName;
		defaultName = _defaultName;
		tutorialComplete = _tutorialComplete;
	}

	[WeaverGenerated]
	public override void CopyStateToBackingFields()
	{
		base.CopyStateToBackingFields();
		_nickName = nickName;
		_defaultName = defaultName;
		_tutorialComplete = tutorialComplete;
	}
}
