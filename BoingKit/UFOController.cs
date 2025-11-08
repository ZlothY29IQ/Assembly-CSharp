using UnityEngine;

namespace BoingKit;

public class UFOController : MonoBehaviour
{
	public float LinearThrust = 3f;

	public float MaxLinearSpeed = 2.5f;

	public float LinearDrag = 4f;

	public float Tilt = 15f;

	public float AngularThrust = 30f;

	public float MaxAngularSpeed = 30f;

	public float AngularDrag = 30f;

	[Range(0f, 1f)]
	public float Hover = 1f;

	public Transform Eyes;

	public float BlinkInterval = 5f;

	private float m_blinkTimer;

	private bool m_lastBlinkWasDouble;

	private Vector3 m_eyeInitScale;

	private Vector3 m_eyeInitPositionLs;

	private Vector3Spring m_eyeScaleSpring;

	private Vector3Spring m_eyePositionLsSpring;

	public Transform Motor;

	public float MotorBaseAngularSpeed = 10f;

	public float MotorMaxAngularSpeed = 10f;

	public ParticleSystem BubbleEmitter;

	public float BubbleBaseEmissionRate = 10f;

	public float BubbleMaxEmissionRate = 10f;

	private Vector3 m_linearVelocity;

	private float m_angularVelocity;

	private float m_yawAngle;

	private Vector3 m_hoverCenter;

	private float m_hoverPhase;

	private float m_motorAngle;

	private void Start()
	{
		m_linearVelocity = Vector3.zero;
		m_angularVelocity = 0f;
		m_yawAngle = base.transform.rotation.eulerAngles.y * MathUtil.Deg2Rad;
		m_hoverCenter = base.transform.position;
		m_hoverPhase = 0f;
		m_motorAngle = 0f;
		if (Eyes != null)
		{
			m_eyeInitScale = Eyes.localScale;
			m_eyeInitPositionLs = Eyes.localPosition;
			m_blinkTimer = BlinkInterval + Random.Range(1f, 2f);
			m_lastBlinkWasDouble = false;
			m_eyeScaleSpring.Reset(m_eyeInitScale);
			m_eyePositionLsSpring.Reset(m_eyeInitPositionLs);
		}
	}

	private void OnEnable()
	{
		Start();
	}

