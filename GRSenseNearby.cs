using System;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;

[Serializable]
public class GRSenseNearby
{
	public float range;

	public float exitRange;

	public float fov;

	[ReadOnly]
	public List<VRRig> rigsNearby;

	private Transform headTransform;

	public void Setup(Transform headTransform)
	{
		rigsNearby = new List<VRRig>();
		this.headTransform = headTransform;
	}

	public void UpdateNearby(List<VRRig> allRigs, GRSenseLineOfSight senseLineOfSight)
	{
		Vector3 position = headTransform.position;
		Vector3 forward = headTransform.rotation * Vector3.forward;
		RemoveNotNearby(position);
		AddNearby(position, forward, allRigs);
		RemoveNoLineOfSight(position, senseLineOfSight);
	}

	public bool IsAnyoneNearby()
	{
		if (!GhostReactorManager.AggroDisabled && rigsNearby != null)
		{
			return rigsNearby.Count > 0;
		}
		return false;
	}

	public static Vector3 GetRigTestLocation(VRRig rig)
	{
		return rig.transform.position;
	}

	public void AddNearby(Vector3 position, Vector3 forward, List<VRRig> allRigs)
	{
		float num = range * range;
		float num2 = Mathf.Cos(fov * (MathF.PI / 180f));
		for (int i = 0; i < allRigs.Count; i++)
		{
			VRRig vRRig = allRigs[i];
			GRPlayer component = vRRig.GetComponent<GRPlayer>();
			if (component.State == GRPlayer.GRPlayerState.Ghost || component.InStealthMode || rigsNearby.Contains(vRRig))
			{
				continue;
			}
			Vector3 vector = GetRigTestLocation(vRRig) - position;
			float sqrMagnitude = vector.sqrMagnitude;
			if (sqrMagnitude > num)
			{
				continue;
			}
			if (sqrMagnitude > 0f)
			{
				float num3 = Mathf.Sqrt(sqrMagnitude);
				if (Vector3.Dot(vector / num3, forward) < num2)
				{
					continue;
				}
			}
			rigsNearby.Add(vRRig);
		}
	}

	public void RemoveNotNearby(Vector3 position)
	{
		float num = exitRange * exitRange;
		for (int i = 0; i < rigsNearby.Count; i++)
		{
			VRRig vRRig = rigsNearby[i];
			if (vRRig != null)
			{
				GRPlayer component = vRRig.GetComponent<GRPlayer>();
				if ((GetRigTestLocation(vRRig) - position).sqrMagnitude <= num && component.State != GRPlayer.GRPlayerState.Ghost && !component.InStealthMode)
				{
					continue;
				}
			}
			rigsNearby.RemoveAt(i);
			i--;
		}
	}

	public void RemoveNoLineOfSight(Vector3 headPos, GRSenseLineOfSight senseLineOfSight)
	{
		for (int i = 0; i < rigsNearby.Count; i++)
		{
			Vector3 rigTestLocation = GetRigTestLocation(rigsNearby[i]);
			if (!senseLineOfSight.HasLineOfSight(headPos, rigTestLocation))
			{
				rigsNearby.RemoveAt(i);
				i--;
			}
		}
	}

	public VRRig PickClosest(out float outDistanceSq)
	{
		Vector3 position = headTransform.position;
		float num = float.MaxValue;
		VRRig result = null;
		for (int i = 0; i < rigsNearby.Count; i++)
		{
			float sqrMagnitude = (GetRigTestLocation(rigsNearby[i]) - position).sqrMagnitude;
			if (sqrMagnitude < num)
			{
				num = sqrMagnitude;
				result = rigsNearby[i];
			}
		}
		outDistanceSq = num;
		return result;
	}
}
