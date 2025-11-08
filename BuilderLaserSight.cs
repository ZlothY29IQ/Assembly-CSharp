using UnityEngine;

public class BuilderLaserSight : MonoBehaviour
{
	public LineRenderer lineRenderer;

	public void Awake()
	{
		if (lineRenderer == null)
		{
			lineRenderer = GetComponentInChildren<LineRenderer>();
		}
		if (lineRenderer != null)
		{
			lineRenderer.enabled = false;
		}
	}

	public void SetPoints(Vector3 start, Vector3 end)
	{
		lineRenderer.positionCount = 2;
		lineRenderer.SetPosition(0, start);
		lineRenderer.SetPosition(1, end);
	}

	public void Show(bool show)
	{
		if (lineRenderer != null)
		{
			lineRenderer.enabled = show;
		}
	}
}
