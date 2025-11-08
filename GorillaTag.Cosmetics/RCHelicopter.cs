using UnityEngine;

namespace GorillaTag.Cosmetics;

public class RCHelicopter : RCVehicle
{
	[SerializeField]
	private float maxAscendSpeed = 6f;

	[SerializeField]
	private float ascendAccelTime = 3f;

	[SerializeField]
	private float gravityCompensation = 0.5f;

	[SerializeField]
	private float maxTurnRate = 90f;

	[SerializeField]
	private float turnAccelTime = 0.75f;

	[SerializeField]
	private float maxHorizontalSpeed = 6f;

	[SerializeField]
	private float horizontalAccelTime = 2f;

	[SerializeField]
	private float maxHorizontalTiltAngle = 45f;

	[SerializeField]
	private Vector2 mainPropellerSpinRateRange = new Vector2(3f, 15f);

	[SerializeField]
	private float backPropellerSpinRate = 5f;

	[SerializeField]
	private Transform verticalPropeller;

	[SerializeField]
	private Transform turnPropeller;

	private Quaternion verticalPropellerBaseRotation;

	private Quaternion turnPropellerBaseRotation;

	private float turnRate;

	private float ascendAccel;

	private float turnAccel;

	private float horizontalAccel;

	protected override void AuthorityBeginDocked()
	{
		base.AuthorityBeginDocked();
		turnRate = 0f;
		verticalPropeller.localRotation = verticalPropellerBaseRotation;
		turnPropeller.localRotation = turnPropellerBaseRotation;
		if (connectedRemote == null)
		{
			base.gameObject.SetActive(value: false);
		}
	}

	protected override void Awake()
	{
		base.Awake();
		verticalPropellerBaseRotation = verticalPropeller.localRotation;
		turnPropellerBaseRotation = turnPropeller.localRotation;
		ascendAccel = maxAscendSpeed / ascendAccelTime;
		turnAccel = maxTurnRate / turnAccelTime;
		horizontalAccel = maxHorizontalSpeed / horizontalAccelTime;
	}

	protected override void SharedUpdate(float dt)
	{
		if (localState == State.Mobilized)
		{
			float num = Mathf.Lerp(mainPropellerSpinRateRange.x, mainPropellerSpinRateRange.y, activeInput.trigger);
			verticalPropeller.Rotate(new Vector3(0f, num * dt, 0f), Space.Self);
			turnPropeller.Rotate(new Vector3(activeInput.joystick.x * backPropellerSpinRate * dt, 0f, 0f), Space.Self);
		}
	}

	private void FixedUpdate()
	{
		if (base.HasLocalAuthority && localState == State.Mobilized)
		{
			float fixedDeltaTime = Time.fixedDeltaTime;
			Vector3 linearVelocity = rb.linearVelocity;
			_ = linearVelocity.magnitude;
			float target = activeInput.joystick.x * maxTurnRate;
			turnRate = Mathf.MoveTowards(turnRate, target, turnAccel * fixedDeltaTime);
			float num = activeInput.joystick.y * maxHorizontalSpeed;
			float x = Mathf.Sign(activeInput.joystick.y) * Mathf.Lerp(0f, maxHorizontalTiltAngle, Mathf.Abs(activeInput.joystick.y));
			base.transform.rotation = Quaternion.Euler(new Vector3(x, turnAccel, 0f));
			float num2 = Mathf.Abs(num);
			Vector3 normalized = Vector3.ProjectOnPlane(base.transform.forward, Vector3.up).normalized;
			float num3 = Vector3.Dot(normalized, linearVelocity);
			if (num2 > 0.01f && ((num > 0f && num > num3) || (num < 0f && num < num3)))
			{
				rb.AddForce(normalized * Mathf.Sign(num) * horizontalAccel * fixedDeltaTime * rb.mass, ForceMode.Force);
			}
			float num4 = activeInput.trigger * maxAscendSpeed;
			if (num4 > 0.01f && linearVelocity.y < num4)
			{
				rb.AddForce(Vector3.up * ascendAccel * rb.mass, ForceMode.Force);
			}
			if (rb.useGravity)
			{
				rb.AddForce(-Physics.gravity * gravityCompensation * rb.mass, ForceMode.Force);
			}
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		if (!other.isTrigger && base.HasLocalAuthority && localState == State.Mobilized)
		{
			AuthorityBeginCrash();
		}
	}
}
