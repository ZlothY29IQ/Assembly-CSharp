using GorillaLocomotion;
using UnityEngine;

public class MatchGTPlayerRotation : MonoBehaviour
{
	public bool matchPosition;

	public bool matchRotation;

	private void LateUpdate()
	{
		if (matchPosition)
		{
			base.transform.position = GTPlayer.Instance.mainCamera.transform.position;
		}
		if (matchRotation)
		{
			base.transform.rotation = GTPlayer.Instance.transform.rotation;
		}
	}
}
