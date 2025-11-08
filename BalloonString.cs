using System.Collections.Generic;
using UnityEngine;

public class BalloonString : MonoBehaviour, IGorillaSliceableSimple
{
	public Transform startPositionXf;

	public Transform endPositionXf;

	private List<Vector3> vertices;

	public int numSegments = 1;

	private LineRenderer lineRenderer;

	private void Awake()
	{
		lineRenderer = GetComponent<LineRenderer>();
		vertices = new List<Vector3>(numSegments + 1);
		if (startPositionXf != null && endPositionXf != null)
		{
			vertices.Add(startPositionXf.position);
			int num = vertices.Count - 2;
			for (int i = 0; i < num; i++)
			{
				float t = (i + 1) / (vertices.Count - 1);
				Vector3 item = Vector3.Lerp(startPositionXf.position, endPositionXf.position, t);
				vertices.Add(item);
			}
			vertices.Add(endPositionXf.position);
		}
	}

	private void UpdateDynamics()
	{
		vertices[0] = startPositionXf.position;
		vertices[vertices.Count - 1] = endPositionXf.position;
	}

	private void UpdateRenderPositions()
	{
		lineRenderer.SetPosition(0, startPositionXf.transform.position);
		lineRenderer.SetPosition(1, endPositionXf.transform.position);
	}

	public void OnEnable()
	{
		GorillaSlicerSimpleManager.RegisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.LateUpdate);
	}

	public void OnDisable()
	{
		GorillaSlicerSimpleManager.UnregisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.LateUpdate);
	}

	public void SliceUpdate()
	{
		if (startPositionXf != null && endPositionXf != null)
		{
			UpdateDynamics();
			UpdateRenderPositions();
		}
	}
}
