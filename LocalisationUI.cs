using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;

public class LocalisationUI : MonoBehaviour
{
	private static LocalisationUI _instance;

	[Header("Text Components")]
	[SerializeField]
	private TMP_Text _titleTxt;

	[SerializeField]
	private TMP_Text _confirmBtnTxt;

	[Header("UI Setup")]
	[SerializeField]
	private KIDUIButton _languageButtonPrefab;

	[SerializeField]
	private Transform _languageButtonGridTransform;

	[SerializeField]
	private Sprite _activeSprite;

	[SerializeField]
	private Sprite _inactiveSprite;

	[SerializeField]
	private TMP_FontAsset _defaultFont;

	[SerializeField]
	private TMP_FontAsset _japaneseFont;

	private Transform _uiTransform;

	private KIDUIButton _activeButton;

	private List<KIDUIButton> _languageButtons = new List<KIDUIButton>();

	private bool _hasConstructedUI;

	public static LocalisationUI Instance => _instance;

	private void Awake()
	{
		if (_instance != null)
		{
			Object.DestroyImmediate(this);
		}
		else
		{
			_instance = this;
		}
	}

	private void Start()
	{
		ConstructLocalisationUI();
		CheckSelectedLanguage();
	}

	private void OnEnable()
	{
		LocalisationManager.RegisterOnLanguageChanged(OnLanguageChanged);
		if (_hasConstructedUI)
		{
			CheckSelectedLanguage();
		}
	}

	private void OnDisable()
	{
		LocalisationManager.UnregisterOnLanguageChanged(OnLanguageChanged);
	}

	public void OnLanguageButtonPressed(KIDUIButton objRef, int languageIndex)
	{
		if (objRef != _activeButton)
		{
			_activeButton?.SetBorderImage(_inactiveSprite);
			objRef.SetBorderImage(_activeSprite);
			_activeButton = objRef;
		}
		if (LocalisationManager.TryGetLocaleBinding(languageIndex, out var loc))
		{
			LocalisationManager.Instance.OnLanguageButtonPressed(loc.Identifier.Code, saveLanguage: false);
		}
	}

	public void OnContinueButtonPressed()
	{
		HandRayController.Instance.DisableHandRays();
		PrivateUIRoom.RemoveUI(GetUITransform());
		LocalisationManager.OnSaveLanguage();
	}

	private void ConstructLocalisationUI()
	{
		foreach (KeyValuePair<int, Locale> item in LocalisationManager.GetAllBindings())
		{
			KIDUIButton newButton = Object.Instantiate(_languageButtonPrefab, _languageButtonGridTransform);
			bool forceEnglishChars = LocalisationManager.CurrentLanguage.Identifier.Code.ToLower() != "ja";
			newButton.SetText(LocalisationManager.LocaleToFriendlyString(item.Value, forceEnglishChars).ToUpper());
			newButton.onClick.AddListener(delegate
			{
				OnLanguageButtonPressed(newButton, item.Key);
			});
			_languageButtons.Add(newButton);
		}
		_hasConstructedUI = true;
	}

	private void CheckSelectedLanguage()
	{
		KIDUIButton kIDUIButton = null;
		for (int i = 0; i < _languageButtons.Count; i++)
		{
			bool forceEnglishChars = LocalisationManager.CurrentLanguage.Identifier.Code.ToLower() != "ja";
			if (!(_languageButtons[i].GetText() != LocalisationManager.LocaleToFriendlyString(LocalisationManager.CurrentLanguage, forceEnglishChars).ToUpper()))
			{
				kIDUIButton = _languageButtons[i];
				break;
			}
		}
		if (!(kIDUIButton == null))
		{
			if (_activeButton != null)
			{
				_activeButton.SetBorderImage(_inactiveSprite);
			}
			kIDUIButton.SetBorderImage(_activeSprite);
			_activeButton = kIDUIButton;
		}
	}

	private void OnLanguageChanged()
	{
		for (int i = 0; i < _languageButtons.Count; i++)
		{
			bool forceEnglishChar = LocalisationManager.CurrentLanguage.Identifier.Code.ToLower() != "ja";
			_languageButtons[i].SetText(LocalisationManager.LocaleDisplayNameToFriendlyString(_languageButtons[i].GetText(), forceEnglishChar).ToUpper());
			if (!(LocalisationManager.CurrentLanguage.Identifier.Code == "ja"))
			{
				_languageButtons[i].SetFont(_defaultFont);
			}
			else
			{
				_languageButtons[i].SetFont(_japaneseFont);
			}
		}
	}

	public static Transform GetUITransform()
	{
		if (Instance == null)
		{
			return null;
		}
		if (Instance._uiTransform == null)
		{
			Instance._uiTransform = Instance.transform.GetChild(0);
		}
		return Instance._uiTransform;
	}
}
