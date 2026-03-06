using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Cysharp.Text;
using GorillaLocomotion;
using GorillaLocomotion.Climbing;
using UnityEngine;
using UnityEngine.XR;

public class GamePlayerLocal : MonoBehaviour, IDelayedExecListener
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

		public bool triggerWasHeld;

		public double gripPressedTime;

		public double triggerPressedTime;
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

	public struct SlotRecoveryData
	{
		public int entityTypeId;

		public long createData;
	}

	public struct GrabSlotExtraRecoveryData
	{
		public Vector3 pos;

		public Quaternion rot;
	}

	private const string preLog = "[GamePlayerLocal]  ";

	private const string preErr = "[GamePlayerLocal]  ERROR!!!  ";

	public GamePlayer gamePlayer;

	private HandData[] hands;

	public const int MAX_INPUT_HISTORY = 32;

	private InputData[] inputData;

	private const string SNAP_SLOTS_SAVE_KEY = "GT_SnappedItems_V1";

	private const float SNAP_SLOTS_SAVE__INTERVAL = 2f;

	[OnEnterPlay_Set(false)]
	private static bool snapSlotsSave_isQueued;

	[OnEnterPlay_Set(0)]
	private static int snapSlotsSave_lastSavedHash;

	[OnEnterPlay_Set(0)]
	private static int snapSlotsSave_frameWhenQueued;

	[OnEnterPlay_Set(0f)]
	private static float snapSlotsSave_lastTime;

	private static readonly SlotRecoveryData[] slotsRecoveryData = new SlotRecoveryData[4];

	private static readonly GrabSlotExtraRecoveryData[] grabSlotsExtraRecoveryData = new GrabSlotExtraRecoveryData[2];

	[OnEnterPlay_SetNull]
	public static volatile GamePlayerLocal instance;

	[NonSerialized]
	public GameEntityManager currGameEntityManager;

	private static readonly List<GameEntityCreateData> _migrationRecoveryList = new List<GameEntityCreateData>(4);

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
		_LoadSnappedPlayerPrefsToCache(gamePlayer);
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
		InputDataMotion data = new InputDataMotion
		{
			position = ControllerInputPoller.DevicePosition(xRNode),
			rotation = ControllerInputPoller.DeviceRotation(xRNode),
			velocity = ControllerInputPoller.DeviceVelocity(xRNode),
			angVelocity = ControllerInputPoller.DeviceAngularVelocity(xRNode),
			time = Time.timeAsDouble
		};
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
			if (TryGetMigrationRecoveryList(newEntityManager, out var out_recoveryList))
			{
				currGameEntityManager.RequestMigrationRecovery(out_recoveryList);
			}
		}
	}

	public void SetGrabbed(GameEntityId gameBallId, int handIndex)
	{
		HandData handData = hands[handIndex];
		handData.gripPressedTime = (gameBallId.IsValid() ? 0.0 : handData.gripPressedTime);
		hands[handIndex] = handData;
		if (handIndex == 0)
		{
			EquipmentInteractor.instance.disableLeftGrab = gameBallId.IsValid();
		}
		else
		{
			EquipmentInteractor.instance.disableRightGrab = gameBallId.IsValid();
		}
	}

	public void ClearGrabbedIfHeld(GameEntityId gameBallId)
	{
		for (int i = 0; i <= 1; i++)
		{
			if (gamePlayer.IsInSlot(i, gameBallId.index))
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
		bool flag = GamePlayer.IsLeftHand(handIndex);
		bool flag2 = ((!flag) ? (EquipmentInteractor.instance.isRightGrabbing && ControllerInputPoller.GetGrab(XRNode.RightHand)) : (EquipmentInteractor.instance.isLeftGrabbing && ControllerInputPoller.GetGrab(XRNode.LeftHand)));
		double timeAsDouble = Time.timeAsDouble;
		if (flag2 && !handData.gripWasHeld)
		{
			handData.gripPressedTime = timeAsDouble;
		}
		double num = timeAsDouble - handData.gripPressedTime;
		handData.gripWasHeld = flag2;
		bool flag3 = (flag ? ControllerInputPoller.GetIndexPressed(XRNode.LeftHand) : ControllerInputPoller.GetIndexPressed(XRNode.RightHand));
		if (flag3 && !handData.gripWasHeld)
		{
			handData.triggerPressedTime = timeAsDouble;
		}
		double num2 = timeAsDouble - handData.triggerPressedTime;
		handData.triggerWasHeld = flag3;
		hands[handIndex] = handData;
		if (flag2 && num < 0.15000000596046448)
		{
			Transform handTransform = gamePlayer.GetHandTransform(handIndex);
			Vector3 position = handTransform.position;
			Vector3 vector = Vector3.Lerp(position, GetFingerTransform(handIndex).position, 0.5f);
			Vector3 closestPointOnBoundingBox = position;
			Quaternion rotation = handTransform.rotation;
			bool fingerPositionUsed;
			GameEntityId gameEntityId = gameEntityManager.TryGrabLocal(position, vector, flag, out closestPointOnBoundingBox, out fingerPositionUsed);
			if (gameEntityId.IsValid())
			{
				Vector3 vector2 = (fingerPositionUsed ? vector : position);
				GameEntity gameEntity = gameEntityManager.GetGameEntity(gameEntityId);
				Vector3 position2 = gameEntity.transform.position + (vector2 - closestPointOnBoundingBox);
				Quaternion rotation2 = gameEntity.transform.rotation;
				GameGrabbable component = gameEntity.GetComponent<GameGrabbable>();
				if ((bool)component && component.GetBestGrabPoint(position, rotation, handIndex, out var grab))
				{
					position2 = grab.position;
					rotation2 = grab.rotation;
				}
				Vector3 vector3 = handTransform.InverseTransformPoint(position2);
				Quaternion quaternion = Quaternion.Inverse(handTransform.rotation) * rotation2;
				gameEntityManager.RequestGrabEntity(gameEntityId, flag, vector3, quaternion);
				SetGrabSlotRecoveryData(handIndex, gameEntity.typeId, gameEntity.createData, vector3, quaternion);
			}
		}
		if (flag3 && num2 < 0.15000000596046448)
		{
			Vector3 position3 = gamePlayer.GetHandTransform(handIndex).position;
			GameTriggerInteractable gameTriggerInteractable = null;
			float num3 = float.MaxValue;
			for (int i = 0; i < GameTriggerInteractable.LocalInteractableTriggers.Count && !GameTriggerInteractable.LocalInteractableTriggers[i].triggerInteractionActive; i++)
			{
				if (GameTriggerInteractable.LocalInteractableTriggers[i].PointWithinInteractableArea(position3))
				{
					float magnitude = (GameTriggerInteractable.LocalInteractableTriggers[i].interactableCenter.position - position3).magnitude;
					if (!(magnitude > num3))
					{
						num3 = magnitude;
						gameTriggerInteractable = GameTriggerInteractable.LocalInteractableTriggers[i];
					}
				}
			}
			if (gameTriggerInteractable != null)
			{
				gameTriggerInteractable.BeginTriggerInteraction(handIndex);
			}
		}
		if (!flag3)
		{
			ClearTriggerInteractables(handIndex);
		}
	}

	private void UpdateHandHolding(GameEntityManager gameEntityManager, int handIndex)
	{
		if (gameEntityManager == null)
		{
			return;
		}
		XRNode xRNode = GetXRNode(handIndex);
		bool flag = GamePlayer.IsLeftHand(handIndex);
		bool grab;
		if (!flag)
		{
			if (EquipmentInteractor.instance.isRightGrabbing)
			{
				grab = ControllerInputPoller.GetGrab(XRNode.RightHand);
				goto IL_004c;
			}
		}
		else if (EquipmentInteractor.instance.isLeftGrabbing)
		{
			grab = ControllerInputPoller.GetGrab(XRNode.LeftHand);
			goto IL_004c;
		}
		goto IL_0051;
		IL_0051:
		GameEntityId grabbedGameEntityId = gamePlayer.GetGrabbedGameEntityId(handIndex);
		GameEntity gameEntity = gameEntityManager.GetGameEntity(grabbedGameEntityId);
		SetSlotRecoveryData(handIndex, -1, 0L);
		GameSnappable component = gameEntity.GetComponent<GameSnappable>();
		if (component != null)
		{
			SuperInfectionSnapPoint superInfectionSnapPoint = component.BestSnapPoint();
			if (superInfectionSnapPoint != null)
			{
				gameEntityManager.RequestSnapEntity(grabbedGameEntityId, flag, superInfectionSnapPoint.jointType);
				if (GameSnappable.TryGetJointToSnapIndex(superInfectionSnapPoint.jointType, out var out_slot))
				{
					SetSlotRecoveryData(out_slot, gameEntity.typeId, gameEntity.createData);
					SaveSnapSlotsRateLimited();
				}
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
		Quaternion handRotOffset = GTPlayer.Instance.GetHandRotOffset(flag);
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
		gameEntityManager.RequestThrowEntity(grabbedGameEntityId, flag, GTPlayer.Instance.HeadCenterPosition, vector3, vector2);
		goto IL_02c7;
		IL_004c:
		if (!grab)
		{
			goto IL_0051;
		}
		goto IL_02c7;
		IL_02c7:
		ClearTriggerInteractables(handIndex);
	}

	private XRNode GetXRNode(int handIndex)
	{
		if (handIndex != 0)
		{
			return XRNode.RightHand;
		}
		return XRNode.LeftHand;
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

	public static bool IsHandHolding(XRNode xrNode)
	{
		return instance.gamePlayer.IsSlotOccupied((xrNode != XRNode.LeftHand) ? 1 : 0);
	}

	public void PlayCatchFx(bool isLeftHand)
	{
		GorillaTagger.Instance.StartVibration(isLeftHand, GorillaTagger.Instance.tapHapticStrength, 0.1f);
	}

	public void PlayThrowFx(bool isLeftHand)
	{
		GorillaTagger.Instance.StartVibration(isLeftHand, GorillaTagger.Instance.tapHapticStrength * 0.15f, 0.1f);
	}

	public void ClearTriggerInteractables(int handIndex)
	{
		for (int i = 0; i < GameTriggerInteractable.LocalInteractableTriggers.Count; i++)
		{
			if (GameTriggerInteractable.LocalInteractableTriggers[i].triggerInteractionActive && GameTriggerInteractable.LocalInteractableTriggers[i].handIndex == handIndex)
			{
				GameTriggerInteractable.LocalInteractableTriggers[i].EndTriggerInteraction();
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void SetSlotRecoveryData(int slot, int typeId, long createData)
	{
		if (GamePlayer.IsSlot(slot))
		{
			SlotRecoveryData slotRecoveryData = slotsRecoveryData[slot];
			slotRecoveryData.entityTypeId = typeId;
			slotRecoveryData.createData = createData;
			slotsRecoveryData[slot] = slotRecoveryData;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void SetGrabSlotRecoveryData(int slot, int typeId, long createData, Vector3 pos, Quaternion rot)
	{
		if (GamePlayer.IsGrabSlot(slot))
		{
			SetSlotRecoveryData(slot, typeId, createData);
			GrabSlotExtraRecoveryData grabSlotExtraRecoveryData = grabSlotsExtraRecoveryData[slot];
			grabSlotExtraRecoveryData.pos = pos;
			grabSlotExtraRecoveryData.rot = rot;
			grabSlotsExtraRecoveryData[slot] = grabSlotExtraRecoveryData;
		}
	}

	internal static void SaveSnapSlotsRateLimited()
	{
		if (!snapSlotsSave_isQueued)
		{
			if (snapSlotsSave_lastTime + 2f < Time.unscaledTime)
			{
				_SaveSnapSlotsImmediately();
				return;
			}
			snapSlotsSave_isQueued = true;
			GTDelayedExec.Add(instance, 2f, 0);
		}
	}

	void IDelayedExecListener.OnDelayedAction(int contextId)
	{
		_SaveSnapSlotsImmediately();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void _SaveSnapSlotsImmediately()
	{
		snapSlotsSave_isQueued = false;
		snapSlotsSave_lastTime = Time.unscaledTime;
		int num = _SnapSlotsSave_GetHash(slotsRecoveryData);
		if (num == snapSlotsSave_lastSavedHash)
		{
			return;
		}
		snapSlotsSave_lastSavedHash = num;
		using Utf16ValueStringBuilder utf16ValueStringBuilder = ZString.CreateStringBuilder(notNested: true);
		for (int i = 2; i <= 3; i++)
		{
			SlotRecoveryData slotRecoveryData = slotsRecoveryData[i];
			if (slotRecoveryData.entityTypeId != -1)
			{
				utf16ValueStringBuilder.Append(i);
				utf16ValueStringBuilder.Append(",");
				utf16ValueStringBuilder.Append(slotRecoveryData.entityTypeId);
				utf16ValueStringBuilder.Append(",");
				utf16ValueStringBuilder.Append(slotRecoveryData.createData);
				utf16ValueStringBuilder.Append("|");
			}
		}
		Debug.Log("POOP - _SaveSnapSlotsImmediately - sb=" + utf16ValueStringBuilder.ToString() + "\n(keywords: gameentitymanager, gameplayer, save)");
		PlayerPrefs.SetString("GT_SnappedItems_V1", utf16ValueStringBuilder.ToString());
		PlayerPrefs.Save();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void _LoadSnappedPlayerPrefsToCache(GamePlayer gamePlayer)
	{
		for (int i = 0; i < 4; i++)
		{
			slotsRecoveryData[i] = new SlotRecoveryData
			{
				entityTypeId = -1,
				createData = 0L
			};
		}
		for (int j = 0; j < 2; j++)
		{
			grabSlotsExtraRecoveryData[j] = new GrabSlotExtraRecoveryData
			{
				pos = Vector3.zero,
				rot = Quaternion.identity
			};
		}
		string text = PlayerPrefs.GetString("GT_SnappedItems_V1");
		string[] array = text.Split('|', StringSplitOptions.RemoveEmptyEntries);
		Debug.Log("POOP - _LoadSnappedPlayerPrefsToCache - rawString=\"" + text + "\"\n(keywords: gameentitymanager, gameplayer, save)");
		string[] array2 = array;
		for (int k = 0; k < array2.Length; k++)
		{
			string[] array3 = array2[k].Split(',');
			if (array3.Length >= 3 && int.TryParse(array3[0], out var result) && result < 4 && GamePlayer.IsSnapSlot(result) && int.TryParse(array3[1], out var result2) && long.TryParse(array3[2], out var result3))
			{
				slotsRecoveryData[result] = new SlotRecoveryData
				{
					entityTypeId = result2,
					createData = result3
				};
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static int _SnapSlotsSave_GetHash(SlotRecoveryData[] slotsCache)
	{
		int num = 67466746;
		for (int i = 2; i <= 3; i++)
		{
			SlotRecoveryData slotRecoveryData = slotsCache[i];
			num = StaticHash.Compute(num, i.GetStaticHash(), slotRecoveryData.entityTypeId.GetStaticHash(), slotRecoveryData.createData.GetStaticHash());
		}
		return num;
	}

	public static bool TryGetMigrationRecoveryList(GameEntityManager newEntityManager, out List<GameEntityCreateData> out_recoveryList)
	{
		out_recoveryList = _migrationRecoveryList;
		_migrationRecoveryList.Clear();
		GamePlayer gamePlayer = instance.gamePlayer;
		for (int i = 0; i < 4; i++)
		{
			SlotRecoveryData slotRecoveryData = slotsRecoveryData[i];
			int entityTypeId = slotRecoveryData.entityTypeId;
			bool flag = entityTypeId != -1;
			GamePlayer.SlotData out_slotData;
			bool flag2 = gamePlayer.TryGetSlotData(i, out out_slotData);
			if (!flag && !flag2)
			{
				continue;
			}
			bool flag3 = newEntityManager != null && newEntityManager == out_slotData.entityManager;
			GameEntity gameEntity = (flag3 ? newEntityManager.GetGameEntity(out_slotData.entityId) : null);
			bool flag4 = gameEntity != null;
			int num = (flag4 ? gameEntity.typeId : (-1));
			bool flag5 = num != -1;
			bool flag6 = entityTypeId == num;
			if (!(flag3 && flag4 && flag6))
			{
				string message = (flag ? "[GamePlayerLocal]  TryGetMigrationRecoveryList: Recovering from mismatch between migrated entities and recovery data." : "[GamePlayerLocal]  ERROR!!!  TryGetMigrationRecoveryList: UNRECOVERABLE mismatch between migrated entities and recovery data.");
				Debug.unityLogger.Log(flag ? LogType.Log : LogType.Error, message);
				if (flag)
				{
					_migrationRecoveryList.Add(new GameEntityCreateData
					{
						entityTypeId = entityTypeId,
						position = (GamePlayer.IsGrabSlot(i) ? grabSlotsExtraRecoveryData[i].pos : Vector3.zero),
						rotation = (GamePlayer.IsGrabSlot(i) ? grabSlotsExtraRecoveryData[i].rot : Quaternion.identity),
						createData = slotRecoveryData.createData,
						createdByEntityId = -1,
						slotIndex = i
					});
				}
			}
		}
		return _migrationRecoveryList.Count > 0;
	}
}
