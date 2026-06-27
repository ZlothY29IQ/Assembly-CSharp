using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using GorillaGameModes;
using GorillaNetworking;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public sealed class GorillaPaintbrawlManager : GorillaGameManager
{
	public enum PaintbrawlStatus
	{
		RedTeam = 1,
		BlueTeam = 2,
		Normal = 4,
		Hit = 8,
		Stunned = 16,
		Grace = 32,
		Eliminated = 64,
		None = 0
	}

	public enum PaintbrawlState
	{
		NotEnoughPlayers,
		GameEnd,
		GameEndWaiting,
		StartCountdown,
		CountingDownToStart,
		GameStart,
		GameRunning
	}

	private float playerMin = 2f;

	public float tagCoolDown = 5f;

	public Dictionary<int, int> playerLives = new Dictionary<int, int>();

	public Dictionary<int, PaintbrawlStatus> playerStatusDict = new Dictionary<int, PaintbrawlStatus>();

	public Dictionary<int, float> playerHitTimes = new Dictionary<int, float>();

	public Dictionary<int, float> playerStunTimes = new Dictionary<int, float>();

	public int[] playerActorNumberArray = new int[10] { -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 };

	public int[] playerLivesArray = new int[10];

	public PaintbrawlStatus[] playerStatusArray = new PaintbrawlStatus[10];

	public bool teamBattle = true;

	public int countDownTime;

	private float timeBattleEnded;

	public float hitCooldown = 3f;

	public float stunGracePeriod = 2f;

	public object objRef;

	private bool playerInList;

	private bool coroutineRunning;

	private int lives;

	private int outLives;

	private int bcount;

	private int rcount;

	private int randInt;

	private float outHitTime;

	private NetworkView tempView;

	private int[] reusableKeyBuffer = new int[20];

	private PaintbrawlStatus tempStatus;

	private PaintbrawlState currentState;

	private bool _isDefaultSlingshotSynced;

	private readonly HashSet<VRRig> _slingshotPreloadedRigs = new HashSet<VRRig>(20);

	private void ActivatePaintbrawlBalloons(bool enable)
	{
		if (GorillaTagger.Instance.offlineVRRig != null)
		{
			GorillaTagger.Instance.offlineVRRig.paintbrawlBalloons.gameObject.SetActive(enable);
		}
	}

	private bool HasFlag(PaintbrawlStatus state, PaintbrawlStatus statusFlag)
	{
		return (state & statusFlag) != 0;
	}

	public override GameModeType GameType()
	{
		return GameModeType.Paintbrawl;
	}

	public override void AddFusionDataBehaviour(NetworkObject behaviour)
	{
		behaviour.AddBehaviour<BattleGameModeData>();
	}

	public override string GameModeName()
	{
		return "PAINTBRAWL";
	}

	public override string GameModeNameRoomLabel()
	{
		if (!LocalisationManager.TryGetKeyForCurrentLocale("GAME_MODE_PAINTBRAWL_ROOM_LABEL", out var result, "(PAINTBRAWL GAME)"))
		{
			Debug.LogError("[LOCALIZATION::GORILLA_GAME_MANAGER] Failed to get key for Game Mode [GAME_MODE_PAINTBRAWL_ROOM_LABEL]");
		}
		return result;
	}

	private void ActivateDefaultSlingShot()
	{
		if (_isDefaultSlingshotSynced && !Slingshot.IsSlingShotEnabled())
		{
			_isDefaultSlingshotSynced = false;
		}
		if (!_isDefaultSlingshotSynced)
		{
			VRRig offlineVRRig = GorillaTagger.Instance.offlineVRRig;
			bool flag = Slingshot.IsSlingShotEnabled();
			if (offlineVRRig != null && !flag)
			{
				CosmeticsController cosmeticsController = CosmeticsController.instance;
				CosmeticsController.CosmeticItem itemFromDict = cosmeticsController.GetItemFromDict("Slingshot");
				cosmeticsController.currentWornSet.HasItemOfCategory(CosmeticsController.CosmeticCategory.Chest);
				cosmeticsController.currentWornSet.HasItem("Slingshot");
				cosmeticsController.ApplyCosmeticItemToSet(cosmeticsController.currentWornSet, itemFromDict, isLeftHand: true, applyToPlayerPrefs: false);
				cosmeticsController.UpdateWornCosmetics(sync: true);
				bool isDefaultSlingshotSynced = cosmeticsController.currentWornSet.HasItemOfCategory(CosmeticsController.CosmeticCategory.Chest);
				cosmeticsController.currentWornSet.HasItem("Slingshot");
				_isDefaultSlingshotSynced = isDefaultSlingshotSynced;
			}
		}
	}

	private void PreloadSlingshotForActiveRigs(string caller)
	{
		int count = CosmeticsV2Spawner_Dirty._gVRRigDatas.Count;
		int num = 0;
		for (int i = 0; i < count; i++)
		{
			CosmeticsV2Spawner_Dirty.VRRigData vRRigData = CosmeticsV2Spawner_Dirty._gVRRigDatas[i];
			if (!(vRRigData.vrRig == null) && !_slingshotPreloadedRigs.Contains(vRRigData.vrRig))
			{
				CosmeticItemRegistry cosmeticsObjectRegistry = vRRigData.vrRig.cosmeticsObjectRegistry;
				if (cosmeticsObjectRegistry != null)
				{
					CosmeticsV2Spawner_Dirty.ProcessLoadOpInfos(vRRigData.vrRig, "Slingshot", cosmeticsObjectRegistry);
					_slingshotPreloadedRigs.Add(vRRigData.vrRig);
					num++;
				}
			}
		}
	}

	public override void Awake()
	{
		base.Awake();
		coroutineRunning = false;
		currentState = PaintbrawlState.NotEnoughPlayers;
	}

	public override void StartPlaying()
	{
		base.StartPlaying();
		_isDefaultSlingshotSynced = false;
		_slingshotPreloadedRigs.Clear();
		PreloadSlingshotForActiveRigs("StartPlaying");
		ActivatePaintbrawlBalloons(enable: true);
		VerifyPlayersInDict(playerLives);
		VerifyPlayersInDict(playerStatusDict);
		VerifyPlayersInDict(playerHitTimes);
		VerifyPlayersInDict(playerStunTimes);
		CopyBattleDictToArray();
		UpdateBattleState();
	}

	public override void StopPlaying()
	{
		base.StopPlaying();
		_isDefaultSlingshotSynced = false;
		PlayerPrefs.GetString("slot_Chest", "NOTHING");
		if (Slingshot.IsSlingShotEnabled())
		{
			CosmeticsController cosmeticsController = CosmeticsController.instance;
			CosmeticsController.CosmeticItem itemFromDict = cosmeticsController.GetItemFromDict("Slingshot");
			if (cosmeticsController.currentWornSet.HasItem("Slingshot"))
			{
				cosmeticsController.ApplyCosmeticItemToSet(cosmeticsController.currentWornSet, itemFromDict, isLeftHand: true, applyToPlayerPrefs: false);
				cosmeticsController.UpdateWornCosmetics(sync: true);
				cosmeticsController.currentWornSet.HasItemOfCategory(CosmeticsController.CosmeticCategory.Chest);
				PlayerPrefs.GetString("slot_Chest", "NOTHING");
			}
		}
		ActivatePaintbrawlBalloons(enable: false);
		StopAllCoroutines();
		coroutineRunning = false;
	}

	public override void ResetGame()
	{
		base.ResetGame();
		playerLives.Clear();
		playerStatusDict.Clear();
		playerHitTimes.Clear();
		playerStunTimes.Clear();
		for (int i = 0; i < playerActorNumberArray.Length; i++)
		{
			playerLivesArray[i] = 0;
			playerActorNumberArray[i] = -1;
			playerStatusArray[i] = PaintbrawlStatus.None;
		}
		currentState = PaintbrawlState.NotEnoughPlayers;
	}

	private int CopyDictKeysToBuffer<T>(Dictionary<int, T> dict)
	{
		int num = 0;
		foreach (KeyValuePair<int, T> item in dict)
		{
			if (num >= reusableKeyBuffer.Length)
			{
				break;
			}
			reusableKeyBuffer[num++] = item.Key;
		}
		return num;
	}

	private void VerifyPlayersInDict<T>(Dictionary<int, T> dict)
	{
		if (dict.Count < 1)
		{
			return;
		}
		int num = CopyDictKeysToBuffer(dict);
		for (int i = 0; i < num; i++)
		{
			if (!Utils.PlayerInRoom(reusableKeyBuffer[i]))
			{
				dict.Remove(reusableKeyBuffer[i]);
			}
		}
	}

	internal override void NetworkLinkSetup(GameModeSerializer netSerializer)
	{
		base.NetworkLinkSetup(netSerializer);
		netSerializer.AddRPCComponent<PaintbrawlRPCs>();
	}

	private void Transition(PaintbrawlState newState)
	{
		currentState = newState;
		Debug.Log("current state is: " + currentState);
	}

	public void UpdateBattleState()
	{
		if (!NetworkSystem.Instance.IsMasterClient)
		{
			return;
		}
		switch (currentState)
		{
		case PaintbrawlState.NotEnoughPlayers:
			if ((float)RoomSystem.PlayersInRoom.Count >= playerMin)
			{
				Transition(PaintbrawlState.StartCountdown);
			}
			break;
		case PaintbrawlState.GameRunning:
			if (CheckForGameEnd())
			{
				Transition(PaintbrawlState.GameEnd);
				PlayerGameEvents.GameModeCompleteRound();
				GorillaGameModes.GameMode.BroadcastRoundComplete();
			}
			if ((float)RoomSystem.PlayersInRoom.Count < playerMin)
			{
				InitializePlayerStatus();
				Transition(PaintbrawlState.NotEnoughPlayers);
			}
			break;
		case PaintbrawlState.GameEnd:
			if (EndBattleGame())
			{
				Transition(PaintbrawlState.GameEndWaiting);
			}
			break;
		case PaintbrawlState.GameEndWaiting:
			if (BattleEnd())
			{
				Transition(PaintbrawlState.StartCountdown);
			}
			break;
		case PaintbrawlState.StartCountdown:
			if (teamBattle)
			{
				RandomizeTeams();
			}
			StartCoroutine(StartBattleCountdown());
			Transition(PaintbrawlState.CountingDownToStart);
			break;
		case PaintbrawlState.CountingDownToStart:
			if (!coroutineRunning)
			{
				Transition(PaintbrawlState.StartCountdown);
			}
			break;
		case PaintbrawlState.GameStart:
			StartBattle();
			Transition(PaintbrawlState.GameRunning);
			break;
		}
		UpdatePlayerStatus();
	}

	private bool CheckForGameEnd()
	{
		int num = 0;
		bcount = 0;
		rcount = 0;
		foreach (NetPlayer item in RoomSystem.PlayersInRoom)
		{
			if (playerLives.TryGetValue(item.ActorNumber, out lives))
			{
				if (lives <= 0)
				{
					continue;
				}
				num++;
				if (teamBattle && playerStatusDict.TryGetValue(item.ActorNumber, out tempStatus))
				{
					if (HasFlag(tempStatus, PaintbrawlStatus.RedTeam))
					{
						rcount++;
					}
					else if (HasFlag(tempStatus, PaintbrawlStatus.BlueTeam))
					{
						bcount++;
					}
				}
			}
			else
			{
				playerLives.Add(item.ActorNumber, 0);
			}
		}
		if (teamBattle && (bcount == 0 || rcount == 0))
		{
			return true;
		}
		if (!teamBattle && num <= 1)
		{
			return true;
		}
		return false;
	}

	public IEnumerator StartBattleCountdown()
	{
		coroutineRunning = true;
		for (countDownTime = 5; countDownTime > 0; countDownTime--)
		{
			try
			{
				RoomSystem.SendSoundEffectAll(6, 0.25f);
				foreach (NetPlayer item in RoomSystem.PlayersInRoom)
				{
					playerLives[item.ActorNumber] = 3;
				}
			}
			catch
			{
			}
			yield return new WaitForSeconds(1f);
		}
		coroutineRunning = false;
		currentState = PaintbrawlState.GameStart;
		yield return null;
	}

	public void StartBattle()
	{
		RoomSystem.SendSoundEffectAll(7, 0.5f);
		foreach (NetPlayer item in RoomSystem.PlayersInRoom)
		{
			playerLives[item.ActorNumber] = 3;
		}
	}

	private bool EndBattleGame()
	{
		if ((float)RoomSystem.PlayersInRoom.Count >= playerMin)
		{
			RoomSystem.SendStatusEffectAll(RoomSystem.StatusEffects.TaggedTime);
			RoomSystem.SendSoundEffectAll(2, 0.25f);
			timeBattleEnded = Time.time;
			return true;
		}
		return false;
	}

	public bool BattleEnd()
	{
		return Time.time > timeBattleEnded + tagCoolDown;
	}

	public bool SlingshotHit(NetPlayer myPlayer, Player otherPlayer)
	{
		if (playerLives.TryGetValue(otherPlayer.ActorNumber, out lives))
		{
			return lives > 0;
		}
		return false;
	}

	public void ReportSlingshotHit(NetPlayer taggedPlayer, Vector3 hitLocation, int projectileCount, PhotonMessageInfoWrapped info)
	{
		NetPlayer player = NetworkSystem.Instance.GetPlayer(info.senderID);
		if (!NetworkSystem.Instance.IsMasterClient || currentState != PaintbrawlState.GameRunning || OnSameTeam(taggedPlayer, player))
		{
			return;
		}
		if (GetPlayerLives(taggedPlayer) > 0 && GetPlayerLives(player) > 0 && !PlayerInHitCooldown(taggedPlayer))
		{
			if (!playerHitTimes.TryGetValue(taggedPlayer.ActorNumber, out outHitTime))
			{
				playerHitTimes.Add(taggedPlayer.ActorNumber, Time.time);
			}
			else
			{
				playerHitTimes[taggedPlayer.ActorNumber] = Time.time;
			}
			playerLives[taggedPlayer.ActorNumber]--;
			RoomSystem.SendSoundEffectOnOther(0, 0.25f, taggedPlayer);
		}
		else
		{
			if (GetPlayerLives(player) != 0 || GetPlayerLives(taggedPlayer) <= 0)
			{
				return;
			}
			tempStatus = GetPlayerStatus(taggedPlayer);
			if (HasFlag(tempStatus, PaintbrawlStatus.Normal) && !PlayerInHitCooldown(taggedPlayer) && !PlayerInStunCooldown(taggedPlayer))
			{
				if (!playerStunTimes.TryGetValue(taggedPlayer.ActorNumber, out outHitTime))
				{
					playerStunTimes.Add(taggedPlayer.ActorNumber, Time.time);
				}
				else
				{
					playerStunTimes[taggedPlayer.ActorNumber] = Time.time;
				}
				RoomSystem.SendStatusEffectToPlayer(RoomSystem.StatusEffects.SetSlowedTime, taggedPlayer);
				RoomSystem.SendSoundEffectOnOther(5, 0.125f, taggedPlayer);
				if (VRRigCache.Instance.TryGetVrrig(taggedPlayer, out var playerRig))
				{
					tempView = playerRig.Rig.netView;
				}
			}
		}
	}

	public override void HitPlayer(NetPlayer player)
	{
		if (NetworkSystem.Instance.IsMasterClient && currentState == PaintbrawlState.GameRunning && GetPlayerLives(player) > 0)
		{
			playerLives[player.ActorNumber] = 0;
			RoomSystem.SendSoundEffectOnOther(0, 0.25f, player);
		}
	}

	public override bool CanAffectPlayer(NetPlayer player, bool thisFrame)
	{
		if (playerLives.TryGetValue(player.ActorNumber, out lives))
		{
			return lives > 0;
		}
		return false;
	}

	public override void OnPlayerEnteredRoom(NetPlayer newPlayer)
	{
		base.OnPlayerEnteredRoom(newPlayer);
		if (NetworkSystem.Instance.IsMasterClient)
		{
			if (currentState == PaintbrawlState.GameRunning)
			{
				playerLives.Add(newPlayer.ActorNumber, 0);
			}
			else
			{
				playerLives.Add(newPlayer.ActorNumber, 3);
			}
			playerStatusDict.Add(newPlayer.ActorNumber, PaintbrawlStatus.None);
			CopyBattleDictToArray();
			if (teamBattle)
			{
				AddPlayerToCorrectTeam(newPlayer);
			}
		}
	}

	public override void OnPlayerLeftRoom(NetPlayer otherPlayer)
	{
		base.OnPlayerLeftRoom(otherPlayer);
		if (playerLives.ContainsKey(otherPlayer.ActorNumber))
		{
			playerLives.Remove(otherPlayer.ActorNumber);
		}
		if (playerStatusDict.ContainsKey(otherPlayer.ActorNumber))
		{
			playerStatusDict.Remove(otherPlayer.ActorNumber);
		}
	}

	public override void OnSerializeRead(object newData)
	{
		PaintbrawlData paintbrawlData = (PaintbrawlData)newData;
		paintbrawlData.playerActorNumberArray.CopyTo(playerActorNumberArray);
		paintbrawlData.playerLivesArray.CopyTo(playerLivesArray);
		paintbrawlData.playerStatusArray.CopyTo(playerStatusArray);
		currentState = paintbrawlData.currentPaintbrawlState;
		CopyArrayToBattleDict();
	}

	public override object OnSerializeWrite()
	{
		CopyBattleDictToArray();
		PaintbrawlData paintbrawlData = default(PaintbrawlData);
		paintbrawlData.playerActorNumberArray.CopyFrom(playerActorNumberArray, 0, playerActorNumberArray.Length);
		paintbrawlData.playerLivesArray.CopyFrom(playerLivesArray, 0, playerLivesArray.Length);
		paintbrawlData.playerStatusArray.CopyFrom(playerStatusArray, 0, playerStatusArray.Length);
		paintbrawlData.currentPaintbrawlState = currentState;
		return paintbrawlData;
	}

	public override void OnSerializeWrite(PhotonStream stream, PhotonMessageInfo info)
	{
		CopyBattleDictToArray();
		for (int i = 0; i < playerLivesArray.Length; i++)
		{
			stream.SendNext(playerActorNumberArray[i]);
			stream.SendNext(playerLivesArray[i]);
			stream.SendNext(playerStatusArray[i]);
		}
		stream.SendNext((int)currentState);
	}

	public override void OnSerializeRead(PhotonStream stream, PhotonMessageInfo info)
	{
		NetworkSystem.Instance.GetPlayer(info.Sender);
		for (int i = 0; i < playerLivesArray.Length; i++)
		{
			playerActorNumberArray[i] = (int)stream.ReceiveNext();
			playerLivesArray[i] = (int)stream.ReceiveNext();
			playerStatusArray[i] = (PaintbrawlStatus)stream.ReceiveNext();
		}
		currentState = (PaintbrawlState)stream.ReceiveNext();
		CopyArrayToBattleDict();
	}

	public override int MyMatIndex(NetPlayer forPlayer)
	{
		tempStatus = GetPlayerStatus(forPlayer);
		if (tempStatus != PaintbrawlStatus.None)
		{
			if (OnRedTeam(tempStatus))
			{
				if (HasFlag(tempStatus, PaintbrawlStatus.Normal))
				{
					return 8;
				}
				if (HasFlag(tempStatus, PaintbrawlStatus.Hit))
				{
					return 9;
				}
				if (HasFlag(tempStatus, PaintbrawlStatus.Stunned))
				{
					return 10;
				}
				if (HasFlag(tempStatus, PaintbrawlStatus.Grace))
				{
					return 10;
				}
				if (HasFlag(tempStatus, PaintbrawlStatus.Eliminated))
				{
					return 11;
				}
			}
			else if (OnBlueTeam(tempStatus))
			{
				if (HasFlag(tempStatus, PaintbrawlStatus.Normal))
				{
					return 4;
				}
				if (HasFlag(tempStatus, PaintbrawlStatus.Hit))
				{
					return 5;
				}
				if (HasFlag(tempStatus, PaintbrawlStatus.Stunned))
				{
					return 6;
				}
				if (HasFlag(tempStatus, PaintbrawlStatus.Grace))
				{
					return 6;
				}
				if (HasFlag(tempStatus, PaintbrawlStatus.Eliminated))
				{
					return 7;
				}
			}
			else
			{
				if (HasFlag(tempStatus, PaintbrawlStatus.Normal))
				{
					return 0;
				}
				if (HasFlag(tempStatus, PaintbrawlStatus.Hit))
				{
					return 1;
				}
				if (HasFlag(tempStatus, PaintbrawlStatus.Stunned))
				{
					return 17;
				}
				if (HasFlag(tempStatus, PaintbrawlStatus.Grace))
				{
					return 17;
				}
				if (HasFlag(tempStatus, PaintbrawlStatus.Eliminated))
				{
					return 16;
				}
			}
		}
		return 0;
	}

	public override float[] LocalPlayerSpeed()
	{
		if (playerStatusDict.TryGetValue(NetworkSystem.Instance.LocalPlayerID, out tempStatus))
		{
			if (HasFlag(tempStatus, PaintbrawlStatus.Normal))
			{
				playerSpeed[0] = 6.5f;
				playerSpeed[1] = 1.1f;
				return playerSpeed;
			}
			if (HasFlag(tempStatus, PaintbrawlStatus.Stunned))
			{
				playerSpeed[0] = 2f;
				playerSpeed[1] = 0.5f;
				return playerSpeed;
			}
			if (HasFlag(tempStatus, PaintbrawlStatus.Eliminated))
			{
				playerSpeed[0] = fastJumpLimit;
				playerSpeed[1] = fastJumpMultiplier;
				return playerSpeed;
			}
		}
		playerSpeed[0] = 6.5f;
		playerSpeed[1] = 1.1f;
		return playerSpeed;
	}

	public override void Tick()
	{
		base.Tick();
		if (NetworkSystem.Instance.IsMasterClient)
		{
			UpdateBattleState();
		}
		PreloadSlingshotForActiveRigs(null);
		ActivateDefaultSlingShot();
	}

	public override void InfrequentUpdate()
	{
		base.InfrequentUpdate();
		foreach (int key in playerLives.Keys)
		{
			playerInList = false;
			foreach (NetPlayer item in RoomSystem.PlayersInRoom)
			{
				if (item.ActorNumber == key)
				{
					playerInList = true;
				}
			}
			if (!playerInList)
			{
				playerLives.Remove(key);
			}
		}
	}

	public int GetPlayerLives(NetPlayer player)
	{
		if (player == null)
		{
			return 0;
		}
		if (playerLives.TryGetValue(player.ActorNumber, out outLives))
		{
			return outLives;
		}
		return 0;
	}

	public bool PlayerInHitCooldown(NetPlayer player)
	{
		if (playerHitTimes.TryGetValue(player.ActorNumber, out var value))
		{
			return value + hitCooldown > Time.time;
		}
		return false;
	}

	public bool PlayerInStunCooldown(NetPlayer player)
	{
		if (playerStunTimes.TryGetValue(player.ActorNumber, out var value))
		{
			return value + hitCooldown + stunGracePeriod > Time.time;
		}
		return false;
	}

	public PaintbrawlStatus GetPlayerStatus(NetPlayer player)
	{
		if (playerStatusDict.TryGetValue(player.ActorNumber, out tempStatus))
		{
			return tempStatus;
		}
		return PaintbrawlStatus.None;
	}

	public bool OnRedTeam(PaintbrawlStatus status)
	{
		return HasFlag(status, PaintbrawlStatus.RedTeam);
	}

	public bool OnRedTeam(NetPlayer player)
	{
		PaintbrawlStatus playerStatus = GetPlayerStatus(player);
		return OnRedTeam(playerStatus);
	}

	public bool OnBlueTeam(PaintbrawlStatus status)
	{
		return HasFlag(status, PaintbrawlStatus.BlueTeam);
	}

	public bool OnBlueTeam(NetPlayer player)
	{
		PaintbrawlStatus playerStatus = GetPlayerStatus(player);
		return OnBlueTeam(playerStatus);
	}

	public bool OnNoTeam(PaintbrawlStatus status)
	{
		if (!OnRedTeam(status))
		{
			return !OnBlueTeam(status);
		}
		return false;
	}

	public bool OnNoTeam(NetPlayer player)
	{
		PaintbrawlStatus playerStatus = GetPlayerStatus(player);
		return OnNoTeam(playerStatus);
	}

	public PaintbrawlStatus GetPlayerTeam(PaintbrawlStatus status)
	{
		if (OnRedTeam(status))
		{
			return PaintbrawlStatus.RedTeam;
		}
		if (OnBlueTeam(status))
		{
			return PaintbrawlStatus.BlueTeam;
		}
		return PaintbrawlStatus.None;
	}

	public PaintbrawlStatus GetPlayerTeam(NetPlayer player)
	{
		PaintbrawlStatus playerStatus = GetPlayerStatus(player);
		return GetPlayerTeam(playerStatus);
	}

	public override bool LocalCanTag(NetPlayer myPlayer, NetPlayer otherPlayer)
	{
		return false;
	}

	public override bool LocalIsTagged(NetPlayer player)
	{
		return GetPlayerLives(player) == 0;
	}

	public bool OnSameTeam(PaintbrawlStatus playerA, PaintbrawlStatus playerB)
	{
		bool num = OnRedTeam(playerA) && OnRedTeam(playerB);
		bool flag = OnBlueTeam(playerA) && OnBlueTeam(playerB);
		return num || flag;
	}

	public bool OnSameTeam(NetPlayer myPlayer, NetPlayer otherPlayer)
	{
		PaintbrawlStatus playerStatus = GetPlayerStatus(myPlayer);
		PaintbrawlStatus playerStatus2 = GetPlayerStatus(otherPlayer);
		return OnSameTeam(playerStatus, playerStatus2);
	}

	public bool LocalCanHit(NetPlayer myPlayer, NetPlayer otherPlayer)
	{
		bool num = !OnSameTeam(myPlayer, otherPlayer);
		bool flag = GetPlayerLives(otherPlayer) != 0;
		return num && flag;
	}

	private void CopyBattleDictToArray()
	{
		for (int i = 0; i < playerLivesArray.Length; i++)
		{
			playerLivesArray[i] = 0;
			playerActorNumberArray[i] = -1;
		}
		int num = 0;
		foreach (KeyValuePair<int, int> playerLife in playerLives)
		{
			if (num >= playerLivesArray.Length)
			{
				break;
			}
			playerActorNumberArray[num] = playerLife.Key;
			playerLivesArray[num] = playerLife.Value;
			playerStatusArray[num] = GetPlayerStatus(NetworkSystem.Instance.GetPlayer(playerLife.Key));
			num++;
		}
	}

	private void CopyArrayToBattleDict()
	{
		for (int i = 0; i < playerLivesArray.Length; i++)
		{
			if (playerActorNumberArray[i] != -1 && Utils.PlayerInRoom(playerActorNumberArray[i]))
			{
				if (playerLives.TryGetValue(playerActorNumberArray[i], out outLives))
				{
					playerLives[playerActorNumberArray[i]] = playerLivesArray[i];
				}
				else
				{
					playerLives.Add(playerActorNumberArray[i], playerLivesArray[i]);
				}
				if (playerStatusDict.ContainsKey(playerActorNumberArray[i]))
				{
					playerStatusDict[playerActorNumberArray[i]] = playerStatusArray[i];
				}
				else
				{
					playerStatusDict.Add(playerActorNumberArray[i], playerStatusArray[i]);
				}
			}
		}
	}

	private PaintbrawlStatus SetFlag(PaintbrawlStatus currState, PaintbrawlStatus flag)
	{
		return currState | flag;
	}

	private PaintbrawlStatus SetFlagExclusive(PaintbrawlStatus currState, PaintbrawlStatus flag)
	{
		return flag;
	}

	private PaintbrawlStatus ClearFlag(PaintbrawlStatus currState, PaintbrawlStatus flag)
	{
		return currState & ~flag;
	}

	private bool FlagIsSet(PaintbrawlStatus currState, PaintbrawlStatus flag)
	{
		return (currState & flag) != 0;
	}

	public void RandomizeTeams()
	{
		int[] array = new int[RoomSystem.PlayersInRoom.Count];
		for (int i = 0; i < RoomSystem.PlayersInRoom.Count; i++)
		{
			array[i] = i;
		}
		System.Random rand = new System.Random();
		int[] array2 = array.OrderBy((int x) => rand.Next()).ToArray();
		PaintbrawlStatus paintbrawlStatus = ((rand.Next(0, 2) == 0) ? PaintbrawlStatus.RedTeam : PaintbrawlStatus.BlueTeam);
		PaintbrawlStatus paintbrawlStatus2 = ((paintbrawlStatus != PaintbrawlStatus.RedTeam) ? PaintbrawlStatus.RedTeam : PaintbrawlStatus.BlueTeam);
		for (int num = 0; num < RoomSystem.PlayersInRoom.Count; num++)
		{
			PaintbrawlStatus value = ((array2[num] % 2 == 0) ? paintbrawlStatus2 : paintbrawlStatus);
			playerStatusDict[RoomSystem.PlayersInRoom[num].ActorNumber] = value;
		}
	}

	public void AddPlayerToCorrectTeam(NetPlayer newPlayer)
	{
		rcount = 0;
		for (int i = 0; i < RoomSystem.PlayersInRoom.Count; i++)
		{
			if (playerStatusDict.ContainsKey(RoomSystem.PlayersInRoom[i].ActorNumber))
			{
				PaintbrawlStatus state = playerStatusDict[RoomSystem.PlayersInRoom[i].ActorNumber];
				rcount = (HasFlag(state, PaintbrawlStatus.RedTeam) ? (rcount + 1) : rcount);
			}
		}
		if ((RoomSystem.PlayersInRoom.Count - 1) / 2 == rcount)
		{
			playerStatusDict[newPlayer.ActorNumber] = ((UnityEngine.Random.Range(0, 2) == 0) ? SetFlag(playerStatusDict[newPlayer.ActorNumber], PaintbrawlStatus.RedTeam) : SetFlag(playerStatusDict[newPlayer.ActorNumber], PaintbrawlStatus.BlueTeam));
		}
		else if (rcount <= (RoomSystem.PlayersInRoom.Count - 1) / 2)
		{
			playerStatusDict[newPlayer.ActorNumber] = SetFlag(playerStatusDict[newPlayer.ActorNumber], PaintbrawlStatus.RedTeam);
		}
	}

	private void InitializePlayerStatus()
	{
		int num = CopyDictKeysToBuffer(playerStatusDict);
		for (int i = 0; i < num; i++)
		{
			playerStatusDict[reusableKeyBuffer[i]] = PaintbrawlStatus.Normal;
		}
	}

	private void UpdatePlayerStatus()
	{
		int num = CopyDictKeysToBuffer(playerStatusDict);
		for (int i = 0; i < num; i++)
		{
			int key = reusableKeyBuffer[i];
			PaintbrawlStatus playerTeam = GetPlayerTeam(playerStatusDict[key]);
			if (playerLives.TryGetValue(key, out outLives) && outLives == 0)
			{
				playerStatusDict[key] = playerTeam | PaintbrawlStatus.Eliminated;
			}
			else if (playerHitTimes.TryGetValue(key, out outHitTime) && outHitTime + hitCooldown > Time.time)
			{
				playerStatusDict[key] = playerTeam | PaintbrawlStatus.Hit;
			}
			else if (playerStunTimes.TryGetValue(key, out outHitTime))
			{
				if (outHitTime + hitCooldown > Time.time)
				{
					playerStatusDict[key] = playerTeam | PaintbrawlStatus.Stunned;
				}
				else if (outHitTime + hitCooldown + stunGracePeriod > Time.time)
				{
					playerStatusDict[key] = playerTeam | PaintbrawlStatus.Grace;
				}
				else
				{
					playerStatusDict[key] = playerTeam | PaintbrawlStatus.Normal;
				}
			}
			else
			{
				playerStatusDict[key] = playerTeam | PaintbrawlStatus.Normal;
			}
		}
	}
}
