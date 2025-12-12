using Cosmetics;
using UnityEngine;

public class CreatorCodeProvider : MonoBehaviour, ICreatorCodeProvider, IBuildValidation
{
	[SerializeField]
	private NexusCreatorCode nexusCreatorCode;

	string ICreatorCodeProvider.TerminalId => nexusCreatorCode.GroupId.Code + nexusCreatorCode.Code;

	GameObject ICreatorCodeProvider.GameObject => base.gameObject;

	bool IBuildValidation.BuildValidationCheck()
	{
		if (nexusCreatorCode == null)
		{
			Debug.LogError("The CreatorCodeProvider component on " + base.name + " must be assigned a nexusCreatorCode.");
			return false;
		}
		return true;
	}

	void ICreatorCodeProvider.GetCreatorCode(out string code, out NexusGroupId[] groups)
	{
		code = nexusCreatorCode.Code;
		groups = new NexusGroupId[1] { nexusCreatorCode.GroupId };
	}
}
