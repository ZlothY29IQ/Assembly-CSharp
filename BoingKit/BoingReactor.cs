namespace BoingKit;

public class BoingReactor : BoingBehavior
{
	protected override void Register()
	{
		BoingManager.Register(this);
	}

	protected override void Unregister()
	{
		BoingManager.Unregister(this);
	}

	public override void PrepareExecute()
	{
		PrepareExecute(accumulateEffectors: true);
	}
}
