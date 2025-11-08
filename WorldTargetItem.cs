using JetBrains.Annotations;
using UnityEngine;

public class WorldTargetItem
{
	public readonly NetPlayer owner;

	public readonly int itemIdx;

	public readonly Transform targetObject;

	public readonly TransferrableObject transferrableObject;

	public bool IsValid()
	{
		if (itemIdx != -1)
		{
			return owner != null;
		}
		return false;
	}

	[CanBeNull]
	public static WorldTargetItem GenerateTargetFromPlayerAndID(NetPlayer owner, int itemIdx)
	{
		VRRig vRRig = GorillaGameManager.StaticFindRigForPlayer(owner);
		if (vRRig == null)
		{
			Debug.LogError("Tried to setup a sharable object but the target rig is null...");
			return null;
		}
		Transform component = vRRig.myBodyDockPositions.TransferrableItem(itemIdx).gameObject.GetComponent<Transform>();
		return new WorldTargetItem(owner, itemIdx, component);
	}

	public static WorldTargetItem GenerateTargetFromWorldSharableItem(NetPlayer owner, int itemIdx, Transform transform)
	{
		return new WorldTargetItem(owner, itemIdx, transform);
	}

	private WorldTargetItem(NetPlayer owner, int itemIdx, Transform transform)
	{
		this.owner = owner;
		this.itemIdx = itemIdx;
		targetObject = transform;
		transferrableObject = transform.GetComponent<TransferrableObject>();
	}

	public override string ToString()
	{
		return $"Id: {itemIdx} ({owner})";
	}
}
