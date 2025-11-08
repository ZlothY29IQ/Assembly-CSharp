using UnityEngine;

public class LineRendererDraw : MonoBehaviour
{
	public LineRenderer lr;

	public Transform[] points;

	public void SetUpLine(Transform[] points)
	{
		lr.positionCount = points.Length;
		this.points = points;
	}

	private void LateUpdate()
	{
		for (int i = 0; i < points.Length; i++)
		{
			lr.SetPosition(i, points[i].position);
		}
	}

	public void Enable(bool enable)
	{
		lr.enabled = enable;
	}
}
