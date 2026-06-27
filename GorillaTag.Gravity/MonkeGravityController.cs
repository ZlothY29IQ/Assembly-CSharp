using System.Collections.Generic;
using System.Runtime.CompilerServices;
using GorillaExtensions;
using UnityEngine;

namespace GorillaTag.Gravity;

public class MonkeGravityController : MonoBehaviour, ICallbackUnique, ICallBack
{
	[SerializeField]
	private Collider m_activatorCollider;

	[SerializeField]
	protected Rigidbody m_targetRigidBody;

	[SerializeField]
	protected Transform m_targetTransform;

	[SerializeField]
	private bool m_instantRotation;

	[SerializeField]
	private bool m_useRotation;

	[SerializeField]
	private bool m_overrideForceMode;

	[SerializeField]
	private ForceMode m_forceModeOverride;

	[SerializeField]
	private BasicGravityZone m_alwaysInZone;

	[Tooltip("The direction we wish to rotate if the target is 180 degrees off.")]
	[SerializeField]
	protected RotationDirection m_preferredRotationDirection = RotationDirection.Right;

	private bool m_register;

	private readonly List<BasicGravityZone> m_gravityZones = new List<BasicGravityZone>(3);

	private bool m_needsRotationRecovery;

	protected bool m_globalGravityIntent;

	private int m_highestAuthorityLevel = int.MinValue;

	public Collider ActivatorCollider => m_activatorCollider;

	public Rigidbody TargetRigidBody => m_targetRigidBody;

	public Transform TargetTransform => m_targetTransform;

	public virtual float Scale => TargetTransform.localScale.x;

	protected bool InstantRotation => m_instantRotation;

	protected bool OverrideForceMode => m_overrideForceMode;

	public RotationDirection PreferredRotationDirection
	{
		get
		{
			return m_preferredRotationDirection;
		}
		set
		{
			m_preferredRotationDirection = value;
		}
	}

	public bool Register => m_register;

	public Vector3 GravityUp { get; private set; } = Vector3.up;

	public Vector3 GravityDown { get; private set; } = Vector3.down;

	public float GravityMultiplier { get; set; } = 1f;

	public Vector3 PersonalGravityDirection { get; set; } = Vector3.up;

	public int GravityZonesCount => m_gravityZones.Count;

	bool ICallbackUnique.Registered { get; set; }

	public void SetPersonalGravityDirection(Vector3 direction)
	{
		PersonalGravityDirection = direction.normalized;
	}

	public void SetPersonalGravityDirection(Transform reference)
	{
		PersonalGravityDirection = reference.up;
	}

	protected virtual void Awake()
	{
		if (m_targetRigidBody.IsNull())
		{
			m_targetRigidBody = GetComponent<Rigidbody>();
		}
		if (m_targetTransform.IsNull())
		{
			m_targetTransform = base.transform;
		}
		if (m_alwaysInZone != null)
		{
			m_alwaysInZone.AddTarget(this);
		}
		else if (m_activatorCollider.IsNull())
		{
			m_activatorCollider = GetComponent<Collider>();
			if (m_activatorCollider.IsNull())
			{
				return;
			}
		}
		if (!m_targetRigidBody.IsNull())
		{
			m_register = true;
			m_globalGravityIntent = m_targetRigidBody.useGravity;
		}
	}

	protected virtual void OnEnable()
	{
		if (m_register)
		{
			MonkeGravityManager.AddMonkeGravityController(this);
		}
	}

	protected virtual void OnDisable()
	{
		if (m_register)
		{
			m_targetRigidBody.useGravity = m_globalGravityIntent;
			MonkeGravityManager.RemoveMonkeGravityController(this);
			for (int num = m_gravityZones.Count - 1; num > -1; num--)
			{
				BasicGravityZone basicGravityZone = m_gravityZones[num];
				basicGravityZone.RemoveTarget(this);
				OnLeftGravityZone(basicGravityZone);
			}
		}
	}

	private (Vector3, Vector3, float, bool) ProcessGravityZones(in Vector3 position)
	{
		Vector3 cumulativeVelocity = Vector3.zero;
		Vector3 cumulativeGravityDirection = Vector3.zero;
		float totalRotationSpeed = 0f;
		int rotationCount = 0;
		BasicGravityZone gZone = null;
		float num = float.MaxValue;
		int num2 = int.MinValue;
		int gravityZonesCount = GravityZonesCount;
		BasicGravityZone gZone2 = null;
		for (int i = 0; i < gravityZonesCount; i++)
		{
			BasicGravityZone basicGravityZone = m_gravityZones[i];
			int authorityLevel = basicGravityZone.AuthorityLevel;
			if (authorityLevel > num2)
			{
				num2 = authorityLevel;
			}
			if (authorityLevel == m_highestAuthorityLevel)
			{
				gZone2 = basicGravityZone;
				float sqrMagnitude = (gZone2.transform.position - position).sqrMagnitude;
				if (sqrMagnitude < num)
				{
					num = sqrMagnitude;
					gZone = gZone2;
				}
				GravityZoneRule gravityRule = gZone2.GravityRule;
				if ((uint)gravityRule > 1u && gravityRule == GravityZoneRule.Additive)
				{
					_ProcessGravityInfo(in gZone2);
				}
			}
		}
		if (num2 != m_highestAuthorityLevel)
		{
			m_highestAuthorityLevel = num2;
			return ProcessGravityZones(in position);
		}
		if (gZone2.IsNotNull() && gZone2.GravityRule == GravityZoneRule.Newest)
		{
			_ProcessGravityInfo(in gZone2);
		}
		if (gZone.IsNotNull() && gZone.GravityRule == GravityZoneRule.Closest)
		{
			_ProcessGravityInfo(in gZone);
		}
		return (cumulativeVelocity, cumulativeGravityDirection, (rotationCount > 0) ? (totalRotationSpeed / (float)rotationCount) : 0f, cumulativeGravityDirection != Vector3.zero);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		void _ProcessGravityInfo(in BasicGravityZone reference)
		{
			if (reference.GetGravityInfo(this, out var info))
			{
				cumulativeVelocity += info.gravityUpDirection * info.gravityStrength;
				if (info.rotate && info.rotationSpeed > 0f)
				{
					int num3 = rotationCount + 1;
					rotationCount = num3;
					totalRotationSpeed += info.rotationSpeed;
					cumulativeGravityDirection += info.rotationDirection * Mathf.Max(0.01f, Mathf.Abs(info.gravityStrength));
				}
			}
		}
	}

