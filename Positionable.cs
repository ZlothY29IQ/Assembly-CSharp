using UnityEngine;

public class Positionable : MonoBehaviour
{
	public void CopyPostion(Transform t)
	{
		base.transform.position = t.position;
	}

	public void StickRightUnder(Transform t)
	{
		base.transform.position = t.position;
		base.transform.localPosition = Vector3.zero;
	}
}
