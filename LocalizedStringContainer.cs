using System;
using UnityEngine;
using UnityEngine.Localization;

[Serializable]
public struct LocalizedStringContainer
{
	[SerializeField]
	private LocalizedString StringReference;

	[SerializeField]
	private string FallbackName;

	public string GetName()
	{
		string text = "";
		text = StringReference.GetLocalizedString()?.ToUpper();
		if (string.IsNullOrEmpty(text) || text.ToLower().Contains("no translation found"))
		{
			return FallbackName;
		}
		return text;
	}
}
