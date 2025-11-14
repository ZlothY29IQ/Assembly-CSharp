using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;

[Serializable]
public struct LocalisationFontPair
{
	public List<Locale> locales;

	public TMP_FontAsset fontAsset;

	public Font legacyFontAsset;

	public float charSpacing;

	public float lineSpacing;

	public float fontSize;

	public bool ContainsLocale(Locale locale)
	{
		_ = locales.Count;
		for (int i = 0; i < locales.Count; i++)
		{
			if (!(locales[i] == null) && locales[i].Identifier.Code == locale.Identifier.Code)
			{
				return true;
			}
		}
		return false;
	}
}
