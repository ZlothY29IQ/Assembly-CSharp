using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class GameSnappable : MonoBehaviour
{
	[Serializable]
	public struct SnapJointOffset
	{
		public SnapJointType jointType;

		public Vector3 positionOffset;

		public Vector3 rotationOffset;
	}

	public GameEntity gameEntity;

	public float snapRadius = 0.15f;

	public SuperInfectionSnapPoint snappedToJoint;

	public AbilitySound snapSound;

	public AbilitySound unsnapSound;

	public AbilityHaptic snapHaptic;

	public SnapJointType snapLocationTypes;

	public List<SnapJointOffset> snapOffsets;

	private void Awake()
	{
	}

	public void GetSnapOffset(SnapJointType jointType, out Vector3 positionOffset, out Quaternion rotationOffset)
	{
		foreach (SnapJointOffset snapOffset in snapOffsets)
		{
			if ((snapOffset.jointType & jointType) != SnapJointType.None)
			{
				positionOffset = snapOffset.positionOffset;
				rotationOffset = Quaternion.Euler(snapOffset.rotationOffset);
				return;
			}
		}
		positionOffset = Vector3.zero;
		rotationOffset = Quaternion.identity;
	}

	public SuperInfectionSnapPoint BestSnapPoint()
	{
		int heldByHandIndex = gameEntity.heldByHandIndex;
		if (heldByHandIndex < 0)
		{
			return null;
		}
		bool num = GamePlayer.IsLeftHand(heldByHandIndex);
		SnapJointType snapJointType = (num ? SnapJointType.HandL : SnapJointType.HandR);
		SnapJointType snapJointType2 = (num ? SnapJointType.ForearmL : SnapJointType.ForearmR);
		List<SuperInfectionSnapPoint> snapPoints = GamePlayerLocal.instance.gamePlayer.snapPointManager.SnapPoints;
		float num2 = float.MaxValue;
		int num3 = -1;
		for (int i = 0; i < snapPoints.Count; i++)
		{
			if (snapPoints[i].jointType != snapJointType && snapPoints[i].jointType != snapJointType2 && (snapPoints[i].jointType & snapLocationTypes) != SnapJointType.None && !snapPoints[i].HasSnapped())
			{
				GetSnapOffset(snapPoints[i].jointType, out var positionOffset, out var rotationOffset);
				float num4 = Vector3.Distance(snapPoints[i].transform.TransformPoint(rotationOffset * positionOffset), base.transform.position);
				float num5 = snapRadius + snapPoints[i].snapPointRadius;
				if (num4 < num2 && num4 < num5)
				{
					num3 = i;
					num2 = num4;
				}
			}
		}
		if (num3 >= 0)
		{
			return snapPoints[num3];
		}
		if ((snapLocationTypes & SnapJointType.Holster) != SnapJointType.None)
		{
			IEnumerable<SuperInfectionSnapPoint> points = (GamePlayerLocal.instance.currGameEntityManager?.superInfectionManager).GetPoints(SnapJointType.Holster);
			SuperInfectionSnapPoint superInfectionSnapPoint = null;
			float num6 = snapRadius;
			foreach (SuperInfectionSnapPoint item in points)
			{
				if (!item.HasSnapped())
				{
					GetSnapOffset(item.jointType, out var positionOffset2, out var rotationOffset2);
					float num7 = Vector3.Distance(item.transform.TransformPoint(rotationOffset2 * positionOffset2), base.transform.position);
					if (num7 < num6)
					{
						superInfectionSnapPoint = item;
						num6 = num7;
					}
				}
			}
			if (superInfectionSnapPoint != null)
			{
				return superInfectionSnapPoint;
			}
		}
		return null;
	}

	public GameEntityId BestSnapPointDock()
	{
		int heldByHandIndex = gameEntity.heldByHandIndex;
		if (heldByHandIndex < 0)
		{
			return GameEntityId.Invalid;
		}
		SnapJointType snapJointType = (GamePlayer.IsLeftHand(heldByHandIndex) ? SnapJointType.HandL : SnapJointType.HandR);
		SnapJointType snapJointType2 = (GamePlayer.IsLeftHand(heldByHandIndex) ? SnapJointType.ForearmL : SnapJointType.ForearmR);
		List<SuperInfectionSnapPoint> snapPoints = GamePlayerLocal.instance.gamePlayer.snapPointManager.SnapPoints;
		float num = float.MaxValue;
		int num2 = -1;
		for (int i = 0; i < snapPoints.Count; i++)
		{
			if (snapPoints[i].jointType != snapJointType && snapPoints[i].jointType != snapJointType2 && (snapPoints[i].jointType & snapLocationTypes) != SnapJointType.None && snapPoints[i].HasSnapped())
			{
				GetSnapOffset(snapPoints[i].jointType, out var positionOffset, out var rotationOffset);
				float num3 = Vector3.Distance(snapPoints[i].transform.TransformPoint(rotationOffset * positionOffset), base.transform.position);
				float num4 = snapRadius + snapPoints[i].snapPointRadius;
				if (num3 < num && num3 < num4)
				{
					num2 = i;
					num = num3;
				}
			}
		}
		if (num2 < 0)
		{
			return GameEntityId.Invalid;
		}
		return snapPoints[num2].GetSnappedEntity().id;
	}

	public bool CanGrabWithHand(bool leftHand)
	{
		if (snappedToJoint == null)
		{
			return true;
		}
		SnapJointType jointType = snappedToJoint.jointType;
		if (!leftHand || jointType == SnapJointType.HandL || jointType == SnapJointType.ForearmL)
		{
			if (!leftHand && jointType != SnapJointType.HandR)
			{
				return jointType != SnapJointType.ForearmR;
			}
			return false;
		}
		return true;
	}

	public void OnSnap()
	{
		snapSound.Play(null);
		snapHaptic.PlayIfSnappedLocal(gameEntity);
	}

	public bool IsSnappedToLeftArm()
	{
		if (snappedToJoint == null)
		{
			return false;
		}
		SnapJointType jointType = snappedToJoint.jointType;
		if (jointType != SnapJointType.HandL)
		{
			return jointType == SnapJointType.ForearmL;
		}
		return true;
	}

	public bool IsSnappedToRightArm()
	{
		if (snappedToJoint == null)
		{
			return false;
		}
		SnapJointType jointType = snappedToJoint.jointType;
		if (jointType != SnapJointType.HandR)
		{
			return jointType == SnapJointType.ForearmR;
		}
		return true;
	}

	public void OnUnsnap()
	{
		unsnapSound.Play(null);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryGetJointToSnapIndex(SnapJointType jointType, out int out_slot)
	{
		out_slot = GetJointToSnapIndex(jointType);
		return out_slot != -1;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int GetJointToSnapIndex(SnapJointType jointType)
	{
		return jointType switch
		{
			SnapJointType.HandL => 2, 
			SnapJointType.HandR => 3, 
			_ => -1, 
		};
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static SnapJointType GetSnapIndexToJoint(int snapIndex)
	{
		return snapIndex switch
		{
			2 => SnapJointType.HandL, 
			3 => SnapJointType.HandR, 
			_ => SnapJointType.None, 
		};
	}
}
