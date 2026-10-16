using System;
using Fusion;

[NetworkBehaviourWeaved(0)]
public abstract class AnnoyingNetworkComponent : NetworkComponent, IAnnoyingFeature
{
	public static event Action<IAnnoyingFeature, bool> AlertActive;

	internal override void OnEnable()
	{
		NetworkBehaviourUtils.InternalOnEnable(this);
		base.OnEnable();
		AnnoyingNetworkComponent.AlertActive?.Invoke(this, arg2: true);
	}

	internal override void OnDisable()
	{
		NetworkBehaviourUtils.InternalOnDisable(this);
		base.OnDisable();
		AnnoyingNetworkComponent.AlertActive?.Invoke(this, arg2: false);
	}

	public abstract void Allowed(bool v);

	[WeaverGenerated]
	public override void CopyBackingFieldsToState(bool P_0)
	{
		base.CopyBackingFieldsToState(P_0);
	}

	[WeaverGenerated]
	public override void CopyStateToBackingFields()
	{
		base.CopyStateToBackingFields();
	}
}
