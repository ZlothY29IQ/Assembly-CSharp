using GorillaNetworking;
using PlayFab;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;

public class PlayFabTitleDataTextDisplay : MonoBehaviour, IBuildValidation
{
	[SerializeField]
	private TextMeshPro textBox;

	[SerializeField]
	private Color newUpdateColor = Color.magenta;

	[SerializeField]
	private Color defaultTextColor = Color.white;

	[Tooltip("PlayFab Title Data key from where to pull display text")]
	[SerializeField]
	private string playfabKey;

	[Tooltip("Text to display when error occurs during fetch")]
	[TextArea(3, 5)]
	[SerializeField]
	private string fallbackText;

	[SerializeField]
	private LocalizedString _fallbackLocalizedText;

	private bool _hasRegisteredCallback;

	private string _cachedText = string.Empty;

	public string playFabKeyValue => playfabKey;

	private void Start()
	{
		if (textBox != null)
		{
			textBox.color = defaultTextColor;
		}
		else
		{
			Debug.LogError("The TextBox is null on this PlayFabTitleDataTextDisplay component");
		}
		PlayFabTitleDataCache.Instance.OnTitleDataUpdate.AddListener(OnNewTitleDataAdded);
		PlayFabTitleDataCache.Instance.GetTitleData(playfabKey, OnTitleDataRequestComplete, OnPlayFabError);
		if (!_hasRegisteredCallback)
		{
			LocalisationManager.RegisterOnLanguageChanged(OnLanguageChanged);
		}
	}

	private void OnEnable()
	{
		if (!(LocalisationManager.Instance == null))
		{
			LocalisationManager.RegisterOnLanguageChanged(OnLanguageChanged);
			_hasRegisteredCallback = true;
		}
	}

	private void OnDisable()
	{
		LocalisationManager.UnregisterOnLanguageChanged(OnLanguageChanged);
		_hasRegisteredCallback = false;
	}

	private void OnPlayFabError(PlayFabError error)
	{
		if (!(textBox != null))
		{
			return;
		}
		Debug.LogError("PlayFabTitleDataTextDisplay: PlayFab error retrieving title data for key '" + playfabKey + "' displayed '" + fallbackText + "': " + error.GenerateErrorReport());
		if (_fallbackLocalizedText == null || _fallbackLocalizedText.IsEmpty)
		{
			textBox.text = fallbackText;
			return;
		}
		if (!LocalisationManager.TryGetTranslationForCurrentLocaleWithLocString(_fallbackLocalizedText, out var result, fallbackText))
		{
			Debug.LogError("[LOCALIZATION::PLAYFAB_TITLEDATA_TEXT_DISPLAY] Failed to get key for PlayFab Title Data Text [_fallbackLocalizedText]");
		}
		textBox.text = result;
	}

	private void OnLanguageChanged()
	{
		if (string.IsNullOrEmpty(_cachedText))
		{
			Debug.LogError("[LOCALIZATION::PLAY_FAB_TITLE_DATA_TEXT_DISPLAY] [_cachedText] is not set yet, is this being called before title data has been obtained?");
		}
		else
		{
			PlayFabTitleDataCache.Instance.GetTitleData(playfabKey, OnTitleDataRequestComplete, OnPlayFabError);
		}
	}

	private void OnTitleDataRequestComplete(string titleDataResult)
	{
		if (textBox != null)
		{
			_cachedText = titleDataResult;
			string text = titleDataResult.Replace("\\r", "\r").Replace("\\n", "\n");
			if (text[0] == '"' && text[text.Length - 1] == '"')
			{
				text = text.Substring(1, text.Length - 2);
			}
			textBox.text = text;
		}
	}

	private void OnNewTitleDataAdded(string key)
	{
		if (key == playfabKey && textBox != null)
		{
			textBox.color = newUpdateColor;
		}
	}

	private void OnDestroy()
	{
		PlayFabTitleDataCache.Instance.OnTitleDataUpdate.RemoveListener(OnNewTitleDataAdded);
	}

	public bool BuildValidationCheck()
	{
		if (textBox == null)
		{
			Debug.LogError("text reference is null! sign text will be broken");
			return false;
		}
		return true;
	}

	public void ChangeTitleDataAtRuntime(string newTitleDataKey)
	{
		playfabKey = newTitleDataKey;
		if (textBox != null)
		{
			textBox.color = defaultTextColor;
		}
		else
		{
			Debug.LogError("The TextBox is null on this PlayFabTitleDataTextDisplay component");
		}
		PlayFabTitleDataCache.Instance.OnTitleDataUpdate.AddListener(OnNewTitleDataAdded);
		PlayFabTitleDataCache.Instance.GetTitleData(playfabKey, OnTitleDataRequestComplete, OnPlayFabError);
	}
}
