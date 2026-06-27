using System.Collections.Generic;
using GorillaLocomotion.Gameplay;
using UnityEngine;

namespace GorillaTagScripts;

[RequireComponent(typeof(Collider))]
public class BuilderPieceHandHold : MonoBehaviour, IGorillaGrabable, IBuilderPieceComponent, ITickSystemTick
{
	private bool initialized;

	private Collider myCollider;

	[SerializeField]
	private bool forceMomentary = true;

	[SerializeField]
	private BuilderPiece myPiece;

	private List<GorillaGrabber> activeGrabbers = new List<GorillaGrabber>(2);

	private bool isGrabbed;

	public bool TickRunning { get; set; }

	private void Initialize()
	{
		if (!initialized)
		{
			myCollider = GetComponent<Collider>();
			initialized = true;
		}
	}

	public bool IsHandHoldMoving()
	{
		return myPiece.IsPieceMoving();
	}

	public bool MomentaryGrabOnly()
	{
		return forceMomentary;
	}

	public virtual bool CanBeGrabbed(GorillaGrabber grabber)
	{
		if (myPiece.state == BuilderPiece.State.AttachedAndPlaced)
		{
			if (myPiece.GetTable().isTableMutable)
			{
				return grabber.Player.scale < 0.5f;
			}
			return true;
		}
		return false;
	}

	public void OnGrabbed(GorillaGrabber grabber, out Transform grabbedTransform, out Vector3 localGrabbedPosition)
	{
		Initialize();
		grabbedTransform = base.transform;
		Vector3 position = grabber.transform.position;
		localGrabbedPosition = base.transform.InverseTransformPoint(position);
		activeGrabbers.Add(grabber);
		isGrabbed = true;
		grabber.Player.AddHandHold(base.transform, localGrabbedPosition, grabber, grabber.IsRightHand, rotatePlayerWhenHeld: false, out var _);
	}

	public void OnGrabReleased(GorillaGrabber grabber)
	{
		Initialize();
		activeGrabbers.Remove(grabber);
		isGrabbed = activeGrabbers.Count < 1;
		grabber.Player.RemoveHandHold(grabber, grabber.IsRightHand);
	}

	public void Tick()
	{
		if (!isGrabbed)
		{
			return;
		}
		foreach (GorillaGrabber activeGrabber in activeGrabbers)
		{
			if (activeGrabber != null && activeGrabber.Player.scale > 0.5f)
			{
				OnGrabReleased(activeGrabber);
			}
		}
	}

	public void OnPieceCreate(int pieceType, int pieceId)
	{
	}

	public void OnPieceDestroy()
	{
	}

	public void OnPiecePlacementDeserialized()
	{
	}

	public void OnPieceActivate()
	{
		if (!TickRunning && myPiece.GetTable().isTableMutable)
		{
			TickSystem<object>.AddCallbackTarget(this);
		}
	}

	public void OnPieceDeactivate()
	{
		if (TickRunning)
		{
			TickSystem<object>.RemoveCallbackTarget(this);
		}
		foreach (GorillaGrabber activeGrabber in activeGrabbers)
		{
			OnGrabReleased(activeGrabber);
		}
	}
}
