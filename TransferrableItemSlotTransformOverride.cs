using System;
using System.Collections.Generic;
using System.Linq;
using GorillaTag;
using GorillaTag.CosmeticSystem;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Serialization;

public class TransferrableItemSlotTransformOverride : MonoBehaviour, IGorillaSliceableSimple, ISpawnable
{
	[FormerlySerializedAs("transformOverridesList")]
	public List<SlotTransformOverride> transformOverridesDeprecated;

	[SerializeReference]
	public List<SlotTransformOverride> transformOverrides;

	private TransferrableObject.PositionState lastPosition;

	[Tooltip("(2024-08-20 MattO) For cosmetics this is almost always assigned to the TransferrableObject component in the same prefab and almost always belonging to the same gameobject as this Component.")]
	public TransferrableObject followingTransferrableObject;

	[Tooltip("(2024-08-20 MattO) This is filled in automatically by the cosmetic spawner.")]
	public SlotTransformOverride defaultPosition;

	[Obsolete("(2024-08-2024) This used to be assigned to `defaultPosition.overrideTransform` before, but was there ever an instance where it wasn't null? Keeping it serialized just in case there is a reason for it.")]
	public Transform defaultTransform;

	public Transform anchor;

	public Dictionary<TransferrableObject.PositionState, Transform> transformFromPosition;

	public static Action<TransferrableObject> OnBringUpWindow;

	bool ISpawnable.IsSpawned { get; set; }

	ECosmeticSelectSide ISpawnable.CosmeticSelectedSide { get; set; }

	void ISpawnable.OnSpawn(VRRig rig)
	{
		defaultPosition = new SlotTransformOverride
		{
			positionState = TransferrableObject.PositionState.None
		};
		lastPosition = TransferrableObject.PositionState.None;
		foreach (SlotTransformOverride item in transformOverridesDeprecated)
		{
			item.Initialize(this, anchor);
		}
	}

	void ISpawnable.OnDespawn()
	{
	}

	public void AddGripPosition(TransferrableObject.PositionState state, TransferrableObjectGripPosition togp)
	{
		foreach (SlotTransformOverride item in transformOverridesDeprecated)
		{
			if (item.positionState == state)
			{
				item.AddSubGrabPoint(togp);
				return;
			}
		}
		SlotTransformOverride slotTransformOverride = new SlotTransformOverride
		{
			positionState = state
		};
		transformOverridesDeprecated.Add(slotTransformOverride);
		slotTransformOverride.AddSubGrabPoint(togp);
	}

	public void OnEnable()
	{
		GorillaSlicerSimpleManager.RegisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.Update);
	}

	public void OnDisable()
	{
		GorillaSlicerSimpleManager.UnregisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.Update);
	}

	public void SliceUpdate()
	{
		if (followingTransferrableObject == null)
		{
			return;
		}
		if (followingTransferrableObject.currentState != lastPosition)
		{
			SlotTransformOverride slotTransformOverride = transformOverridesDeprecated.Find((SlotTransformOverride x) => (x.positionState & followingTransferrableObject.currentState) != 0);
			if (slotTransformOverride != null && slotTransformOverride.positionState == TransferrableObject.PositionState.None)
			{
				slotTransformOverride = defaultPosition;
			}
		}
		lastPosition = followingTransferrableObject.currentState;
	}

	private void Awake()
	{
		GenerateTransformFromPositionState();
	}

	public void GenerateTransformFromPositionState()
	{
		transformFromPosition = new Dictionary<TransferrableObject.PositionState, Transform>();
		if (transformOverridesDeprecated.Count > 0)
		{
			transformFromPosition[TransferrableObject.PositionState.None] = transformOverridesDeprecated[0].overrideTransform;
		}
		foreach (TransferrableObject.PositionState item in Enum.GetValues(typeof(TransferrableObject.PositionState)).Cast<TransferrableObject.PositionState>())
		{
			if (item == TransferrableObject.PositionState.None)
			{
				transformFromPosition[item] = null;
				continue;
			}
			Transform value = null;
			foreach (SlotTransformOverride item2 in transformOverridesDeprecated)
			{
				if ((item2.positionState & item) != TransferrableObject.PositionState.None)
				{
					value = item2.overrideTransform;
					break;
				}
			}
			transformFromPosition[item] = value;
		}
	}

	[CanBeNull]
	public Transform GetTransformFromPositionState(TransferrableObject.PositionState currentState)
	{
		if (transformFromPosition == null)
		{
			GenerateTransformFromPositionState();
		}
		return transformFromPosition[currentState];
	}

	public bool GetTransformFromPositionState(TransferrableObject.PositionState currentState, AdvancedItemState advancedItemState, Transform targetDockXf, out Matrix4x4 matrix4X4)
	{
		if (currentState == TransferrableObject.PositionState.None)
		{
			if (transformOverridesDeprecated.Count > 0)
			{
				matrix4X4 = transformOverridesDeprecated[0].overrideTransformMatrix;
				return true;
			}
			matrix4X4 = Matrix4x4.identity;
			return false;
		}
		foreach (SlotTransformOverride item in transformOverridesDeprecated)
		{
			if ((item.positionState & currentState) == 0)
			{
				continue;
			}
			if (item.useAdvancedGrab)
			{
				if (advancedItemState.index >= item.multiPoints.Count)
				{
					matrix4X4 = item.overrideTransformMatrix;
					return true;
				}
				SubGrabPoint subGrabPoint = item.multiPoints[advancedItemState.index];
				matrix4X4 = subGrabPoint.GetTransformFromPositionState(advancedItemState, item, targetDockXf);
				return true;
			}
			matrix4X4 = item.overrideTransformMatrix;
			return true;
		}
		matrix4X4 = Matrix4x4.identity;
		return false;
	}

	public AdvancedItemState GetAdvancedItemStateFromHand(TransferrableObject.PositionState currentState, Transform handTransform, Transform targetDock)
	{
		foreach (SlotTransformOverride item in transformOverridesDeprecated)
		{
			if ((item.positionState & currentState) == 0 || item.multiPoints.Count == 0)
			{
				continue;
			}
			SubGrabPoint subGrabPoint = item.multiPoints[0];
			float num = float.PositiveInfinity;
			int index = -1;
			for (int i = 0; i < item.multiPoints.Count; i++)
			{
				SubGrabPoint subGrabPoint2 = item.multiPoints[i];
				if (!(subGrabPoint2.gripPoint == null))
				{
					float num2 = subGrabPoint2.EvaluateScore(base.transform, handTransform, targetDock);
					if (num2 < num)
					{
						subGrabPoint = subGrabPoint2;
						num = num2;
						index = i;
					}
				}
			}
			AdvancedItemState advancedItemStateFromHand = subGrabPoint.GetAdvancedItemStateFromHand(base.transform, handTransform, targetDock, item);
			advancedItemStateFromHand.index = index;
			return advancedItemStateFromHand;
		}
		return new AdvancedItemState();
	}

	public void Edit()
	{
		if (OnBringUpWindow != null)
		{
			OnBringUpWindow(GetComponent<TransferrableObject>());
		}
	}
}
