using System.Collections.Generic;
using UnityEngine;

namespace GorillaTag.Cosmetics;

public class AOESender : MonoBehaviour
{
	private enum FalloffMode
	{
		None,
		Linear,
		AnimationCurve
	}

	[Min(0f)]
	[SerializeField]
	private float radius = 3f;

	[SerializeField]
	private LayerMask layerMask = -1;

	[SerializeField]
	private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Collide;

	[Tooltip("If empty, all AOEReceiver targets pass. If not empty, only receivers with these tags pass.")]
	[SerializeField]
	private string[] includeTags;

	[SerializeField]
	private FalloffMode falloffMode = FalloffMode.Linear;

	[SerializeField]
	private AnimationCurve falloffCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);

	[Tooltip("Base strength before distance falloff.")]
	[SerializeField]
	private float strength = 1f;

	[Tooltip("Optional after falloff, applied as: max(minStrength, base*falloff).")]
	[SerializeField]
	private float minStrength;

	[SerializeField]
	private bool applyOnEnable;

	[Min(0f)]
	[SerializeField]
	private float repeatInterval;

	[SerializeField]
	[Tooltip("Max colliders captured per trigger/apply.")]
	private int maxColliders = 16;

	private Collider[] hits;

	private readonly HashSet<AOEReceiver> visited = new HashSet<AOEReceiver>();

	private float nextTime;

	private void Awake()
	{
		if (hits == null || hits.Length != maxColliders)
		{
			hits = new Collider[Mathf.Max(8, maxColliders)];
		}
	}

	private void OnEnable()
	{
		if (applyOnEnable)
		{
			ApplyAOE();
		}
		nextTime = Time.time + repeatInterval;
	}

	private void Update()
	{
		if (repeatInterval > 0f && Time.time >= nextTime)
		{
			ApplyAOE();
			nextTime = Time.time + repeatInterval;
		}
	}

	public void ApplyAOE()
	{
		ApplyAOE(base.transform.position);
	}

	public void ApplyAOE(Vector3 worldOrigin)
	{
		visited.Clear();
		int num = Physics.OverlapSphereNonAlloc(worldOrigin, radius, hits, layerMask, triggerInteraction);
		float num2 = Mathf.Max(0.0001f, radius);
		for (int i = 0; i < num; i++)
		{
			Collider collider = hits[i];
			if ((bool)collider)
			{
				AOEReceiver componentInChildren = (collider.attachedRigidbody ? collider.attachedRigidbody.transform : collider.transform).GetComponentInChildren<AOEReceiver>(includeInactive: true);
				if (componentInChildren != null && TagValidation(componentInChildren.gameObject) && !visited.Contains(componentInChildren))
				{
					visited.Add(componentInChildren);
					float num3 = Vector3.Distance(worldOrigin, componentInChildren.transform.position);
					float num4 = Mathf.Clamp01(num3 / num2);
					float num5 = EvaluateFalloff(num4);
					float finalStrength = Mathf.Max(minStrength, strength * num5);
					AOEReceiver.AOEContext AOEContext = new AOEReceiver.AOEContext
					{
						origin = worldOrigin,
						radius = radius,
						instigator = base.gameObject,
						baseStrength = strength,
						finalStrength = finalStrength,
						distance = num3,
						normalizedDistance = num4
					};
					componentInChildren.ReceiveAOE(in AOEContext);
				}
			}
		}
	}

	private float EvaluateFalloff(float t)
	{
		return falloffMode switch
		{
			FalloffMode.None => 1f, 
			FalloffMode.Linear => 1f - t, 
			FalloffMode.AnimationCurve => Mathf.Max(0f, falloffCurve.Evaluate(t)), 
			_ => 1f, 
		};
	}

	private bool TagValidation(GameObject go)
	{
		if (go == null)
		{
			return false;
		}
		if (includeTags == null || includeTags.Length == 0)
		{
			return true;
		}
		string text = go.tag;
		string[] array = includeTags;
		foreach (string text2 in array)
		{
			if (!string.IsNullOrEmpty(text2) && text == text2)
			{
				return true;
			}
		}
		return false;
	}
}
