using System;
using System.Collections;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using GorillaExtensions;
using GorillaLocomotion;
using GorillaNetworking;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class GreyZoneManager : MonoBehaviourPun, IPunObservable, IInRoomCallbacks
{
	[OnEnterPlay_SetNull]
	public static volatile GreyZoneManager Instance;

	[SerializeField]
	private float greyZoneActiveDuration = 90f;

	[SerializeField]
	private float[] gravityFactorOptions = new float[3] { 0.25f, 0.5f, 0.75f };

	[SerializeField]
	private int gravityFactorOptionSelection = 1;

	[SerializeField]
	private float summoningActivationTime = 3f;

	[SerializeField]
	private AudioSource greyZoneAmbience;

	[SerializeField]
	private float ambienceFadeTime = 4f;

	[SerializeField]
	private bool forceTimeOfDayToNight;

	[SerializeField]
	private float skyMonsterMovementEnterTime = 4.5f;

	[SerializeField]
	private float skyMonsterMovementExitTime = 3.2f;

	[SerializeField]
	private float skyMonsterDistGravityRampBuffer = 0.15f;

	[SerializeField]
	[Range(0f, 1f)]
	private float gravityReductionAmount = 1f;

	private float simpleGravityFactor = 1f;

	[SerializeField]
	private ParticleSystem greyZoneParticles;

	[SerializeField]
	private float particlePredictiveSpawnMaxDist = 4f;

	[SerializeField]
	private float particlePredictiveSpawnVelocityFactor = 0.5f;

	private bool photonConnectedDuringActivation;

	private double greyZoneActivationTime;

	private bool greyZoneActive;

	private bool _tickRunning;

	private float summoningProgress;

	private List<GreyZoneSummoner> activeSummoners = new List<GreyZoneSummoner>();

	private Dictionary<int, (VRRig, GreyZoneSummoner)> summoningPlayers = new Dictionary<int, (VRRig, GreyZoneSummoner)>();

	private Dictionary<int, float> summoningPlayerProgress = new Dictionary<int, float>();

	private HashSet<int> invalidSummoners = new HashSet<int>();

	private Coroutine audioFadeCoroutine;

	private Player[] roomPlayerList;

	private ShaderHashId _GreyZoneActive = new ShaderHashId("_GreyZoneActive");

	private MoonController moonController;

	private float skyMonsterMovementVelocity;

	private bool gravityOverrideSet;

	private float greyZoneAmbienceVolume = 0.15f;

	private int greyZoneAvailableDayOfYear = new DateTime(2024, 10, 25).DayOfYear;

	public Action OnGreyZoneActivated;

	public Action OnGreyZoneDeactivated;

	public bool GreyZoneActive => greyZoneActive;

	public bool GreyZoneAvailable
	{
		get
		{
			bool result = false;
			if (GorillaComputer.instance != null)
			{
				result = GorillaComputer.instance.GetServerTime().DayOfYear >= greyZoneAvailableDayOfYear;
			}
			return result;
		}
	}

	public int GravityFactorSelection => gravityFactorOptionSelection;

	public bool TickRunning
	{
		get
		{
			return _tickRunning;
		}
		set
		{
			_tickRunning = value;
		}
	}

	public bool HasAuthority
	{
		get
		{
			if (PhotonNetwork.InRoom)
			{
				return base.photonView.IsMine;
			}
			return true;
		}
	}

	public float SummoningProgress => summoningProgress;

	public void RegisterSummoner(GreyZoneSummoner summoner)
	{
		if (!activeSummoners.Contains(summoner))
		{
			activeSummoners.Add(summoner);
		}
	}

	public void DeregisterSummoner(GreyZoneSummoner summoner)
	{
		if (activeSummoners.Contains(summoner))
		{
			activeSummoners.Remove(summoner);
		}
	}

	public void RegisterMoon(MoonController moon)
	{
		moonController = moon;
	}

	public void UnregisterMoon(MoonController moon)
	{
		if (moonController == moon)
		{
			moonController = null;
		}
	}

	public void ActivateGreyZoneAuthority()
	{
		greyZoneActive = true;
		photonConnectedDuringActivation = PhotonNetwork.InRoom;
		greyZoneActivationTime = (photonConnectedDuringActivation ? PhotonNetwork.Time : ((double)Time.time));
		ActivateGreyZoneLocal();
	}

	private void ActivateGreyZoneLocal()
	{
		Shader.SetGlobalInt(_GreyZoneActive, 1);
		GTPlayer instance = GTPlayer.Instance;
		if (instance != null)
		{
			instance.SetGravityOverride(this, GravityOverrideFunction);
			gravityOverrideSet = true;
		}
		if (MusicManager.Instance != null)
		{
			MusicManager.Instance.FadeOutMusic(2f);
		}
		if (audioFadeCoroutine != null)
		{
			StopCoroutine(audioFadeCoroutine);
		}
		audioFadeCoroutine = StartCoroutine(FadeAudioIn(greyZoneAmbience, greyZoneAmbienceVolume, ambienceFadeTime));
		if (greyZoneAmbience != null)
		{
			greyZoneAmbience.GTPlay();
		}
		greyZoneParticles.gameObject.SetActive(value: true);
		summoningProgress = 1f;
		UpdateSummonerVisuals();
		for (int i = 0; i < activeSummoners.Count; i++)
		{
			activeSummoners[i].OnGreyZoneActivated();
		}
		if (OnGreyZoneActivated != null)
		{
			OnGreyZoneActivated();
		}
	}

	public void LocalSimpleActivation(bool onOff, float gravityFactor)
	{
		GTPlayer instance = GTPlayer.Instance;
		if (!(instance == null) && PlayerPrefs.GetString("didTutorial", "nope") == "done")
		{
			simpleGravityFactor = Mathf.Clamp(gravityFactor, 0f, 5f);
			Shader.SetGlobalInt(_GreyZoneActive, onOff ? 1 : 0);
			if (onOff)
			{
				instance.SetGravityOverride(this, SimpleGravityOverrideFunction);
			}
			else
			{
				instance.UnsetGravityOverride(this);
			}
			gravityOverrideSet = onOff;
			greyZoneParticles.gameObject.SetActive(onOff);
		}
	}

	public void DeactivateGreyZoneAuthority()
	{
		greyZoneActive = false;
		foreach (KeyValuePair<int, (VRRig, GreyZoneSummoner)> summoningPlayer in summoningPlayers)
		{
			summoningPlayerProgress[summoningPlayer.Key] = 0f;
		}
		DeactivateGreyZoneLocal();
	}

	private void DeactivateGreyZoneLocal()
	{
		Shader.SetGlobalInt(_GreyZoneActive, 0);
		if (MusicManager.Instance != null)
		{
			MusicManager.Instance.FadeInMusic(4f);
		}
		if (audioFadeCoroutine != null)
		{
			StopCoroutine(audioFadeCoroutine);
		}
		audioFadeCoroutine = StartCoroutine(FadeAudioOut(greyZoneAmbience, ambienceFadeTime));
		greyZoneParticles.gameObject.SetActive(value: false);
		summoningProgress = 0f;
		UpdateSummonerVisuals();
		if (OnGreyZoneDeactivated != null)
		{
			OnGreyZoneDeactivated();
		}
	}

	public void ForceStopGreyZone()
	{
		greyZoneActive = false;
		Shader.SetGlobalInt(_GreyZoneActive, 0);
		GTPlayer instance = GTPlayer.Instance;
		if (instance != null)
		{
			instance.UnsetGravityOverride(this);
		}
		gravityOverrideSet = false;
		if (moonController != null)
		{
			moonController.UpdateDistance(1f);
		}
		if (MusicManager.Instance != null)
		{
			MusicManager.Instance.FadeInMusic(0f);
		}
		if (greyZoneAmbience != null)
		{
			greyZoneAmbience.volume = 0f;
			greyZoneAmbience.GTStop();
		}
		greyZoneParticles.gameObject.SetActive(value: false);
		summoningProgress = 0f;
		UpdateSummonerVisuals();
		if (OnGreyZoneDeactivated != null)
		{
			OnGreyZoneDeactivated();
		}
	}

	public void GravityOverrideFunction(GTPlayer player)
	{
		gravityReductionAmount = 0f;
		if (moonController != null)
		{
			gravityReductionAmount = Mathf.InverseLerp(1f - skyMonsterDistGravityRampBuffer, skyMonsterDistGravityRampBuffer, moonController.Distance);
		}
		float num = Mathf.Lerp(1f, gravityFactorOptions[gravityFactorOptionSelection], gravityReductionAmount);
		player.AddForce(Physics.gravity * num * player.scale, ForceMode.Acceleration);
	}

	public void SimpleGravityOverrideFunction(GTPlayer player)
	{
		player.AddForce(Physics.gravity * simpleGravityFactor * player.scale, ForceMode.Acceleration);
	}

	private IEnumerator FadeAudioIn(AudioSource source, float maxVolume, float duration)
	{
		if (source != null)
		{
			float startingVolume = source.volume;
			float startTime = Time.time;
			source.GTPlay();
			for (float num = 0f; num < 1f; num = (Time.time - startTime) / duration)
			{
				source.volume = Mathf.Lerp(startingVolume, maxVolume, num);
				yield return null;
			}
			source.volume = maxVolume;
		}
	}

	private IEnumerator FadeAudioOut(AudioSource source, float duration)
	{
		if (source != null)
		{
			float startingVolume = source.volume;
			float startTime = Time.time;
			for (float num = 0f; num < 1f; num = (Time.time - startTime) / duration)
			{
				source.volume = Mathf.Lerp(startingVolume, 0f, num);
				yield return null;
			}
			source.volume = 0f;
			source.Stop();
		}
	}

	public void VRRigEnteredSummonerProximity(VRRig rig, GreyZoneSummoner summoner)
	{
		if (!summoningPlayers.ContainsKey(rig.Creator.ActorNumber))
		{
			summoningPlayers.Add(rig.Creator.ActorNumber, (rig, summoner));
			summoningPlayerProgress.Add(rig.Creator.ActorNumber, 0f);
		}
	}

	public void VRRigExitedSummonerProximity(VRRig rig, GreyZoneSummoner summoner)
	{
		if (summoningPlayers.ContainsKey(rig.Creator.ActorNumber))
		{
			summoningPlayers.Remove(rig.Creator.ActorNumber);
			summoningPlayerProgress.Remove(rig.Creator.ActorNumber);
		}
	}

	private void UpdateSummonerVisuals()
	{
		bool greyZoneAvailable = GreyZoneAvailable;
		for (int i = 0; i < activeSummoners.Count; i++)
		{
			activeSummoners[i].UpdateProgressFeedback(greyZoneAvailable);
		}
	}

	private void ValidateSummoningPlayers()
	{
		invalidSummoners.Clear();
		foreach (KeyValuePair<int, (VRRig, GreyZoneSummoner)> summoningPlayer in summoningPlayers)
		{
			VRRig item = summoningPlayer.Value.Item1;
			GreyZoneSummoner item2 = summoningPlayer.Value.Item2;
			if (item.Creator.ActorNumber != summoningPlayer.Key || (item.head.rigTarget.position - item2.SummoningFocusPoint).sqrMagnitude > item2.SummonerMaxDistance * item2.SummonerMaxDistance)
			{
				invalidSummoners.Add(summoningPlayer.Key);
			}
		}
		foreach (int invalidSummoner in invalidSummoners)
		{
			summoningPlayers.Remove(invalidSummoner);
			summoningPlayerProgress.Remove(invalidSummoner);
		}
	}

	private int DayNightOverrideFunction(int inputIndex)
	{
		int num = 0;
		int num2 = 8;
		int num3 = inputIndex - num;
		int num4 = num2 - inputIndex;
		if (num3 <= 0 || num4 <= 0)
		{
			return inputIndex;
		}
		if (num4 > num3)
		{
			return num2;
		}
		return num;
	}

	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
			greyZoneAmbienceVolume = greyZoneAmbience.volume;
		}
		else
		{
			UnityEngine.Object.Destroy(this);
		}
	}

	private void OnEnable()
	{
		if (forceTimeOfDayToNight)
		{
			BetterDayNightManager instance = BetterDayNightManager.instance;
			if (instance != null)
			{
				instance.SetTimeIndexOverrideFunction(DayNightOverrideFunction);
			}
		}
	}

	private void OnDisable()
	{
		ForceStopGreyZone();
		if (forceTimeOfDayToNight)
		{
			BetterDayNightManager instance = BetterDayNightManager.instance;
			if (instance != null)
			{
				instance.UnsetTimeIndexOverrideFunction();
			}
		}
	}

	private void Update()
	{
		if (HasAuthority)
		{
			AuthorityUpdate();
		}
		SharedUpdate();
	}

	private void AuthorityUpdate()
	{
		float deltaTime = Time.deltaTime;
		if (greyZoneActive)
		{
			summoningProgress = 1f;
			double num = ((photonConnectedDuringActivation && PhotonNetwork.InRoom) ? PhotonNetwork.Time : ((photonConnectedDuringActivation || PhotonNetwork.InRoom) ? (-100.0) : ((double)Time.time)));
			if (num > greyZoneActivationTime + (double)greyZoneActiveDuration || num < greyZoneActivationTime - 10.0)
			{
				DeactivateGreyZoneAuthority();
			}
		}
		else
		{
			if (!GreyZoneAvailable)
			{
				return;
			}
			roomPlayerList = PhotonNetwork.PlayerList;
			int num2 = 1;
			if (roomPlayerList != null && roomPlayerList.Length != 0)
			{
				num2 = Mathf.Max((roomPlayerList.Length + 1) / 2, 1);
			}
			float num3 = 0f;
			float num4 = 1f / summoningActivationTime;
			foreach (KeyValuePair<int, (VRRig, GreyZoneSummoner)> summoningPlayer in summoningPlayers)
			{
				VRRig item = summoningPlayer.Value.Item1;
				GreyZoneSummoner item2 = summoningPlayer.Value.Item2;
				float current2 = summoningPlayerProgress[summoningPlayer.Key];
				Vector3 lhs = item2.SummoningFocusPoint - item.leftHand.rigTarget.position;
				Vector3 rhs = -item.leftHand.rigTarget.right;
				bool flag = Vector3.Dot(lhs, rhs) > 0f;
				Vector3 lhs2 = item2.SummoningFocusPoint - item.rightHand.rigTarget.position;
				Vector3 right = item.rightHand.rigTarget.right;
				bool flag2 = Vector3.Dot(lhs2, right) > 0f;
				current2 = ((!(flag && flag2)) ? Mathf.MoveTowards(current2, 0f, num4 * deltaTime) : Mathf.MoveTowards(current2, 1f, num4 * deltaTime));
				num3 += current2;
				summoningPlayerProgress[summoningPlayer.Key] = current2;
			}
			float num5 = 0.95f;
			summoningProgress = Mathf.Clamp01(num3 / num5 / (float)num2);
			UpdateSummonerVisuals();
			if (summoningProgress > 0.99f)
			{
				ActivateGreyZoneAuthority();
			}
		}
	}

	private void SharedUpdate()
	{
		GTPlayer instance = GTPlayer.Instance;
		if (greyZoneActive)
		{
			Vector3 vector = Vector3.ClampMagnitude(instance.InstantaneousVelocity * particlePredictiveSpawnVelocityFactor, particlePredictiveSpawnMaxDist);
			greyZoneParticles.transform.position = instance.HeadCenterPosition + Vector3.down * 0.5f + vector;
		}
		else if (gravityOverrideSet && gravityReductionAmount < 0.01f)
		{
			instance.UnsetGravityOverride(this);
			gravityOverrideSet = false;
		}
		float num = (greyZoneActive ? 0f : 1f);
		float smoothTime = (greyZoneActive ? skyMonsterMovementEnterTime : skyMonsterMovementExitTime);
		if (moonController != null && moonController.Distance != num)
		{
			float num2 = Mathf.SmoothDamp(moonController.Distance, num, ref skyMonsterMovementVelocity, smoothTime);
			if ((double)Mathf.Abs(num2 - num) < 0.001)
			{
				num2 = num;
			}
			moonController.UpdateDistance(num2);
		}
	}

	public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
	{
		if (stream.IsWriting)
		{
			stream.SendNext(greyZoneActive);
			stream.SendNext(greyZoneActivationTime);
			stream.SendNext(photonConnectedDuringActivation);
			stream.SendNext(gravityFactorOptionSelection);
			stream.SendNext(summoningProgress);
		}
		else if (stream.IsReading && info.Sender.IsMasterClient)
		{
			bool flag = greyZoneActive;
			greyZoneActive = (bool)stream.ReceiveNext();
			greyZoneActivationTime = ((double)stream.ReceiveNext()).GetFinite();
			photonConnectedDuringActivation = (bool)stream.ReceiveNext();
			gravityFactorOptionSelection = (int)stream.ReceiveNext();
			summoningProgress = ((float)stream.ReceiveNext()).ClampSafe(0f, 1f);
			UpdateSummonerVisuals();
			if (greyZoneActive && !flag)
			{
				ActivateGreyZoneLocal();
			}
			else if (!greyZoneActive && flag)
			{
				DeactivateGreyZoneLocal();
			}
		}
	}

	public void OnPlayerEnteredRoom(Player newPlayer)
	{
	}

	public void OnPlayerLeftRoom(Player otherPlayer)
	{
		ValidateSummoningPlayers();
	}

	public void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
	{
	}

	public void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
	{
	}

	public void OnMasterClientSwitched(Player newMasterClient)
	{
		ValidateSummoningPlayers();
	}
}
