using System.Collections.Generic;
using GorillaTag;
using UnityEngine;

public class SuperInfectionSnapPointManager : MonoBehaviour
{
	public List<SuperInfectionSnapPoint> SnapPoints;

	private Dictionary<SnapJointType, SuperInfectionSnapPoint> snapPointDict = new Dictionary<SnapJointType, SuperInfectionSnapPoint>();

	public void Awake()
	{
		VRRig componentInParent = GetComponentInParent<VRRig>(includeInactive: true);
		ISpawnable[] componentsInChildren = GetComponentsInChildren<ISpawnable>(includeInactive: true);
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].OnSpawn(componentInParent);
		}
	}

	public void Start()
	{
		foreach (SuperInfectionSnapPoint snapPoint in SnapPoints)
		{
			snapPoint.Initialize();
			snapPointDict[snapPoint.jointType] = snapPoint;
		}
	}

	public void Clear()
	{
		foreach (SuperInfectionSnapPoint snapPoint in SnapPoints)
		{
			snapPoint.Clear();
		}
		snapPointDict.Clear();
	}

	public SuperInfectionSnapPoint FindSnapPoint(SnapJointType jointType)
	{
		if (jointType == SnapJointType.None)
		{
			return null;
		}
		if (snapPointDict.ContainsKey(jointType))
		{
			return snapPointDict[jointType];
		}
		return null;
	}

	public static SuperInfectionSnapPoint FindSnapPoint(GamePlayer player, SnapJointType jointType)
	{
		if (player == null)
		{
			return null;
		}
		return player.snapPointManager.FindSnapPoint(jointType);
	}

	public void DropAllSnappedAuthority()
	{
		for (int i = 0; i < SnapPoints.Count; i++)
		{
			GameEntity snappedEntity = SnapPoints[i].GetSnappedEntity();
			if (!(snappedEntity == null))
			{
				Vector3 position = snappedEntity.transform.position;
				snappedEntity.manager.RequestGrabEntity(snappedEntity.id, isLeftHand: false, Vector3.zero, Quaternion.identity);
				snappedEntity.manager.RequestThrowEntity(snappedEntity.id, isLeftHand: false, position, Vector3.zero, Vector3.zero);
			}
		}
	}
}
