using UnityEngine;

namespace BoingKit;

public class BoingEffector : BoingBase
{
	public struct Params
	{
		public static readonly int Stride = 80;

		public Vector3 PrevPosition;

		private float m_padding0;

		public Vector3 CurrPosition;

		private float m_padding1;

		public Vector3 LinearVelocityDir;

		private float m_padding2;

		public float Radius;

		public float FullEffectRadius;

		public float MoveDistance;

		public float LinearImpulse;

		public float RotateAngle;

		public float AngularImpulse;

		public Bits32 Bits;

		private int m_padding3;

		public Params(BoingEffector effector)
		{
			Bits = default(Bits32);
			Bits.SetBit(0, effector.ContinuousMotion);
			float num = ((effector.MaxImpulseSpeed > MathUtil.Epsilon) ? Mathf.Min(1f, effector.LinearSpeed / effector.MaxImpulseSpeed) : 1f);
			PrevPosition = effector.m_prevPosition;
			CurrPosition = effector.m_currPosition;
			LinearVelocityDir = VectorUtil.NormalizeSafe(effector.LinearVelocity, Vector3.zero);
			Radius = effector.Radius;
			FullEffectRadius = Radius * effector.FullEffectRadiusRatio;
			MoveDistance = effector.MoveDistance;
			LinearImpulse = num * effector.LinearImpulse;
			RotateAngle = effector.RotationAngle * MathUtil.Deg2Rad;
			AngularImpulse = num * effector.AngularImpulse * MathUtil.Deg2Rad;
			m_padding0 = 0f;
			m_padding1 = 0f;
			m_padding2 = 0f;
			m_padding3 = 0;
		}

		public void Fill(BoingEffector effector)
		{
			this = new Params(effector);
		}

		private void SuppressWarnings()
		{
			m_padding0 = 0f;
			m_padding1 = 0f;
			m_padding2 = 0f;
			m_padding3 = 0;
			m_padding0 = m_padding1;
			m_padding1 = m_padding2;
			m_padding2 = m_padding3;
			m_padding3 = (int)m_padding0;
		}
	}

	[Header("Metrics")]
	[Range(0f, 20f)]
	[Tooltip("Maximum radius of influence.")]
	public float Radius = 3f;

	[Range(0f, 1f)]
	[Tooltip("Fraction of Radius past which influence begins decaying gradually to zero exactly at Radius.\n\ne.g. With a Radius of 10.0 and FullEffectRadiusRatio of 0.5, reactors within distance of 5.0 will be fully influenced, reactors at distance of 7.5 will experience 50% influence, and reactors past distance of 10.0 will not be influenced at all.")]
	public float FullEffectRadiusRatio = 0.5f;

	[Header("Dynamics")]
	[Range(0f, 100f)]
	[Tooltip("Speed of this effector at which impulse effects will be at maximum strength.\n\ne.g. With a MaxImpulseSpeed of 10.0 and an effector traveling at speed of 4.0, impulse effects will be at 40% maximum strength.")]
	public float MaxImpulseSpeed = 5f;

	[Tooltip("This affects impulse-related effects.\n\nIf checked, continuous motion will be simulated between frames. This means even if an effector \"teleports\" by moving a huge distance between frames, the effector will still affect all reactors caught on the effector's path in between frames, not just the reactors around the effector's discrete positions at different frames.")]
	public bool ContinuousMotion;

	[Header("Position Effect")]
	[Range(-10f, 10f)]
	[Tooltip("Distance to push away reactors at maximum influence.\n\ne.g. With a MoveDistance of 2.0, a Radius of 10.0, a FullEffectRadiusRatio of 0.5, and a reactor at distance of 7.5 away from effector, the reactor will be pushed away to 50% of maximum influence, i.e. 50% of MoveDistance, which is a distance of 1.0 away from the effector.")]
	public float MoveDistance = 0.5f;

	[Range(-200f, 200f)]
	[Tooltip("Under maximum impulse influence (within distance of Radius * FullEffectRadiusRatio and with effector moving at speed faster or equal to MaxImpulaseSpeed), a reactor's movement speed will be maintained to be at least as fast as LinearImpulse (unit: distance per second) in the direction of effector's movement direction.\n\ne.g. With a LinearImpulse of 2.0, a Radius of 10.0, a FullEffectRadiusRatio of 0.5, and a reactor at distance of 7.5 away from effector, the reactor's movement speed in the direction of effector's movement direction will be maintained to be at least 50% of LinearImpulse, which is 1.0 per second.")]
	public float LinearImpulse = 5f;

	[Header("Rotation Effect")]
	[Range(-180f, 180f)]
	[Tooltip("Angle (in degrees) to rotate reactors at maximum influence. The rotation will point reactors' up vectors (defined individually in the reactor component) away from the effector.\n\ne.g. With a RotationAngle of 20.0, a Radius of 10.0, a FullEffectRadiusRatio of 0.5, and a reactor at distance of 7.5 away from effector, the reactor will be rotated to 50% of maximum influence, i.e. 50% of RotationAngle, which is 10 degrees.")]
	public float RotationAngle = 20f;

	[Range(-2000f, 2000f)]
	[Tooltip("Under maximum impulse influence (within distance of Radius * FullEffectRadiusRatio and with effector moving at speed faster or equal to MaxImpulaseSpeed), a reactor's rotation speed will be maintained to be at least as fast as AngularImpulse (unit: degrees per second) in the direction of effector's movement direction, i.e. the reactor's up vector will be pulled in the direction of effector's movement direction.\n\ne.g. With a AngularImpulse of 20.0, a Radius of 10.0, a FullEffectRadiusRatio of 0.5, and a reactor at distance of 7.5 away from effector, the reactor's rotation speed in the direction of effector's movement direction will be maintained to be at least 50% of AngularImpulse, which is 10.0 degrees per second.")]
	public float AngularImpulse = 400f;

	[Header("Debug")]
	[Tooltip("If checked, gizmos of reactor fields affected by this effector will be drawn.")]
	public bool DrawAffectedReactorFieldGizmos;

	private Vector3 m_currPosition;

	private Vector3 m_prevPosition;

	private Vector3 m_linearVelocity;

	public Vector3 LinearVelocity => m_linearVelocity;

	public float LinearSpeed => m_linearVelocity.magnitude;

	public void OnEnable()
	{
		m_currPosition = base.transform.position;
		m_prevPosition = base.transform.position;
		m_linearVelocity = Vector3.zero;
		BoingManager.Register(this);
	}

	public void OnDisable()
	{
		BoingManager.Unregister(this);
	}

	public void Update()
	{
		float deltaTime = Time.deltaTime;
		if (!(deltaTime < MathUtil.Epsilon))
		{
			m_linearVelocity = (base.transform.position - m_prevPosition) / deltaTime;
			m_prevPosition = m_currPosition;
			m_currPosition = base.transform.position;
		}
	}

	public void OnDrawGizmosSelected()
	{
		if (base.isActiveAndEnabled)
		{
			if (FullEffectRadiusRatio < 1f)
			{
				Gizmos.color = new Color(1f, 0.5f, 0.2f, 0.4f);
				Gizmos.DrawWireSphere(base.transform.position, Radius);
			}
			Gizmos.color = new Color(1f, 0.5f, 0.2f, 1f);
			Gizmos.DrawWireSphere(base.transform.position, Radius * FullEffectRadiusRatio);
		}
	}
}
