using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BubbleGumEvents : MonoBehaviour
{
	public enum EdibleState
	{
		A = 1,
		B = 2,
		C = 4,
		D = 8
	}

	[SerializeField]
	private EdibleHoldable _edible;

	[SerializeField]
	private GumBubble _bubble;

	private static Dictionary<GameObject, GumBubble> gTargetCache = new Dictionary<GameObject, GumBubble>(16);

	private void OnEnable()
	{
		_edible.onBiteWorld.AddListener(OnBiteWorld);
		_edible.onBiteView.AddListener(OnBiteView);
	}

	private void OnDisable()
	{
		_edible.onBiteWorld.RemoveListener(OnBiteWorld);
		_edible.onBiteView.RemoveListener(OnBiteView);
	}

	public void OnBiteView(VRRig rig, int nextState)
	{
		OnBite(rig, nextState, isViewRig: true);
	}

	public void OnBiteWorld(VRRig rig, int nextState)
	{
		OnBite(rig, nextState, isViewRig: false);
	}

	public void OnBite(VRRig rig, int nextState, bool isViewRig)
	{
		GorillaTagger instance = GorillaTagger.Instance;
		GameObject gameObject = null;
		if (isViewRig && instance != null)
		{
			gameObject = instance.gameObject;
		}
		else if (!isViewRig)
		{
			gameObject = rig.gameObject;
		}
		if (!gTargetCache.TryGetValue(gameObject, out _bubble))
		{
			_bubble = gameObject.GetComponentsInChildren<GumBubble>(includeInactive: true).FirstOrDefault((GumBubble g) => g.transform.parent.name == "$gum");
			if (isViewRig)
			{
				_bubble.audioSource = instance.offlineVRRig.tagSound;
				_bubble.targetScale = Vector3.one * 1.36f;
			}
			else
			{
				_bubble.audioSource = rig.tagSound;
				_bubble.targetScale = Vector3.one * 2f;
			}
			gTargetCache.Add(gameObject, _bubble);
		}
		_bubble?.transform.parent.gameObject.SetActive(value: true);
		_bubble?.InflateDelayed();
	}
}
