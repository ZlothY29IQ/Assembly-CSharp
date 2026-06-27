using UnityEngine;

namespace GorillaTag.GuidedRefs;

public abstract class BaseGuidedRefTargetMono : MonoBehaviour, IGuidedRefTargetMono, IGuidedRefMonoBehaviour, IGuidedRefObject
{
	public GuidedRefBasicTargetInfo guidedRefTargetInfo;

	GuidedRefBasicTargetInfo IGuidedRefTargetMono.GRefTargetInfo
	{
		get
		{
			return guidedRefTargetInfo;
		}
		set
		{
			guidedRefTargetInfo = value;
		}
	}

	Object IGuidedRefTargetMono.GuidedRefTargetObject => this;

	protected virtual void Awake()
	{
		((IGuidedRefObject)this).GuidedRefInitialize();
	}

	protected virtual void OnDestroy()
	{
		GuidedRefHub.UnregisterTarget(this);
	}

	void IGuidedRefObject.GuidedRefInitialize()
	{
		GuidedRefHub.RegisterTarget(this, guidedRefTargetInfo.hubIds, this);
	}

	int IGuidedRefObject.GetInstanceID()
	{
		return GetInstanceID();
	}
}
