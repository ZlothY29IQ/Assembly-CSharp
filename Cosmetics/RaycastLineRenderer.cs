using GorillaTag.Reactions;
using UnityEngine;

namespace Cosmetics;

public class RaycastLineRenderer : MonoBehaviour, ITickSystemPost
{
	private enum DirectionSpace
	{
		Local,
		World
	}

	private enum CastAxis
	{
		Forward,
		Back,
		Right,
		Left,
		Up,
		Down
	}

	[Tooltip("Origin of the line. The ray is cast from this transform's position.")]
	[SerializeField]
	private Transform origin;

	[SerializeField]
	private DirectionSpace directionSpace;

	[SerializeField]
	private CastAxis directionAxis;

	[Tooltip("Maximum length of the line in meters")]
	[SerializeField]
	private float maxDistance = 10f;

	[SerializeField]
	private LayerMask hitLayers = 134218241;

	[Tooltip("Line renderer drawn between the origin and the hit point")]
	[SerializeField]
	private LineRenderer lineRenderer;

	[Tooltip("Object placed at the point of contact.\nThis must already live in the prefab")]
	[SerializeField]
	private GameObject impactFx;

	[Tooltip("Align the impact FX up (y+) axis to the surface normal at the hit point.")]
	[SerializeField]
	private bool orientImpactToSurface = true;

	[Tooltip("Needs to be in the object pool system")]
	[SerializeField]
	private SpawnWorldEffects surfaceEffectSpawner;

	private RaycastHit hit;

	public bool PostTickRunning { get; set; }

	private void Awake()
	{
		if (lineRenderer == null)
		{
			lineRenderer = GetComponentInChildren<LineRenderer>();
		}
		if (lineRenderer != null)
		{
			lineRenderer.useWorldSpace = true;
		}
	}

	private void OnDisable()
	{
		DisableLine();
	}

	public void EnableLine()
	{
		if (lineRenderer != null)
		{
			lineRenderer.enabled = true;
		}
		TickSystem<object>.AddPostTickCallback(this);
	}

	public void DisableLine()
	{
		TickSystem<object>.RemovePostTickCallback(this);
		if (lineRenderer != null)
		{
			lineRenderer.enabled = false;
		}
		if (impactFx != null)
		{
			impactFx.SetActive(value: false);
		}
	}

	public void PostTick()
	{
		UpdateLine();
	}

	private void UpdateLine()
	{
		if (origin == null || lineRenderer == null)
		{
			return;
		}
		Vector3 position = origin.position;
		Vector3 rayDirection = GetRayDirection();
		bool flag = Physics.Raycast(position, rayDirection, out hit, maxDistance, hitLayers, QueryTriggerInteraction.Ignore);
		Vector3 position2 = (flag ? hit.point : (position + rayDirection * maxDistance));
		lineRenderer.positionCount = 2;
		lineRenderer.SetPosition(0, position);
		lineRenderer.SetPosition(1, position2);
		if (flag && surfaceEffectSpawner != null)
		{
			surfaceEffectSpawner.RequestSpawn(hit.point, hit.normal);
		}
		if (impactFx == null)
		{
			return;
		}
		if (flag)
		{
			impactFx.transform.position = hit.point;
			if (orientImpactToSurface)
			{
				impactFx.transform.up = hit.normal;
			}
			if (!impactFx.activeSelf)
			{
				impactFx.SetActive(value: true);
			}
		}
		else if (impactFx.activeSelf)
		{
			impactFx.SetActive(value: false);
		}
	}

	private Vector3 GetRayDirection()
	{
		if (directionSpace == DirectionSpace.World)
		{
			return directionAxis switch
			{
				CastAxis.Forward => Vector3.forward, 
				CastAxis.Back => Vector3.back, 
				CastAxis.Right => Vector3.right, 
				CastAxis.Left => Vector3.left, 
				CastAxis.Up => Vector3.up, 
				CastAxis.Down => Vector3.down, 
				_ => Vector3.forward, 
			};
		}
		return directionAxis switch
		{
			CastAxis.Forward => origin.forward, 
			CastAxis.Back => -origin.forward, 
			CastAxis.Right => origin.right, 
			CastAxis.Left => -origin.right, 
			CastAxis.Up => origin.up, 
			CastAxis.Down => -origin.up, 
			_ => origin.forward, 
		};
	}
}
