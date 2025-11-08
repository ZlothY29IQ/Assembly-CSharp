using System.Collections.Generic;
using GorillaExtensions;
using GorillaGameModes;
using GorillaNetworking;
using GorillaTagScripts.VirtualStumpCustomMaps;
using Modio.Mods;
using TMPro;
using UnityEngine;

public class VirtualStumpTeleporter : MonoBehaviour, IBuildValidation, IGorillaSliceableSimple
{
	[SerializeField]
	private float stayInTriggerDuration = 3f;

	[SerializeField]
	private TMP_Text[] countdownTexts;

	[SerializeField]
	private GameObject[] handHoldObjects;

	[SerializeField]
	private List<GameObject> accessDeniedDisabledObjects = new List<GameObject>();

	[SerializeField]
	private List<GameObject> accessDeniedEnabledObjects = new List<GameObject>();

	[SerializeField]
	private Transform returnLocation;

	[SerializeField]
	private GTZone entranceZone = GTZone.arcade;

	[SerializeField]
	private GorillaNetworkJoinTrigger exitVStumpJoinTrigger;

	[SerializeField]
	private long autoLoadMapModId = ModId.Null;

	[SerializeField]
	private GameModeType autoLoadGamemode = GameModeType.None;

	[SerializeField]
	private GameModeType forcedGamemodeUponReturn = GameModeType.None;

	[SerializeField]
	private ParticleSystem teleportToVStumpVFX;

	[SerializeField]
	private ParticleSystem returnFromVStumpVFX;

	[SerializeField]
	private AudioSource teleporterSFXAudioSource;

	[SerializeField]
	private List<AudioClip> teleportingPlayerSoundClips = new List<AudioClip>();

	[SerializeField]
	private List<AudioClip> observerSoundClips = new List<AudioClip>();

	[SerializeField]
	private VirtualStumpTeleporterSerializer netSerializer;

	private VirtualStumpTeleporterSerializer mySerializer;

	private bool accessDenied;

	private bool teleporting;

	private float triggerEntryTime = -1f;

	[OnEnterPlay_Set(0)]
	private static ushort lastLoggingHandsMsgId;

	public bool BuildValidationCheck()
	{
		if (netSerializer.IsNull())
		{
			Debug.LogError("VStump Teleporter \"" + base.gameObject.GetPath() + "\" needs a reference to a VirtualStumpTeleporterSerializer for networked FX to function. Check out the teleporter prefabs in arcade or the stump", this);
			return false;
		}
		return true;
	}

	public void SliceUpdate()
	{
		if (!accessDenied && NetworkSystem.Instance.netState != NetSystemState.Idle && NetworkSystem.Instance.netState != NetSystemState.InGame)
		{
			DenyAccess();
		}
		if (accessDenied && (NetworkSystem.Instance.netState == NetSystemState.Idle || NetworkSystem.Instance.netState == NetSystemState.InGame) && !UGCPermissionManager.IsUGCDisabled)
		{
			AllowAccess();
		}
	}

