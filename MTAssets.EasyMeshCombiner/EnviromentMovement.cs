using UnityEngine;

namespace MTAssets.EasyMeshCombiner;

public class EnviromentMovement : MonoBehaviour
{
	private Vector3 nextPosition = Vector3.zero;

	private Transform thisTransform;

	public Vector3 pos1;

	public Vector3 pos2;

	private void Start()
	{
		thisTransform = base.gameObject.GetComponent<Transform>();
		nextPosition = pos1;
	}

	private void Update()
	{
		if (Vector3.Distance(thisTransform.position, nextPosition) > 0.5f)
		{
			base.transform.position = Vector3.Lerp(thisTransform.position, nextPosition, 2f * Time.deltaTime);
		}
		else if (nextPosition == pos1)
		{
			nextPosition = pos2;
		}
		else if (nextPosition == pos2)
		{
			nextPosition = pos1;
		}
	}
}
