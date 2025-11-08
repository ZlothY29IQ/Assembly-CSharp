using System;
using UnityEngine;

[Serializable]
public class TitleDataLocalization
{
	public string English;

	public string French;

	public string German;

	public string Spanish;

	public string Italian;

	public string Japanese;

	public string GetLocalizedText()
	{
		Debug.Log("TODO: JH - Review localization method");
		return LocalisationManager.CurrentLanguage.Identifier.Code switch
		{
			"fr" => French, 
			"es" => Spanish, 
			"it" => Italian, 
			"de" => German, 
			"ja" => Japanese, 
			_ => English, 
		};
	}
}
