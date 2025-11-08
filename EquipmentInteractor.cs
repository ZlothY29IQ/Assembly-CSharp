using System.Collections.Generic;
using GorillaLocomotion.Climbing;
using UnityEngine;
using UnityEngine.XR;

public class EquipmentInteractor : MonoBehaviour
{
	[OnEnterPlay_SetNull]
	public static volatile EquipmentInteractor instance;

	[OnEnterPlay_Set(false)]
	public static bool hasInstance;

	public IHoldableObject leftHandHeldEquipment;

	public IHoldableObject rightHandHeldEquipment;

	public BuilderPieceInteractor builderPieceInteractor;

	public GameObject rightHand;

	public GameObject leftHand;

	public InputDevice leftHandDevice;

	public InputDevice rightHandDevice;

	public List<InteractionPoint> overlapInteractionPointsLeft = new List<InteractionPoint>();

	public List<InteractionPoint> overlapInteractionPointsRight = new List<InteractionPoint>();

	public float grabRadius;

	public float grabThreshold = 0.7f;

	public float grabHysteresis = 0.05f;

	public bool wasLeftGrabPressed;

	public bool wasRightGrabPressed;

	public bool isLeftGrabbing;

	public bool isRightGrabbing;

	public bool justReleased;

	public bool justGrabbed;

	public bool disableLeftGrab;

	public bool disableRightGrab;

	public bool autoGrabLeft;

	public bool autoGrabRight;

	private float grabValue;

	private float tempValue;

	private DropZone tempZone;

	private bool iteratingInteractionPoints;

	private List<InteractionPoint> interactionPointsToRemove = new List<InteractionPoint>();

	[SerializeField]
	private GorillaHandClimber bodyClimber;

	[SerializeField]
	private GorillaHandClimber leftClimber;

	[SerializeField]
	private GorillaHandClimber rightClimber;

	public GorillaHandClimber BodyClimber => bodyClimber;

	public GorillaHandClimber LeftClimber => leftClimber;

	public GorillaHandClimber RightClimber => rightClimber;

	private void Awake()
	{
		if (instance == null)
		{
			instance = this;
			hasInstance = true;
		}
		else if (instance != this)
		{
			Object.Destroy(base.gameObject);
		}
		autoGrabLeft = true;
		autoGrabRight = true;
	}

	private void OnDestroy()
	{
		if (instance == this)
		{
			hasInstance = false;
			instance = null;
		}
	}

	public void ReleaseRightHand()
	{
		if (rightHandHeldEquipment != null)
		{
			rightHandHeldEquipment.OnRelease(null, rightHand);
		}
		if (leftHandHeldEquipment != null)
		{
			leftHandHeldEquipment.OnRelease(null, rightHand);
		}
		autoGrabRight = true;
	}

	public void ReleaseLeftHand()
	{
		if (rightHandHeldEquipment != null)
		{
			rightHandHeldEquipment.OnRelease(null, leftHand);
		}
		if (leftHandHeldEquipment != null)
		{
			leftHandHeldEquipment.OnRelease(null, leftHand);
		}
		autoGrabLeft = true;
	}

	public void ForceStopClimbing()
	{
		bodyClimber.ForceStopClimbing();
		leftClimber.ForceStopClimbing();
		rightClimber.ForceStopClimbing();
	}

	public bool GetIsHolding(XRNode node)
	{
		if (node == XRNode.LeftHand)
		{
			return leftHandHeldEquipment != null;
		}
		return rightHandHeldEquipment != null;
	}

	public bool IsGrabDisabled(XRNode node)
	{
		if (node == XRNode.LeftHand)
		{
			return disableLeftGrab;
		}
		return disableRightGrab;
	}

	public void InteractionPointDisabled(InteractionPoint interactionPoint)
	{
		if (iteratingInteractionPoints)
		{
			interactionPointsToRemove.Add(interactionPoint);
			return;
		}
		if (overlapInteractionPointsLeft != null)
		{
			overlapInteractionPointsLeft.Remove(interactionPoint);
		}
		if (overlapInteractionPointsRight != null)
		{
			overlapInteractionPointsRight.Remove(interactionPoint);
		}
	}

	public bool CanGrabLeft()
	{
		if (!disableLeftGrab && leftHandHeldEquipment == null)
		{
			return builderPieceInteractor.heldPiece[0] == null;
		}
		return false;
	}

	public bool CanGrabRight()
	{
		if (!disableRightGrab && rightHandHeldEquipment == null)
		{
			return builderPieceInteractor.heldPiece[1] == null;
		}
		return false;
	}

	private void LateUpdate()
	{
		if (!ApplicationQuittingState.IsQuitting)
		{
			leftClimber.CheckHandClimber();
			rightClimber.CheckHandClimber();
			CheckInputValue(isLeftHand: true);
			isLeftGrabbing = (wasLeftGrabPressed && grabValue > grabThreshold - grabHysteresis) || (!wasLeftGrabPressed && grabValue > grabThreshold + grabHysteresis);
			if ((bool)leftClimber && leftClimber.isClimbingOrGrabbing)
			{
				isLeftGrabbing = false;
			}
			CheckInputValue(isLeftHand: false);
			isRightGrabbing = (wasRightGrabPressed && grabValue > grabThreshold - grabHysteresis) || (!wasRightGrabPressed && grabValue > grabThreshold + grabHysteresis);
			if ((bool)rightClimber && rightClimber.isClimbingOrGrabbing)
			{
				isRightGrabbing = false;
			}
			BuilderPiece builderPiece = null;
			BuilderPiece builderPiece2 = null;
			builderPiece = builderPieceInteractor.heldPiece[0];
			builderPiece2 = builderPieceInteractor.heldPiece[1];
			FireHandInteractions(leftHand, isLeftHand: true, builderPiece);
			FireHandInteractions(rightHand, isLeftHand: false, builderPiece2);
			if (!isRightGrabbing && wasRightGrabPressed)
			{
				ReleaseRightHand();
			}
			if (!isLeftGrabbing && wasLeftGrabPressed)
			{
				ReleaseLeftHand();
			}
			builderPieceInteractor.OnLateUpdate();
			if (GameBallPlayerLocal.instance != null)
			{
				GameBallPlayerLocal.instance.OnUpdateInteract();
			}
			if (GamePlayerLocal.instance != null)
			{
				GamePlayerLocal.instance.OnUpdateInteract();
			}
			wasLeftGrabPressed = isLeftGrabbing;
			wasRightGrabPressed = isRightGrabbing;
		}
	}

