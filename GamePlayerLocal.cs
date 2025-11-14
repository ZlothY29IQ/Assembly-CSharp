using System;
using System.Collections.Generic;
using GorillaLocomotion;
using GorillaLocomotion.Climbing;
using UnityEngine;
using UnityEngine.XR;

public class GamePlayerLocal : MonoBehaviour
{
	private enum HandGrabState
	{
		Empty,
		Holding
	}

	private struct HandData
	{
		public HandGrabState grabState;

		public bool gripWasHeld;

		public double gripPressedTime;

		public GameEntityId grabbedGameBallId;
	}

	public struct InputDataMotion
	{
		public double time;

		public Vector3 position;

		public Quaternion rotation;

		public Vector3 velocity;

		public Vector3 angVelocity;
	}

	public class InputData
	{
		public int maxInputs;

		public List<InputDataMotion> inputMotionHistory;

		public InputData(int maxInputs)
		{
			this.maxInputs = maxInputs;
			inputMotionHistory = new List<InputDataMotion>(maxInputs);
		}

		public void AddInput(InputDataMotion data)
		{
			if (inputMotionHistory.Count >= maxInputs)
			{
				inputMotionHistory.RemoveAt(0);
			}
			inputMotionHistory.Add(data);
		}

		public float GetMaxSpeed(float ignoreRecent, float window)
		{
			double timeAsDouble = Time.timeAsDouble;
			double num = timeAsDouble - (double)ignoreRecent - (double)window;
			double num2 = timeAsDouble - (double)ignoreRecent;
			float num3 = 0f;
			for (int num4 = inputMotionHistory.Count - 1; num4 >= 0; num4--)
			{
				InputDataMotion inputDataMotion = inputMotionHistory[num4];
				if (!(inputDataMotion.time > num2))
				{
					if (inputDataMotion.time < num)
					{
						break;
					}
					float sqrMagnitude = inputDataMotion.velocity.sqrMagnitude;
					if (sqrMagnitude > num3)
					{
						num3 = sqrMagnitude;
					}
				}
			}
			return Mathf.Sqrt(num3);
		}

		public Vector3 GetAvgVel(float ignoreRecent, float window)
		{
			double timeAsDouble = Time.timeAsDouble;
			double num = timeAsDouble - (double)ignoreRecent - (double)window;
			double num2 = timeAsDouble - (double)ignoreRecent;
			Vector3 zero = Vector3.zero;
			int num3 = 0;
			for (int num4 = inputMotionHistory.Count - 1; num4 >= 0; num4--)
			{
				InputDataMotion inputDataMotion = inputMotionHistory[num4];
				if (!(inputDataMotion.time > num2))
				{
					if (inputDataMotion.time < num)
					{
						break;
					}
					zero += inputDataMotion.velocity;
					num3++;
				}
			}
			if (num3 == 0)
			{
				return Vector3.zero;
			}
			return zero / num3;
		}
	}

	public GamePlayer gamePlayer;

	private HandData[] hands;

	public const int MAX_INPUT_HISTORY = 32;

	private InputData[] inputData;

	[OnEnterPlay_SetNull]
	public static volatile GamePlayerLocal instance;

	[NonSerialized]
	public GameEntityManager currGameEntityManager;

	private void Awake()
	{
		instance = this;
		hands = new HandData[2];
		inputData = new InputData[2];
		for (int i = 0; i < inputData.Length; i++)
		{
			inputData[i] = new InputData(32);
		}
		RoomSystem.JoinedRoomEvent += new Action(OnJoinRoom);
	}

	private void OnJoinRoom()
	{
		gamePlayer.MigrateHeldActorNumbers();
	}

	public void OnUpdateInteract()
	{
		for (int i = 0; i < inputData.Length; i++)
		{
			UpdateInput(i);
		}
		for (int j = 0; j < hands.Length; j++)
		{
			UpdateHand(currGameEntityManager, j);
		}
	}

	private void UpdateInput(int handIndex)
	{
		XRNode xRNode = GetXRNode(handIndex);
		InputDataMotion data = default(InputDataMotion);
		data.position = ControllerInputPoller.DevicePosition(xRNode);
		data.rotation = ControllerInputPoller.DeviceRotation(xRNode);
		data.velocity = ControllerInputPoller.DeviceVelocity(xRNode);
		data.angVelocity = ControllerInputPoller.DeviceAngularVelocity(xRNode);
		data.time = Time.timeAsDouble;
		inputData[handIndex].AddInput(data);
	}

	private void UpdateHand(GameEntityManager emptyHandManager, int handIndex)
	{
		if (!gamePlayer.GetGrabbedGameEntityIdAndManager(handIndex, out var manager).IsValid())
		{
			UpdateHandEmpty(emptyHandManager, handIndex);
		}
		else
		{
			UpdateHandHolding(manager, handIndex);
		}
	}

	public void MigrateToEntityManager(GameEntityManager newEntityManager)
	{
		if (!(currGameEntityManager == newEntityManager))
		{
			if (newEntityManager.IsAuthority())
			{
				gamePlayer.MigrateToEntityManager(newEntityManager);
			}
			currGameEntityManager = newEntityManager;
		}
	}