	private void FixedUpdate()
	{
		float fixedDeltaTime = Time.fixedDeltaTime;
		Vector3 zero = Vector3.zero;
		if (Input.GetKey(KeyCode.W))
		{
			zero += Vector3.forward;
		}
		if (Input.GetKey(KeyCode.S))
		{
			zero += Vector3.back;
		}
		if (Input.GetKey(KeyCode.A))
		{
			zero += Vector3.left;
		}
		if (Input.GetKey(KeyCode.D))
		{
			zero += Vector3.right;
		}
		if (Input.GetKey(KeyCode.R))
		{
			zero += Vector3.up;
		}
		if (Input.GetKey(KeyCode.F))
		{
			zero += Vector3.down;
		}
		if (zero.sqrMagnitude > MathUtil.Epsilon)
		{
			zero = zero.normalized * LinearThrust;
			m_linearVelocity += zero * fixedDeltaTime;
			m_linearVelocity = VectorUtil.ClampLength(m_linearVelocity, 0f, MaxLinearSpeed);
		}
		else
		{
			m_linearVelocity = VectorUtil.ClampLength(m_linearVelocity, 0f, Mathf.Max(0f, m_linearVelocity.magnitude - LinearDrag * fixedDeltaTime));
		}
		float magnitude = m_linearVelocity.magnitude;
		float t = magnitude * MathUtil.InvSafe(MaxLinearSpeed);
		Quaternion quaternion = Quaternion.identity;
		float num = 1f;
		float num2 = 0f;
		if (magnitude > MathUtil.Epsilon)
		{
			Vector3 linearVelocity = m_linearVelocity;
			linearVelocity.y = 0f;
			num = ((m_linearVelocity.magnitude > 0.01f) ? (1f - Mathf.Clamp01(Mathf.Abs(m_linearVelocity.y) / m_linearVelocity.magnitude)) : 0f);
			num2 = Mathf.Min(1f, magnitude / Mathf.Max(MathUtil.Epsilon, MaxLinearSpeed)) * num;
			Vector3 normalized = Vector3.Cross(Vector3.up, linearVelocity).normalized;
			float angle = Tilt * MathUtil.Deg2Rad * num2;
			quaternion = QuaternionUtil.AxisAngle(normalized, angle);
		}
		float num3 = 0f;
		if (Input.GetKey(KeyCode.Q))
		{
			num3 -= 1f;
		}
		if (Input.GetKey(KeyCode.E))
		{
			num3 += 1f;
		}
		bool key = Input.GetKey(KeyCode.LeftControl);
		if (Mathf.Abs(num3) > MathUtil.Epsilon)
		{
			float num4 = MaxAngularSpeed * (key ? 2.5f : 1f);
			num3 *= AngularThrust * MathUtil.Deg2Rad;
			m_angularVelocity += num3 * fixedDeltaTime;
			m_angularVelocity = Mathf.Clamp(m_angularVelocity, (0f - num4) * MathUtil.Deg2Rad, num4 * MathUtil.Deg2Rad);
		}
		else
		{
			m_angularVelocity -= Mathf.Sign(m_angularVelocity) * Mathf.Min(Mathf.Abs(m_angularVelocity), AngularDrag * MathUtil.Deg2Rad * fixedDeltaTime);
		}
		m_yawAngle += m_angularVelocity * fixedDeltaTime;
		Quaternion quaternion2 = QuaternionUtil.AxisAngle(Vector3.up, m_yawAngle);
		m_hoverCenter += m_linearVelocity * fixedDeltaTime;
		m_hoverPhase += Time.deltaTime;
		Vector3 vector = 0.05f * Mathf.Sin(1.37f * m_hoverPhase) * Vector3.right + 0.05f * Mathf.Sin(1.93f * m_hoverPhase + 1.234f) * Vector3.forward + 0.04f * Mathf.Sin(0.97f * m_hoverPhase + 4.321f) * Vector3.up;
		vector *= Hover;
		Quaternion quaternion3 = Quaternion.FromToRotation(Vector3.up, vector + Vector3.up);
		base.transform.position = m_hoverCenter + vector;
		base.transform.rotation = quaternion * quaternion2 * quaternion3;
		if (Motor != null)
		{
			float num5 = Mathf.Lerp(MotorBaseAngularSpeed, MotorMaxAngularSpeed, num2);
			m_motorAngle += num5 * MathUtil.Deg2Rad * fixedDeltaTime;
			Motor.localRotation = QuaternionUtil.AxisAngle(Vector3.up, m_motorAngle - m_yawAngle);
		}
		if (BubbleEmitter != null)
		{
			ParticleSystem.EmissionModule emission = BubbleEmitter.emission;
			emission.rateOverTime = Mathf.Lerp(BubbleBaseEmissionRate, BubbleMaxEmissionRate, t);
		}
		if (Eyes != null)
		{
			m_blinkTimer -= fixedDeltaTime;
			if (m_blinkTimer <= 0f)
			{
				bool flag = !m_lastBlinkWasDouble && Random.Range(0f, 1f) > 0.75f;
				m_blinkTimer = (flag ? 0.2f : (BlinkInterval + Random.Range(1f, 2f)));
				m_lastBlinkWasDouble = flag;
				m_eyeScaleSpring.Value.y = 0f;
				m_eyePositionLsSpring.Value.y -= 0.025f;
			}
			Eyes.localScale = m_eyeScaleSpring.TrackDampingRatio(m_eyeInitScale, 30f, 0.8f, fixedDeltaTime);
			Eyes.localPosition = m_eyePositionLsSpring.TrackDampingRatio(m_eyeInitPositionLs, 30f, 0.8f, fixedDeltaTime);
		}
	}
}
