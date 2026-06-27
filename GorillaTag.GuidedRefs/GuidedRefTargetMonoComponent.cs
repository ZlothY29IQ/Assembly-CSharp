using UnityEngine;

namespace GorillaTag.GuidedRefs;

public class GuidedRefTargetMonoComponent : MonoBehaviour, IGuidedRefTargetMono, IGuidedRefMonoBehaviour, IGuidedRefObject
{
	[SerializeField]
	private Component targetComponent;

	[SerializeField]
	private GuidedRefBasicTargetInfo guidedRefTargetInfo;

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

	public Object GuidedRefTargetObject => targetComponent;

	protected void Awake()
	{
		((IGuidedRefObject)this).GuidedRefInitialize();
	}

	protected void OnDestroy()
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