	private void FireHandInteractions(GameObject interactingHand, bool isLeftHand, BuilderPiece pieceInHand)
	{
		if (isLeftHand)
		{
			justGrabbed = (isLeftGrabbing && !wasLeftGrabPressed) || (isLeftGrabbing && autoGrabLeft);
			justReleased = leftHandHeldEquipment != null && !isLeftGrabbing && wasLeftGrabPressed;
		}
		else
		{
			justGrabbed = (isRightGrabbing && !wasRightGrabPressed) || (isRightGrabbing && autoGrabRight);
			justReleased = rightHandHeldEquipment != null && !isRightGrabbing && wasRightGrabPressed;
		}
		List<InteractionPoint> obj = (isLeftHand ? overlapInteractionPointsLeft : overlapInteractionPointsRight);
		bool num = (isLeftHand ? (leftHandHeldEquipment != null) : (rightHandHeldEquipment != null));
		bool flag = pieceInHand != null;
		bool flag2 = (isLeftHand ? disableLeftGrab : disableRightGrab);
		bool flag3 = !num && !flag && !flag2;
		iteratingInteractionPoints = true;
		foreach (InteractionPoint item in obj)
		{
			if (flag3 && item != null)
			{
				if (justGrabbed)
				{
					item.Holdable.OnGrab(item, interactingHand);
				}
				else
				{
					item.Holdable.OnHover(item, interactingHand);
				}
			}
			if (!justReleased)
			{
				continue;
			}
			tempZone = item.GetComponent<DropZone>();
			if (!(tempZone != null))
			{
				continue;
			}
			if (interactingHand == leftHand)
			{
				if (leftHandHeldEquipment != null)
				{
					leftHandHeldEquipment.OnRelease(tempZone, interactingHand);
				}
			}
			else if (rightHandHeldEquipment != null)
			{
				rightHandHeldEquipment.OnRelease(tempZone, interactingHand);
			}
		}
		iteratingInteractionPoints = false;
		foreach (InteractionPoint item2 in interactionPointsToRemove)
		{
			if (overlapInteractionPointsLeft != null)
			{
				overlapInteractionPointsLeft.Remove(item2);
			}
			if (overlapInteractionPointsRight != null)
			{
				overlapInteractionPointsRight.Remove(item2);
			}
		}
		interactionPointsToRemove.Clear();
	}

	public void UpdateHandEquipment(IHoldableObject newEquipment, bool forLeftHand)
	{
		if (forLeftHand)
		{
			if (newEquipment != null && newEquipment == rightHandHeldEquipment && !newEquipment.TwoHanded)
			{
				rightHandHeldEquipment = null;
			}
			if (leftHandHeldEquipment != null)
			{
				leftHandHeldEquipment.DropItemCleanup();
			}
			leftHandHeldEquipment = newEquipment;
			autoGrabLeft = false;
		}
		else
		{
			if (newEquipment != null && newEquipment == leftHandHeldEquipment && !newEquipment.TwoHanded)
			{
				leftHandHeldEquipment = null;
			}
			if (rightHandHeldEquipment != null)
			{
				rightHandHeldEquipment.DropItemCleanup();
			}
			rightHandHeldEquipment = newEquipment;
			autoGrabRight = false;
		}
	}

	public void CheckInputValue(bool isLeftHand)
	{
		if (isLeftHand)
		{
			grabValue = ControllerInputPoller.GripFloat(XRNode.LeftHand);
			tempValue = ControllerInputPoller.TriggerFloat(XRNode.LeftHand);
		}
		else
		{
			grabValue = ControllerInputPoller.GripFloat(XRNode.RightHand);
			tempValue = ControllerInputPoller.TriggerFloat(XRNode.RightHand);
		}
		grabValue = Mathf.Max(grabValue, tempValue);
	}

	public void ForceDropEquipment(IHoldableObject equipment)
	{
		if (rightHandHeldEquipment == equipment)
		{
			rightHandHeldEquipment = null;
		}
		if (leftHandHeldEquipment == equipment)
		{
			leftHandHeldEquipment = null;
		}
	}

	public void ForceDropAnyEquipment()
	{
		rightHandHeldEquipment = null;
		leftHandHeldEquipment = null;
	}

	public void ForceDropManipulatableObject(HoldableObject manipulatableObject)
	{
		if ((HoldableObject)rightHandHeldEquipment == manipulatableObject)
		{
			rightHandHeldEquipment.OnRelease(null, rightHand);
			rightHandHeldEquipment = null;
			autoGrabRight = false;
		}
		if ((HoldableObject)leftHandHeldEquipment == manipulatableObject)
		{
			leftHandHeldEquipment.OnRelease(null, leftHand);
			leftHandHeldEquipment = null;
			autoGrabLeft = false;
		}
	}
}
