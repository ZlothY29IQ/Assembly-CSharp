using GorillaGameModes;
using GorillaNetworking;
using UnityEngine;

namespace GorillaTagScripts;

public sealed class GorillaAmbushManager : GorillaTagManager
{
	public GameObject handTapFX;

	public GorillaSkin ambushSkin;

	[SerializeField]
	private AudioClip[] firstPersonTaggedSounds;

	[SerializeField]
	private float firstPersonTaggedSoundVolume;

	private static int handTapHash = -1;

	public float handTapScaleFactor = 0.5f;

	public float crawlingSpeedForMaxVolume;

	[SerializeField]
	private XSceneRef scryingPlaneRef;

	[SerializeField]
	private XSceneRef scryingPlane3pRef;

	private const int STEALTH_MATERIAL_INDEX = 13;

	private MeshRenderer scryingPlane;

	private bool hasScryingPlane;

	private MeshRenderer scryingPlane3p;

	private bool hasScryingPlane3p;

	public static int HandEffectHash => handTapHash;

	public static float HandFXScaleModifier { get; private set; }

	[field: SerializeField]
	public bool isGhostTag { get; private set; }

	public override GameModeType GameType()
	{
		if (!isGhostTag)
		{
			return GameModeType.Ambush;
		}
		return GameModeType.Ghost;
	}

	public override void Awake()
	{
		base.Awake();
		if (handTapFX != null)
		{
			handTapHash = PoolUtils.GameObjHashCode(handTapFX);
		}
		HandFXScaleModifier = handTapScaleFactor;
	}

	private void Start()
	{
		hasScryingPlane = scryingPlaneRef.TryResolve(out scryingPlane);
		hasScryingPlane3p = scryingPlane3pRef.TryResolve(out scryingPlane3p);
	}

	public override string GameModeName()
	{
		if (!isGhostTag)
		{
			return "AMBUSH";
		}
		return "GHOST";
	}

	public override string GameModeNameRoomLabel()
	{
		string text = (isGhostTag ? "GAME_MODE_GHOST_ROOM_LABEL" : "GAME_MODE_AMBUSH_ROOM_LABEL");
		string defaultResult = (isGhostTag ? "(GHOST GAME)" : "(AMBUSH GAME)");
		if (!LocalisationManager.TryGetKeyForCurrentLocale(text, out var result, defaultResult))
		{
			Debug.LogError("[LOCALIZATION::GORILLA_GAME_MANAGER] Failed to get key for Game Mode [" + text + "]");
		}
		return result;
	}

	public override void UpdatePlayerAppearance(VRRig rig)
	{
		int materialIndex = MyMatIndex(rig.creator);
		rig.ChangeMaterialLocal(materialIndex);
		bool flag = IsInfected(rig.Creator);
		bool flag2 = IsInfected(NetworkSystem.Instance.LocalPlayer);
		rig.bodyRenderer.SetGameModeBodyType(flag ? GorillaBodyType.Skeleton : GorillaBodyType.Default);
		rig.SetInvisibleToLocalPlayer(flag && !flag2);
		if (isGhostTag && rig.isOfflineVRRig)
		{
			CosmeticsController.instance.SetHideCosmeticsFromRemotePlayers(flag);
			if (hasScryingPlane)
			{
				scryingPlane.enabled = flag2;
			}
			if (hasScryingPlane3p)
			{
				scryingPlane3p.enabled = flag2;
			}
		}
	}

	public override int MyMatIndex(NetPlayer forPlayer)
	{
		if (!IsInfected(forPlayer))
		{
			return 0;
		}
		return 13;
	}

	public override void StopPlaying()
	{
		base.StopPlaying();
		foreach (RigContainer activeRigContainer in VRRigCache.ActiveRigContainers)
		{
			VRRig rig = activeRigContainer.Rig;
			GorillaSkin.ApplyToRig(rig, null, GorillaSkin.SkinType.gameMode);
			rig.bodyRenderer.SetGameModeBodyType(GorillaBodyType.Default);
			rig.SetInvisibleToLocalPlayer(invisible: false);
		}
		CosmeticsController.instance.SetHideCosmeticsFromRemotePlayers(hideCosmetics: false);
		if (hasScryingPlane)
		{
			scryingPlane.enabled = false;
		}
		if (hasScryingPlane3p)
		{
			scryingPlane3p.enabled = false;
		}
	}
}
