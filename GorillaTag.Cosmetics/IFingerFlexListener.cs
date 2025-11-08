namespace GorillaTag.Cosmetics;

public interface IFingerFlexListener
{
	public enum ComponentActivator
	{
		FingerReleased,
		FingerFlexed,
		FingerStayed
	}

	bool FingerFlexValidation(bool isLeftHand)
	{
		return true;
	}

	void OnButtonPressed(bool isLeftHand, float value);

	void OnButtonReleased(bool isLeftHand, float value);

	void OnButtonPressStayed(bool isLeftHand, float value);
}
