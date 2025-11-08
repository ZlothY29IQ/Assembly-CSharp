using TMPro;
using UnityEngine;
using UnityEngine.Localization;

namespace GorillaTagScripts.VirtualStumpCustomMaps.UI;

public class CustomMapsKeyButton : GorillaKeyButton<CustomMapKeyboardBinding>
{
	[SerializeField]
	private bool _isLocalized;

	[SerializeField]
	private LocalizedString _localizedName;

	[SerializeField]
	private TMP_Text _buttonDisplayNameTxt;

	protected override void OnEnableEvents()
	{
		base.OnEnableEvents();
		if (_isLocalized)
		{
			OnLanguageChanged();
			LocalisationManager.RegisterOnLanguageChanged(OnLanguageChanged);
		}
	}

	protected override void OnDisableEvents()
	{
		base.OnDisableEvents();
		if (_isLocalized)
		{
			LocalisationManager.UnregisterOnLanguageChanged(OnLanguageChanged);
		}
	}

	public static string BindingToString(CustomMapKeyboardBinding binding)
	{
		if (binding < CustomMapKeyboardBinding.up || (binding > CustomMapKeyboardBinding.option3 && binding < CustomMapKeyboardBinding.at))
		{
			if (binding >= CustomMapKeyboardBinding.up)
			{
				return binding.ToString();
			}
			int num = (int)binding;
			return num.ToString();
		}
		return binding switch
		{
			CustomMapKeyboardBinding.at => "@", 
			CustomMapKeyboardBinding.dash => "-", 
			CustomMapKeyboardBinding.period => ".", 
			CustomMapKeyboardBinding.underscore => "_", 
			CustomMapKeyboardBinding.plus => "+", 
			CustomMapKeyboardBinding.space => " ", 
			_ => "", 
		};
	}

	protected override void OnButtonPressedEvent()
	{
	}

	private void OnLanguageChanged()
	{
		if (_isLocalized)
		{
			if (_buttonDisplayNameTxt == null)
			{
				Debug.LogError("[LOCALIZATION::CUSTOM_MAPS_KEY_BUTTON] [_buttonDisplayNameTxt] has not been assigned and is NULL", this);
			}
			else if (_localizedName == null || _localizedName.IsEmpty)
			{
				Debug.LogError("[LOCALIZATION::CUSTOM_MAPS_KEY_BUTTON] [_localizedName] has not been assigned", this);
			}
			else
			{
				_buttonDisplayNameTxt.text = _localizedName.GetLocalizedString();
			}
		}
	}
}