	public void OnEnable()
	{
		if (netSerializer.IsNull())
		{
			Debug.LogWarning("[VStumpTeleporter.OnEnable] Net Serializer is null for \"" + base.gameObject.GetPath() + "\", networked teleport FX will not function.");
		}
		if (UGCPermissionManager.IsUGCDisabled || (NetworkSystem.Instance.netState != NetSystemState.Idle && NetworkSystem.Instance.netState != NetSystemState.InGame))
		{
			_ = lastLoggingHandsMsgId;
			_ = 1;
			lastLoggingHandsMsgId = 1;
			DenyAccess();
		}
		else
		{
			_ = lastLoggingHandsMsgId;
			_ = 2;
			lastLoggingHandsMsgId = 2;
			AllowAccess();
		}
		UGCPermissionManager.SubscribeToUGCEnabled(OnUGCEnabled);
		UGCPermissionManager.SubscribeToUGCDisabled(OnUGCDisabled);
		GorillaSlicerSimpleManager.RegisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.Update);
	}

	public void OnDisable()
	{
		AllowAccess();
		UGCPermissionManager.UnsubscribeFromUGCEnabled(OnUGCEnabled);
		UGCPermissionManager.UnsubscribeFromUGCDisabled(OnUGCDisabled);
		GorillaSlicerSimpleManager.UnregisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.Update);
	}

	private void OnUGCEnabled()
	{
		AllowAccess();
		_ = lastLoggingHandsMsgId;
		_ = 3;
		lastLoggingHandsMsgId = 3;
	}

	private void OnUGCDisabled()
	{
		DenyAccess();
		_ = lastLoggingHandsMsgId;
		_ = 4;
		lastLoggingHandsMsgId = 4;
	}

	public void OnTriggerEnter(Collider other)
	{
		if (!UGCPermissionManager.IsUGCDisabled && !accessDenied && !teleporting && !CustomMapManager.WaitingForRoomJoin && !CustomMapManager.WaitingForDisconnect && other.gameObject == GorillaTagger.Instance.headCollider.gameObject)
		{
			triggerEntryTime = Time.time;
			ShowCountdownText();
		}
	}

	public void OnTriggerStay(Collider other)
	{
		if (!UGCPermissionManager.IsUGCDisabled && !accessDenied && other.gameObject == GorillaTagger.Instance.headCollider.gameObject && triggerEntryTime >= 0f)
		{
			UpdateCountdownText();
			if (!teleporting && triggerEntryTime + stayInTriggerDuration <= Time.time)
			{
				TeleportPlayer();
				HideCountdownText();
			}
		}
	}

	public void OnTriggerExit(Collider other)
	{
		if (!UGCPermissionManager.IsUGCDisabled && !accessDenied && other.gameObject == GorillaTagger.Instance.headCollider.gameObject)
		{
			triggerEntryTime = -1f;
			HideCountdownText();
		}
	}

	private void ShowCountdownText()
	{
		if (UGCPermissionManager.IsUGCDisabled || accessDenied || countdownTexts.IsNullOrEmpty())
		{
			return;
		}
		int num = 1 + Mathf.FloorToInt(stayInTriggerDuration);
		for (int i = 0; i < countdownTexts.Length; i++)
		{
			if (!countdownTexts[i].IsNull())
			{
				countdownTexts[i].text = num.ToString();
				countdownTexts[i].gameObject.SetActive(value: true);
			}
		}
	}

	private void HideCountdownText()
	{
		if (countdownTexts.IsNullOrEmpty())
		{
			return;
		}
		for (int i = 0; i < countdownTexts.Length; i++)
		{
			if (!countdownTexts[i].IsNull())
			{
				countdownTexts[i].text = "";
				countdownTexts[i].gameObject.SetActive(value: false);
			}
		}
	}

	private void UpdateCountdownText()
	{
		if (UGCPermissionManager.IsUGCDisabled || accessDenied || countdownTexts.IsNullOrEmpty())
		{
			return;
		}
		float f = stayInTriggerDuration - (Time.time - triggerEntryTime);
		int num = 1 + Mathf.FloorToInt(f);
		for (int i = 0; i < countdownTexts.Length; i++)
		{
			if (!countdownTexts[i].IsNull())
			{
				countdownTexts[i].text = num.ToString();
			}
		}
	}

	public void TeleportPlayer()
	{
		if (!UGCPermissionManager.IsUGCDisabled && !accessDenied && !teleporting)
		{
			teleporting = true;
			CustomMapManager.TeleportToVirtualStump(this, FinishTeleport);
		}
	}

	private void FinishTeleport(bool success = true)
	{
		if (teleporting)
		{
			teleporting = false;
			triggerEntryTime = -1f;
		}
	}

	private void DenyAccess()
	{
		accessDenied = true;
		foreach (GameObject accessDeniedEnabledObject in accessDeniedEnabledObjects)
		{
			accessDeniedEnabledObject.SetActive(value: true);
		}
		foreach (GameObject accessDeniedDisabledObject in accessDeniedDisabledObjects)
		{
			accessDeniedDisabledObject.SetActive(value: false);
		}
	}

	private void AllowAccess()
	{
		if (UGCPermissionManager.IsUGCDisabled)
		{
			return;
		}
		accessDenied = false;
		foreach (GameObject accessDeniedEnabledObject in accessDeniedEnabledObjects)
		{
			accessDeniedEnabledObject.SetActive(value: false);
		}
		foreach (GameObject accessDeniedDisabledObject in accessDeniedDisabledObjects)
		{
			accessDeniedDisabledObject.SetActive(value: true);
		}
	}

	private short GetIndex()
	{
		if (!netSerializer.IsNotNull())
		{
			return -1;
		}
		return netSerializer.GetTeleporterIndex(this);
	}

	public GTZone GetZone()
	{
		return entranceZone;
	}

	public GorillaNetworkJoinTrigger GetExitVStumpJoinTrigger()
	{
		return exitVStumpJoinTrigger;
	}

	public Transform GetReturnTransform()
	{
		return returnLocation;
	}

	public long GetAutoLoadMapModId()
	{
		return autoLoadMapModId;
	}

	public GameModeType GetAutoLoadGamemode()
	{
		return autoLoadGamemode;
	}

	public GameModeType GetReturnGamemode()
	{
		return forcedGamemodeUponReturn;
	}

	public void PlayTeleportEffects(bool forLocalPlayer, bool toVStump, AudioSource vStumpSFXAudioSource = null, bool sendRPC = false)
	{
		if (sendRPC && netSerializer.IsNotNull())
		{
			netSerializer.NotifyPlayerTeleporting(GetIndex(), vStumpSFXAudioSource);
		}
		ParticleSystem particleSystem;
		if (toVStump)
		{
			particleSystem = teleportToVStumpVFX;
			if (forLocalPlayer && vStumpSFXAudioSource.IsNotNull() && !teleportingPlayerSoundClips.IsNullOrEmpty())
			{
				vStumpSFXAudioSource.clip = teleportingPlayerSoundClips[Random.Range(0, teleportingPlayerSoundClips.Count)];
				vStumpSFXAudioSource.Play();
			}
			if (!forLocalPlayer && teleporterSFXAudioSource.IsNotNull() && !observerSoundClips.IsNullOrEmpty())
			{
				teleporterSFXAudioSource.clip = observerSoundClips[Random.Range(0, observerSoundClips.Count)];
				teleporterSFXAudioSource.Play();
			}
		}
		else
		{
			particleSystem = returnFromVStumpVFX;
			if (teleporterSFXAudioSource.IsNotNull())
			{
				if (forLocalPlayer && !teleportingPlayerSoundClips.IsNullOrEmpty())
				{
					teleporterSFXAudioSource.clip = teleportingPlayerSoundClips[Random.Range(0, teleportingPlayerSoundClips.Count)];
				}
				else if (!forLocalPlayer && !observerSoundClips.IsNullOrEmpty())
				{
					teleporterSFXAudioSource.clip = observerSoundClips[Random.Range(0, observerSoundClips.Count)];
				}
				teleporterSFXAudioSource.Play();
			}
		}
		if (particleSystem.IsNotNull())
		{
			particleSystem.Play();
		}
	}
}