	public virtual void CallBack()
	{
		(Vector3, Vector3, float, bool) tuple = default((Vector3, Vector3, float, bool));
		if (m_gravityZones.Count > 0)
		{
			tuple = ProcessGravityZones(GetWorldPoint());
			GravityDown = tuple.Item1.normalized;
			GravityUp = -GravityDown;
		}
		else
		{
			GravityInfo defaultGravityInfo = MonkeGravityManager.DefaultGravityInfo;
			tuple.Item1 = defaultGravityInfo.gravityUpDirection * defaultGravityInfo.gravityStrength;
			tuple.Item2 = defaultGravityInfo.rotationDirection;
			tuple.Item3 = defaultGravityInfo.rotationSpeed;
			tuple.Item4 = defaultGravityInfo.rotate;
			GravityUp = defaultGravityInfo.gravityUpDirection;
			GravityDown = -GravityUp;
		}
		ApplyGravityForce(in tuple.Item1);
		if ((tuple.Item4 || m_needsRotationRecovery) && m_useRotation && tuple.Item2 != Vector3.zero)
		{
			ApplyGravityUpRotation(in tuple.Item2, tuple.Item3 * Time.fixedDeltaTime);
		}
	}

	public virtual Vector3 GetWorldPoint()
	{
		return m_targetTransform.position;
	}

	public virtual void OnEnteredGravityZone(BasicGravityZone zone)
	{
		if (!m_gravityZones.Contains(zone))
		{
			m_gravityZones.Add(zone);
		}
		if (m_targetRigidBody.useGravity)
		{
			m_targetRigidBody.useGravity = false;
		}
		int authorityLevel = zone.AuthorityLevel;
		if (authorityLevel > m_highestAuthorityLevel)
		{
			m_highestAuthorityLevel = authorityLevel;
		}
	}

	public virtual void OnLeftGravityZone(BasicGravityZone zone)
	{
		m_gravityZones.Remove(zone);
		if (m_gravityZones.Count < 1)
		{
			m_targetRigidBody.useGravity = m_globalGravityIntent;
			m_needsRotationRecovery = true;
			m_highestAuthorityLevel = int.MinValue;
			return;
		}
		int num = int.MinValue;
		foreach (BasicGravityZone gravityZone in m_gravityZones)
		{
			if (gravityZone.AuthorityLevel > num)
			{
				num = gravityZone.AuthorityLevel;
			}
		}
		m_highestAuthorityLevel = num;
	}

	public virtual void ApplyGravityForce(in Vector3 force, ForceMode forceType = ForceMode.Acceleration)
	{
		if (!m_targetRigidBody.isKinematic)
		{
			m_targetRigidBody.AddForce(force * GravityMultiplier, m_overrideForceMode ? m_forceModeOverride : forceType);
		}
	}

	public void ClearRotationRecovery()
	{
		m_needsRotationRecovery = false;
	}

	public virtual void ApplyGravityUpRotation(in Vector3 upDir, float speed)
	{
		Vector3 up = m_targetTransform.up;
		Vector3 toDirection;
		if (!m_instantRotation)
		{
			Vector3 vector = upDir;
			if (vector == up * -1f)
			{
				switch (m_preferredRotationDirection)
				{
				case RotationDirection.Left:
					vector = m_targetTransform.right * -1f;
					break;
				case RotationDirection.Right:
					vector = m_targetTransform.right;
					break;
				case RotationDirection.Forward:
					vector = m_targetTransform.forward;
					break;
				case RotationDirection.Backward:
					vector = m_targetTransform.forward * -1f;
					break;
				}
			}
			toDirection = Vector3.RotateTowards(up, vector, speed, 0f);
		}
		else
		{
			toDirection = upDir;
		}
		Quaternion quaternion = Quaternion.FromToRotation(up, toDirection);
		m_targetRigidBody.MoveRotation(quaternion * m_targetTransform.rotation);
		if (quaternion == Quaternion.identity)
		{
			m_needsRotationRecovery = false;
		}
	}
}
