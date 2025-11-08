using GorillaNetworking;

public class UnlockCompButton : GorillaPressableButton
{
	public string gameMode;

	private bool initialized;

	public override void Start()
	{
		initialized = false;
	}

	public void Update()
	{
		if (testPress)
		{
			testPress = false;
			ButtonActivation();
		}
		if (!initialized && GorillaComputer.instance != null)
		{
			isOn = GorillaComputer.instance.allowedInCompetitive;
			UpdateColor();
			initialized = true;
		}
	}

	public override void ButtonActivation()
	{
		if (!isOn)
		{
			base.ButtonActivation();
			GorillaComputer.instance.CompQueueUnlockButtonPress();
			isOn = true;
			UpdateColor();
		}
	}
}
