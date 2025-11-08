using UnityEngine;

public class XfToXfLine : MonoBehaviour
{
	public Transform pt0;

	public Transform pt1;

	private LineRenderer lineRenderer;

	private void Awake()
	{
		lineRenderer = GetComponent<LineRenderer>();
	}

	private void Update()
	{
		lineRenderer.SetPosition(0, pt0.transform.position);
		lineRenderer.SetPosition(1, pt1.transform.position);
	}
}
