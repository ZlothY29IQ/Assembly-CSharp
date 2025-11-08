using UnityEngine;

[CreateAssetMenu(fileName = "NewLegalAgreementAsset", menuName = "Gorilla Tag/Legal Agreement Asset")]
public class LegalAgreementTextAsset : ScriptableObject
{
	public enum PostAcceptAction
	{
		NONE
	}

	public string title;

	public string playFabKey;

	public string latestVersionKey;

	[TextArea(3, 5)]
	public string errorMessage;

	public bool optional;

	public PostAcceptAction optInAction;

	public string confirmString;
}
