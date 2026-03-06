using GorillaExtensions;
using UnityEngine;

public class HotPepperEvents : MonoBehaviour
{
	public enum EdibleState
	{
		A = 1,
		B = 2,
		C = 4,
		D = 8
	}

	[SerializeField]
	private EdibleHoldable _pepper;

	[SerializeField]
	private CosmeticRefID m_targetEffectID = CosmeticRefID.HotPepperFaceEffect;

	private void OnEnable()
	{
		_pepper.onBiteWorld.AddListener(OnBiteWorld);
		_pepper.onBiteView.AddListener(OnBiteView);
	}

	private void OnDisable()
	{
		_pepper.onBiteWorld.RemoveListener(OnBiteWorld);
		_pepper.onBiteView.RemoveListener(OnBiteView);
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
		if (nextState != 8)
		{
			return;
		}
		GameObject gameObject = rig.cosmeticReferences.Get(m_targetEffectID);
		if (!gameObject.IsNull())
		{
			HotPepperFace component = gameObject.GetComponent<HotPepperFace>();
			if (!component.IsNull())
			{
				component.PlayFX(1f);
			}
		}
	}
}
