using System;
using System.Collections.Generic;
using System.Linq;
using GorillaExtensions;
using UnityEngine;

[DefaultExecutionOrder(1300)]
public class GTPosRotConstraintManager : MonoBehaviour
{
	public struct Range
	{
		public int start;

		public int end;
	}

	public static GTPosRotConstraintManager instance;

	public static bool hasInstance = false;

	private const int _kComponentsCapacity = 256;

	private const int _kConstraintsCapacity = 1024;

	[NonSerialized]
	public Dictionary<Transform, Transform> originalParent;

	[NonSerialized]
	public Dictionary<Transform, Vector3> originalOffset;

	[NonSerialized]
	public Dictionary<Transform, Vector3> originalScale;

	[NonSerialized]
	public Dictionary<Transform, Quaternion> originalRot;

	[NonSerialized]
	public List<GTPosRotConstraints> constraintsToDisable;

	[OnEnterPlay_Clear]
	private static readonly List<GorillaPosRotConstraint> constraints = new List<GorillaPosRotConstraint>(1024);

	[OnEnterPlay_Clear]
	public static readonly Dictionary<int, Range> componentRanges = new Dictionary<int, Range>(256);

	protected void Awake()
	{
		if (hasInstance && instance != this)
		{
			UnityEngine.Object.Destroy(this);
		}
		else
		{
			SetInstance(this);
		}
	}

	protected void OnDestroy()
	{
		if (instance == this)
		{
			hasInstance = false;
			instance = null;
		}
	}

	public void InvokeConstraint(GorillaPosRotConstraint constraint, int index)
	{
		Transform source = constraint.source;
		Transform follower = constraint.follower;
		Vector3 position = source.position + source.TransformVector(constraint.positionOffset);
		Quaternion rotation = source.rotation * constraint.rotationOffset;
		follower.SetPositionAndRotation(position, rotation);
	}

	protected void LateUpdate()
	{
		if (constraintsToDisable.Count <= 0)
		{
			return;
		}
		for (int num = constraintsToDisable.Count - 1; num >= 0; num--)
		{
			for (int i = 0; i < constraintsToDisable[num].constraints.Length; i++)
			{
				Transform follower = constraintsToDisable[num].constraints[i].follower;
				if (originalParent.TryGetValue(follower, out var value) && !(follower == null) && !(value == null))
				{
					follower.SetParent(originalParent[follower], worldPositionStays: true);
					follower.localRotation = originalRot[follower];
					follower.localPosition = originalOffset[follower];
					follower.localScale = originalScale[follower];
					InvokeConstraint(constraintsToDisable[num].constraints[i], num);
				}
			}
			constraintsToDisable.RemoveAt(num);
		}
	}

	public static void CreateManager()
	{
		GTPosRotConstraintManager gTPosRotConstraintManager = new GameObject("GTPosRotConstraintManager").AddComponent<GTPosRotConstraintManager>();
		constraints.Clear();
		componentRanges.Clear();
		SetInstance(gTPosRotConstraintManager);
	}

	private static void SetInstance(GTPosRotConstraintManager manager)
	{
		instance = manager;
		hasInstance = true;
		instance.originalParent = new Dictionary<Transform, Transform>();
		instance.originalOffset = new Dictionary<Transform, Vector3>();
		instance.originalScale = new Dictionary<Transform, Vector3>();
		instance.originalRot = new Dictionary<Transform, Quaternion>();
		instance.constraintsToDisable = new List<GTPosRotConstraints>();
		if (Application.isPlaying)
		{
			manager.transform.SetParent(null, worldPositionStays: false);
			UnityEngine.Object.DontDestroyOnLoad(manager);
		}
	}

	public static void Register(GTPosRotConstraints component)
	{
		if (!hasInstance)
		{
			CreateManager();
		}
		int instanceID = component.GetInstanceID();
		if (componentRanges.ContainsKey(instanceID))
		{
			return;
		}
		for (int i = 0; i < component.constraints.Length; i++)
		{
			if (!component.constraints[i].follower)
			{
				Debug.LogError("Cannot add constraints for GTPosRotConstraints component because the `follower` Transform is null " + $"at index {i}. Path in scene: {component.transform.GetPathQ()}", component);
				return;
			}
			if (!component.constraints[i].source)
			{
				Debug.LogError("Cannot add constraints for GTPosRotConstraints component because the `source` Transform is null " + $"at index {i}. Path in scene: {component.transform.GetPathQ()}", component);
				return;
			}
		}
		Range value = new Range
		{
			start = constraints.Count,
			end = constraints.Count + component.constraints.Length - 1
		};
		componentRanges.Add(instanceID, value);
		constraints.AddRange(component.constraints);
		if (instance.constraintsToDisable.Contains(component))
		{
			instance.constraintsToDisable.Remove(component);
		}
		for (int j = 0; j < component.constraints.Length; j++)
		{
			Transform follower = component.constraints[j].follower;
			if (instance.originalParent.ContainsKey(follower))
			{
				component.constraints[j].follower.SetParent(instance.originalParent[follower], worldPositionStays: true);
				follower.localRotation = instance.originalRot[follower];
				follower.localPosition = instance.originalOffset[follower];
				follower.localScale = instance.originalScale[follower];
			}
			else
			{
				instance.originalParent[follower] = follower.parent;
				instance.originalRot[follower] = follower.localRotation;
				instance.originalOffset[follower] = follower.localPosition;
				instance.originalScale[follower] = follower.localScale;
			}
			instance.InvokeConstraint(component.constraints[j], j);
			component.constraints[j].follower.SetParent(component.constraints[j].source);
		}
	}

	public static void Unregister(GTPosRotConstraints component)
	{
		int instanceID = component.GetInstanceID();
		if (!hasInstance || !componentRanges.TryGetValue(instanceID, out var value))
		{
			return;
		}
		constraints.RemoveRange(value.start, 1 + value.end - value.start);
		componentRanges.Remove(instanceID);
		int[] array = componentRanges.Keys.ToArray();
		foreach (int key in array)
		{
			Range range = componentRanges[key];
			if (range.start > value.end)
			{
				componentRanges[key] = new Range
				{
					start = range.start - value.end + value.start - 1,
					end = range.end - value.end + value.start - 1
				};
			}
		}
		if (!instance.constraintsToDisable.Contains(component))
		{
			instance.constraintsToDisable.Add(component);
		}
	}
}
