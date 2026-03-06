using UnityEngine;

public class DoNotShip : MonoBehaviour, IBuildValidation
{
	bool IBuildValidation.BuildValidationCheck()
	{
		Debug.LogError("This build has a an object '" + base.gameObject.name + "' in it that was marked as 'Do Not Ship'");
		return false;
	}
}
