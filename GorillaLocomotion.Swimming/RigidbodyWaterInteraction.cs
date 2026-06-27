using System.Collections.Generic;
using AA;
using UnityEngine;

namespace GorillaLocomotion.Swimming;

[RequireComponent(typeof(Rigidbody))]
public class RigidbodyWaterInteraction : MonoBehaviour
{
	public bool applyDamping = true;

	public bool applyBuoyancyForce = true;

	public bool applyAngularDrag = true;

	public bool applyWaterCurrents = true;

	public bool applySurfaceTorque = true;

	public float underWaterDampingHalfLife = 0.25f;

	public float waterSurfaceDampingHalfLife = 1f;

	public float underWaterBuoyancyFactor = 0.5f;

	public float angularDrag = 0.5f;

	public float surfaceTorqueAmount = 0.5f;

	public bool enablePreciseWaterCollision;

	public float objectRadiusForWaterCollision = 0.25f;

	[Range(0f, 1f)]
	public float buoyancyEquilibrium = 0.8f;

	private Rigidbody rb;

	private List<WaterVolume> overlappingWaterVolumes = new List<WaterVolume>();

	private List<WaterCurrent> activeWaterCurrents = new List<WaterCurrent>(16);

	private float baseAngularDrag = 0.05f;

	protected void Awake()
	{
		rb = GetComponent<Rigidbody>();
		baseAngularDrag = rb.angularDamping;
		RigidbodyWaterInteractionManager.RegisterRBWI(this);
	}

	protected void OnEnable()
	{
		overlappingWaterVolumes.Clear();
		RigidbodyWaterInteractionManager.RegisterRBWI(this);
	}

	protected void OnDisable()
	{
		overlappingWaterVolumes.Clear();
		RigidbodyWaterInteractionManager.UnregisterRBWI(this);
	}

	private void OnDestroy()
	{
		RigidbodyWaterInteractionManager.UnregisterRBWI(this);
	}

