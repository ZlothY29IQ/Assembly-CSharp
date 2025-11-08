using UnityEngine;

public class GorillaSceneCamera : MonoBehaviour
{
	public GorillaSceneTransform[] sceneTransforms;

	public void SetSceneCamera(int sceneIndex)
	{
		base.transform.position = sceneTransforms[sceneIndex].scenePosition;
		base.transform.eulerAngles = sceneTransforms[sceneIndex].sceneRotation;
	}
}
