using UnityEngine;

public class FirstPersonXRaySpecs : MonoBehaviour
{
	private void OnEnable()
	{
		GorillaBodyRenderer.SetAllSkeletons(allSkeletons: true);
	}

	private void OnDisable()
	{
		GorillaBodyRenderer.SetAllSkeletons(allSkeletons: false);
	}
}