	public void InvokeFixedUpdate()
	{
		if (rb.isKinematic)
		{
			return;
		}
		bool flag = overlappingWaterVolumes.Count > 0;
		WaterVolume.SurfaceQuery surfaceQuery = default(WaterVolume.SurfaceQuery);
		float num = float.MinValue;
		if (flag && enablePreciseWaterCollision)
		{
			Vector3 vector = base.transform.position + GTPlayerTransform.PhysicsDown * 2f * objectRadiusForWaterCollision * buoyancyEquilibrium;
			bool flag2 = false;
			activeWaterCurrents.Clear();
			for (int i = 0; i < overlappingWaterVolumes.Count; i++)
			{
				if (overlappingWaterVolumes[i].GetSurfaceQueryForPoint(vector, out var result))
				{
					float num2 = Vector3.Dot(result.surfacePoint - vector, result.surfaceNormal);
					if (num2 > num)
					{
						num = num2;
						surfaceQuery = result;
						flag2 = true;
					}
					WaterCurrent current = overlappingWaterVolumes[i].Current;
					if (applyWaterCurrents && current != null && num2 > 0f && !activeWaterCurrents.Contains(current))
					{
						activeWaterCurrents.Add(current);
					}
				}
			}
			if (flag2)
			{
				bool num3 = num > (0f - (1f - buoyancyEquilibrium)) * 2f * objectRadiusForWaterCollision;
				float num4 = (enablePreciseWaterCollision ? objectRadiusForWaterCollision : 0f);
				Vector3 vector2 = surfaceQuery.surfacePoint - surfaceQuery.surfaceNormal * surfaceQuery.maxDepth;
				bool flag3 = Vector3.Dot(base.transform.position + surfaceQuery.surfaceNormal * num4 - vector2, surfaceQuery.surfaceNormal) > 0f;
				flag = num3 && flag3;
			}
			else
			{
				flag = false;
			}
		}
		if (flag)
		{
			float fixedDeltaTime = Time.fixedDeltaTime;
			Vector3 vector3 = rb.linearVelocity;
			Vector3 zero = Vector3.zero;
			if (applyWaterCurrents)
			{
				Vector3 zero2 = Vector3.zero;
				for (int j = 0; j < activeWaterCurrents.Count; j++)
				{
					if (activeWaterCurrents[j].GetCurrentAtPoint(startingVelocity: vector3 + zero, worldPoint: base.transform.position, dt: fixedDeltaTime, currentVelocity: out var currentVelocity, velocityChange: out var velocityChange))
					{
						zero2 += currentVelocity;
						zero += velocityChange;
					}
				}
				if (enablePreciseWaterCollision)
				{
					Vector3 position = (surfaceQuery.surfacePoint + (base.transform.position + GTPlayerTransform.PhysicsDown * objectRadiusForWaterCollision)) * 0.5f;
					rb.AddForceAtPosition(zero * rb.mass, position, ForceMode.Impulse);
				}
				else
				{
					vector3 += zero;
				}
			}
			if (applyBuoyancyForce)
			{
				Vector3 zero3 = Vector3.zero;
				if (enablePreciseWaterCollision)
				{
					float b = 2f * objectRadiusForWaterCollision * buoyancyEquilibrium;
					float num5 = Mathf.InverseLerp(0f, b, num);
					zero3 = GTPlayerTransform.PhysicsUp * Physics.gravity.magnitude * underWaterBuoyancyFactor * num5 * fixedDeltaTime;
				}
				else
				{
					zero3 = GTPlayerTransform.PhysicsUp * Physics.gravity.magnitude * underWaterBuoyancyFactor * fixedDeltaTime;
				}
				if (zero.sqrMagnitude > 0.001f)
				{
					float magnitude = zero.magnitude;
					Vector3 vector4 = zero / magnitude;
					float num6 = Vector3.Dot(zero3, vector4);
					if (num6 < 0f)
					{
						zero3 -= num6 * vector4;
					}
				}
				vector3 += zero3;
			}
			float magnitude2 = vector3.magnitude;
			if (magnitude2 > 0.001f && applyDamping)
			{
				Vector3 vector5 = vector3 / magnitude2;
				float num7 = Spring.DamperDecayExact(magnitude2, underWaterDampingHalfLife, fixedDeltaTime);
				if (enablePreciseWaterCollision)
				{
					float a = Spring.DamperDecayExact(magnitude2, waterSurfaceDampingHalfLife, fixedDeltaTime);
					float t = Mathf.Clamp((0f - Vector3.Dot(base.transform.position - surfaceQuery.surfacePoint, surfaceQuery.surfaceNormal)) / objectRadiusForWaterCollision, -1f, 1f) * 0.5f + 0.5f;
					vector3 = Mathf.Lerp(a, num7, t) * vector5;
				}
				else
				{
					vector3 = num7 * vector5;
				}
			}
			if (applySurfaceTorque && enablePreciseWaterCollision)
			{
				float num8 = Vector3.Dot(base.transform.position - surfaceQuery.surfacePoint, surfaceQuery.surfaceNormal);
				if (num8 < objectRadiusForWaterCollision && num8 > 0f)
				{
					Vector3 rhs = vector3 - Vector3.Dot(vector3, surfaceQuery.surfaceNormal) * surfaceQuery.surfaceNormal;
					Vector3 normalized = Vector3.Cross(surfaceQuery.surfaceNormal, rhs).normalized;
					float num9 = Vector3.Dot(rb.angularVelocity, normalized);
					float num10 = rhs.magnitude / objectRadiusForWaterCollision - num9;
					if (num10 > 0f)
					{
						rb.AddTorque(surfaceTorqueAmount * num10 * normalized, ForceMode.Acceleration);
					}
				}
			}
			rb.linearVelocity = vector3;
			rb.angularDamping = angularDrag;
		}
		else
		{
			rb.angularDamping = baseAngularDrag;
		}
	}

	protected void OnTriggerEnter(Collider other)
	{
		WaterVolume component = other.GetComponent<WaterVolume>();
		if (component != null && !overlappingWaterVolumes.Contains(component))
		{
			overlappingWaterVolumes.Add(component);
		}
	}

	protected void OnTriggerExit(Collider other)
	{
		WaterVolume component = other.GetComponent<WaterVolume>();
		if (component != null && overlappingWaterVolumes.Contains(component))
		{
			overlappingWaterVolumes.Remove(component);
		}
	}
}
