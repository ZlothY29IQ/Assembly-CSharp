using System;
using System.Collections.Generic;
using GorillaExtensions;
using GorillaGameModes;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class GorillaGuardianZoneManager : MonoBehaviourPunCallbacks, IPunObservable, IGorillaSliceableSimple
{
	public static List<GorillaGuardianZoneManager> zoneManagers = new List<GorillaGuardianZoneManager>();

	[SerializeField]
	private GTZone zone;

	[SerializeField]
	private SizeChanger guardianSizeChanger;

	[SerializeField]
	private TappableGuardianIdol idol;

	[SerializeField]
	private List<Transform> idolPositions;

	[Space]
	[SerializeField]
	private float requiredActivationTime = 10f;

	[SerializeField]
	private float activationTimePerTap = 1f;

	[Space]
	[SerializeField]
	private bool knockbackIncludesGuardian = true;

	[SerializeField]
	private float idolKnockbackRadius = 6f;

	[SerializeField]
	private float idolKnockbackStrengthVert = 12f;

	[SerializeField]
	private float idolKnockbackStrengthHoriz = 15f;

	[Space]
	[SerializeField]
	private SoundBankPlayer PlayerGainGuardianSFX;

	[SerializeField]
	private SoundBankPlayer PlayerLostGuardianSFX;

	[SerializeField]
	private SoundBankPlayer ObserverGainGuardianSFX;

	private NetPlayer guardianPlayer;

	private NetPlayer _previousGuardian;

	private int currentIdol = -1;

	private int idolMoveCount;

	private List<Transform> _sortedIdolPositions = new List<Transform>();

	private float _currentActivationTime = -1f;

	private float _lastTappedTime;

	private bool _progressing;

	private float _idolActivationDisplay;

	private bool _zoneIsActive;

	private bool _zoneStateChanged;

	public NetPlayer CurrentGuardian => guardianPlayer;

	public void Awake()
	{
		zoneManagers.Add(this);
		idol.gameObject.SetActive(value: false);
		foreach (Transform idolPosition in idolPositions)
		{
			idolPosition.gameObject.SetActive(value: false);
		}
		if (GameMode.ActiveGameMode is GorillaGuardianManager { isPlaying: not false } && PhotonNetwork.IsMasterClient)
		{
			StartPlaying();
		}
	}

	private void Start()
	{
		ZoneManagement instance = ZoneManagement.instance;
		instance.onZoneChanged = (Action)Delegate.Combine(instance.onZoneChanged, new Action(OnZoneChanged));
	}

	public void OnDestroy()
	{
		ZoneManagement instance = ZoneManagement.instance;
		instance.onZoneChanged = (Action)Delegate.Remove(instance.onZoneChanged, new Action(OnZoneChanged));
		zoneManagers.Remove(this);
	}

	public override void OnEnable()
	{
		base.OnEnable();
		GorillaSlicerSimpleManager.RegisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.Update);
	}

	public override void OnDisable()
	{
		base.OnDisable();
		GorillaSlicerSimpleManager.UnregisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.Update);
	}

	public void SliceUpdate()
	{
		float idolActivationDisplay = _idolActivationDisplay;
		float num = 0f;
		if (_currentActivationTime < 0f)
		{
			_idolActivationDisplay = 0f;
			_progressing = false;
		}
		else
		{
			num = Mathf.Min(Time.time - _lastTappedTime, activationTimePerTap);
			_progressing = num < activationTimePerTap;
			_idolActivationDisplay = (_currentActivationTime + num) / requiredActivationTime;
		}
		if (idolActivationDisplay != _idolActivationDisplay)
		{
			idol.UpdateActivationProgress(_currentActivationTime + num, _progressing);
		}
	}

	public override void OnLeftRoom()
	{
		base.OnLeftRoom();
		StopPlaying();
	}

	public override void OnPlayerLeftRoom(Player otherPlayer)
	{
		if (guardianPlayer == null || guardianPlayer.GetPlayerRef() == otherPlayer)
		{
			SetGuardian(null);
		}
		if (_previousGuardian?.GetPlayerRef() == otherPlayer)
		{
			_previousGuardian = null;
		}
	}

	private void OnZoneChanged()
	{
		bool flag = ZoneManagement.IsInZone(zone);
		if (flag != _zoneIsActive || !_zoneStateChanged)
		{
			_zoneIsActive = flag;
			idol.OnZoneActiveStateChanged(_zoneIsActive);
			_zoneStateChanged = true;
		}
		if (_zoneIsActive && GameMode.ActiveGameMode is GorillaGuardianManager { isPlaying: not false } gorillaGuardianManager && gorillaGuardianManager.IsPlayerGuardian(NetworkSystem.Instance.LocalPlayer) && guardianPlayer != null && guardianPlayer != NetworkSystem.Instance.LocalPlayer)
		{
			gorillaGuardianManager.RequestEjectGuardian(NetworkSystem.Instance.LocalPlayer);
		}
	}

	public void StartPlaying()
	{
		if (IsZoneValid())
		{
			_currentActivationTime = -1f;
			if (guardianPlayer != null && !guardianPlayer.InRoom())
			{
				SetGuardian(null);
				_previousGuardian = null;
			}
			idol.gameObject.SetActive(value: true);
			SelectNextIdol();
			SetIdolPosition(currentIdol);
		}
	}

	public void StopPlaying()
	{
		_currentActivationTime = -1f;
		currentIdol = -1;
		idol.gameObject.SetActive(value: false);
		_progressing = false;
		_lastTappedTime = 0f;
		SetGuardian(null);
		_previousGuardian = null;
	}

	public void SetScaleCenterPoint(Transform scaleCenterPoint)
	{
		guardianSizeChanger.SetScaleCenterPoint(scaleCenterPoint);
	}

	public void IdolWasTapped(NetPlayer tapper)
	{
		if ((tapper == null || (GameMode.ParticipatingPlayers.Contains(tapper) && tapper != guardianPlayer)) && IsZoneValid() && UpdateTapCount(tapper))
		{
			IdolActivated(tapper);
		}
	}

	public bool IsZoneValid()
	{
		if (!NetworkSystem.Instance.SessionIsPrivate)
		{
			return ZoneManagement.IsInZone(zone);
		}
		return true;
	}

	private bool UpdateTapCount(NetPlayer tapper)
	{
		if (guardianPlayer == null && _previousGuardian == null)
		{
			return true;
		}
		if (_currentActivationTime < 0f)
		{
			_currentActivationTime = 0f;
			_lastTappedTime = Time.time;
		}
		if (!_progressing)
		{
			float num = Mathf.Min(Time.time - _lastTappedTime, activationTimePerTap);
			_lastTappedTime = Time.time;
			if (num + _currentActivationTime >= requiredActivationTime)
			{
				return true;
			}
			_currentActivationTime += num;
		}
		return false;
	}

	private void IdolActivated(NetPlayer activater)
	{
		_currentActivationTime = -1f;
		SetGuardian(activater);
		SelectNextIdol();
		MoveIdolPosition(currentIdol);
	}

	public void SetGuardian(NetPlayer newGuardian)
	{
		if (guardianPlayer == newGuardian)
		{
			return;
		}
		if (guardianPlayer != null)
		{
			if (NetworkSystem.Instance.LocalPlayer == guardianPlayer)
			{
				PlayerLostGuardianSFX.Play();
			}
			if (VRRigCache.Instance.TryGetVrrig(guardianPlayer, out var playerRig))
			{
				playerRig.Rig.EnableGuardianEjectWatch(on: false);
				guardianSizeChanger.unacceptRig(playerRig.Rig);
				int num = (RoomSystem.JoinedRoom ? playerRig.netView.ViewID : playerRig.CachedNetViewID);
				if (GorillaTagger.Instance.offlineVRRig.grabbedRopeIndex == num)
				{
					GorillaTagger.Instance.offlineVRRig.DroppedByPlayer(playerRig.Rig, Vector3.zero);
					if (guardianPlayer == NetworkSystem.Instance.LocalPlayer)
					{
						bool forLeftHand = GorillaTagger.Instance.offlineVRRig.grabbedRopeBoneIndex == 1;
						EquipmentInteractor.instance.UpdateHandEquipment(null, forLeftHand);
					}
				}
			}
		}
		_previousGuardian = guardianPlayer;
		guardianPlayer = newGuardian;
		if (guardianPlayer != null)
		{
			if (NetworkSystem.Instance.LocalPlayer == guardianPlayer)
			{
				PlayerGainGuardianSFX.Play();
			}
			else
			{
				ObserverGainGuardianSFX.Play();
			}
			if (VRRigCache.Instance.TryGetVrrig(guardianPlayer, out var playerRig2))
			{
				playerRig2.Rig.EnableGuardianEjectWatch(on: true);
				guardianSizeChanger.acceptRig(playerRig2.Rig);
			}
			PlayerGameEvents.GameModeCompleteRound();
			if (NetworkSystem.Instance.LocalPlayer == guardianPlayer)
			{
				PlayerGameEvents.GameModeObjectiveTriggered();
			}
		}
	}

	public bool IsPlayerGuardian(NetPlayer player)
	{
		return player == guardianPlayer;
	}

	private int SelectNextIdol()
	{
		if (idolPositions == null || idolPositions.Count == 0)
		{
			GTDev.Log("No Guardian Idols possible to select.");
			return -1;
		}
		currentIdol = SelectRandomIdol();
		return currentIdol;
	}

	private int SelectRandomIdol()
	{
		if (currentIdol != -1 && idolPositions.Count > 1)
		{
			return (currentIdol + UnityEngine.Random.Range(1, idolPositions.Count)) % idolPositions.Count;
		}
		return UnityEngine.Random.Range(0, idolPositions.Count);
	}

	private int SelectFarthestFromGuardian()
	{
		if (!(GorillaGameManager.instance is GorillaGuardianManager))
		{
			return SelectRandomIdol();
		}
		if (guardianPlayer != null && VRRigCache.Instance.TryGetVrrig(guardianPlayer, out var playerRig))
		{
			Vector3 position = playerRig.transform.position;
			int num = -1;
			float num2 = 0f;
			for (int i = 0; i < idolPositions.Count; i++)
			{
				float num3 = Vector3.SqrMagnitude(idolPositions[i].transform.position - position);
				if (num3 > num2)
				{
					num2 = num3;
					num = i;
				}
			}
			if (num != -1)
			{
				return num;
			}
		}
		return SelectRandomIdol();
	}

	private int SelectFarFromNearestPlayer()
	{
		List<Transform> list = SortByDistanceToNearestPlayer();
		if (list.Count > 1 && currentIdol >= 0 && currentIdol < list.Count)
		{
			list.Remove(idolPositions[currentIdol]);
		}
		int index = UnityEngine.Random.Range(list.Count / 2, list.Count);
		Transform item = list[index];
		return idolPositions.IndexOf(item);
	}

	private List<Transform> SortByDistanceToNearestPlayer()
	{
		List<Vector3> playerPositions = new List<Vector3>();
		foreach (RigContainer activeRigContainer in VRRigCache.ActiveRigContainers)
		{
			if (!activeRigContainer.IsNull())
			{
				playerPositions.Add(activeRigContainer.transform.position);
			}
		}
		_sortedIdolPositions.Clear();
		foreach (Transform idolPosition in idolPositions)
		{
			_sortedIdolPositions.Add(idolPosition);
		}
		_sortedIdolPositions.Sort(CompareNearestPlayerDistance);
		return _sortedIdolPositions;
		int CompareNearestPlayerDistance(Transform idol1, Transform idol2)
		{
			float num = GetClosestPlayerSqrDistance(idol1.position);
			float value = GetClosestPlayerSqrDistance(idol2.position);
			return num.CompareTo(value);
		}
		float GetClosestPlayerSqrDistance(Vector3 idolPosition)
		{
			float num = float.PositiveInfinity;
			foreach (Vector3 item in playerPositions)
			{
				float num2 = Vector3.SqrMagnitude(idolPosition - item);
				if (num2 < num)
				{
					num = num2;
				}
			}
			return num;
		}
	}

	public void TriggerIdolKnockback()
	{
		if (!PhotonNetwork.IsMasterClient)
		{
			return;
		}
		for (int i = 0; i < RoomSystem.PlayersInRoom.Count; i++)
		{
			if ((knockbackIncludesGuardian || RoomSystem.PlayersInRoom[i] != guardianPlayer) && VRRigCache.Instance.TryGetVrrig(RoomSystem.PlayersInRoom[i], out var playerRig))
			{
				Vector3 vector = playerRig.Rig.transform.position - idol.transform.position;
				if (Vector3.SqrMagnitude(vector) < idolKnockbackRadius * idolKnockbackRadius)
				{
					Vector3 velocity = (vector - Vector3.up * Vector3.Dot(Vector3.up, vector)).normalized * idolKnockbackStrengthHoriz + Vector3.up * idolKnockbackStrengthVert;
					RoomSystem.LaunchPlayer(RoomSystem.PlayersInRoom[i], velocity);
				}
			}
		}
	}

	private void SetIdolPosition(int index)
	{
		if (index < 0 || index >= idolPositions.Count)
		{
			GTDev.Log("Invalid index received");
			return;
		}
		idol.gameObject.SetActive(value: true);
		idol.SetPosition(idolPositions[index].position);
	}

	private void MoveIdolPosition(int index)
	{
		if (index < 0 || index >= idolPositions.Count)
		{
			GTDev.Log("Invalid index received");
			return;
		}
		idol.gameObject.SetActive(value: true);
		idol.MovePositions(idolPositions[index].position);
		if (base.photonView.IsMine)
		{
			idolMoveCount++;
		}
	}

	public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
	{
		NetPlayer player = NetworkSystem.Instance.GetPlayer(info.Sender);
		if (!(GameMode.ActiveGameMode is GorillaGuardianManager { isPlaying: not false }) || player != NetworkSystem.Instance.MasterClient)
		{
			return;
		}
		if (stream.IsWriting)
		{
			stream.SendNext((guardianPlayer != null) ? guardianPlayer.ActorNumber : 0);
			stream.SendNext(_currentActivationTime);
			stream.SendNext(currentIdol);
			stream.SendNext(idolMoveCount);
			return;
		}
		int num = (int)stream.ReceiveNext();
		float num2 = (float)stream.ReceiveNext();
		int num3 = (int)stream.ReceiveNext();
		int num4 = (int)stream.ReceiveNext();
		if (float.IsNaN(num2) || float.IsInfinity(num2))
		{
			return;
		}
		SetGuardian((num != 0) ? NetworkSystem.Instance.GetPlayer(num) : null);
		if (num2 != _currentActivationTime)
		{
			_currentActivationTime = num2;
			_lastTappedTime = Time.time;
		}
		if (num3 != currentIdol || num4 != idolMoveCount)
		{
			if (currentIdol == -1)
			{
				SetIdolPosition(num3);
			}
			else
			{
				MoveIdolPosition(num3);
			}
			currentIdol = num3;
			idolMoveCount = num4;
		}
	}
}
