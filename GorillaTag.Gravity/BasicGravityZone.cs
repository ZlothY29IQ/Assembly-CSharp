using System;
using System.Collections;
using System.Collections.Generic;
using GT_CustomMapSupportRuntime;
using GorillaLocomotion;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace GorillaTag.Gravity;

public class BasicGravityZone : MonoBehaviour, ICallbackUnique, ICallBack
{
	[Header("Gravity Settings")]
	[Tooltip("negative number pulls, positive number expels")]
	public float gravityStrength = -9.81f;

	[Tooltip("Filter which players are affected based on scale. Small = scale < 1")]
	[SerializeField]
	private GravityZoneScaleFilter scaleFilter;

	[Tooltip("- Newest: Only in effect when this is the newest zone entered by the physics object. \n\n- Closest: if this gravity zone is the closest, then it will have effect. \n\n- Additive:  always in effect when a physics object is inside.")]
	[SerializeField]
	private GravityZoneRule m_gravityRule = GravityZoneRule.Closest;

	[Tooltip("The gravity zone with the highest authority will cause gravity zones with a lower authority level to be ignored. Gravity zones with the same authority level will follow the Gravity Rule setting.")]
	[SerializeField]
	private int m_authorityLevel;

	[Header("Rotation Settings")]
	[Tooltip("If enabled, rotates the target away from gravity direction to be upside down")]
	[SerializeField]
	protected bool invertRotationDirection;

	[SerializeField]
	protected bool rotateTarget = true;

	[SerializeField]
	private bool m_useRotationSpeedOverride;

	[SerializeField]
	[FormerlySerializedAs("rotationSpeed")]
	private float m_rotationSpeedOverride = 10f;

	[NonSerialized]
	private float m_rotationSpeed;

	[Header("Events")]
	public UnityEvent onLocalPlayerEntered;

	public UnityEvent onLocalPlayerExited;

	protected Vector3 m_gravityDirection;

	protected ListProcessor<MonkeGravityController> m_gravityTargets = new ListProcessor<MonkeGravityController>(5);

	private Dictionary<MonkeGravityController, GravityInfo> m_targetGravityInfos = new Dictionary<MonkeGravityController, GravityInfo>(5);

	private readonly Dictionary<MonkeGravityController, Coroutine> m_pendingTransitions = new Dictionary<MonkeGravityController, Coroutine>(5);

	public GravityZoneRule GravityRule => m_gravityRule;

	public int AuthorityLevel => m_authorityLevel;

	protected float RotationSpeed => m_rotationSpeed;

	private IReadOnlyList<MonkeGravityController> GravityTargets => m_gravityTargets.GetReadonlyList();

	bool ICallbackUnique.Registered { get; set; }

	protected virtual void Awake()
	{
		CalculateDependentVars();
	}

	private void CalculateDependentVars()
	{
		m_gravityDirection = base.gameObject.transform.up;
		invertRotationDirection = (gravityStrength > 0f && !invertRotationDirection) || (gravityStrength <= 0f && invertRotationDirection);
		m_rotationSpeed = (m_useRotationSpeedOverride ? m_rotationSpeedOverride : MonkeGravityManager.DefaultGravityInfo.rotationSpeed);
	}

	protected virtual void OnEnable()
	{
		m_gravityTargets.ItemProcessor = ProcessGravityTargets;
	}

	protected virtual void OnDisable()
	{
		foreach (KeyValuePair<MonkeGravityController, Coroutine> pendingTransition in m_pendingTransitions)
		{
			if (pendingTransition.Value != null)
			{
				StopCoroutine(pendingTransition.Value);
			}
		}
		m_pendingTransitions.Clear();
		m_gravityTargets.ItemProcessor = ProcessRemoveTargets;
		m_gravityTargets.ProcessList();
		m_gravityTargets.Clear();
		m_targetGravityInfos.Clear();
		MonkeGravityManager.RemoveGravityCallback(this);
	}

	public virtual void CallBack()
	{
		m_gravityTargets.ProcessList();
	}

	private void ProcessRemoveTargets(in MonkeGravityController target)
	{
		target.OnLeftGravityZone(this);
	}

	private void ProcessGravityTargets(in MonkeGravityController targetController)
	{
		if (!PassesScaleFilter(targetController))
		{
			OnTargetFilteredOut(targetController);
		}
		GravityInfo value = default(GravityInfo);
		Vector3 offsetFromGravity = GetGravityVectorAtPoint(targetController.GetWorldPoint(), in targetController);
		Vector3 gravityDirection = (value.gravityUpDirection = offsetFromGravity.normalized);
		value.rotationDirection = GetRotationDirection(in gravityDirection);
		value.gravityStrength = GetGravityStrength(in offsetFromGravity);
		value.rotationSpeed = GetRotationSpeed(in offsetFromGravity);
		value.rotate = GetRotationIntent(in offsetFromGravity);
		m_targetGravityInfos[targetController] = value;
	}

	protected virtual Vector3 GetGravityVectorAtPoint(in Vector3 worldPosition, in MonkeGravityController controller)
	{
		return m_gravityDirection;
	}

	protected virtual float GetGravityStrength(in Vector3 offsetFromGravity)
	{
		return gravityStrength;
	}

	protected virtual bool GetRotationIntent(in Vector3 offsetFromGravity)
	{
		return rotateTarget;
	}

	protected virtual Vector3 GetRotationDirection(in Vector3 gravityDirection)
	{
		if (invertRotationDirection)
		{
			return -gravityDirection;
		}
		return gravityDirection;
	}

