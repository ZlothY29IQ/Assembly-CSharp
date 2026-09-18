using GorillaTag;
using GorillaTag.CosmeticSystem;
using Unity.Cinemachine;
using UnityEngine;

public class FloppyFold : MonoBehaviour, ISpawnable
{
	public enum Axis
	{
		X,
		Y,
		Z
	}

	[SerializeField]
	private Axis LocalRotationAxis;

	[SerializeField]
	private Vector3 LocalCenterOfMass = Vector3.forward;

	[SerializeField]
	private float minAngle = -45f;

	[SerializeField]
	private float maxAngle = 45f;

	[SerializeField]
	private float freeMinAngle = -30f;

	[SerializeField]
	private float freeMaxAngle = 30f;

	[SerializeField]
	private float springStrength = 400f;

	[SerializeField]
	private float drag;

	[SerializeField]
	private float gravity;

	[SerializeField]
	private float localFriction;

	[SerializeField]
	private bool IgnorePlayerMovement;

	private Transform rigRoot;

	private float angle;

	private Vector3 lastWorldPosition;

	private Vector3 velocity;

	private Vector3 lastRigLocalPosition;

	private Vector3 lastRigLocalVelocity;

	public bool IsSpawned { get; set; }

	public ECosmeticSelectSide CosmeticSelectedSide { get; set; }

	private void Start()
	{
		Vector3 vector = AxisVector(LocalRotationAxis);
		angle = Vector3.SignedAngle(Vector3.ProjectOnPlane(LocalCenterOfMass, vector), Vector3.ProjectOnPlane(base.transform.localRotation * LocalCenterOfMass, vector), vector);
		Reanchor();
	}

	public void OnSpawn(VRRig rig)
	{
		rigRoot = ((rig != null) ? rig.transform : null);
		Reanchor();
	}

	public void OnDespawn()
	{
		rigRoot = null;
	}

	private void Reanchor()
	{
		velocity = Vector3.zero;
		lastWorldPosition = base.transform.TransformPoint(LocalCenterOfMass);
		RememberInRigSpace();
	}

	private void RememberInRigSpace()
	{
		if (!(rigRoot == null))
		{
			lastRigLocalPosition = rigRoot.InverseTransformPoint(lastWorldPosition);
			lastRigLocalVelocity = rigRoot.InverseTransformVector(velocity);
		}
	}

	private void Update()
	{
		float deltaTime = Time.deltaTime;
		if (deltaTime <= 0f)
		{
			return;
		}
		if (IgnorePlayerMovement && rigRoot != null)
		{
			lastWorldPosition = rigRoot.TransformPoint(lastRigLocalPosition);
			velocity = rigRoot.TransformVector(lastRigLocalVelocity);
		}
		Vector3 vector = AxisVector(LocalRotationAxis);
		Vector3 normalized = base.transform.parent.TransformDirection(vector).normalized;
		Vector3 position = base.transform.position;
		Vector3 vector2 = (base.transform.TransformPoint(LocalCenterOfMass) - position).ProjectOntoPlane(normalized);
		if (!(vector2.sqrMagnitude <= Mathf.Epsilon))
		{
			Vector3 to = (lastWorldPosition + velocity * deltaTime - position).ProjectOntoPlane(normalized);
			float num = Vector3.SignedAngle(vector2, to, normalized);
			num *= Mathf.Exp((0f - drag) * deltaTime);
			num = Mathf.MoveTowards(num, 0f, localFriction * deltaTime * deltaTime);
			num += 57.29578f * Vector3.Dot(Vector3.Cross(vector2, Vector3.down * gravity), normalized) / vector2.sqrMagnitude * deltaTime * deltaTime;
			angle += num;
			if (angle > freeMaxAngle)
			{
				angle -= (angle - freeMaxAngle) * springStrength * deltaTime * deltaTime;
			}
			else if (angle < freeMinAngle)
			{
				angle += (freeMinAngle - angle) * springStrength * deltaTime * deltaTime;
			}
			angle = Mathf.Clamp(angle, minAngle, maxAngle);
			base.transform.localRotation = Quaternion.AngleAxis(angle, vector);
			Vector3 vector3 = base.transform.TransformPoint(LocalCenterOfMass);
			velocity = (vector3 - lastWorldPosition) / deltaTime;
			lastWorldPosition = vector3;
			RememberInRigSpace();
		}
	}

	private static Vector3 AxisVector(Axis axis)
	{
		return axis switch
		{
			Axis.X => Vector3.right, 
			Axis.Y => Vector3.up, 
			_ => Vector3.forward, 
		};
	}
}
