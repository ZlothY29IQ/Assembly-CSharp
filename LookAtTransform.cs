using UnityEngine;

public class LookAtTransform : MonoBehaviour
{
	[SerializeField]
	private Transform lookAt;

	private void Update()
	{
		base.transform.rotation = Quaternion.LookRotation(lookAt.position - base.transform.position);
	}
}
