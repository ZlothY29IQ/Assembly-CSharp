using UnityEngine;
using UnityEngine.Events;

namespace GorillaTagScripts.Builder;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(GorillaSurfaceOverride))]
public class BuilderPieceTappable : MonoBehaviour, IBuilderPieceComponent, IBuilderPieceFunctional, IBuilderTappable
{
	private enum FunctionalState
	{
		Idle,
		Tap
	}

	[SerializeField]
	protected BuilderPiece myPiece;

	[SerializeField]
	protected float tapCooldown = 0.5f;

	private bool isPieceActive;

	private float lastTapTime;

	private FunctionalState currentState;

	[Tooltip("Called on all clients when this collider is tapped by anyone")]
	[SerializeField]
	protected UnityEvent OnTapped;

	public virtual bool CanTap()
	{
		if (isPieceActive)
		{
			return Time.time > lastTapTime + tapCooldown;
		}
		return false;
	}

	public void OnTapLocal(float tapStrength)
	{
		if (NetworkSystem.Instance.InRoom && CanTap())
		{
			myPiece.GetTable().builderNetworking.RequestFunctionalPieceStateChange(myPiece.pieceId, 1);
		}
	}

	public virtual void OnTapReplicated()
	{
		OnTapped?.Invoke();
	}

	public void OnPieceCreate(int pieceType, int pieceId)
	{
		currentState = FunctionalState.Idle;
	}

	public void OnPieceDestroy()
	{
	}

	public void OnPiecePlacementDeserialized()
	{
	}

	public void OnPieceActivate()
	{
		isPieceActive = true;
	}

	public void OnPieceDeactivate()
	{
		isPieceActive = false;
		if (currentState == FunctionalState.Tap)
		{
			myPiece.SetFunctionalPieceState(0, NetworkSystem.Instance.LocalPlayer, NetworkSystem.Instance.ServerTimestamp);
			myPiece.GetTable().UnregisterFunctionalPiece(this);
		}
	}

	public void OnStateChanged(byte newState, NetPlayer instigator, int timeStamp)
	{
		if (IsStateValid(newState))
		{
			if (newState == 1 && currentState != FunctionalState.Tap)
			{
				lastTapTime = Time.time;
				OnTapReplicated();
				myPiece.GetTable().RegisterFunctionalPiece(this);
			}
			currentState = (FunctionalState)newState;
		}
	}

	public void OnStateRequest(byte newState, NetPlayer instigator, int timeStamp)
	{
		if (NetworkSystem.Instance.IsMasterClient && IsStateValid(newState) && instigator != null && newState == 1 && CanTap())
		{
			myPiece.GetTable().builderNetworking.FunctionalPieceStateChangeMaster(myPiece.pieceId, newState, instigator.GetPlayerRef(), timeStamp);
		}
	}

	public bool IsStateValid(byte state)
	{
		return state <= 1;
	}

	public void FunctionalPieceUpdate()
	{
		if (lastTapTime + tapCooldown < Time.time)
		{
			myPiece.SetFunctionalPieceState(0, NetworkSystem.Instance.LocalPlayer, NetworkSystem.Instance.ServerTimestamp);
			myPiece.GetTable().UnregisterFunctionalPiece(this);
		}
	}
}
