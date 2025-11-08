using GorillaTag;
using UnityEngine;

public class ScienceExperimentSceneElement : MonoBehaviour, ITickSystemPost
{
	public ScienceExperimentElementID elementID;

	private Transform followElement;

	bool ITickSystemPost.PostTickRunning { get; set; }

	void ITickSystemPost.PostTick()
	{
		base.transform.position = followElement.position;
		base.transform.rotation = followElement.rotation;
		base.transform.localScale = followElement.localScale;
	}

	private void Start()
	{
		followElement = ScienceExperimentManager.instance.GetElement(elementID);
		TickSystem<object>.AddPostTickCallback(this);
	}

	private void OnDestroy()
	{
		TickSystem<object>.RemovePostTickCallback(this);
	}
}
