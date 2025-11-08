using UnityEngine;

public class SkeletonPathingNode : MonoBehaviour
{
	public bool ejectionPoint;

	public SkeletonPathingNode[] connectedNodes;

	public float distanceToExitNode;

	private void Awake()
	{
		base.gameObject.SetActive(value: false);
	}
}
