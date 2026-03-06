using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GorillaGameModes;
using Photon.Pun;
using UnityEngine;

namespace GorillaTagScripts;

public sealed class GorillaFreezeTagManager : GorillaTagManager
{
	public Dictionary<NetPlayer, float> currentFrozen = new Dictionary<NetPlayer, float>(10);

	public float freezeDuration;

	public int infectMorePlayerLowerThreshold = 6;

	public int infectMorePlayerUpperThreshold = 10;

	[Space]
	[Header("Frozen player jump settings")]
	public float frozenPlayerFastJumpLimit;

	public float frozenPlayerFastJumpMultiplier;

	public float frozenPlayerSlowJumpLimit;

	public float frozenPlayerSlowJumpMultiplier;

	[GorillaSoundLookup]
	public int[] frozenHandTapIndices;

	private float fastJumpLimitCached;

	private float fastJumpMultiplierCached;

	private float slowJumpLimitCached;

	private float slowJumpMultiplierCached;

	private VRRig localVRRig;

	private int hapticStrength;

	private List<NetPlayer> currentRoundInfectedPlayers = new List<NetPlayer>(10);

	private List<NetPlayer> lastRoundInfectedPlayers = new List<NetPlayer>(10);

	public override GameModeType GameType()
	{
		return GameModeType.FreezeTag;
	}

	public override string GameModeName()
	{
		return "FREEZE TAG";
	}

	public override string GameModeNameRoomLabel()
	{
		if (!LocalisationManager.TryGetKeyForCurrentLocale("GAME_MODE_FREEZE_TAG_ROOM_LABEL", out var result, "(FREEZE TAG GAME)"))
		{
			Debug.LogError("[LOCALIZATION::GORILLA_GAME_MANAGER] Failed to get key for Game Mode [GAME_MODE_FREEZE_TAG_ROOM_LABEL]");
		}
		return result;
	}

	public override void Awake()
	{
		base.Awake();
		fastJumpLimitCached = fastJumpLimit;
		fastJumpMultiplierCached = fastJumpMultiplier;
		slowJumpLimitCached = slowJumpLimit;
		slowJumpMultiplierCached = slowJumpMultiplier;
	}

	public override void UpdateState()
	{
		if (!NetworkSystem.Instance.IsMasterClient)
		{
			return;
		}
		foreach (KeyValuePair<NetPlayer, float> item in currentFrozen.ToList())
		{
			if (Time.time - item.Value >= freezeDuration)
			{
				currentFrozen.Remove(item.Key);
				AddInfectedPlayer(item.Key, withTagStop: false);
				RoomSystem.SendSoundEffectAll(11, 0.25f);
			}
		}
		if (GameMode.ParticipatingPlayers.Count < 1)
		{
			ResetGame();
			SetisCurrentlyTag(newTagSetting: true);
			return;
		}
		if (isCurrentlyTag && currentIt == null)
		{
			int index = Random.Range(0, GameMode.ParticipatingPlayers.Count);
			ChangeCurrentIt(GameMode.ParticipatingPlayers[index], withTagFreeze: false);
		}
		else if (isCurrentlyTag && GameMode.ParticipatingPlayers.Count >= infectedModeThreshold)
		{
			ResetGame();
			int index2 = Random.Range(0, GameMode.ParticipatingPlayers.Count);
			AddInfectedPlayer(GameMode.ParticipatingPlayers[index2]);
		}
		else if (!isCurrentlyTag && GameMode.ParticipatingPlayers.Count < infectedModeThreshold)
		{
			ResetGame();
			SetisCurrentlyTag(newTagSetting: true);
			int index3 = Random.Range(0, GameMode.ParticipatingPlayers.Count);
			ChangeCurrentIt(GameMode.ParticipatingPlayers[index3], withTagFreeze: false);
		}
		else if (!isCurrentlyTag && currentInfected.Count == 0)
		{
			int index4 = Random.Range(0, GameMode.ParticipatingPlayers.Count);
			AddInfectedPlayer(GameMode.ParticipatingPlayers[index4]);
		}
		bool flag = true;
		foreach (NetPlayer participatingPlayer in GameMode.ParticipatingPlayers)
		{
			if (!IsFrozen(participatingPlayer) && !IsInfected(participatingPlayer))
			{
				flag = false;
				break;
			}
		}
		if (flag && !isCurrentlyTag)
		{
			InfectionRoundEnd();
		}
	}

