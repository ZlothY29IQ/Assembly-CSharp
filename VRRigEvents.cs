using GorillaTag;
using UnityEngine;

[RequireComponent(typeof(RigContainer))]
public class VRRigEvents : MonoBehaviour, IPreDisable
{
	[SerializeField]
	private RigContainer rigRef;

	public DelegateListProcessor<RigContainer> disableEvent = new DelegateListProcessor<RigContainer>(5);

	public DelegateListProcessor<RigContainer> enableEvent = new DelegateListProcessor<RigContainer>(5);

	public void PreDisable()
	{
		disableEvent?.InvokeSafe(in rigRef);
	}

	public void SendPostEnableEvent()
	{
		enableEvent?.InvokeSafe(in rigRef);
	}
}
