using UnityEngine;
using UnityEngine.Playables;

public class LocalChestController : MonoBehaviour
{
	public PlayableDirector director;

	public MazePlayerCollection playerCollectionVolume;

	private bool isOpen;

	private void OnTriggerEnter(Collider other)
	{
		if (isOpen)
		{
			return;
		}
		TransformFollow component = other.GetComponent<TransformFollow>();
		if (component == null)
		{
			return;
		}
		Transform transformToFollow = component.transformToFollow;
		if (!(transformToFollow == null))
		{
			VRRig componentInParent = transformToFollow.GetComponentInParent<VRRig>();
			if (!(componentInParent == null) && (!(playerCollectionVolume != null) || playerCollectionVolume.containedRigs.Contains(componentInParent)))
			{
				isOpen = true;
				director.Play();
			}
		}
	}
}
