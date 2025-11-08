using UnityEngine;

public class GhostLabButton : GorillaPressableButton, IBuildValidation
{
	public GhostLab ghostLab;

	public int buttonIndex;

	public bool forSingleDoor;

	public bool BuildValidationCheck()
	{
		if (ghostLab == null)
		{
			Debug.LogError("ghostlab is missing", this);
			return false;
		}
		return true;
	}

	public override void ButtonActivation()
	{
		base.ButtonActivation();
		ghostLab.DoorButtonPress(buttonIndex, forSingleDoor);
	}
}
