using System;
using Photon.Pun;
using UnityEngine;

namespace GorillaTag.Cosmetics;

public class ProximityLookAt : MonoBehaviour, IGorillaSliceableSimple
{
	public enum LocalAxis
	{
		Forward,
		Back,
		Right,
		Left,
		Up,
		Down
	}

	[Header("Settings")]
	[SerializeField]
	private Transform[] lookTransforms;

	[Tooltip("The local axis that points 'forward' on this transform.")]
	[SerializeField]
	private LocalAxis localForward = LocalAxis.Down;

	[SerializeField]
	private float lookRadius = 0.5f;

	[Tooltip("The cone angle in degrees used to detect nearby players.Only players within this angle of the forward direction are considered as targets.")]
	[SerializeField]
	private float targetSearchAngleDegrees = 60f;

	[Tooltip("How far in degrees the transform can physically rotate from its rest position.Should be less than or equal to targetSearchAngleDegrees")]
	[SerializeField]
	private float lookAtAngleDegreeMax = 45f;

	[SerializeField]
	private float rotSpeed = 180f;

	[Tooltip("Seconds to hold the current target before switching to a new one")]
	[SerializeField]
	private float targetSwitchCooldown = 0.5f;

	[Tooltip("Whether the cosmetic owner can be considered as a look target.")]
	[SerializeField]
	private bool includeOwner;

	[Header("Pivot Clamping (Optional)")]
	[Tooltip("Assign a pivot transform to constrain rotation relative to it. Leave empty to skip clamping.")]
	[SerializeField]
	private Transform pivotConstraint;

	[SerializeField]
	private float minPivotY = -1f;

	[SerializeField]
	private float maxPivotY = 1f;

	private TransferrableObject transferableParent;

	private VRRig ownerRig;

	private Transform lookTarget;

	private Vector3 normalizedLocalForward;

	private float cosAngle;

	private float sqrRadius;

	private float lastTargetSwitchTime = float.NegativeInfinity;

	private void OnEnable()
	{
		GorillaSlicerSimpleManager.RegisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.Update);
		if (transferableParent != null)
		{
			ownerRig = transferableParent.ownerRig;
		}
		if (ownerRig == null)
		{
			ownerRig = GetComponentInParent<VRRig>();
		}
		if (ownerRig == null)
		{
			ownerRig = GorillaTagger.Instance.offlineVRRig;
		}
		CacheSettings();
	}

	private void OnDisable()
	{
		GorillaSlicerSimpleManager.UnregisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.Update);
		lookTarget = null;
		lastTargetSwitchTime = float.NegativeInfinity;
	}

	private void OnValidate()
	{
		CacheSettings();
	}

	private void CacheSettings()
	{
		normalizedLocalForward = LocalAxisToVector(localForward);
		cosAngle = Mathf.Cos(targetSearchAngleDegrees * (MathF.PI / 180f));
		sqrRadius = lookRadius * lookRadius;
	}

	private static Vector3 LocalAxisToVector(LocalAxis axis)
	{
		return axis switch
		{
			LocalAxis.Forward => Vector3.forward, 
			LocalAxis.Back => Vector3.back, 
			LocalAxis.Right => Vector3.right, 
			LocalAxis.Left => Vector3.left, 
			LocalAxis.Up => Vector3.up, 
			LocalAxis.Down => Vector3.down, 
			_ => Vector3.forward, 
		};
	}

	public void SliceUpdate()
	{
		Transform transform = FindTarget();
		if (!(transform == lookTarget) && !(Time.time - lastTargetSwitchTime < targetSwitchCooldown))
		{
			lookTarget = transform;
			lastTargetSwitchTime = Time.time;
		}
	}

	private void LateUpdate()
	{
		if (lookTransforms == null)
		{
			return;
		}
		Vector3 vector = base.transform.TransformDirection(normalizedLocalForward);
		for (int i = 0; i < lookTransforms.Length; i++)
		{
			Transform transform = lookTransforms[i];
			if (!(transform == null))
			{
				Vector3 target = ((lookTarget != null) ? (lookTarget.position - transform.position).normalized : vector);
				target = Vector3.RotateTowards(vector, target, lookAtAngleDegreeMax * (MathF.PI / 180f), 0f);
				if (pivotConstraint != null)
				{
					Vector3 vector2 = pivotConstraint.InverseTransformDirection(target);
					vector2.y = Mathf.Clamp(vector2.y, minPivotY, maxPivotY);
					target = pivotConstraint.TransformDirection(vector2.normalized);
				}
				Vector3 forward = Vector3.RotateTowards(transform.rotation * Vector3.forward, target, rotSpeed * (MathF.PI / 180f) * Time.deltaTime, 0f);
				transform.rotation = ((pivotConstraint != null) ? Quaternion.LookRotation(forward, pivotConstraint.up) : Quaternion.LookRotation(forward));
			}
		}
	}

	private Transform FindTarget()
	{
		if (!PhotonNetwork.InRoom)
		{
			return GorillaTagger.Instance.offlineVRRig.tagSound.transform;
		}
		Vector3 lhs = base.transform.TransformDirection(normalizedLocalForward);
		float num = float.NegativeInfinity;
		Transform result = null;
		foreach (VRRig activeRig in VRRigCache.ActiveRigs)
		{
			if (!includeOwner && activeRig == ownerRig)
			{
				continue;
			}
			Vector3 vector = activeRig.tagSound.transform.position - base.transform.position;
			if (!(vector.sqrMagnitude > sqrRadius))
			{
				Vector3 normalized = vector.normalized;
				float num2 = Vector3.Dot(lhs, normalized);
				if (!(num2 < cosAngle) && num2 > num)
				{
					num = num2;
					result = activeRig.tagSound.transform;
				}
			}
		}
		return result;
	}
}
