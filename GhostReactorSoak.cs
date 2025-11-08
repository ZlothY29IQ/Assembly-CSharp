using GorillaNetworking;
using Photon.Pun;
using UnityEngine;

public class GhostReactorSoak
{
	public enum State
	{
		Disconnected,
		Connecting,
		Active
	}

	public static GhostReactorSoak instance;

	private const string SOAK_ROOM = "AKJSOAK";

	private const float MIN_CONNECTED_TIME = 5f;

	private const float MAX_CONNECTED_TIME = 60f;

	private const float MIN_DISCONNECTED_TIME = 3f;

	private const float MAX_DISCONNECTED_TIME = 6f;

	public GRPlayer grPlayer;

	public GhostReactorManager grManager;

	public State state;

	public double stateStartTime;

	public double reconnectTime;

	public double disconnectTime;

	public void Setup(GRPlayer grPlayer)
	{
		this.grPlayer = grPlayer;
		instance = this;
		if (IsSoaking())
		{
			Debug.LogFormat("Soak Setup {0} InRoom {1} Auth {2}", state, grManager != null && grManager.IsAuthority(), PhotonNetwork.InRoom);
		}
	}

	public bool IsSoaking()
	{
		return false;
	}

	public void OnUpdate()
	{
		if (!IsSoaking())
		{
			return;
		}
		GameEntityManager managerForZone = GameEntityManager.GetManagerForZone(grPlayer.gamePlayer.rig.zoneEntity.currentZone);
		if (managerForZone == null)
		{
			return;
		}
		grManager = managerForZone.ghostReactorManager;
		if (grManager == null)
		{
			return;
		}
		double timeAsDouble = Time.timeAsDouble;
		switch (state)
		{
		case State.Disconnected:
			if (!PhotonNetwork.InRoom && timeAsDouble > reconnectTime)
			{
				SetState(State.Connecting);
			}
			break;
		case State.Connecting:
			if (grManager.IsZoneActive())
			{
				SetState(State.Active);
			}
			else if (timeAsDouble > stateStartTime + 15.0)
			{
				SetState(State.Disconnected);
			}
			break;
		case State.Active:
			UpdateActive();
			if (timeAsDouble > disconnectTime)
			{
				SetState(State.Disconnected);
			}
			else if (!PhotonNetwork.InRoom)
			{
				SetState(State.Disconnected);
			}
			break;
		}
	}

	private int GetActorNumber()
	{
		if (grPlayer.gamePlayer.rig.OwningNetPlayer == null)
		{
			return -1;
		}
		return grPlayer.gamePlayer.rig.OwningNetPlayer.ActorNumber;
	}

	public void SetState(State newState)
	{
		state = newState;
		stateStartTime = Time.timeAsDouble;
		Debug.LogFormat("Soak Set State {0} Player {1} InRoom {2} Auth {3}", state, GetActorNumber(), grManager != null && grManager.IsAuthority(), PhotonNetwork.InRoom);
		switch (state)
		{
		case State.Disconnected:
			LeaveRoom();
			reconnectTime = stateStartTime + (double)Random.Range(3f, 6f);
			break;
		case State.Connecting:
			JoinRoom();
			break;
		case State.Active:
			disconnectTime = stateStartTime + (double)Random.Range(5f, 60f);
			break;
		}
	}

	public void JoinRoom()
	{
		Debug.LogFormat("Soak Join Room {0}", "AKJSOAK");
		PhotonNetworkController.Instance.AttemptToJoinSpecificRoom("AKJSOAK", JoinType.Solo);
	}

	public void LeaveRoom()
	{
		Debug.LogFormat("Soak Leave Room");
		NetworkSystem.Instance.ReturnToSinglePlayer();
	}

	private void UpdateActive()
	{
	}
}
