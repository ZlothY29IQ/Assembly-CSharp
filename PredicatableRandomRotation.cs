using UnityEngine;

public class PredicatableRandomRotation : MonoBehaviour
{
	[SerializeField]
	private Vector3 rot = Vector3.zero;

	[SerializeField]
	private Transform source;

	private void Start()
	{
		if (source == null)
		{
			source = base.transform;
		}
	}

	private void Update()
	{
		float num = (source.position.x * source.position.x + source.position.y * source.position.y + source.position.z * source.position.z) % 1f;
		base.transform.Rotate(rot * num);
	}
}
