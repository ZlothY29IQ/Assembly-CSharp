using System;
using GorillaExtensions;
using UnityEngine;

public class BalloonDynamics : MonoBehaviour, ITetheredObjectBehavior
{
	private Rigidbody rb;

	private Collider balloonCollider;

	private Bounds bounds;

	public float bouyancyForce = 1f;

	public float bouyancyMinHeight = 10f;

	public float bouyancyMaxHeight = 20f;

	private float bouyancyActualHeight = 20f;

	public float varianceMaxheight = 5f;

	public float airResistance = 0.01f;

	public GameObject knot;

	private Rigidbody knotRb;

	public Transform grabPt;

	private Transform grabPtInitParent;

	public float stringLength = 2f;

	public float stringStrength = 0.9f;

	public float stringStretch = 0.1f;

	public float maximumVelocity = 2f;

	public float upRightTorque = 1f;

	public float antiSpinTorque;

	private bool enableDynamics;

	private bool enableDistanceConstraints;

	public float balloonScale = 1f;

	public float bopSpeed = 1f;

	public float bopSpeedCap;

	[SerializeField]
	private AudioSource balloonBopSource;

	public bool ColliderEnabled
	{
		get
		{
			if ((bool)balloonCollider)
			{
				return balloonCollider.enabled;
			}
			return false;
		}
	}

	private void Awake()
	{
		rb = GetComponent<Rigidbody>();
		knotRb = knot.GetComponent<Rigidbody>();
		balloonCollider = GetComponent<Collider>();
		grabPtInitParent = grabPt.transform.parent;
	}

	private void Start()
	{
		airResistance = Mathf.Clamp(airResistance, 0f, 1f);
		balloonCollider.enabled = false;
	}

	public void ReParent()
	{
		if (grabPt != null)
		{
			grabPt.transform.parent = grabPtInitParent.transform;
		}
		bouyancyActualHeight = UnityEngine.Random.Range(bouyancyMinHeight, bouyancyMaxHeight);
	}

	private void ApplyBouyancyForce()
	{
		float num = bouyancyActualHeight + Mathf.Sin(Time.time) * varianceMaxheight;
		float num2 = (num - base.transform.position.y) / num;
		float y = bouyancyForce * num2 * balloonScale;
		rb.AddForce(new Vector3(0f, y, 0f) * rb.mass, ForceMode.Force);
	}

	private void ApplyUpRightForce()
	{
		Vector3 torque = Vector3.Cross(base.transform.up, Vector3.up) * upRightTorque * balloonScale;
		rb.AddTorque(torque);
	}

	private void ApplyAntiSpinForce()
	{
		Vector3 vector = rb.transform.InverseTransformDirection(rb.angularVelocity);
		rb.AddRelativeTorque(0f, (0f - vector.y) * antiSpinTorque, 0f);
	}

	private void ApplyAirResistance()
	{
		rb.linearVelocity *= 1f - airResistance;
	}

	private void ApplyDistanceConstraint()
	{
		_ = knot.transform.position - base.transform.position;
		Vector3 vector = grabPt.transform.position - knot.transform.position;
		Vector3 normalized = vector.normalized;
		float magnitude = vector.magnitude;
		float num = stringLength * balloonScale;
		if (magnitude > num)
		{
			Vector3 vector2 = Vector3.Dot(knotRb.linearVelocity, normalized) * normalized;
			float num2 = magnitude - num;
			float num3 = num2 / Time.fixedDeltaTime;
			if (vector2.magnitude < num3)
			{
				float b = num3 - vector2.magnitude;
				float num4 = Mathf.Clamp01(num2 / stringStretch);
				Vector3 vector3 = Mathf.Lerp(0f, b, num4 * num4) * normalized * stringStrength;
				rb.AddForceAtPosition(vector3 * rb.mass, knot.transform.position, ForceMode.Impulse);
			}
		}
	}

	public void EnableDynamics(bool enable, bool collider, bool kinematic)
	{
		bool flag = !enableDynamics && enable;
		enableDynamics = enable;
		if ((bool)balloonCollider)
		{
			balloonCollider.enabled = collider;
		}
		if (rb != null)
		{
			rb.isKinematic = kinematic;
			if (!kinematic && flag)
			{
				rb.linearVelocity = Vector3.zero;
				rb.angularVelocity = Vector3.zero;
			}
		}
	}

	public void EnableDistanceConstraints(bool enable, float scale = 1f)
	{
		enableDistanceConstraints = enable;
		balloonScale = scale;
	}

	private void FixedUpdate()
	{
		if (enableDynamics && !rb.isKinematic)
		{
			ApplyBouyancyForce();
			if (antiSpinTorque > 0f)
			{
				ApplyAntiSpinForce();
			}
			ApplyUpRightForce();
			ApplyAirResistance();
			if (enableDistanceConstraints)
			{
				ApplyDistanceConstraint();
			}
			Vector3 linearVelocity = rb.linearVelocity;
			float magnitude = linearVelocity.magnitude;
			rb.linearVelocity = linearVelocity.normalized * Mathf.Min(magnitude, maximumVelocity * balloonScale);
		}
	}

	void ITetheredObjectBehavior.DbgClear()
	{
		throw new NotImplementedException();
	}

	bool ITetheredObjectBehavior.IsEnabled()
	{
		return base.enabled;
	}

	void ITetheredObjectBehavior.TriggerEnter(Collider other, ref Vector3 force, ref Vector3 collisionPt, ref bool transferOwnership)
	{
		if (!other.gameObject.IsOnLayer(UnityLayer.GorillaHand) || !rb)
		{
			return;
		}
		transferOwnership = true;
		TransformFollow component = other.gameObject.GetComponent<TransformFollow>();
		if ((bool)component)
		{
			Vector3 vector = (component.transform.position - component.prevPos) / Time.deltaTime;
			force = vector * bopSpeed;
			force = Mathf.Min(maximumVelocity, force.magnitude) * force.normalized * balloonScale;
			if (bopSpeedCap > 0f && force.IsLongerThan(bopSpeedCap))
			{
				force = force.normalized * bopSpeedCap;
			}
			collisionPt = other.ClosestPointOnBounds(base.transform.position);
			rb.AddForceAtPosition(force * rb.mass, collisionPt, ForceMode.Impulse);
			if (balloonBopSource != null)
			{
				balloonBopSource.GTPlay();
			}
			GorillaTriggerColliderHandIndicator component2 = other.GetComponent<GorillaTriggerColliderHandIndicator>();
			if (component2 != null)
			{
				float amplitude = GorillaTagger.Instance.tapHapticStrength / 4f;
				float fixedDeltaTime = Time.fixedDeltaTime;
				GorillaTagger.Instance.StartVibration(component2.isLeftHand, amplitude, fixedDeltaTime);
			}
		}
	}

	public bool ReturnStep()
	{
		return true;
	}
}