	public override void Tick()
	{
		base.Tick();
		if ((bool)localVRRig)
		{
			localVRRig.IsFrozen = IsFrozen(NetworkSystem.Instance.LocalPlayer);
		}
	}

	public override void StartPlaying()
	{
		base.StartPlaying();
		localVRRig = FindPlayerVRRig(NetworkSystem.Instance.LocalPlayer);
		if (!NetworkSystem.Instance.IsMasterClient)
		{
			return;
		}
		NetPlayer[] array = lastRoundInfectedPlayers.ToArray();
		foreach (NetPlayer netPlayer in array)
		{
			if (netPlayer != null && !netPlayer.InRoom)
			{
				lastRoundInfectedPlayers.Remove(netPlayer);
			}
		}
		array = currentRoundInfectedPlayers.ToArray();
		foreach (NetPlayer netPlayer2 in array)
		{
			if (netPlayer2 != null && !netPlayer2.InRoom)
			{
				currentRoundInfectedPlayers.Remove(netPlayer2);
			}
		}
	}

	public override void ReportTag(NetPlayer taggedPlayer, NetPlayer taggingPlayer)
	{
		if (!NetworkSystem.Instance.IsMasterClient)
		{
			return;
		}
		taggingRig = FindPlayerVRRig(taggingPlayer);
		taggedRig = FindPlayerVRRig(taggedPlayer);
		if (taggingRig == null || taggedRig == null)
		{
			return;
		}
		Debug.LogWarning("Report TAG - tagged " + taggedRig.playerNameVisible + ", tagging " + taggingRig.playerNameVisible);
		if (isCurrentlyTag)
		{
			if (taggingPlayer == currentIt && taggingPlayer != taggedPlayer && (double)Time.time > lastTag + (double)tagCoolDown)
			{
				AddLastTagged(taggedPlayer, taggingPlayer);
				ChangeCurrentIt(taggedPlayer, withTagFreeze: false);
				lastTag = Time.time;
			}
		}
		else if (currentInfected.Contains(taggingPlayer) && !currentInfected.Contains(taggedPlayer) && !currentFrozen.ContainsKey(taggedPlayer) && (double)Time.time > lastTag + (double)tagCoolDown)
		{
			if (!taggingRig.IsPositionInRange(taggedRig.transform.position, 6f) && !taggingRig.CheckTagDistanceRollback(taggedRig, 6f, 0.2f))
			{
				MonkeAgent.instance.SendReport("extremely far tag", taggingPlayer.UserId, taggingPlayer.NickName);
				return;
			}
			AddLastTagged(taggedPlayer, taggingPlayer);
			AddFrozenPlayer(taggedPlayer);
		}
		else if (!currentInfected.Contains(taggingPlayer) && !currentInfected.Contains(taggedPlayer) && currentFrozen.ContainsKey(taggedPlayer) && (double)Time.time > lastTag + (double)tagCoolDown)
		{
			if (!taggingRig.IsPositionInRange(taggedRig.transform.position, 6f) && !taggingRig.CheckTagDistanceRollback(taggedRig, 6f, 0.2f))
			{
				MonkeAgent.instance.SendReport("extremely far tag", taggingPlayer.UserId, taggingPlayer.NickName);
			}
			else
			{
				UnfreezePlayer(taggedPlayer);
			}
		}
	}

	public override bool LocalCanTag(NetPlayer myPlayer, NetPlayer otherPlayer)
	{
		if (isCurrentlyTag)
		{
			if (myPlayer == currentIt)
			{
				return myPlayer != otherPlayer;
			}
			return false;
		}
		if (currentInfected.Contains(myPlayer) && !currentFrozen.ContainsKey(otherPlayer) && !currentInfected.Contains(otherPlayer))
		{
			return true;
		}
		if (!currentInfected.Contains(myPlayer) && !currentFrozen.ContainsKey(myPlayer) && (currentInfected.Contains(otherPlayer) || currentFrozen.ContainsKey(otherPlayer)))
		{
			return true;
		}
		return false;
	}

	public override bool LocalIsTagged(NetPlayer player)
	{
		if (isCurrentlyTag)
		{
			return currentIt == player;
		}
		if (!currentInfected.Contains(player))
		{
			return currentFrozen.ContainsKey(player);
		}
		return true;
	}

