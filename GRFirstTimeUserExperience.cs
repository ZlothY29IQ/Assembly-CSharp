using System;
using System.Collections.Generic;
using GorillaLocomotion;
using GorillaNetworking;
using UnityEngine;

public class GRFirstTimeUserExperience : MonoBehaviour
{
	public enum TransitionState
	{
		Waiting,
		Flicker,
		Logo,
		ZoneLoad,
		Teleport,
		Exit
	}

	public Transform spawnPoint;

	public GameObject rootObject;

	public GameObject flickerSphere;

	public GameObject logoQuad;

	public AnimationCurve flickerTimeline;

	public float flickerDuration = 3f;

	public GTZone teleportZone = GTZone.none;

	public Transform teleportLocation;

	public float transitionDelay = 60f;

	public float logoDisplayTime = 4f;

	public float teleportSettleTime = 1f;

	public GorillaNetworkJoinTrigger joinRoomTrigger;

	public List<AudioClip> flickerAudio = new List<AudioClip>();

	public List<DisableGameObjectDelayed> delayObjects;

	private Transform flickerSphereOrigParent;

	private float stateStartTime = -1f;

	private bool flickerLightWasOff;

	private int flickerAudioCount;

	private AudioSource audioSource;

	private TransitionState transitionState;

	public GameLight playerLight;

	[ContextMenu("Set Player Pref")]
	private void RemovePlayerPref()
	{
		PlayerPrefs.SetString("spawnInWrongStump", "flagged");
		PlayerPrefs.Save();
	}

	private void OnEnable()
	{
		audioSource = GetComponent<AudioSource>();
		flickerSphere.SetActive(value: false);
		logoQuad.SetActive(value: false);
		flickerSphereOrigParent = flickerSphere.transform.parent;
		GameLightingManager.instance.SetCustomDynamicLightingEnabled(enable: true);
		playerLight = GorillaTagger.Instance.mainCamera.GetComponentInChildren<GameLight>(includeInactive: true);
		playerLight.gameObject.SetActive(value: true);
		ChangeState(TransitionState.Waiting);
	}

	public void ChangeState(TransitionState state)
	{
		transitionState = state;
		switch (state)
		{
		case TransitionState.Waiting:
			stateStartTime = Time.time;
			break;
		case TransitionState.Flicker:
			transitionState = TransitionState.Flicker;
			flickerSphere.transform.SetParent(GTPlayer.Instance.headCollider.transform, worldPositionStays: false);
			flickerSphere.SetActive(value: true);
			logoQuad.SetActive(value: false);
			stateStartTime = Time.time;
			break;
		case TransitionState.Logo:
			stateStartTime = Time.time;
			flickerSphere.SetActive(value: true);
			logoQuad.SetActive(value: true);
			break;
		case TransitionState.ZoneLoad:
		{
			ZoneManagement instance = ZoneManagement.instance;
			instance.OnSceneLoadsCompleted = (Action)Delegate.Combine(instance.OnSceneLoadsCompleted, new Action(OnZoneLoadComplete));
			ZoneManagement.SetActiveZone(teleportZone);
			break;
		}
		case TransitionState.Teleport:
			PhotonNetworkController.Instance.AttemptToJoinPublicRoom(joinRoomTrigger);
			GTPlayer.Instance.TeleportTo(teleportLocation.position, teleportLocation.rotation);
			GTPlayer.Instance.InitializeValues();
			stateStartTime = Time.time;
			break;
		case TransitionState.Exit:
			flickerSphere.transform.SetParent(flickerSphereOrigParent, worldPositionStays: false);
			flickerSphere.SetActive(value: false);
			logoQuad.SetActive(value: false);
			rootObject.SetActive(value: false);
			GorillaTagger.Instance.mainCamera.GetComponentInChildren<GameLight>(includeInactive: true).gameObject.SetActive(value: false);
			break;
		}
	}

	private void OnZoneLoadComplete()
	{
		ZoneManagement instance = ZoneManagement.instance;
		instance.OnSceneLoadsCompleted = (Action)Delegate.Remove(instance.OnSceneLoadsCompleted, new Action(OnZoneLoadComplete));
		ChangeState(TransitionState.Teleport);
	}

	public void InterruptWaitingTimer()
	{
		stateStartTime = -1f;
		for (int i = 0; i < delayObjects.Count; i++)
		{
			delayObjects[i].enabledTime = stateStartTime;
		}
	}

	private void Update()
	{
		switch (transitionState)
		{
		case TransitionState.Waiting:
			if (PrivateUIRoom.GetInOverlay())
			{
				if (stateStartTime >= 0f)
				{
					InterruptWaitingTimer();
				}
			}
			else if (stateStartTime < 0f)
			{
				stateStartTime = Time.time;
			}
			if (stateStartTime >= 0f && Time.time - stateStartTime >= transitionDelay)
			{
				ChangeState(TransitionState.Flicker);
			}
			break;
		case TransitionState.Flicker:
		{
			float num = Time.time - stateStartTime;
			if (stateStartTime >= 0f && num >= flickerDuration)
			{
				ChangeState(TransitionState.Logo);
				break;
			}
			bool flag = flickerTimeline.Evaluate(num / flickerDuration) < 0f;
			flickerSphere.SetActive(flag);
			if (flag && !flickerLightWasOff)
			{
				if (audioSource != null && flickerAudioCount < flickerAudio.Count && flickerAudio[flickerAudioCount] != null)
				{
					audioSource.PlayOneShot(flickerAudio[flickerAudioCount]);
				}
				flickerAudioCount++;
			}
			flickerLightWasOff = flag;
			break;
		}
		case TransitionState.Logo:
			if (stateStartTime >= 0f && Time.time - stateStartTime >= logoDisplayTime)
			{
				ChangeState(TransitionState.ZoneLoad);
			}
			break;
		case TransitionState.Teleport:
			if (stateStartTime >= 0f && Time.time - stateStartTime >= teleportSettleTime)
			{
				ChangeState(TransitionState.Exit);
			}
			break;
		case TransitionState.ZoneLoad:
			break;
		}
	}
}
