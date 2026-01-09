using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Events;

[DisallowMultipleComponent]
public class LocalizedText : LocalizeStringEvent
{
	[SerializeField]
	private bool _isLocalized;

	[SerializeField]
	private bool _isNewKey;

	[SerializeField]
	private string _newKeyName;

	[SerializeField]
	private ELocale _previewLocale;

	[SerializeField]
	private List<LocalisationFontPair> _localisationFontsOverrides = new List<LocalisationFontPair>();

	private static List<ELocale> _cachedELocalesList = new List<ELocale>();

	private TextComponentLegacySupportStore _textComponent;

	private TextComponentLegacySupportStore TextComponent
	{
		get
		{
			if (!_textComponent.IsValid)
			{
				_textComponent = new TextComponentLegacySupportStore(base.transform);
			}
			return _textComponent;
		}
	}

	public bool HasFontOverrides()
	{
		return _localisationFontsOverrides.Count > 0;
	}

	private void Awake()
	{
		_textComponent = new TextComponentLegacySupportStore(base.transform);
		base.OnUpdateString = new UnityEventString();
		base.OnUpdateString.AddListener(delegate(string val)
		{
			OnLocaleChanged(val);
		});
		if (!TextComponent.IsValid)
		{
			base.gameObject.AddComponent<TMP_Text>();
			_textComponent = new TextComponentLegacySupportStore(base.transform);
		}
	}

	protected override async void UpdateString(string value)
	{
		if (LocalisationManager.ApplicationRunning && !LocalisationManager.IsReady)
		{
			await Task.Yield();
		}
		base.UpdateString(value);
	}

	private async void OnLocaleChanged(string newText)
	{
		if (LocalisationManager.ApplicationRunning && !LocalisationManager.IsReady)
		{
			await Task.Yield();
		}
		if (ApplicationQuittingState.IsQuitting)
		{
			return;
		}
		if (GetLocalizedFonts(out var fontData))
		{
			if (fontData.fontAsset == null)
			{
				if (LocalisationManager.GetFontAssetForCurrentLocale(out var result))
				{
					TextComponent.SetFont(result.fontAsset, result.legacyFontAsset);
				}
			}
			else
			{
				TextComponent.SetFont(fontData.fontAsset, fontData.legacyFontAsset);
			}
			if (fontData.fontSize != 0f && HasFontOverrides())
			{
				TextComponent.SetFontSize(fontData.fontSize);
			}
		}
		else
		{
			_ = Time.time;
			_ = 10f;
		}
		if (HasFontOverrides())
		{
			TextComponent.SetCharSpacing(fontData.charSpacing);
		}
		TextComponent.SetText(newText);
	}

	private bool GetLocalizedFonts(out LocalisationFontPair fontData)
	{
		fontData = default(LocalisationFontPair);
		if (!HasFontOverrides())
		{
			return LocalisationManager.GetFontAssetForCurrentLocale(out fontData);
		}
		for (int i = 0; i < _localisationFontsOverrides.Count; i++)
		{
			if (_localisationFontsOverrides[i].ContainsLocale(LocalisationManager.CurrentLanguage))
			{
				fontData = new LocalisationFontPair
				{
					fontAsset = _localisationFontsOverrides[i].fontAsset,
					legacyFontAsset = _localisationFontsOverrides[i].legacyFontAsset,
					charSpacing = _localisationFontsOverrides[i].charSpacing
				};
				return true;
			}
		}
		return LocalisationManager.GetFontAssetForCurrentLocale(out fontData);
	}
}