	public override void NewVRRig(NetPlayer player, int vrrigPhotonViewID, bool didTutorial)
	{
		if (NetworkSystem.Instance.IsMasterClient)
		{
			GameMode.RefreshPlayers();
			if (!isCurrentlyTag && !IsInfected(player))
			{
				AddInfectedPlayer(player);
				currentRoundInfectedPlayers.Add(player);
			}
			UpdateInfectionState();
		}
	}

	protected override IEnumerator InfectionRoundEndingCoroutine()
	{
		while ((double)Time.time < timeInfectedGameEnded + (double)tagCoolDown)
		{
			yield return new WaitForSeconds(0.1f);
		}
		if (!isCurrentlyTag && waitingToStartNextInfectionGame)
		{
			ClearInfectionState();
			currentFrozen.Clear();
			GameMode.RefreshPlayers();
			lastRoundInfectedPlayers.Clear();
			lastRoundInfectedPlayers.AddRange(currentRoundInfectedPlayers);
			currentRoundInfectedPlayers.Clear();
			List<NetPlayer> participatingPlayers = GameMode.ParticipatingPlayers;
			int num = 0;
			if (participatingPlayers.Count > 0 && participatingPlayers.Count < infectMorePlayerLowerThreshold)
			{
				num = 1;
			}
			else if (participatingPlayers.Count >= infectMorePlayerLowerThreshold && participatingPlayers.Count < infectMorePlayerUpperThreshold)
			{
				num = 2;
			}
			else if (participatingPlayers.Count >= infectMorePlayerUpperThreshold)
			{
				num = 3;
			}
			for (int i = 0; i < num; i++)
			{
				TryAddNewInfectedPlayer();
			}
			lastTag = Time.time;
		}
		yield return null;
	}

	public override void ResetGame()
	{
		base.ResetGame();
		currentFrozen.Clear();
		currentRoundInfectedPlayers.Clear();
		lastRoundInfectedPlayers.Clear();
	}

	private new void AddInfectedPlayer(NetPlayer infectedPlayer, bool withTagStop = true)
	{
		if (NetworkSystem.Instance.IsMasterClient)
		{
			currentInfected.Add(infectedPlayer);
			if (!withTagStop)
			{
				RoomSystem.SendStatusEffectToPlayer(RoomSystem.StatusEffects.JoinedTaggedTime, infectedPlayer);
			}
			else
			{
				RoomSystem.SendStatusEffectToPlayer(RoomSystem.StatusEffects.TaggedTime, infectedPlayer);
			}
			RoomSystem.SendSoundEffectOnOther(0, 0.25f, infectedPlayer);
			UpdateInfectionState();
		}
	}

	private void TryAddNewInfectedPlayer()
	{
		List<NetPlayer> participatingPlayers = GameMode.ParticipatingPlayers;
		int index = Random.Range(0, participatingPlayers.Count);
		for (int i = 0; i < 10; i++)
		{
			if (!lastRoundInfectedPlayers.Contains(participatingPlayers[index]))
			{
				break;
			}
			index = Random.Range(0, participatingPlayers.Count);
		}
		AddInfectedPlayer(participatingPlayers[index]);
		currentRoundInfectedPlayers.Add(participatingPlayers[index]);
	}

	public override int MyMatIndex(NetPlayer forPlayer)
	{
		if (isCurrentlyTag && forPlayer == currentIt)
		{
			return 14;
		}
		if (currentInfected.Contains(forPlayer))
		{
			return 14;
		}
		return 0;
	}

	public override void UpdatePlayerAppearance(VRRig rig)
	{
		NetPlayer netPlayer = (rig.isOfflineVRRig ? NetworkSystem.Instance.LocalPlayer : rig.creator);
		rig.UpdateFrozenEffect(IsFrozen(netPlayer));
		int materialIndex = MyMatIndex(netPlayer);
		rig.ChangeMaterialLocal(materialIndex);
	}

	private void UnfreezePlayer(NetPlayer taggedPlayer)
	{
		if (NetworkSystem.Instance.IsMasterClient && currentFrozen.ContainsKey(taggedPlayer))
		{
			currentFrozen.Remove(taggedPlayer);
			RoomSystem.SendStatusEffectToPlayer(RoomSystem.StatusEffects.UnTagged, taggedPlayer);
			RoomSystem.SendSoundEffectAll(10, 0.25f, stopCurrentAudio: true);
		}
	}

