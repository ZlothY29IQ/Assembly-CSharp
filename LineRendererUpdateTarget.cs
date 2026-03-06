using UnityEngine;

public class LineRendererUpdateTarget : MonoBehaviourPostTick
{
	private LineRenderer lineRenderer;

	public Transform targetTransform;

	public override void PostTick()
	{
		if (!(lineRenderer == null) && !(targetTransform == null) && lineRenderer.positionCount == 2)
		{
			if (!targetTransform.gameObject.activeSelf)
			{
				lineRenderer.enabled = false;
				return;
			}
			lineRenderer.enabled = true;
			lineRenderer.SetPosition(0, base.transform.position);
			lineRenderer.SetPosition(1, targetTransform.position);
		}
	}

	private void Awake()
	{
		lineRenderer = GetComponent<LineRenderer>();
		lineRenderer.useWorldSpace = true;
	}
}
