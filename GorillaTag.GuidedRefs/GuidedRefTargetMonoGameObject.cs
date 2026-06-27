using UnityEngine;

namespace GorillaTag.GuidedRefs;

public class GuidedRefTargetMonoGameObject : MonoBehaviour, IGuidedRefTargetMono, IGuidedRefMonoBehaviour, IGuidedRefObject
{
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

	public Object GuidedRefTargetObject => base.gameObject;

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
