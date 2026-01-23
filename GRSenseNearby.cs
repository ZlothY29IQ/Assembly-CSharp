using System;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;

[Serializable]
public class GRSenseNearby
{
	public float range;

	public float hearingRange;

	public float exitRange;

	public float fov;

	[ReadOnly]
	public List<VRRig> rigsNearby;

	private Transform headTransform;

	private GameEntity _entity;

	private bool BossEntityPresent => GhostReactorManager.Get(_entity).GetBossEntity() != null;

	public void Setup(Transform headTransform, GameEntity entity)
	{
		rigsNearby = new List<VRRig>();
		this.headTransform = headTransform;
		_entity = entity;
	}

	public void OnHitByPlayer(int hitByActorId)
	{
		GRPlayer gRPlayer = GRPlayer.Get(hitByActorId);
		if (gRPlayer != null)
		{
			VRRig rig = gRPlayer.gamePlayer.rig;
			if (!rigsNearby.Contains(rig))
			{
				rigsNearby.Add(rig);
			}
		}
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

	public bool IsAnyoneNearby(float range, bool ignoreBossEntity = false)
	{
		if (!ignoreBossEntity && BossEntityPresent && rigsNearby.Count > 0)
		{
			return true;
		}
		if (!IsAnyoneNearby())
		{
			return false;
		}
		Vector3 position = headTransform.position;
		float num = range * range;
		for (int i = 0; i < rigsNearby.Count; i++)
		{
			if (!(rigsNearby[i] == null) && (GetRigTestLocation(rigsNearby[i]) - position).sqrMagnitude <= num)
			{
				return true;
			}
		}
		return false;
	}

	public static Vector3 GetRigTestLocation(VRRig rig)
	{
		return rig.transform.position;
	}

	public void AddNearby(Vector3 position, Vector3 forward, List<VRRig> allRigs)
	{
		if (BossEntityPresent)
		{
			foreach (VRRig allRig in allRigs)
			{
				if (!rigsNearby.Contains(allRig))
				{
					rigsNearby.Add(allRig);
				}
			}
			return;
		}
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
			float num3 = hearingRange * hearingRange;
			if (!(sqrMagnitude < num3))
			{
				if (!(sqrMagnitude < num))
				{
					continue;
				}
				if (sqrMagnitude > 0f)
				{
					float num4 = Mathf.Sqrt(sqrMagnitude);
					if (Vector3.Dot(vector / num4, forward) < num2)
					{
						continue;
					}
				}
			}
			rigsNearby.Add(vRRig);
		}
	}

	public void RemoveNotNearby(Vector3 position)
	{
		if (BossEntityPresent)
		{
			return;
		}
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
		if (BossEntityPresent)
		{
			return;
		}
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