	private void AddFrozenPlayer(NetPlayer taggedPlayer)
	{
		if (NetworkSystem.Instance.IsMasterClient && !currentFrozen.ContainsKey(taggedPlayer))
		{
			currentFrozen.Add(taggedPlayer, Time.time);
			RoomSystem.SendStatusEffectToPlayer(RoomSystem.StatusEffects.FrozenTime, taggedPlayer);
			RoomSystem.SendSoundEffectAll(9, 0.25f);
			RoomSystem.SendSoundEffectToPlayer(12, 0.05f, taggedPlayer);
		}
	}

	public bool IsFrozen(NetPlayer player)
	{
		return currentFrozen.ContainsKey(player);
	}

	public override float[] LocalPlayerSpeed()
	{
		fastJumpLimit = fastJumpLimitCached;
		fastJumpMultiplier = fastJumpMultiplierCached;
		slowJumpLimit = slowJumpLimitCached;
		slowJumpMultiplier = slowJumpMultiplierCached;
		if (isCurrentlyTag)
		{
			if (NetworkSystem.Instance.LocalPlayer == currentIt)
			{
				playerSpeed[0] = fastJumpLimit;
				playerSpeed[1] = fastJumpMultiplier;
				return playerSpeed;
			}
			playerSpeed[0] = slowJumpLimit;
			playerSpeed[1] = slowJumpMultiplier;
			return playerSpeed;
		}
		if (!currentInfected.Contains(NetworkSystem.Instance.LocalPlayer) && !currentFrozen.ContainsKey(NetworkSystem.Instance.LocalPlayer))
		{
			playerSpeed[0] = InterpolatedNoobJumpSpeed(currentInfected.Count);
			playerSpeed[1] = InterpolatedNoobJumpMultiplier(currentInfected.Count);
			return playerSpeed;
		}
		if (currentFrozen.ContainsKey(NetworkSystem.Instance.LocalPlayer))
		{
			fastJumpLimit = frozenPlayerFastJumpLimit;
			fastJumpMultiplier = frozenPlayerFastJumpMultiplier;
			slowJumpLimit = frozenPlayerSlowJumpLimit;
			slowJumpMultiplier = frozenPlayerSlowJumpMultiplier;
		}
		playerSpeed[0] = InterpolatedInfectedJumpSpeed(currentInfected.Count);
		playerSpeed[1] = InterpolatedInfectedJumpMultiplier(currentInfected.Count);
		return playerSpeed;
	}

	public int GetFrozenHandTapAudioIndex()
	{
		int num = Random.Range(0, frozenHandTapIndices.Length);
		return frozenHandTapIndices[num];
	}

	public override void OnPlayerLeftRoom(NetPlayer otherPlayer)
	{
		base.OnPlayerLeftRoom(otherPlayer);
		if (NetworkSystem.Instance.IsMasterClient)
		{
			if (isCurrentlyTag && ((otherPlayer != null && otherPlayer == currentIt) || currentIt.ActorNumber == otherPlayer.ActorNumber) && GameMode.ParticipatingPlayers.Count > 0)
			{
				int index = Random.Range(0, GameMode.ParticipatingPlayers.Count);
				ChangeCurrentIt(GameMode.ParticipatingPlayers[index], withTagFreeze: false);
			}
			if (currentInfected.Contains(otherPlayer))
			{
				currentInfected.Remove(otherPlayer);
			}
			if (currentFrozen.ContainsKey(otherPlayer))
			{
				currentFrozen.Remove(otherPlayer);
			}
			UpdateState();
		}
	}

	public override void StopPlaying()
	{
		base.StopPlaying();
		foreach (VRRig activeRig in VRRigCache.ActiveRigs)
		{
			activeRig.ForceResetFrozenEffect();
		}
	}

	public override void OnSerializeRead(PhotonStream stream, PhotonMessageInfo info)
	{
		base.OnSerializeRead(stream, info);
		currentFrozen.Clear();
		int num = (int)stream.ReceiveNext();
		for (int i = 0; i < num; i++)
		{
			int playerID = (int)stream.ReceiveNext();
			float value = (float)stream.ReceiveNext();
			NetPlayer player = NetworkSystem.Instance.GetPlayer(playerID);
			currentFrozen.Add(player, value);
		}
	}

	public override void OnSerializeWrite(PhotonStream stream, PhotonMessageInfo info)
	{
		base.OnSerializeWrite(stream, info);
		stream.SendNext(currentFrozen.Count);
		foreach (KeyValuePair<NetPlayer, float> item in currentFrozen)
		{
			stream.SendNext(item.Key.ActorNumber);
			stream.SendNext(item.Value);
		}
	}
}
