using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using GorillaGameModes;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public sealed class GorillaHuntManager : GorillaGameManager
{
	public float tagCoolDown = 5f;

	public int[] currentHuntedArray = new int[10] { -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 };

	public List<NetPlayer> currentHunted = new List<NetPlayer>(10);

	public int[] currentTargetArray = new int[10] { -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 };

	public List<NetPlayer> currentTarget = new List<NetPlayer>(10);

	public bool huntStarted;

	public bool waitingToStartNextHuntGame;

	public bool inStartCountdown;

	public int countDownTime;

	public double timeHuntGameEnded;

	public float timeLastSlowTagged;

	public object objRef;

	private int iterator1;

	private NetPlayer tempRandPlayer;

	private int tempRandIndex;

	private int notHuntedCount;

	private int tempTargetIndex;

	private NetPlayer tempPlayer;

	private int copyListToArrayIndex;

	private int copyArrayToListIndex;

	public override GameModeType GameType()
	{
		return GameModeType.HuntDown;
	}

	public override string GameModeName()
	{
		return "HUNTDOWN";
	}

	public override string GameModeNameRoomLabel()
	{
		if (!LocalisationManager.TryGetKeyForCurrentLocale("GAME_MODE_HUNT_ROOM_LABEL", out var result, "(HUNTDOWN GAME)"))
		{
			Debug.LogError("[LOCALIZATION::GORILLA_GAME_MANAGER] Failed to get key for Game Mode [GAME_MODE_HUNT_ROOM_LABEL]");
		}
		return result;
	}

	public override void AddFusionDataBehaviour(NetworkObject behaviour)
	{
		behaviour.AddBehaviour<HuntGameModeData>();
	}

	public override void StartPlaying()
	{
		base.StartPlaying();
		GorillaTagger.Instance.offlineVRRig.huntComputer.SetActive(value: true);
		if (!NetworkSystem.Instance.IsMasterClient)
		{
			return;
		}
		for (int i = 0; i < currentHunted.Count; i++)
		{
			tempPlayer = currentHunted[i];
			if (tempPlayer == null || !tempPlayer.InRoom())
			{
				currentHunted.RemoveAt(i);
				i--;
			}
		}
		for (int i = 0; i < currentTarget.Count; i++)
		{
			tempPlayer = currentTarget[i];
			if (tempPlayer == null || !tempPlayer.InRoom())
			{
				currentTarget.RemoveAt(i);
				i--;
			}
		}
		UpdateState();
	}

	public override void StopPlaying()
	{
		base.StopPlaying();
		GorillaTagger.Instance.offlineVRRig.huntComputer.SetActive(value: false);
		StopAllCoroutines();
	}

	public override void ResetGame()
	{
		base.ResetGame();
		currentHunted.Clear();
		currentTarget.Clear();
		for (int i = 0; i < currentHuntedArray.Length; i++)
		{
			currentHuntedArray[i] = -1;
			currentTargetArray[i] = -1;
		}
		huntStarted = false;
		waitingToStartNextHuntGame = false;
		inStartCountdown = false;
		timeHuntGameEnded = 0.0;
		countDownTime = 0;
		timeLastSlowTagged = 0f;
	}

	public void UpdateState()
	{
		if (!NetworkSystem.Instance.IsMasterClient)
		{
			return;
		}
		if (NetworkSystem.Instance.RoomPlayerCount <= 3)
		{
			CleanUpHunt();
			huntStarted = false;
			waitingToStartNextHuntGame = false;
			for (iterator1 = 0; iterator1 < RoomSystem.PlayersInRoom.Count; iterator1++)
			{
				RoomSystem.SendSoundEffectToPlayer(0, 0.25f, RoomSystem.PlayersInRoom[iterator1]);
			}
		}
		else if (NetworkSystem.Instance.RoomPlayerCount > 3 && !huntStarted && !waitingToStartNextHuntGame && !inStartCountdown)
		{
			Utils.Log("<color=red> there are enough players</color>", this);
			StartCoroutine(StartHuntCountdown());
		}
		else
		{
			UpdateHuntState();
		}
	}

	public void CleanUpHunt()
	{
		if (NetworkSystem.Instance.IsMasterClient)
		{
			currentHunted.Clear();
			currentTarget.Clear();
		}
	}

	public IEnumerator StartHuntCountdown()
	{
		if (NetworkSystem.Instance.IsMasterClient && !inStartCountdown)
		{
			inStartCountdown = true;
			countDownTime = 5;
			CleanUpHunt();
			while (countDownTime > 0)
			{
				yield return new WaitForSeconds(1f);
				countDownTime--;
			}
			StartHunt();
		}
		yield return null;
	}

	public void StartHunt()
	{
		if (!NetworkSystem.Instance.IsMasterClient)
		{
			return;
		}
		huntStarted = true;
		waitingToStartNextHuntGame = false;
		countDownTime = 0;
		inStartCountdown = false;
		CleanUpHunt();
		for (iterator1 = 0; iterator1 < NetworkSystem.Instance.AllNetPlayers.Count(); iterator1++)
		{
			if (currentTarget.Count < 10)
			{
				currentTarget.Add(NetworkSystem.Instance.AllNetPlayers[iterator1]);
				RoomSystem.SendSoundEffectToPlayer(0, 0.25f, NetworkSystem.Instance.AllNetPlayers[iterator1]);
			}
		}
		RandomizePlayerList(ref currentTarget);
	}

	public void RandomizePlayerList(ref List<NetPlayer> listToRandomize)
	{
		for (int i = 0; i < listToRandomize.Count - 1; i++)
		{
			tempRandIndex = Random.Range(i, listToRandomize.Count);
			tempRandPlayer = listToRandomize[i];
			listToRandomize[i] = listToRandomize[tempRandIndex];
			listToRandomize[tempRandIndex] = tempRandPlayer;
		}
	}

	public IEnumerator HuntEnd()
	{
		if (NetworkSystem.Instance.IsMasterClient)
		{
			while ((double)Time.time < timeHuntGameEnded + (double)tagCoolDown)
			{
				yield return new WaitForSeconds(0.1f);
			}
			if (waitingToStartNextHuntGame)
			{
				StartCoroutine(StartHuntCountdown());
			}
			yield return null;
		}
		yield return null;
	}

	public void UpdateHuntState()
	{
		if (!NetworkSystem.Instance.IsMasterClient)
		{
			return;
		}
		notHuntedCount = 0;
		foreach (NetPlayer item in RoomSystem.PlayersInRoom)
		{
			if (currentTarget.Contains(item) && !currentHunted.Contains(item))
			{
				notHuntedCount++;
			}
		}
		if (notHuntedCount <= 2 && huntStarted)
		{
			EndHuntGame();
		}
	}

	private void EndHuntGame()
	{
		if (!NetworkSystem.Instance.IsMasterClient)
		{
			return;
		}
		foreach (NetPlayer item in RoomSystem.PlayersInRoom)
		{
			RoomSystem.SendStatusEffectToPlayer(RoomSystem.StatusEffects.TaggedTime, item);
			RoomSystem.SendSoundEffectToPlayer(2, 0.25f, item);
		}
		huntStarted = false;
		timeHuntGameEnded = Time.time;
		waitingToStartNextHuntGame = true;
		StartCoroutine(HuntEnd());
	}

	public override bool LocalCanTag(NetPlayer myPlayer, NetPlayer otherPlayer)
	{
		if (waitingToStartNextHuntGame || countDownTime > 0 || GorillaTagger.Instance.currentStatus == GorillaTagger.StatusEffect.Frozen)
		{
			return false;
		}
		if (currentHunted.Contains(myPlayer) && !currentHunted.Contains(otherPlayer) && Time.time > timeLastSlowTagged + 1f)
		{
			timeLastSlowTagged = Time.time;
			return true;
		}
		if (IsTargetOf(myPlayer, otherPlayer))
		{
			return true;
		}
		return false;
	}

	public override bool LocalIsTagged(NetPlayer player)
	{
		if (waitingToStartNextHuntGame || countDownTime > 0)
		{
			return false;
		}
		return currentHunted.Contains(player);
	}

	public override void ReportTag(NetPlayer taggedPlayer, NetPlayer taggingPlayer)
	{
		if (NetworkSystem.Instance.IsMasterClient && !waitingToStartNextHuntGame)
		{
			if ((currentHunted.Contains(taggingPlayer) || !currentTarget.Contains(taggingPlayer)) && !currentHunted.Contains(taggedPlayer) && currentTarget.Contains(taggedPlayer))
			{
				RoomSystem.SendStatusEffectToPlayer(RoomSystem.StatusEffects.SetSlowedTime, taggedPlayer);
				RoomSystem.SendSoundEffectOnOther(5, 0.125f, taggedPlayer);
			}
			else if (IsTargetOf(taggingPlayer, taggedPlayer))
			{
				RoomSystem.SendStatusEffectToPlayer(RoomSystem.StatusEffects.TaggedTime, taggedPlayer);
				RoomSystem.SendSoundEffectOnOther(0, 0.25f, taggedPlayer);
				currentHunted.Add(taggedPlayer);
				UpdateHuntState();
			}
		}
	}

	public bool IsTargetOf(NetPlayer huntingPlayer, NetPlayer huntedPlayer)
	{
		if (!currentHunted.Contains(huntingPlayer) && !currentHunted.Contains(huntedPlayer) && currentTarget.Contains(huntingPlayer) && currentTarget.Contains(huntedPlayer))
		{
			return huntedPlayer == GetTargetOf(huntingPlayer);
		}
		return false;
	}

	public NetPlayer GetTargetOf(NetPlayer netPlayer)
	{
		if (currentHunted.Contains(netPlayer) || !currentTarget.Contains(netPlayer))
		{
			return null;
		}
		tempTargetIndex = currentTarget.IndexOf(netPlayer);
		for (int num = (tempTargetIndex + 1) % currentTarget.Count; num != tempTargetIndex; num = (num + 1) % currentTarget.Count)
		{
			if (currentTarget[num] == netPlayer)
			{
				return null;
			}
			if (!currentHunted.Contains(currentTarget[num]) && currentTarget[num] != null)
			{
				return currentTarget[num];
			}
		}
		return null;
	}

	public override void HitPlayer(NetPlayer taggedPlayer)
	{
		if (NetworkSystem.Instance.IsMasterClient && !waitingToStartNextHuntGame && !currentHunted.Contains(taggedPlayer) && currentTarget.Contains(taggedPlayer))
		{
			RoomSystem.SendStatusEffectToPlayer(RoomSystem.StatusEffects.TaggedTime, taggedPlayer);
			RoomSystem.SendSoundEffectOnOther(0, 0.25f, taggedPlayer);
			currentHunted.Add(taggedPlayer);
			UpdateHuntState();
		}
	}

	public override bool CanAffectPlayer(NetPlayer player, bool thisFrame)
	{
		if (!waitingToStartNextHuntGame && !currentHunted.Contains(player))
		{
			return currentTarget.Contains(player);
		}
		return false;
	}

	public override void OnPlayerEnteredRoom(NetPlayer newPlayer)
	{
		base.OnPlayerEnteredRoom(newPlayer);
		_ = NetworkSystem.Instance.IsMasterClient;
	}

	public override void NewVRRig(NetPlayer player, int vrrigPhotonViewID, bool didTutorial)
	{
		base.NewVRRig(player, vrrigPhotonViewID, didTutorial);
		if (NetworkSystem.Instance.IsMasterClient)
		{
			if (!waitingToStartNextHuntGame && huntStarted)
			{
				currentHunted.Add(player);
			}
			UpdateState();
		}
	}

	public override void OnPlayerLeftRoom(NetPlayer otherPlayer)
	{
		base.OnPlayerLeftRoom(otherPlayer);
		if (NetworkSystem.Instance.IsMasterClient)
		{
			if (currentTarget.Contains(otherPlayer))
			{
				currentTarget.Remove(otherPlayer);
			}
			if (currentHunted.Contains(otherPlayer))
			{
				currentHunted.Remove(otherPlayer);
			}
			UpdateState();
		}
	}

	private void CopyHuntDataListToArray()
	{
		for (copyListToArrayIndex = 0; copyListToArrayIndex < 10; copyListToArrayIndex++)
		{
			currentHuntedArray[copyListToArrayIndex] = -1;
			currentTargetArray[copyListToArrayIndex] = -1;
		}
		for (copyListToArrayIndex = currentHunted.Count - 1; copyListToArrayIndex >= 0; copyListToArrayIndex--)
		{
			if (currentHunted[copyListToArrayIndex] == null)
			{
				currentHunted.RemoveAt(copyListToArrayIndex);
			}
		}
		for (copyListToArrayIndex = currentTarget.Count - 1; copyListToArrayIndex >= 0; copyListToArrayIndex--)
		{
			if (currentTarget[copyListToArrayIndex] == null)
			{
				currentTarget.RemoveAt(copyListToArrayIndex);
			}
		}
		for (copyListToArrayIndex = 0; copyListToArrayIndex < currentHunted.Count; copyListToArrayIndex++)
		{
			currentHuntedArray[copyListToArrayIndex] = currentHunted[copyListToArrayIndex].ActorNumber;
		}
		for (copyListToArrayIndex = 0; copyListToArrayIndex < currentTarget.Count; copyListToArrayIndex++)
		{
			currentTargetArray[copyListToArrayIndex] = currentTarget[copyListToArrayIndex].ActorNumber;
		}
	}

	private void CopyHuntDataArrayToList()
	{
		currentTarget.Clear();
		for (copyArrayToListIndex = 0; copyArrayToListIndex < currentTargetArray.Length; copyArrayToListIndex++)
		{
			if (currentTargetArray[copyArrayToListIndex] != -1)
			{
				tempPlayer = NetworkSystem.Instance.GetPlayer(currentTargetArray[copyArrayToListIndex]);
				if (tempPlayer != null)
				{
					currentTarget.Add(tempPlayer);
				}
			}
		}
		currentHunted.Clear();
		for (copyArrayToListIndex = 0; copyArrayToListIndex < currentHuntedArray.Length; copyArrayToListIndex++)
		{
			if (currentHuntedArray[copyArrayToListIndex] != -1)
			{
				tempPlayer = NetworkSystem.Instance.GetPlayer(currentHuntedArray[copyArrayToListIndex]);
				if (tempPlayer != null)
				{
					currentHunted.Add(tempPlayer);
				}
			}
		}
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
		waitingToStartNextHuntGame = false;
		UpdateHuntState();
	}

	public override void OnSerializeRead(object newData)
	{
		HuntData huntData = (HuntData)newData;
		huntData.currentHuntedArray.CopyTo(currentHuntedArray);
		huntData.currentTargetArray.CopyTo(currentTargetArray);
		huntStarted = huntData.huntStarted;
		waitingToStartNextHuntGame = huntData.waitingToStartNextHuntGame;
		countDownTime = huntData.countDownTime;
		CopyHuntDataArrayToList();
	}

	public override object OnSerializeWrite()
	{
		CopyHuntDataListToArray();
		HuntData huntData = default(HuntData);
		huntData.currentHuntedArray.CopyFrom(currentHuntedArray, 0, currentHuntedArray.Length);
		huntData.currentTargetArray.CopyFrom(currentTargetArray, 0, currentTargetArray.Length);
		huntData.huntStarted = huntStarted;
		huntData.waitingToStartNextHuntGame = waitingToStartNextHuntGame;
		huntData.countDownTime = countDownTime;
		return huntData;
	}

	public override void OnSerializeWrite(PhotonStream stream, PhotonMessageInfo info)
	{
		CopyHuntDataListToArray();
		stream.SendNext(currentHuntedArray[0]);
		stream.SendNext(currentHuntedArray[1]);
		stream.SendNext(currentHuntedArray[2]);
		stream.SendNext(currentHuntedArray[3]);
		stream.SendNext(currentHuntedArray[4]);
		stream.SendNext(currentHuntedArray[5]);
		stream.SendNext(currentHuntedArray[6]);
		stream.SendNext(currentHuntedArray[7]);
		stream.SendNext(currentHuntedArray[8]);
		stream.SendNext(currentHuntedArray[9]);
		stream.SendNext(currentTargetArray[0]);
		stream.SendNext(currentTargetArray[1]);
		stream.SendNext(currentTargetArray[2]);
		stream.SendNext(currentTargetArray[3]);
		stream.SendNext(currentTargetArray[4]);
		stream.SendNext(currentTargetArray[5]);
		stream.SendNext(currentTargetArray[6]);
		stream.SendNext(currentTargetArray[7]);
		stream.SendNext(currentTargetArray[8]);
		stream.SendNext(currentTargetArray[9]);
		stream.SendNext(huntStarted);
		stream.SendNext(waitingToStartNextHuntGame);
		stream.SendNext(countDownTime);
	}

	public override void OnSerializeRead(PhotonStream stream, PhotonMessageInfo info)
	{
		currentHuntedArray[0] = (int)stream.ReceiveNext();
		currentHuntedArray[1] = (int)stream.ReceiveNext();
		currentHuntedArray[2] = (int)stream.ReceiveNext();
		currentHuntedArray[3] = (int)stream.ReceiveNext();
		currentHuntedArray[4] = (int)stream.ReceiveNext();
		currentHuntedArray[5] = (int)stream.ReceiveNext();
		currentHuntedArray[6] = (int)stream.ReceiveNext();
		currentHuntedArray[7] = (int)stream.ReceiveNext();
		currentHuntedArray[8] = (int)stream.ReceiveNext();
		currentHuntedArray[9] = (int)stream.ReceiveNext();
		currentTargetArray[0] = (int)stream.ReceiveNext();
		currentTargetArray[1] = (int)stream.ReceiveNext();
		currentTargetArray[2] = (int)stream.ReceiveNext();
		currentTargetArray[3] = (int)stream.ReceiveNext();
		currentTargetArray[4] = (int)stream.ReceiveNext();
		currentTargetArray[5] = (int)stream.ReceiveNext();
		currentTargetArray[6] = (int)stream.ReceiveNext();
		currentTargetArray[7] = (int)stream.ReceiveNext();
		currentTargetArray[8] = (int)stream.ReceiveNext();
		currentTargetArray[9] = (int)stream.ReceiveNext();
		huntStarted = (bool)stream.ReceiveNext();
		waitingToStartNextHuntGame = (bool)stream.ReceiveNext();
		countDownTime = (int)stream.ReceiveNext();
		CopyHuntDataArrayToList();
	}

	public override int MyMatIndex(NetPlayer forPlayer)
	{
		NetPlayer targetOf = GetTargetOf(forPlayer);
		if (currentHunted.Contains(forPlayer) || (huntStarted && targetOf == null))
		{
			return 3;
		}
		return 0;
	}

	public override float[] LocalPlayerSpeed()
	{
		if (!currentHunted.Contains(NetworkSystem.Instance.LocalPlayer) && (!huntStarted || GetTargetOf(NetworkSystem.Instance.LocalPlayer) != null))
		{
			if (GorillaTagger.Instance.currentStatus != GorillaTagger.StatusEffect.Slowed)
			{
				return new float[2] { 6.5f, 1.1f };
			}
			return new float[2] { 5.5f, 0.9f };
		}
		return new float[2] { 8.5f, 1.3f };
	}

	public override void InfrequentUpdate()
	{
		base.InfrequentUpdate();
	}
}
