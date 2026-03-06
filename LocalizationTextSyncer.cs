using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LocalizationTextSyncer : MonoBehaviour
{
	[Serializable]
	public struct TextCompSyncData
	{
		public TMP_Text textComponent;

		public bool overrideLanguageSettings;

		public List<LocalisationFontPair> _fontOverrides;

		public bool GetOverrideForLanguage(out LocalisationFontPair fontData)
		{
			fontData = default(LocalisationFontPair);
			for (int i = 0; i < _fontOverrides.Count; i++)
			{
				if (_fontOverrides[i].ContainsLocale(LocalisationManager.CurrentLanguage))
				{
					fontData = _fontOverrides[i];
					return true;
				}
			}
			return false;
		}
	}

	[SerializeField]
	[Tooltip("List of all the Text Components - and optional overrides - that will be updated when langauge changes")]
	private List<TextCompSyncData> _textComponentsToSync = new List<TextCompSyncData>();

	[SerializeField]
	[Tooltip("List of optional overrides that will be applied to ALL Text Components on this object")]
	private List<LocalisationFontPair> _universalFontOverrides = new List<LocalisationFontPair>();

	private void Start()
	{
		OnLanguageChanged();
	}

	private void OnEnable()
	{
		LocalisationManager.RegisterOnLanguageChanged(OnLanguageChanged);
		if (!(LocalisationManager.Instance == null))
		{
			OnLanguageChanged();
		}
	}

	private void OnDisable()
	{
		LocalisationManager.UnregisterOnLanguageChanged(OnLanguageChanged);
	}

	private void OnDestroy()
	{
		LocalisationManager.UnregisterOnLanguageChanged(OnLanguageChanged);
	}

	private void OnLanguageChanged()
	{
		LocalisationManager.GetFontAssetForCurrentLocale(out var result);
		LocalisationFontPair fontDataOverride;
		bool flag = TryGetFontDataOverride(out fontDataOverride);
		if (!flag && !LocalisationManager.GetFontAssetForCurrentLocale(out fontDataOverride))
		{
			return;
		}
		foreach (TextCompSyncData item in _textComponentsToSync)
		{
			if (item.textComponent == null)
			{
				continue;
			}
			if (item.overrideLanguageSettings && item.GetOverrideForLanguage(out var fontData))
			{
				fontDataOverride = fontData;
			}
			if (fontDataOverride.fontAsset != null)
			{
				item.textComponent.font = fontDataOverride.fontAsset;
			}
			else
			{
				item.textComponent.font = result.fontAsset;
			}
			if (flag)
			{
				item.textComponent.characterSpacing = fontDataOverride.charSpacing;
				item.textComponent.lineSpacing = fontDataOverride.lineSpacing;
				if (fontDataOverride.fontSize != 0f)
				{
					TMP_Text textComponent = item.textComponent;
					float fontSize = (item.textComponent.fontSizeMax = fontDataOverride.fontSize);
					textComponent.fontSize = fontSize;
				}
			}
		}
	}

	private bool TryGetFontDataOverride(out LocalisationFontPair fontDataOverride)
	{
		fontDataOverride = default(LocalisationFontPair);
		for (int i = 0; i < _universalFontOverrides.Count; i++)
		{
			if (_universalFontOverrides[i].ContainsLocale(LocalisationManager.CurrentLanguage))
			{
				fontDataOverride = _universalFontOverrides[i];
				return true;
			}
		}
		return false;
	}
}
