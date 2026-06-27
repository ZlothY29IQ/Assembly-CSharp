using GorillaExtensions;
using GorillaTag.GuidedRefs;
using UnityEngine;

public class GorillaGeoHideShowTrigger : GorillaTriggerBox, IGuidedRefReceiverMono, IGuidedRefMonoBehaviour, IGuidedRefObject
{
	[SerializeField]
	private GameObject[] makeSureThisIsDisabled;

	[SerializeField]
	private GuidedRefReceiverArrayInfo makeSureThisIsDisabled_gRefs = new GuidedRefReceiverArrayInfo(useRecommendedDefaults: false);

	[SerializeField]
	private GameObject[] makeSureThisIsEnabled;

	[SerializeField]
	private GuidedRefReceiverArrayInfo makeSureThisIsEnabled_gRefs = new GuidedRefReceiverArrayInfo(useRecommendedDefaults: false);

	private bool _guidedRefsAreFullyResolved;

	int IGuidedRefReceiverMono.GuidedRefsWaitingToResolveCount { get; set; }

	protected void Awake()
	{
		((IGuidedRefObject)this).GuidedRefInitialize();
	}

	public override void OnBoxTriggered()
	{
		if (!_guidedRefsAreFullyResolved)
		{
			return;
		}
		GameObject[] array;
		if (makeSureThisIsDisabled != null)
		{
			array = makeSureThisIsDisabled;
			foreach (GameObject gameObject in array)
			{
				if (gameObject == null)
				{
					Debug.LogError("GorillaGeoHideShowTrigger: null item in makeSureThisIsDisabled. \"" + base.transform.GetPath() + "\"", this);
					return;
				}
				gameObject.SetActive(value: false);
			}
		}
		if (makeSureThisIsEnabled == null)
		{
			return;
		}
		array = makeSureThisIsEnabled;
		foreach (GameObject gameObject2 in array)
		{
			if (gameObject2 == null)
			{
				Debug.LogError("GorillaGeoHideShowTrigger: null item in makeSureThisIsDisabled. \"" + base.transform.GetPath() + "\"", this);
				break;
			}
			gameObject2.SetActive(value: true);
		}
	}

	void IGuidedRefObject.GuidedRefInitialize()
	{
		GuidedRefHub.RegisterReceiverArray(this, "makeSureThisIsDisabled", ref makeSureThisIsDisabled, ref makeSureThisIsDisabled_gRefs);
		GuidedRefHub.RegisterReceiverArray(this, "makeSureThisIsEnabled", ref makeSureThisIsEnabled, ref makeSureThisIsEnabled_gRefs);
		GuidedRefHub.ReceiverFullyRegistered(this);
	}

	bool IGuidedRefReceiverMono.GuidedRefTryResolveReference(GuidedRefTryResolveInfo target)
	{
		if (GuidedRefHub.TryResolveArrayItem(this, makeSureThisIsDisabled, makeSureThisIsDisabled_gRefs, target))
		{
			return true;
		}
		if (GuidedRefHub.TryResolveArrayItem(this, makeSureThisIsDisabled, makeSureThisIsEnabled_gRefs, target))
		{
			return true;
		}
		return false;
	}

	void IGuidedRefReceiverMono.OnAllGuidedRefsResolved()
	{
		_guidedRefsAreFullyResolved = true;
	}

	void IGuidedRefReceiverMono.OnGuidedRefTargetDestroyed(int fieldId)
	{
		_guidedRefsAreFullyResolved = false;
	}

	int IGuidedRefObject.GetInstanceID()
	{
		return GetInstanceID();
	}
}
