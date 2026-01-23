using System.Collections.Generic;
using GorillaLocomotion;
using GorillaLocomotion.Climbing;
using UnityEngine;
using UnityEngine.XR;

public class GameBallPlayerLocal : MonoBehaviour
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

		public GameBallId grabbedGameBallId;
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

	public GameBallPlayer gamePlayer;

	private const int MAX_INPUT_HISTORY = 32;

	private HandData[] hands;

	private InputData[] inputData;

	[OnEnterPlay_SetNull]
	public static volatile GameBallPlayerLocal instance;

	private void Awake()
	{
		instance = this;
		hands = new HandData[2];
		inputData = new InputData[2];
		for (int i = 0; i < inputData.Length; i++)
		{
			inputData[i] = new InputData(32);
		}
		Application.quitting += _OnApplicationQuit;
	}

	private static void _OnApplicationQuit()
	{
		if (MonkeBallGame.Instance != null)
		{
			MonkeBallGame.Instance.OnPlayerDestroy();
		}
	}

	private void OnApplicationPause(bool pause)
	{
		if (pause && MonkeBallGame.Instance != null)
		{
			MonkeBallGame.Instance.OnPlayerDestroy();
		}
	}

	private void OnDestroy()
	{
		if (!ApplicationQuittingState.IsQuitting && MonkeBallGame.Instance != null)
		{
			MonkeBallGame.Instance.OnPlayerDestroy();
		}
	}

	public void OnUpdateInteract()
	{
		if (ZoneManagement.IsInZone(GTZone.arena))
		{
			for (int i = 0; i < inputData.Length; i++)
			{
				UpdateInput(i);
			}
			for (int j = 0; j < hands.Length; j++)
			{
				UpdateHand(j);
			}
		}
	}

	private void UpdateInput(int handIndex)
	{
		XRNode xRNode = GetXRNode(handIndex);
		InputDataMotion data = default(InputDataMotion);
		InputDevice deviceAtXRNode = InputDevices.GetDeviceAtXRNode(xRNode);
		deviceAtXRNode.TryGetFeatureValue(CommonUsages.devicePosition, out data.position);
		deviceAtXRNode.TryGetFeatureValue(CommonUsages.deviceRotation, out data.rotation);
		deviceAtXRNode.TryGetFeatureValue(CommonUsages.deviceVelocity, out data.velocity);
		deviceAtXRNode.TryGetFeatureValue(CommonUsages.deviceAngularVelocity, out data.angVelocity);
		data.time = Time.timeAsDouble;
		inputData[handIndex].AddInput(data);
	}

	private void UpdateHand(int handIndex)
	{
		if (!(GameBallManager.Instance == null))
		{
			if (!gamePlayer.GetGameBallId(handIndex).IsValid())
			{
				UpdateHandEmpty(handIndex);
			}
			else
			{
				UpdateHandHolding(handIndex);
			}
		}
	}

	public void SetGrabbed(GameBallId gameBallId, int handIndex)
	{
		HandData handData = hands[handIndex];
		handData.gripPressedTime = 0.0;
		hands[handIndex] = handData;
		UpdateStuckState();
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

	private void UpdateStuckState()
	{
		bool disableMovement = false;
		for (int i = 0; i < hands.Length; i++)
		{
			if (gamePlayer.GetGameBallId(i).IsValid())
			{
				disableMovement = true;
				break;
			}
		}
		GTPlayer.Instance.disableMovement = disableMovement;
	}

	private void UpdateHandEmpty(int handIndex)
	{
		HandData handData = hands[handIndex];
		bool flag = ControllerInputPoller.GripFloat(GetXRNode(handIndex)) > 0.7f;
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
		Vector3 position = GetHandTransform(handIndex).position;
		GameBallId gameBallId = GameBallManager.Instance.TryGrabLocal(position, gamePlayer.teamId);
		float num2 = 0.15f;
		if (gameBallId.IsValid())
		{
			bool flag2 = IsLeftHand(handIndex);
			BodyDockPositions myBodyDockPositions = GorillaTagger.Instance.offlineVRRig.myBodyDockPositions;
			Transform obj = (flag2 ? myBodyDockPositions.leftHandTransform : myBodyDockPositions.rightHandTransform);
			GameBall gameBall = GameBallManager.Instance.GetGameBall(gameBallId);
			Vector3 position2 = gameBall.transform.position;
			Vector3 vector = gameBall.transform.position - position;
			if (vector.sqrMagnitude > num2 * num2)
			{
				position2 = position + vector.normalized * num2;
			}
			Vector3 localPosition = obj.InverseTransformPoint(position2);
			Quaternion localRotation = Quaternion.Inverse(obj.rotation) * gameBall.transform.rotation;
			obj.InverseTransformPoint(gameBall.transform.position);
			GameBallManager.Instance.RequestGrabBall(gameBallId, flag2, localPosition, localRotation);
		}
	}

	private void UpdateHandHolding(int handIndex)
	{
		XRNode xRNode = GetXRNode(handIndex);
		if (ControllerInputPoller.GripFloat(xRNode) > 0.7f)
		{
			return;
		}
		InputDevice deviceAtXRNode = InputDevices.GetDeviceAtXRNode(xRNode);
		deviceAtXRNode.TryGetFeatureValue(CommonUsages.deviceAngularVelocity, out var value);
		deviceAtXRNode.TryGetFeatureValue(CommonUsages.deviceRotation, out var value2);
		Transform transform = GorillaTagger.Instance.offlineVRRig.transform;
		Quaternion rotation = GTPlayer.Instance.turnParent.transform.rotation;
		InputData inputData = this.inputData[handIndex];
		Vector3 vector = inputData.GetMaxSpeed(0f, 0.05f) * inputData.GetAvgVel(0f, 0.05f).normalized;
		vector = rotation * vector;
		vector *= transform.localScale.x;
		value = rotation * -(Quaternion.Inverse(value2) * value);
		GameBallId gameBallId = gamePlayer.GetGameBallId(handIndex);
		GameBall gameBall = GameBallManager.Instance.GetGameBall(gameBallId);
		if (!(gameBall == null) && !gameBall.IsLaunched)
		{
			if (gameBall.disc)
			{
				Vector3 vector2 = gameBall.transform.rotation * gameBall.localDiscUp;
				vector2.Normalize();
				float num = Vector3.Dot(vector2, value);
				value = vector2 * num;
				value *= 1.25f;
				vector *= 1.25f;
			}
			else
			{
				vector *= 1.5f;
			}
			GorillaVelocityTracker bodyVelocityTracker = GTPlayer.Instance.bodyVelocityTracker;
			vector += bodyVelocityTracker.GetAverageVelocity(worldSpace: true, 0.05f);
			GameBallManager.Instance.RequestThrowBall(gameBallId, IsLeftHand(handIndex), vector, value);
		}
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
		BodyDockPositions myBodyDockPositions = GorillaTagger.Instance.offlineVRRig.myBodyDockPositions;
		return ((handIndex == 0) ? myBodyDockPositions.leftHandTransform : myBodyDockPositions.rightHandTransform).parent;
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

	public void PlayCatchFx(bool isLeftHand)
	{
		GorillaTagger.Instance.StartVibration(isLeftHand, GorillaTagger.Instance.tapHapticStrength, 0.1f);
	}

	public void PlayThrowFx(bool isLeftHand)
	{
		GorillaTagger.Instance.StartVibration(isLeftHand, GorillaTagger.Instance.tapHapticStrength * 0.15f, 0.1f);
	}
}