	public void SetGrabbed(GameEntityId gameBallId, int handIndex)
	{
		HandData handData = hands[handIndex];
		handData.gripPressedTime = (gameBallId.IsValid() ? 0.0 : handData.gripPressedTime);
		handData.grabbedGameBallId = gameBallId;
		hands[handIndex] = handData;
		switch (handIndex)
		{
		case 0:
			EquipmentInteractor.instance.disableLeftGrab = gameBallId.IsValid();
			break;
		case 1:
			EquipmentInteractor.instance.disableRightGrab = gameBallId.IsValid();
			break;
		}
	}

	public void ClearGrabbedIfHeld(GameEntityId gameBallId)
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
		SetGrabbed(GameEntityId.Invalid, handIndex);
	}

	private void UpdateStuckState()
	{
		bool disableMovement = false;
		for (int i = 0; i < hands.Length; i++)
		{
			if (gamePlayer.GetGrabbedGameEntityId(i).IsValid())
			{
				disableMovement = true;
				break;
			}
		}
		GTPlayer.Instance.disableMovement = disableMovement;
	}

	private void UpdateHandEmpty(GameEntityManager gameEntityManager, int handIndex)
	{
		if (gamePlayer.IsGrabbingDisabled() || gameEntityManager == null)
		{
			return;
		}
		HandData handData = hands[handIndex];
		bool flag = ((!IsLeftHand(handIndex)) ? (EquipmentInteractor.instance.isRightGrabbing && ControllerInputPoller.GetGrab(XRNode.RightHand)) : (EquipmentInteractor.instance.isLeftGrabbing && ControllerInputPoller.GetGrab(XRNode.LeftHand)));
		double timeAsDouble = Time.timeAsDouble;
		if (flag && !handData.gripWasHeld)
		{
			handData.gripPressedTime = timeAsDouble;
		}
		double num = timeAsDouble - handData.gripPressedTime;
		handData.gripWasHeld = flag;
		hands[handIndex] = handData;
		if (!flag || !(num < 0.15000000596046448))
		{
			return;
		}
		Transform handTransform = GetHandTransform(handIndex);
		Vector3 position = handTransform.position;
		Transform fingerTransform = GetFingerTransform(handIndex);
		position = Vector3.Lerp(position, fingerTransform.position, 0.5f);
		Vector3 closestPointOnBoundingBox = position;
		Quaternion rotation = handTransform.rotation;
		bool isLeftHand = IsLeftHand(handIndex);
		GameEntityId gameEntityId = gameEntityManager.TryGrabLocal(position, isLeftHand, out closestPointOnBoundingBox);
		if (gameEntityId.IsValid())
		{
			Transform handTransform2 = GetHandTransform(handIndex);
			GameEntity gameEntity = gameEntityManager.GetGameEntity(gameEntityId);
			Vector3 position2 = gameEntity.transform.position + (position - closestPointOnBoundingBox);
			Quaternion rotation2 = gameEntity.transform.rotation;
			GameGrabbable component = gameEntity.GetComponent<GameGrabbable>();
			if ((bool)component && component.GetBestGrabPoint(position, rotation, handIndex, out var grab))
			{
				position2 = grab.position;
				rotation2 = grab.rotation;
			}
			Vector3 localPosition = handTransform2.InverseTransformPoint(position2);
			Quaternion localRotation = Quaternion.Inverse(handTransform2.rotation) * rotation2;
			gameEntityManager.RequestGrabEntity(gameEntityId, isLeftHand, localPosition, localRotation);
		}
	}

	private void UpdateHandHolding(GameEntityManager gameEntityManager, int handIndex)
	{
		if (gameEntityManager == null)
		{
			return;
		}
		XRNode xRNode = GetXRNode(handIndex);
		bool grab;
		if (!IsLeftHand(handIndex))
		{
			if (EquipmentInteractor.instance.isRightGrabbing)
			{
				grab = ControllerInputPoller.GetGrab(XRNode.RightHand);
				goto IL_004a;
			}
		}
		else if (EquipmentInteractor.instance.isLeftGrabbing)
		{
			grab = ControllerInputPoller.GetGrab(XRNode.LeftHand);
			goto IL_004a;
		}
		goto IL_004f;
		IL_004f:
		GameEntityId grabbedGameEntityId = gamePlayer.GetGrabbedGameEntityId(handIndex);
		GameEntity gameEntity = gameEntityManager.GetGameEntity(grabbedGameEntityId);
		GameSnappable component = gameEntity.GetComponent<GameSnappable>();
		if (component != null)
		{
			SuperInfectionSnapPoint superInfectionSnapPoint = component.BestSnapPoint();
			if (superInfectionSnapPoint != null)
			{
				gameEntityManager.RequestSnapEntity(grabbedGameEntityId, IsLeftHand(handIndex), superInfectionSnapPoint.jointType);
				return;
			}
		}
		GameDockable component2 = gameEntity.GetComponent<GameDockable>();
		if (component2 != null)
		{
			GameEntityId gameEntityId = component2.BestDock();
			if (gameEntityId != GameEntityId.Invalid)
			{
				Transform dockablePoint = component2.GetDockablePoint();
				Quaternion quaternion = Quaternion.Inverse(Quaternion.Inverse(component2.transform.rotation) * dockablePoint.rotation);
				Vector3 vector = quaternion * -component2.transform.InverseTransformPoint(dockablePoint.position);
				GameEntity gameEntity2 = gameEntityManager.GetGameEntity(gameEntityId);
				if (gameEntity2 != null)
				{
					GameDock component3 = gameEntity2.GetComponent<GameDock>();
					if (component3 != null)
					{
						Transform dockMarker = component3.dockMarker;
						Vector3 position = dockMarker.transform.TransformPoint(vector);
						vector = gameEntity2.transform.InverseTransformPoint(position);
						Quaternion quaternion2 = dockMarker.rotation * quaternion;
						quaternion = Quaternion.Inverse(gameEntity2.transform.rotation) * quaternion2;
					}
				}
				gameEntityManager.RequestAttachEntity(grabbedGameEntityId, gameEntityId, 0, vector, quaternion);
				return;
			}
		}
		Vector3 vector2 = ControllerInputPoller.DeviceAngularVelocity(xRNode);
		Quaternion quaternion3 = ControllerInputPoller.DeviceRotation(xRNode);
		Quaternion handRotOffset = GTPlayer.Instance.GetHandRotOffset(IsLeftHand(handIndex));
		Transform transform = GorillaTagger.Instance.offlineVRRig.transform;
		Quaternion rotation = GTPlayer.Instance.turnParent.transform.rotation;
		InputData inputData = this.inputData[handIndex];
		Vector3 vector3 = inputData.GetMaxSpeed(0f, 0.05f) * inputData.GetAvgVel(0f, 0.05f).normalized;
		vector3 = rotation * vector3;
		vector3 *= transform.localScale.x;
		vector2 = rotation * quaternion3 * handRotOffset * vector2;
		gamePlayer.GetGrabbedGameEntityId(handIndex);
		GorillaVelocityTracker bodyVelocityTracker = GTPlayer.Instance.bodyVelocityTracker;
		vector3 += bodyVelocityTracker.GetAverageVelocity(worldSpace: true, 0.05f);
		gameEntityManager.RequestThrowEntity(grabbedGameEntityId, IsLeftHand(handIndex), GTPlayer.Instance.HeadCenterPosition, vector3, vector2);
		return;
		IL_004a:
		if (grab)
		{
			return;
		}
		goto IL_004f;
	}

	private XRNode GetXRNode(int handIndex)
	{
		if (handIndex != 0)
		{
			return XRNode.RightHand;
		}
		return XRNode.LeftHand;
	}

	private Transform GetHandTransform(int handIndex)
	{
		return GamePlayer.GetHandTransform(GorillaTagger.Instance.offlineVRRig, handIndex);
	}

	private Transform GetFingerTransform(int handIndex)
	{
		GorillaTagger gorillaTagger = GorillaTagger.Instance;
		return handIndex switch
		{
			0 => gorillaTagger.leftHandTriggerCollider.transform, 
			1 => gorillaTagger.rightHandTriggerCollider.transform, 
			_ => null, 
		};
	}

	public Vector3 GetHandVelocity(int handIndex)
	{
		Quaternion rotation = GTPlayer.Instance.turnParent.transform.rotation;
		InputData inputData = this.inputData[handIndex];
		Vector3 vector = inputData.GetMaxSpeed(0f, 0.05f) * inputData.GetAvgVel(0f, 0.05f).normalized;
		vector = rotation * vector;
		return vector * base.transform.localScale.x;
	}

	public Vector3 GetHandAngularVelocity(int handIndex)
	{
		int node = ((handIndex == 0) ? 4 : 5);
		Quaternion rotation = GTPlayer.Instance.turnParent.transform.rotation;
		Quaternion rotation2 = ControllerInputPoller.DeviceRotation((XRNode)node);
		Vector3 vector = ControllerInputPoller.DeviceAngularVelocity((XRNode)node);
		return rotation * -(Quaternion.Inverse(rotation2) * vector);
	}

	public float GetHandSpeed(int handIndex)
	{
		return inputData[handIndex].GetMaxSpeed(0f, 0.05f);
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

	public static bool IsHandHolding(int handIndex)
	{
		GameEntityManager manager;
		return instance.gamePlayer.GetGrabbedGameEntityIdAndManager(handIndex, out manager).IsValid();
	}

	public static bool IsHandHolding(XRNode xrNode)
	{
		return IsHandHolding((xrNode != XRNode.LeftHand) ? 1 : 0);
	}

	public void PlayCatchFx(bool isLeftHand)
	{
		GorillaTagger.Instance.StartVibration(isLeftHand, GorillaTagger.Instance.tapHapticStrength, 0.1f);
	}

	public void PlayThrowFx(bool isLeftHand)
	{
		GorillaTagger.Instance.StartVibration(isLeftHand, GorillaTagger.Instance.tapHapticStrength * 0.15f, 0.1f);
	}
}