	protected virtual float GetRotationSpeed(in Vector3 offsetFromGravity)
	{
		return m_rotationSpeed;
	}

	public bool GetGravityInfo(MonkeGravityController target, out GravityInfo info)
	{
		return m_targetGravityInfos.TryGetValue(target, out info);
	}

	public void AddTargetLocalPlayer()
	{
		AddTarget(GTPlayerTransform.Instance);
	}

	public void RemoveTargetLocalPlayer()
	{
		RemoveTarget(GTPlayerTransform.Instance);
	}

	public void RemoveTarget(MonkeGravityController target)
	{
		CancelPending(target);
		RemoveTargetImmediate(target);
	}

	public void AddTarget(MonkeGravityController target)
	{
		CancelPending(target);
		AddTargetImmediate(target);
	}

	public void RemoveTarget(MonkeGravityController target, float delay)
	{
		if (delay <= 0f || !base.isActiveAndEnabled)
		{
			RemoveTarget(target);
			return;
		}
		CancelPending(target);
		if (m_gravityTargets.Contains(in target))
		{
			m_pendingTransitions[target] = StartCoroutine(DelayedTransition(target, delay, add: false));
		}
	}

	public void AddTarget(MonkeGravityController target, float delay)
	{
		if (delay <= 0f || !base.isActiveAndEnabled)
		{
			AddTarget(target);
			return;
		}
		CancelPending(target);
		if (!m_gravityTargets.Contains(in target))
		{
			m_pendingTransitions[target] = StartCoroutine(DelayedTransition(target, delay, add: true));
		}
	}

	private void CancelPending(MonkeGravityController target)
	{
		if (m_pendingTransitions.TryGetValue(target, out var value))
		{
			if (value != null)
			{
				StopCoroutine(value);
			}
			m_pendingTransitions.Remove(target);
		}
	}

	private IEnumerator DelayedTransition(MonkeGravityController target, float delay, bool add)
	{
		yield return new WaitForSeconds(delay);
		m_pendingTransitions.Remove(target);
		if (add)
		{
			AddTargetImmediate(target);
		}
		else
		{
			RemoveTargetImmediate(target);
		}
	}

	private void RemoveTargetImmediate(MonkeGravityController target)
	{
		if (target.Register && m_gravityTargets.Remove(in target))
		{
			m_targetGravityInfos.Remove(target);
			target.OnLeftGravityZone(this);
			OnTargetExited(target);
			if (target == GTPlayerTransform.Instance)
			{
				onLocalPlayerExited?.Invoke();
			}
			if (m_gravityTargets.Count < 1)
			{
				MonkeGravityManager.RemoveGravityCallback(this);
			}
		}
	}

	private void AddTargetImmediate(MonkeGravityController target)
	{
		if (target.Register && !m_gravityTargets.Contains(in target))
		{
			m_gravityTargets.Add(in target);
			target.OnEnteredGravityZone(this);
			if (target == GTPlayerTransform.Instance)
			{
				onLocalPlayerEntered?.Invoke();
			}
			MonkeGravityManager.AddGravityCallback(this);
		}
	}

	protected virtual void OnTargetExited(MonkeGravityController target)
	{
	}

	protected virtual void OnTargetFilteredOut(MonkeGravityController target)
	{
	}

	private bool PassesScaleFilter(MonkeGravityController target)
	{
		if (scaleFilter == GravityZoneScaleFilter.Anyone)
		{
			return true;
		}
		bool flag = target.Scale < 1f;
		if (scaleFilter != GravityZoneScaleFilter.SmallOnly)
		{
			return !flag;
		}
		return flag;
	}

	private void OnTriggerEnter(Collider other)
	{
		(bool, MonkeGravityController) monkeGravityController = MonkeGravityManager.GetMonkeGravityController(other);
		if (monkeGravityController.Item1)
		{
			AddTarget(monkeGravityController.Item2);
		}
	}

	private void OnTriggerExit(Collider other)
	{
		(bool, MonkeGravityController) monkeGravityController = MonkeGravityManager.GetMonkeGravityController(other);
		if (monkeGravityController.Item1)
		{
			RemoveTarget(monkeGravityController.Item2);
		}
	}

	public void CopyProperties(BasicGravityZoneSettings settings)
	{
		gravityStrength = settings.gravityStrength;
		scaleFilter = settings.scaleFilter switch
		{
			BasicGravityZoneSettings.GravityZoneScaleFilter.Anyone => GravityZoneScaleFilter.Anyone, 
			BasicGravityZoneSettings.GravityZoneScaleFilter.SmallOnly => GravityZoneScaleFilter.SmallOnly, 
			BasicGravityZoneSettings.GravityZoneScaleFilter.NotSmall => GravityZoneScaleFilter.NotSmall, 
			_ => throw new NotImplementedException(), 
		};
		m_gravityRule = settings.gravityRule switch
		{
			BasicGravityZoneSettings.GravityZoneRule.Newest => GravityZoneRule.Newest, 
			BasicGravityZoneSettings.GravityZoneRule.Closest => GravityZoneRule.Closest, 
			BasicGravityZoneSettings.GravityZoneRule.Additive => GravityZoneRule.Additive, 
			_ => throw new NotImplementedException(), 
		};
		m_authorityLevel = settings.authorityLevel;
		invertRotationDirection = settings.invertRotationDirection;
		rotateTarget = settings.rotateTarget;
		m_useRotationSpeedOverride = settings.useRotationSpeedOverride;
		m_rotationSpeedOverride = settings.rotationSpeedOverride;
		CalculateDependentVars();
	}
}
