using System;
using GorillaGameModes;
using GorillaTag;
using Photon.Pun;
using UnityEngine;

public sealed class SuperInfectionGame : GorillaTagManager
{
	[SerializeField]
	private int _mySuperExampleSerializedField = 123;

	private ESuperInfectionGameState _gameState_previous;

	public new static SuperInfectionGame instance { get; private set; }

	[DebugReadout]
	public ESuperInfectionGameState gameState { get; private set; }

	public override GameModeType GameType()
	{
		return GameModeType.SuperInfect;
	}

	public override void Awake()
	{
		instance = this;
		gameState = ESuperInfectionGameState.Stopped;
		base.Awake();
	}

	public override void OnEnable()
	{
		base.OnEnable();
		SIProgression.Instance?.ResetTelemetryIntervalData();
	}

	public override void OnDisable()
	{
		base.OnDisable();
	}

	public override void Tick()
	{
		base.Tick();
	}

	public override void StartPlaying()
	{
		gameState = ESuperInfectionGameState.Starting;
		base.StartPlaying();
		if (NetworkSystem.Instance.IsMasterClient)
		{
			SIProgression.Instance.AddRoundTelemetry();
		}
		VRRig.LocalRig.EnableSuperInfectionHands(on: true);
		for (int i = 0; i < currentNetPlayerArray.Length; i++)
		{
			if (VRRigCache.Instance.TryGetVrrig(currentNetPlayerArray[i], out var playerRig))
			{
				playerRig.Rig.EnableSuperInfectionHands(on: true);
			}
		}
	}

	public override void StopPlaying()
	{
		base.StopPlaying();
		gameState = ESuperInfectionGameState.Stopped;
		VRRig.LocalRig.EnableSuperInfectionHands(on: false);
	}

	public override void OnPlayerEnteredRoom(NetPlayer newPlayer)
	{
		base.OnPlayerEnteredRoom(newPlayer);
		if (VRRigCache.Instance.TryGetVrrig(newPlayer, out var playerRig))
		{
			playerRig.Rig.EnableSuperInfectionHands(on: true);
		}
	}

	public override string GameModeName()
	{
		return "SUPER INFECTION";
	}

	public override string GameModeNameRoomLabel()
	{
		if (!LocalisationManager.TryGetKeyForCurrentLocale("GAME_MODE_SUPER_INFECTION_ROOM_LABEL", out var result, "(SUPER INFECTION GAME)"))
		{
			Debug.LogError("[LOCALIZATION::GORILLA_GAME_MANAGER] Failed to get key for Game Mode [GAME_MODE_SUPER_INFECTION_ROOM_LABEL]");
		}
		return result;
	}

	public override void InfrequentUpdate()
	{
		base.InfrequentUpdate();
	}

	protected override void InfectionRoundStart()
	{
		base.InfectionRoundStart();
		gameState = ESuperInfectionGameState.Playing;
	}

	protected override void InfectionRoundEnd()
	{
		base.InfectionRoundEnd();
		gameState = ESuperInfectionGameState.RoundRestarting;
		SuperInfectionManager.activeSuperInfectionManager.zoneSuperInfection.ResetPerRoundResources();
	}

	public override bool LocalCanTag(NetPlayer myPlayer, NetPlayer otherPlayer)
	{
		return base.LocalCanTag(myPlayer, otherPlayer);
	}

	public override void UpdatePlayerAppearance(VRRig rig)
	{
		base.UpdatePlayerAppearance(rig);
	}

	public override int MyMatIndex(NetPlayer forPlayer)
	{
		return base.MyMatIndex(forPlayer);
	}

	public override void OnSerializeWrite(PhotonStream stream, PhotonMessageInfo info)
	{
		base.OnSerializeWrite(stream, info);
		stream.SendNext(gameState);
	}

	public override void OnSerializeRead(PhotonStream stream, PhotonMessageInfo info)
	{
		base.OnSerializeRead(stream, info);
		ESuperInfectionGameState eSuperInfectionGameState = (ESuperInfectionGameState)stream.ReceiveNext();
		if (Enum.IsDefined(typeof(ESuperInfectionGameState), gameState))
		{
			gameState = eSuperInfectionGameState;
			if (gameState != _gameState_previous)
			{
				_OnGameStateChanged();
				_gameState_previous = gameState;
			}
		}
	}

	public void _OnGameStateChanged()
	{
		if (gameState == ESuperInfectionGameState.Starting)
		{
			SIProgression.Instance.AddRoundTelemetry();
		}
		GTDev.Log($"Game state changed to {gameState} ...\n(was {_gameState_previous}).");
	}

	public override void HandleTagBroadcast(NetPlayer taggedPlayer, NetPlayer taggingPlayer)
	{
		try
		{
			SIProgression.Instance.HandleTagTelemetry(taggedPlayer, taggingPlayer);
		}
		catch (Exception exception)
		{
			Debug.LogException(exception, this);
		}
		if (VRRigCache.Instance.TryGetVrrig(taggedPlayer, out var _) && VRRigCache.Instance.TryGetVrrig(taggingPlayer, out var _) && taggingPlayer.ActorNumber == SIPlayer.LocalPlayer.ActorNr)
		{
			if (SIProgression.Instance.heldOrSnappedByGadgetPageType[SITechTreePageId.Dash] > 0)
			{
				PlayerGameEvents.MiscEvent("SIDashTag");
			}
			if (SIProgression.Instance.heldOrSnappedByGadgetPageType[SITechTreePageId.Thruster] > 0)
			{
				PlayerGameEvents.MiscEvent("SIThrusterTag");
			}
			if (SIProgression.Instance.heldOrSnappedByGadgetPageType[SITechTreePageId.Stilt] > 0)
			{
				PlayerGameEvents.MiscEvent("SIStiltTag");
			}
			if (SIProgression.Instance.heldOrSnappedByGadgetPageType[SITechTreePageId.Platform] > 0)
			{
				PlayerGameEvents.MiscEvent("SIPlatformTag");
			}
			if (SIProgression.Instance.heldOrSnappedByGadgetPageType[SITechTreePageId.Blaster] > 0)
			{
				PlayerGameEvents.MiscEvent("SIBlasterTag");
			}
			if (SIProgression.Instance.heldOrSnappedOthersGadgets > 0)
			{
				PlayerGameEvents.MiscEvent("SIBorrowedGadgetTag");
			}
			PlayerGameEvents.MiscEvent("SIGameModeTag");
		}
	}
}
