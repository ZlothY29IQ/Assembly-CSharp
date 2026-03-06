using System;
using System.Collections;
using System.Collections.Generic;
using Fusion;
using GorillaGameModes;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class GorillaTagManager : GorillaGameManager
{
	public new const int k_defaultMatIndex = 0;

	public const int k_itMatIndex = 1;

	public const int k_infectedMatIndex = 2;

	public float tagCoolDown = 5f;

	public int infectedModeThreshold = 4;

	public const byte ReportTagEvent = 1;

	public const byte ReportInfectionTagEvent = 2;

	[NonSerialized]
	public List<NetPlayer> currentInfected = new List<NetPlayer>(20);

	[NonSerialized]
	public int[] currentInfectedArray;

	[NonSerialized]
	public NetPlayer currentIt;

	[NonSerialized]
	public NetPlayer lastInfectedPlayer;

	public double lastTag;

	public double timeInfectedGameEnded;

	public bool waitingToStartNextInfectionGame;

	public bool isCurrentlyTag;

	private int tempItInt;

	private int iterator1;

	private NetPlayer tempPlayer;

	private bool allInfected;

	public float[] inspectorLocalPlayerSpeed;

	private protected VRRig taggingRig;

	private protected VRRig taggedRig;

	private NetPlayer lastTaggedPlayer;

	private double lastQuestTagTime;

	public override void Awake()
	{
		base.Awake();
		currentInfectedArray = new int[20];
		for (int i = 0; i < currentInfectedArray.Length; i++)
		{
			currentInfectedArray[i] = -1;
		}
	}

	public override void StartPlaying()
	{
		base.StartPlaying();
		if (!NetworkSystem.Instance.IsMasterClient)
		{
			return;
		}
		for (int i = 0; i < currentInfected.Count; i++)
		{
			tempPlayer = currentInfected[i];
			if (tempPlayer == null || !tempPlayer.InRoom())
			{
				currentInfected.RemoveAt(i);
				i--;
			}
		}
		if (currentIt != null && !currentIt.InRoom())
		{
			currentIt = null;
		}
		if (lastInfectedPlayer != null && !lastInfectedPlayer.InRoom())
		{
			lastInfectedPlayer = null;
		}
		UpdateState();
	}

	public override void StopPlaying()
	{
		base.StopPlaying();
		StopAllCoroutines();
		lastTaggedActorNr.Clear();
	}

	public override void ResetGame()
	{
		base.ResetGame();
		for (int i = 0; i < currentInfectedArray.Length; i++)
		{
			currentInfectedArray[i] = -1;
		}
		currentInfected.Clear();
		lastTag = 0.0;
		timeInfectedGameEnded = 0.0;
		allInfected = false;
		isCurrentlyTag = false;
		waitingToStartNextInfectionGame = false;
		currentIt = null;
		lastInfectedPlayer = null;
	}

	public virtual void UpdateState()
	{
		if (NetworkSystem.Instance.IsMasterClient)
		{
			if (GorillaGameModes.GameMode.ParticipatingPlayers.Count < 1)
			{
				isCurrentlyTag = true;
				ClearInfectionState();
				lastInfectedPlayer = null;
				currentIt = null;
			}
			else if (isCurrentlyTag && currentIt == null)
			{
				int index = UnityEngine.Random.Range(0, GorillaGameModes.GameMode.ParticipatingPlayers.Count);
				ChangeCurrentIt(GorillaGameModes.GameMode.ParticipatingPlayers[index], withTagFreeze: false);
			}
			else if (isCurrentlyTag && GorillaGameModes.GameMode.ParticipatingPlayers.Count >= infectedModeThreshold)
			{
				SetisCurrentlyTag(newTagSetting: false);
				ClearInfectionState();
				int index2 = UnityEngine.Random.Range(0, GorillaGameModes.GameMode.ParticipatingPlayers.Count);
				AddInfectedPlayer(GorillaGameModes.GameMode.ParticipatingPlayers[index2]);
				lastInfectedPlayer = GorillaGameModes.GameMode.ParticipatingPlayers[index2];
			}
			else if (!isCurrentlyTag && GorillaGameModes.GameMode.ParticipatingPlayers.Count < infectedModeThreshold)
			{
				ClearInfectionState();
				lastInfectedPlayer = null;
				SetisCurrentlyTag(newTagSetting: true);
				int index3 = UnityEngine.Random.Range(0, GorillaGameModes.GameMode.ParticipatingPlayers.Count);
				ChangeCurrentIt(GorillaGameModes.GameMode.ParticipatingPlayers[index3], withTagFreeze: false);
			}
			else if (!isCurrentlyTag && currentInfected.Count == 0)
			{
				int index4 = UnityEngine.Random.Range(0, GorillaGameModes.GameMode.ParticipatingPlayers.Count);
				AddInfectedPlayer(GorillaGameModes.GameMode.ParticipatingPlayers[index4]);
			}
			else if (!isCurrentlyTag)
			{
				UpdateInfectionState();
			}
		}
	}

	public override void InfrequentUpdate()
	{
		base.InfrequentUpdate();
		if (NetworkSystem.Instance.IsMasterClient)
		{
			UpdateState();
		}
		inspectorLocalPlayerSpeed = LocalPlayerSpeed();
	}

	protected virtual IEnumerator InfectionRoundEndingCoroutine()
	{
		while ((double)Time.time < timeInfectedGameEnded + (double)tagCoolDown)
		{
			yield return new WaitForSeconds(0.1f);
		}
		if (!isCurrentlyTag && waitingToStartNextInfectionGame)
		{
			InfectionRoundStart();
		}
		yield return null;
	}

	protected virtual void InfectionRoundStart()
	{
		ClearInfectionState();
		GorillaGameModes.GameMode.RefreshPlayers();
		List<NetPlayer> participatingPlayers = GorillaGameModes.GameMode.ParticipatingPlayers;
		if (participatingPlayers.Count <= 0)
		{
			return;
		}
		int index = UnityEngine.Random.Range(0, participatingPlayers.Count);
		for (int i = 0; i < 10; i++)
		{
			if (participatingPlayers[index] != lastInfectedPlayer)
			{
				break;
			}
			index = UnityEngine.Random.Range(0, participatingPlayers.Count);
		}
		AddInfectedPlayer(participatingPlayers[index]);
		lastInfectedPlayer = participatingPlayers[index];
		lastTag = Time.time;
	}

	public virtual void UpdateInfectionState()
	{
		if (!NetworkSystem.Instance.IsMasterClient)
		{
			return;
		}
		allInfected = true;
		foreach (NetPlayer participatingPlayer in GorillaGameModes.GameMode.ParticipatingPlayers)
		{
			if (!currentInfected.Contains(participatingPlayer))
			{
				allInfected = false;
				break;
			}
		}
		if (!isCurrentlyTag && !waitingToStartNextInfectionGame && allInfected)
		{
			InfectionRoundEnd();
		}
	}

	public void UpdateTagState(bool withTagFreeze = true)
	{
		if (!NetworkSystem.Instance.IsMasterClient)
		{
			return;
		}
		foreach (NetPlayer participatingPlayer in GorillaGameModes.GameMode.ParticipatingPlayers)
		{
			if (currentIt == participatingPlayer)
			{
				if (withTagFreeze)
				{
					RoomSystem.SendStatusEffectToPlayer(RoomSystem.StatusEffects.TaggedTime, participatingPlayer);
				}
				else
				{
					RoomSystem.SendStatusEffectToPlayer(RoomSystem.StatusEffects.JoinedTaggedTime, participatingPlayer);
				}
				RoomSystem.SendSoundEffectOnOther(0, 0.25f, participatingPlayer);
				break;
			}
		}
	}

	protected virtual void InfectionRoundEnd()
	{
		if (!NetworkSystem.Instance.IsMasterClient)
		{
			return;
		}
		foreach (NetPlayer participatingPlayer in GorillaGameModes.GameMode.ParticipatingPlayers)
		{
			RoomSystem.SendSoundEffectToPlayer(2, 0.25f, participatingPlayer, stopCurrentAudio: true);
		}
		PlayerGameEvents.GameModeCompleteRound();
		GorillaGameModes.GameMode.BroadcastRoundComplete();
		lastTaggedActorNr.Clear();
		waitingToStartNextInfectionGame = true;
		timeInfectedGameEnded = Time.time;
		StartCoroutine(InfectionRoundEndingCoroutine());
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
		if (currentInfected.Contains(myPlayer))
		{
			return !currentInfected.Contains(otherPlayer);
		}
		return false;
	}

	public override bool LocalIsTagged(NetPlayer player)
	{
		if (isCurrentlyTag)
		{
			return currentIt == player;
		}
		return currentInfected.Contains(player);
	}

	public override void LocalTag(NetPlayer taggedPlayer, NetPlayer taggingPlayer, bool bodyHit, bool leftHand)
	{
		if (LocalCanTag(NetworkSystem.Instance.LocalPlayer, taggedPlayer) && (double)Time.time > lastQuestTagTime + (double)tagCoolDown)
		{
			PlayerGameEvents.MiscEvent("GameModeTag");
			lastQuestTagTime = Time.time;
			if (!isCurrentlyTag)
			{
				PlayerGameEvents.GameModeObjectiveTriggered();
			}
		}
	}

	protected float InterpolatedInfectedJumpMultiplier(int infectedCount)
	{
		if (GorillaGameModes.GameMode.ParticipatingPlayers.Count < 2)
		{
			return fastJumpMultiplier;
		}
		return (fastJumpMultiplier - slowJumpMultiplier) / (float)(GorillaGameModes.GameMode.ParticipatingPlayers.Count - 1) * (float)(GorillaGameModes.GameMode.ParticipatingPlayers.Count - infectedCount) + slowJumpMultiplier;
	}

	protected float InterpolatedInfectedJumpSpeed(int infectedCount)
	{
		if (GorillaGameModes.GameMode.ParticipatingPlayers.Count < 2)
		{
			return fastJumpLimit;
		}
		return (fastJumpLimit - slowJumpLimit) / (float)(GorillaGameModes.GameMode.ParticipatingPlayers.Count - 1) * (float)(GorillaGameModes.GameMode.ParticipatingPlayers.Count - infectedCount) + slowJumpLimit;
	}

	protected float InterpolatedNoobJumpMultiplier(int infectedCount)
	{
		if (GorillaGameModes.GameMode.ParticipatingPlayers.Count < 2)
		{
			return slowJumpMultiplier;
		}
		return (fastJumpMultiplier - slowJumpMultiplier) / (float)(GorillaGameModes.GameMode.ParticipatingPlayers.Count - 1) * (float)(infectedCount - 1) * 0.9f + slowJumpMultiplier;
	}

	protected float InterpolatedNoobJumpSpeed(int infectedCount)
	{
		if (GorillaGameModes.GameMode.ParticipatingPlayers.Count < 2)
		{
			return slowJumpLimit;
		}
		return (fastJumpLimit - fastJumpLimit) / (float)(GorillaGameModes.GameMode.ParticipatingPlayers.Count - 1) * (float)(infectedCount - 1) * 0.9f + slowJumpLimit;
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
		taggedRig.SetTaggedBy(taggingRig);
		if (isCurrentlyTag)
		{
			if (taggingPlayer == currentIt && taggingPlayer != taggedPlayer && (double)Time.time > lastTag + (double)tagCoolDown)
			{
				AddLastTagged(taggedPlayer, taggingPlayer);
				ChangeCurrentIt(taggedPlayer);
				lastTag = Time.time;
				HandleTagBroadcast(taggedPlayer, taggingPlayer);
				GorillaGameModes.GameMode.BroadcastTag(taggedPlayer, taggingPlayer);
			}
		}
		else if (currentInfected.Contains(taggingPlayer) && !currentInfected.Contains(taggedPlayer) && (double)Time.time > lastTag + (double)tagCoolDown)
		{
			if (!taggingRig.IsPositionInRange(taggedRig.transform.position, 6f) && !taggingRig.CheckTagDistanceRollback(taggedRig, 6f, 0.2f))
			{
				MonkeAgent.instance.SendReport("extremely far tag", taggingPlayer.UserId, taggingPlayer.NickName);
				return;
			}
			HandleTagBroadcast(taggedPlayer, taggingPlayer);
			GorillaGameModes.GameMode.BroadcastTag(taggedPlayer, taggingPlayer);
			AddLastTagged(taggedPlayer, taggingPlayer);
			AddInfectedPlayer(taggedPlayer);
			_ = currentInfected.Count;
		}
	}

	public override void HitPlayer(NetPlayer taggedPlayer)
	{
		if (!NetworkSystem.Instance.IsMasterClient)
		{
			return;
		}
		taggedRig = FindPlayerVRRig(taggedPlayer);
		if (!(taggedRig == null) && !waitingToStartNextInfectionGame && !((double)Time.time < timeInfectedGameEnded + (double)(2f * tagCoolDown)))
		{
			if (isCurrentlyTag)
			{
				AddLastTagged(taggedPlayer, taggedPlayer);
				ChangeCurrentIt(taggedPlayer, withTagFreeze: false);
			}
			else if (!currentInfected.Contains(taggedPlayer))
			{
				AddLastTagged(taggedPlayer, taggedPlayer);
				AddInfectedPlayer(taggedPlayer, withTagStop: false);
				_ = currentInfected.Count;
			}
		}
	}

	public override bool CanAffectPlayer(NetPlayer player, bool thisFrame)
	{
		if (isCurrentlyTag)
		{
			return currentIt != player && thisFrame;
		}
		if (waitingToStartNextInfectionGame || (double)Time.time < timeInfectedGameEnded + (double)(2f * tagCoolDown))
		{
			return false;
		}
		return !currentInfected.Contains(player);
	}

	public bool IsInfected(NetPlayer player)
	{
		if (isCurrentlyTag)
		{
			return currentIt == player;
		}
		return currentInfected.Contains(player);
	}

	public override void NewVRRig(NetPlayer player, int vrrigPhotonViewID, bool didTutorial)
	{
		base.NewVRRig(player, vrrigPhotonViewID, didTutorial);
		if (!NetworkSystem.Instance.IsMasterClient)
		{
			return;
		}
		bool num = isCurrentlyTag;
		UpdateState();
		if (!num && !isCurrentlyTag)
		{
			if (didTutorial)
			{
				AddInfectedPlayer(player, withTagStop: false);
			}
			UpdateInfectionState();
		}
	}

	public override void OnPlayerLeftRoom(NetPlayer otherPlayer)
	{
		base.OnPlayerLeftRoom(otherPlayer);
		if (!NetworkSystem.Instance.IsMasterClient)
		{
			return;
		}
		while (currentInfected.Contains(otherPlayer))
		{
			currentInfected.Remove(otherPlayer);
		}
		if (isCurrentlyTag && ((otherPlayer != null && otherPlayer == currentIt) || currentIt.ActorNumber == otherPlayer.ActorNumber))
		{
			if (GorillaGameModes.GameMode.ParticipatingPlayers.Count > 0)
			{
				int index = UnityEngine.Random.Range(0, GorillaGameModes.GameMode.ParticipatingPlayers.Count);
				ChangeCurrentIt(GorillaGameModes.GameMode.ParticipatingPlayers[index], withTagFreeze: false);
			}
		}
		else if (!isCurrentlyTag && GorillaGameModes.GameMode.ParticipatingPlayers.Count >= infectedModeThreshold)
		{
			UpdateInfectionState();
		}
		UpdateState();
	}

	private void CopyInfectedListToArray()
	{
		for (iterator1 = 0; iterator1 < currentInfectedArray.Length; iterator1++)
		{
			currentInfectedArray[iterator1] = -1;
		}
		for (iterator1 = currentInfected.Count - 1; iterator1 >= 0; iterator1--)
		{
			if (currentInfected[iterator1] == null)
			{
				currentInfected.RemoveAt(iterator1);
			}
		}
		for (iterator1 = 0; iterator1 < currentInfected.Count; iterator1++)
		{
			currentInfectedArray[iterator1] = currentInfected[iterator1].ActorNumber;
		}
	}

	private void CopyInfectedArrayToList()
	{
		currentInfected.Clear();
		for (iterator1 = 0; iterator1 < currentInfectedArray.Length; iterator1++)
		{
			if (currentInfectedArray[iterator1] != -1)
			{
				tempPlayer = NetworkSystem.Instance.GetPlayer(currentInfectedArray[iterator1]);
				if (tempPlayer != null)
				{
					currentInfected.Add(tempPlayer);
				}
			}
		}
	}

	protected virtual void ChangeCurrentIt(NetPlayer newCurrentIt, bool withTagFreeze = true)
	{
		lastTag = Time.time;
		currentIt = newCurrentIt;
		UpdateTagState(withTagFreeze);
	}

	public void SetisCurrentlyTag(bool newTagSetting)
	{
		if (newTagSetting)
		{
			isCurrentlyTag = true;
		}
		else
		{
			isCurrentlyTag = false;
		}
		RoomSystem.SendSoundEffectAll(2, 0.25f);
	}

	public virtual void AddInfectedPlayer(NetPlayer infectedPlayer, bool withTagStop = true)
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

	public void ClearInfectionState()
	{
		currentInfected.Clear();
		waitingToStartNextInfectionGame = false;
	}

	public override void OnMasterClientSwitched(Player newMasterClient)
	{
		base.OnMasterClientSwitched(newMasterClient);
		if (NetworkSystem.Instance.IsMasterClient)
		{
			CopyRoomDataToLocalData();
			UpdateState();
		}
	}

	public void CopyRoomDataToLocalData()
	{
		lastTag = 0.0;
		timeInfectedGameEnded = 0.0;
		waitingToStartNextInfectionGame = false;
		if (isCurrentlyTag)
		{
			UpdateTagState();
		}
		else
		{
			UpdateInfectionState();
		}
	}

	public override void OnSerializeRead(object newData)
	{
		TagData tagData = (TagData)newData;
		isCurrentlyTag = tagData.isCurrentlyTag;
		tempItInt = tagData.currentItID;
		currentIt = ((tempItInt != -1) ? NetworkSystem.Instance.GetPlayer(tempItInt) : null);
		tagData.infectedPlayerList.CopyTo(currentInfectedArray);
		CopyInfectedArrayToList();
	}

	public override object OnSerializeWrite()
	{
		CopyInfectedListToArray();
		TagData tagData = new TagData
		{
			isCurrentlyTag = isCurrentlyTag,
			currentItID = ((currentIt != null) ? currentIt.ActorNumber : (-1))
		};
		tagData.infectedPlayerList.CopyFrom(currentInfectedArray, 0, currentInfectedArray.Length);
		return tagData;
	}

	public override void OnSerializeWrite(PhotonStream stream, PhotonMessageInfo info)
	{
		CopyInfectedListToArray();
		stream.SendNext(isCurrentlyTag);
		stream.SendNext((currentIt != null) ? currentIt.ActorNumber : (-1));
		stream.SendNext(currentInfectedArray[0]);
		stream.SendNext(currentInfectedArray[1]);
		stream.SendNext(currentInfectedArray[2]);
		stream.SendNext(currentInfectedArray[3]);
		stream.SendNext(currentInfectedArray[4]);
		stream.SendNext(currentInfectedArray[5]);
		stream.SendNext(currentInfectedArray[6]);
		stream.SendNext(currentInfectedArray[7]);
		stream.SendNext(currentInfectedArray[8]);
		stream.SendNext(currentInfectedArray[9]);
		stream.SendNext(currentInfectedArray[10]);
		stream.SendNext(currentInfectedArray[11]);
		stream.SendNext(currentInfectedArray[12]);
		stream.SendNext(currentInfectedArray[13]);
		stream.SendNext(currentInfectedArray[14]);
		stream.SendNext(currentInfectedArray[15]);
		stream.SendNext(currentInfectedArray[16]);
		stream.SendNext(currentInfectedArray[17]);
		stream.SendNext(currentInfectedArray[18]);
		stream.SendNext(currentInfectedArray[19]);
		WriteLastTagged(stream);
	}

	public override void OnSerializeRead(PhotonStream stream, PhotonMessageInfo info)
	{
		NetworkSystem.Instance.GetPlayer(info.Sender);
		bool flag = currentIt == NetworkSystem.Instance.LocalPlayer;
		bool flag2 = currentInfected.Contains(NetworkSystem.Instance.LocalPlayer);
		isCurrentlyTag = (bool)stream.ReceiveNext();
		tempItInt = (int)stream.ReceiveNext();
		currentIt = ((tempItInt != -1) ? NetworkSystem.Instance.GetPlayer(tempItInt) : null);
		currentInfectedArray[0] = (int)stream.ReceiveNext();
		currentInfectedArray[1] = (int)stream.ReceiveNext();
		currentInfectedArray[2] = (int)stream.ReceiveNext();
		currentInfectedArray[3] = (int)stream.ReceiveNext();
		currentInfectedArray[4] = (int)stream.ReceiveNext();
		currentInfectedArray[5] = (int)stream.ReceiveNext();
		currentInfectedArray[6] = (int)stream.ReceiveNext();
		currentInfectedArray[7] = (int)stream.ReceiveNext();
		currentInfectedArray[8] = (int)stream.ReceiveNext();
		currentInfectedArray[9] = (int)stream.ReceiveNext();
		currentInfectedArray[10] = (int)stream.ReceiveNext();
		currentInfectedArray[11] = (int)stream.ReceiveNext();
		currentInfectedArray[12] = (int)stream.ReceiveNext();
		currentInfectedArray[13] = (int)stream.ReceiveNext();
		currentInfectedArray[14] = (int)stream.ReceiveNext();
		currentInfectedArray[15] = (int)stream.ReceiveNext();
		currentInfectedArray[16] = (int)stream.ReceiveNext();
		currentInfectedArray[17] = (int)stream.ReceiveNext();
		currentInfectedArray[18] = (int)stream.ReceiveNext();
		currentInfectedArray[19] = (int)stream.ReceiveNext();
		ReadLastTagged(stream);
		CopyInfectedArrayToList();
		if (isCurrentlyTag)
		{
			if (!flag && currentIt == NetworkSystem.Instance.LocalPlayer)
			{
				lastQuestTagTime = Time.time;
			}
		}
		else if (!flag2 && currentInfected.Contains(NetworkSystem.Instance.LocalPlayer))
		{
			lastQuestTagTime = Time.time;
		}
	}

	public override GameModeType GameType()
	{
		return GameModeType.Infection;
	}

	public override string GameModeName()
	{
		return "INFECTION";
	}

	public override string GameModeNameRoomLabel()
	{
		if (!LocalisationManager.TryGetKeyForCurrentLocale("GAME_MODE_INFECTION_ROOM_LABEL", out var result, "(INFECTION GAME)"))
		{
			Debug.LogError("[LOCALIZATION::GORILLA_GAME_MANAGER] Failed to get key for Game Mode [GAME_MODE_INFECTION_ROOM_LABEL]");
		}
		return result;
	}

	public override void AddFusionDataBehaviour(NetworkObject netObject)
	{
		netObject.AddBehaviour<TagGameModeData>();
	}

	public override int MyMatIndex(NetPlayer forPlayer)
	{
		if (isCurrentlyTag && forPlayer == currentIt)
		{
			return 1;
		}
		if (currentInfected.Contains(forPlayer))
		{
			return 2;
		}
		return 0;
	}

	public override float[] LocalPlayerSpeed()
	{
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
		if (currentInfected.Contains(NetworkSystem.Instance.LocalPlayer))
		{
			playerSpeed[0] = InterpolatedInfectedJumpSpeed(currentInfected.Count);
			playerSpeed[1] = InterpolatedInfectedJumpMultiplier(currentInfected.Count);
			return playerSpeed;
		}
		playerSpeed[0] = InterpolatedNoobJumpSpeed(currentInfected.Count);
		playerSpeed[1] = InterpolatedNoobJumpMultiplier(currentInfected.Count);
		return playerSpeed;
	}
}
