using UnityEngine;

public class GameBallPlayer : MonoBehaviour
{
	private struct HandData
	{
		public GameBallId grabbedGameBallId;
	}

	public VRRig rig;

	public int teamId;

	private HandData[] hands;

	public const int MAX_HANDS = 2;

	public const int LEFT_HAND = 0;

	public const int RIGHT_HAND = 1;

	private int inGoalZone;

	private void Awake()
	{
		hands = new HandData[2];
		for (int i = 0; i < 2; i++)
		{
			ClearGrabbed(i);
		}
		teamId = -1;
	}

	public void CleanupPlayer()
	{
		MonkeBallPlayer component = GetComponent<MonkeBallPlayer>();
		if (component != null)
		{
			component.currGoalZone = null;
			for (int i = 0; i < MonkeBallGame.Instance.goalZones.Count; i++)
			{
				MonkeBallGame.Instance.goalZones[i].CleanupPlayer(component);
			}
		}
	}

	public void SetGrabbed(GameBallId gameBallId, int handIndex)
	{
		if (gameBallId.IsValid())
		{
			ClearGrabbedIfHeld(gameBallId);
		}
		HandData handData = hands[handIndex];
		handData.grabbedGameBallId = gameBallId;
		hands[handIndex] = handData;
	}

	public void ClearGrabbedIfHeld(GameBallId gameBallId)
	{
		for (int i = 0; i < 2; i++)
		{
			if (hands[i].grabbedGameBallId == gameBallId)
			{
				ClearGrabbed(i);
			}
		}
	}

	public void ClearGrabbed(int handIndex)
	{
		SetGrabbed(GameBallId.Invalid, handIndex);
	}

	public void ClearAllGrabbed()
	{
		for (int i = 0; i < hands.Length; i++)
		{
			ClearGrabbed(i);
		}
	}

	public void SetInGoalZone(bool inZone)
	{
		if (inZone)
		{
			inGoalZone++;
		}
		else
		{
			inGoalZone--;
		}
	}

	public bool IsHoldingBall()
	{
		return GetGameBallId().IsValid();
	}

	public GameBallId GetGameBallId(int handIndex)
	{
		return hands[handIndex].grabbedGameBallId;
	}

	public int FindHandIndex(GameBallId gameBallId)
	{
		for (int i = 0; i < hands.Length; i++)
		{
			if (hands[i].grabbedGameBallId == gameBallId)
			{
				return i;
			}
		}
		return -1;
	}

	public GameBallId GetGameBallId()
	{
		for (int i = 0; i < hands.Length; i++)
		{
			if (hands[i].grabbedGameBallId.IsValid())
			{
				return hands[i].grabbedGameBallId;
			}
		}
		return GameBallId.Invalid;
	}

	public bool IsLocalPlayer()
	{
		return VRRigCache.Instance.localRig.Creator.ActorNumber == rig.OwningNetPlayer.ActorNumber;
	}

	public static bool IsLeftHand(int handIndex)
	{
		return handIndex == 0;
	}

	public static int GetHandIndex(bool leftHand)
	{
		if (!leftHand)
		{
			return 1;
		}
		return 0;
	}

	public static VRRig GetRig(int actorNumber)
	{
		NetPlayer player = NetworkSystem.Instance.GetPlayer(actorNumber);
		if (player == null || player.IsNull || !VRRigCache.Instance.TryGetVrrig(player, out var playerRig))
		{
			return null;
		}
		return playerRig.Rig;
	}

	public static GameBallPlayer GetGamePlayer(int actorNumber)
	{
		if (actorNumber < 0)
		{
			return null;
		}
		VRRig vRRig = GetRig(actorNumber);
		if (vRRig == null)
		{
			return null;
		}
		return vRRig.GetComponent<GameBallPlayer>();
	}

	public static GameBallPlayer GetGamePlayer(Collider collider, bool bodyOnly = false)
	{
		Transform parent = collider.transform;
		while (parent != null)
		{
			GameBallPlayer component = parent.GetComponent<GameBallPlayer>();
			if (component != null)
			{
				return component;
			}
			if (bodyOnly)
			{
				break;
			}
			parent = parent.parent;
		}
		return null;
	}
}
