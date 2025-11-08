using System;
using UnityEngine;

public class KiteDynamics : MonoBehaviour, ITetheredObjectBehavior
{
	private Rigidbody rb;

	private Collider balloonCollider;

	private Bounds bounds;

	[SerializeField]
	private float bouyancyMinHeight = 10f;

	[SerializeField]
	private float bouyancyMaxHeight = 20f;

	private float bouyancyActualHeight = 20f;

	[SerializeField]
	private float airResistance = 0.01f;

	public GameObject knot;

	private Rigidbody knotRb;

	public Transform grabPt;

	private Transform grabPtInitParent;

	[SerializeField]
	private float maximumVelocity = 2f;

	private bool enableDynamics;

	[SerializeField]
	private float balloonScale = 1f;

	private Vector3 grabPtPosition;

	[SerializeField]
	private Quaternion ctrlRotation;

	[SerializeField]
	private float returnSpeed = 50f;

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
		grabPtPosition = grabPt.position;
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

	public void EnableDynamics(bool enable, bool collider, bool kinematic)
	{
		enableDynamics = enable;
		if ((bool)balloonCollider)
		{
			balloonCollider.enabled = collider;
		}
		if (rb != null)
		{
			rb.isKinematic = kinematic;
			if (!enable)
			{
				rb.linearVelocity = Vector3.zero;
				rb.angularVelocity = Vector3.zero;
			}
		}
	}

	public void EnableDistanceConstraints(bool enable, float scale = 1f)
	{
		rb.useGravity = !enable;
		balloonScale = scale;
		grabPtPosition = grabPt.position;
	}

	private void FixedUpdate()
	{
		if (!rb.isKinematic && !rb.useGravity && enableDynamics)
		{
			Vector3 vector = (grabPt.position - grabPtPosition) * 100f;
			vector = Matrix4x4.Rotate(ctrlRotation).MultiplyVector(vector);
			rb.AddForce(vector, ForceMode.Force);
			Vector3 linearVelocity = rb.linearVelocity;
			float magnitude = linearVelocity.magnitude;
			rb.linearVelocity = linearVelocity.normalized * Mathf.Min(magnitude, maximumVelocity * balloonScale);
			base.transform.LookAt(base.transform.position - rb.linearVelocity);
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
		transferOwnership = false;
	}

	public bool ReturnStep()
	{
		rb.isKinematic = true;
		base.transform.position = Vector3.MoveTowards(base.transform.position, grabPt.position, Time.deltaTime * returnSpeed);
		return base.transform.position == grabPt.position;
	}
}
