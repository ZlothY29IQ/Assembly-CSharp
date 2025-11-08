using UnityEngine;

public class BeePerchPoint : MonoBehaviour
{
	[SerializeField]
	private Vector3 localPosition;

	public Vector3 GetPoint()
	{
		return base.transform.TransformPoint(localPosition);
	}
}
