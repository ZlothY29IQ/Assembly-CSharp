using UnityEngine;
using UnityEngine.UI;

public class CreatorCodeSmallDisplay : MonoBehaviour
{
	public Text codeText;

	private const string CreatorCode = "CREATOR CODE: ";

	private const string CreatorSupported = "SUPPORTED: ";

	private const string NoCreator = "<NONE>";

	private void Awake()
	{
		codeText.text = "CREATOR CODE: <NONE>";
		ATM_Manager.instance.smallDisplays.Add(this);
	}

	public void SetCode(string code)
	{
		if (code == "")
		{
			codeText.text = "CREATOR CODE: <NONE>";
		}
		else
		{
			codeText.text = "CREATOR CODE: " + code;
		}
	}

	public void SuccessfulPurchase(string memberName)
	{
		if (!string.IsNullOrWhiteSpace(memberName))
		{
			codeText.text = "SUPPORTED: " + memberName + "!";
		}
	}
}
