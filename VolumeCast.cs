using System;
using System.Collections.Generic;
using Drawing;
using GorillaTag;
using UnityEngine;

public class VolumeCast : MonoBehaviourGizmos
{
	public enum VolumeShape
	{
		Box,
		Cylinder
	}

	public VolumeShape shape;

	[Space]
	public Vector3 center;

	public Vector3 size = Vector3.one;

	public float height = 1f;

	public float radius = 1f;

	private const int MAX_HITS = 8;

	[Space]
	public UnityLayerMask physicsMask = UnityLayerMask.Everything;

	public bool includeTriggers;

	[Space]
	[SerializeField]
	private bool _simulateInEditMode;

	[NonSerialized]
	[DebugReadout]
	private int _capHits;

	[NonSerialized]
	[DebugReadout]
	private Collider[] _capOverlaps = new Collider[8];

	[NonSerialized]
	[DebugReadout]
	private int _boxHits;

	[NonSerialized]
	[DebugReadout]
	private Collider[] _boxOverlaps = new Collider[8];

	[NonSerialized]
	[DebugReadout]
	private int _hits;

	[NonSerialized]
	[DebugReadout]
	private Collider[] _overlaps = new Collider[8];

	[NonSerialized]
	[DebugReadout]
	private bool _colliding;

	[NonSerialized]
	private HashSet<Collider> _set = new HashSet<Collider>(8);

	public bool CheckOverlaps()
	{
		Transform transform = base.transform;
		Vector3 lossyScale = transform.lossyScale;
		Quaternion rotation = transform.rotation;
		int num = (int)physicsMask;
		QueryTriggerInteraction queryTriggerInteraction = ((!includeTriggers) ? QueryTriggerInteraction.Ignore : QueryTriggerInteraction.Collide);
		GetEndsAndRadius(transform, center, height, radius, out var a, out var b, out var r);
		Vector3 vector;
		Vector3 halfExtents;
		switch (shape)
		{
		case VolumeShape.Box:
			vector = transform.TransformPoint(center);
			halfExtents = Vector3.Scale(lossyScale, size * 0.5f).Abs();
			break;
		case VolumeShape.Cylinder:
			vector = (a + b) * 0.5f;
			halfExtents = new Vector3(r, Vector3.Distance(a, b) * 0.5f, r);
			break;
		default:
			return false;
		}
		Array.Clear(_boxOverlaps, 0, 8);
		_boxHits = Physics.OverlapBoxNonAlloc(vector, halfExtents, _boxOverlaps, rotation, num, queryTriggerInteraction);
		if (shape != VolumeShape.Cylinder)
		{
			return _colliding = _boxHits > 0;
		}
		_hits = 0;
		Array.Clear(_capOverlaps, 0, 8);
		Array.Clear(_overlaps, 0, 8);
		_capHits = Physics.OverlapCapsuleNonAlloc(a, b, r, _capOverlaps, num, queryTriggerInteraction);
		_set.Clear();
		int num2 = Math.Max(_capHits, _boxHits);
		Collider[] array = ((_capHits < _boxHits) ? _capOverlaps : _boxOverlaps);
		Collider[] array2 = ((_capHits < _boxHits) ? _boxOverlaps : _capOverlaps);
		for (int i = 0; i < num2; i++)
		{
			Collider collider = array[i];
			if ((bool)collider && !_set.Add(collider))
			{
				_overlaps[_hits++] = collider;
			}
			Collider collider2 = array2[i];
			if ((bool)collider2 && !_set.Add(collider2))
			{
				_overlaps[_hits++] = collider2;
			}
		}
		return _colliding = _hits > 0;
	}

	private static void GetEndsAndRadius(Transform t, Vector3 center, float height, float radius, out Vector3 a, out Vector3 b, out float r)
	{
		float num = height * 0.5f;
		Vector3 lossyScale = t.lossyScale;
		a = t.TransformPoint(center + Vector3.down * num);
		b = t.TransformPoint(center + Vector3.up * num);
		r = Math.Max(Math.Abs(lossyScale.x), Math.Abs(lossyScale.z)) * radius;
	}
}
