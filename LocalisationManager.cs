using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;

public class LocalisationManager : MonoBehaviour
{
	public const string ENGLISH_IDENTIFIER = "en";

	public const string FRENCH_IDENTIFIER = "fr";

	public const string GERMAN_IDENTIFIER = "de";

	public const string ITALIAN_IDENTIFIER = "it";

	public const string SPANISH_IDENTIFIER = "es";

	public const string JAPENESE_IDENTIFIER = "ja";

	private static LocalisationManager _instance;

	[SerializeField]
	private List<LocalisationFontPair> _localisationFonts = new List<LocalisationFontPair>();

	private bool _cachedHasInitialised;

	private static bool _hasInitialised = false;

	private const string LANGUAGE_SET_PLAYER_PREF = "has-set-language";

	private const string LOC_SYSTEM_PLAYER_PREF = "selected-locale";

	private static Locale _initLocale;

	private static Action _onLanguageChanged;

	private Coroutine _updateLangCoroutine;

	private static CancellationTokenSource _requestCancellationSource;

	private static Dictionary<int, Locale> _localeDisplayBinding = new Dictionary<int, Locale>();

	private static Dictionary<string, StringTable> _localeTablePairs = new Dictionary<string, StringTable>();

	private static Dictionary<string, LocalisationFontPair> _localisationFontDict = new Dictionary<string, LocalisationFontPair>();

	public static LocalisationManager Instance => _instance;

	public static bool IsReady
	{
		get
		{
			if (Instance != null)
			{
				return _localeTablePairs.Count != 0;
			}
			return false;
		}
	}

	public static bool LanguageSet => PlayerPrefs.GetInt("has-set-language", 0) == 1;

	public static Locale CurrentLanguage => LocalizationSettings.SelectedLocale;

	private static string LanugageSetPlayerPrefKey => "selected-locale";

	public static bool ApplicationRunning
	{
		get
		{
			if (Application.isPlaying)
			{
				return !ApplicationQuittingState.IsQuitting;
			}
			return false;
		}
	}

	private void Awake()
	{
		if (_instance != null)
		{
			UnityEngine.Object.DestroyImmediate(this);
			return;
		}
		_instance = this;
		base.transform.SetParent(null);
		UnityEngine.Object.DontDestroyOnLoad(base.gameObject);
		_localisationFontDict.Clear();
		for (int i = 0; i < _localisationFonts.Count; i++)
		{
			for (int j = 0; j < _localisationFonts[i].locales.Count; j++)
			{
				if (!(_localisationFonts[i].locales[j] == null) && !_localisationFontDict.ContainsKey(_localisationFonts[i].locales[j].Identifier.Code) && !(_localisationFonts[i].fontAsset == null))
				{
					_localisationFontDict.Add(_localisationFonts[i].locales[j].Identifier.Code, _localisationFonts[i]);
					Debug.Log("[LOCALIZATION::MANAGER] Added new Locale-Font pair to Dictionary: [" + _localisationFonts[i].locales[j].LocaleName + "]");
				}
			}
		}
		_requestCancellationSource = new CancellationTokenSource();
	}

	private async void Start()
	{
		TryUpdateLanguage(_initLocale, saveLanguage: false);
		if (!LanguageSet)
		{
			HandRayController.Instance.EnableHandRays();
			PrivateUIRoom.AddUI(LocalisationUI.GetUITransform());
		}
	}

	private void OnDestroy()
	{
		_requestCancellationSource.Cancel();
		_onLanguageChanged = null;
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
	private static void InitialiseLocTables()
	{
		CultureInfo.CurrentCulture = new CultureInfo("en");
		CacheLocTables();
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	private static void InitialiseLanguage()
	{
		_hasInitialised = false;
		string text = PlayerPrefs.GetString(LanugageSetPlayerPrefKey, "");
		Locale result = null;
		if (!string.IsNullOrEmpty(text) && LanguageSet)
		{
			LoadPreviousLanguage(text, out result);
		}
		else
		{
			DefaultLocaleFallback(out result);
		}
		MothershipClientApiUnity.SetLanguage(result.Identifier.Code);
		_initLocale = result;
		_hasInitialised = true;
	}

	private static void CacheLocTables()
	{
		_localeTablePairs.Clear();
		_ = Time.time;
		foreach (Locale locale in LocalizationSettings.AvailableLocales.Locales)
		{
			AsyncOperationHandle<IList<StringTable>> allTables = LocalizationSettings.StringDatabase.GetAllTables(locale);
			allTables.WaitForCompletion();
			IList<StringTable> result = allTables.Result;
			if (result.Count != 0)
			{
				_ = result.Count;
				_ = 1;
				_localeTablePairs.Add(locale.Identifier.Code, result[0]);
			}
		}
	}

	public void OnLanguageButtonPressed(string langCode, bool saveLanguage)
	{
		if (TryGetLocaleFromCode(langCode, out var result))
		{
			TryUpdateLanguage(result, saveLanguage);
		}
	}

	private void ReconstructBindings()
	{
		int num = 1;
		_localeDisplayBinding.Clear();
		foreach (Locale locale in LocalizationSettings.AvailableLocales.Locales)
		{
			_localeDisplayBinding.Add(num, locale);
			num++;
		}
	}

	private static void LoadPreviousLanguage(string languageCode, out Locale result)
	{
		if (!TryGetLocaleFromCode(languageCode, out result))
		{
			DefaultLocaleFallback(out result);
			return;
		}
		PlayerPrefs.SetString(LanugageSetPlayerPrefKey, result.Identifier.Code);
		PlayerPrefs.SetInt("has-set-language", 1);
		PlayerPrefs.Save();
	}

	private static void DefaultLocaleFallback(out Locale result)
	{
		if (SysLangToLoc(Application.systemLanguage, out result))
		{
			PlayerPrefs.SetString(LanugageSetPlayerPrefKey, result.Identifier.Code);
			PlayerPrefs.SetInt("has-set-language", 1);
			PlayerPrefs.Save();
		}
	}

	private static bool SysLangToLoc(SystemLanguage sysLanguage, out Locale language)
	{
		switch (sysLanguage)
		{
		default:
			language = LocalizationSettings.Instance.GetAvailableLocales().GetLocale("en");
			_ = language == null;
			return false;
		case SystemLanguage.English:
			language = LocalizationSettings.Instance.GetAvailableLocales().GetLocale("en");
			return language != null;
		case SystemLanguage.French:
			language = LocalizationSettings.Instance.GetAvailableLocales().GetLocale("fr");
			return language != null;
		case SystemLanguage.German:
			language = LocalizationSettings.Instance.GetAvailableLocales().GetLocale("de");
			return language != null;
		case SystemLanguage.Spanish:
			language = LocalizationSettings.Instance.GetAvailableLocales().GetLocale("es");
			return language != null;
		}
	}

	private void TryUpdateLanguage(Locale newLocale, bool saveLanguage = true)
	{
		if (_updateLangCoroutine != null)
		{
			StopCoroutine(_updateLangCoroutine);
		}
		_updateLangCoroutine = StartCoroutine(UpdateLanguage(newLocale, saveLanguage));
	}

	private IEnumerator UpdateLanguage(Locale newLocale, bool saveLanguage)
	{
		if (!_cachedHasInitialised)
		{
			yield return LocalizationSettings.InitializationOperation;
		}
		_cachedHasInitialised = true;
		if (!(CurrentLanguage.Identifier.Code == newLocale.Identifier.Code))
		{
			TelemetryData telemetryData = new TelemetryData
			{
				EventName = "language_changed",
				CustomTags = new string[1] { LocalizationTelemetry.GameVersionCustomTag },
				BodyData = new Dictionary<string, string>
				{
					{
						"starting_language",
						CurrentLanguage.Identifier.Code
					},
					{
						"new_language",
						newLocale.Identifier.Code
					}
				}
			};
			MothershipClientApiUnity.SetLanguage(newLocale.Identifier.Code);
			GorillaTelemetry.EnqueueTelemetryEvent(telemetryData.EventName, telemetryData.BodyData, telemetryData.CustomTags);
			LocalizationSettings.SelectedLocale = newLocale;
			GameEvents.LanguageEvent?.Invoke();
			_onLanguageChanged?.Invoke();
			if (saveLanguage)
			{
				OnSaveLanguage();
			}
		}
	}

	public static bool TryGetLocaleFromCode(string code, out Locale result)
	{
		result = LocalizationSettings.AvailableLocales.GetLocale(code);
		return result != null;
	}

	public static void RegisterOnLanguageChanged(Action callback)
	{
		_onLanguageChanged = (Action)Delegate.Combine(_onLanguageChanged, callback);
	}

	public static void UnregisterOnLanguageChanged(Action callback)
	{
		_onLanguageChanged = (Action)Delegate.Remove(_onLanguageChanged, callback);
	}

	public static bool GetFontAssetForCurrentLocale(out LocalisationFontPair result)
	{
		result = default(LocalisationFontPair);
		if (Instance == null)
		{
			_ = ApplicationRunning;
			return false;
		}
		if (!_localisationFontDict.ContainsKey(CurrentLanguage.Identifier.Code))
		{
			_ = Time.time;
			_ = 10f;
			return false;
		}
		result = _localisationFontDict[CurrentLanguage.Identifier.Code];
		return true;
	}

	public static void OnSaveLanguage()
	{
		PlayerPrefs.SetString(LanugageSetPlayerPrefKey, CurrentLanguage.Identifier.Code);
		PlayerPrefs.SetInt("has-set-language", 1);
		PlayerPrefs.Save();
	}

	public static bool TryGetLocaleBinding(int binding, out Locale loc)
	{
		loc = null;
		if (Instance == null)
		{
			return false;
		}
		if (_localeDisplayBinding.Count != LocalizationSettings.AvailableLocales.Locales.Count)
		{
			Instance.ReconstructBindings();
		}
		return _localeDisplayBinding.TryGetValue(binding, out loc);
	}

	public static Dictionary<int, Locale> GetAllBindings()
	{
		if (_localeDisplayBinding.Count != LocalizationSettings.AvailableLocales.Locales.Count)
		{
			Instance.ReconstructBindings();
		}
		return _localeDisplayBinding;
	}

	public static bool TryGetKeyForCurrentLocale(string key, out string result, string defaultResult = "")
	{
		result = defaultResult;
		if (ApplicationQuittingState.IsQuitting)
		{
			return false;
		}
		if (_localeTablePairs.Count == 0)
		{
			return false;
		}
		if (!_localeTablePairs.TryGetValue(CurrentLanguage.Identifier.Code, out var value))
		{
			return false;
		}
		TableEntry entry = value.GetEntry(key);
		if (entry == null)
		{
			return false;
		}
		if (string.IsNullOrEmpty(entry.LocalizedValue))
		{
			result = defaultResult;
			return true;
		}
		result = entry.LocalizedValue;
		return true;
	}

	public static bool TryGetKeyForEnglishString(string englishString, out string result)
	{
		result = "";
		if (_localeTablePairs.Count == 0)
		{
			return false;
		}
		if (!_localeTablePairs.TryGetValue("en", out var value))
		{
			return false;
		}
		foreach (StringTableEntry value2 in value.Values)
		{
			if (!englishString.Contains(value2.LocalizedValue))
			{
				result = value2.LocalizedValue;
				return true;
			}
		}
		return false;
	}

	public static bool TryGetTranslationForCurrentLocaleWithLocString(LocalizedString key, out string result, string defaultResult = "", UnityEngine.Object context = null)
	{
		result = defaultResult;
		_ = (string)key.TableReference;
		StringTable table = LocalizationSettings.StringDatabase.GetTable(key.TableReference);
		if (table == null)
		{
			return false;
		}
		TableEntry entryFromReference = table.GetEntryFromReference(key.TableEntryReference);
		if (entryFromReference == null)
		{
			return false;
		}
		result = entryFromReference.LocalizedValue;
		return true;
	}

	public static string LocaleToFriendlyString(Locale locale = null, bool forceEnglishChars = false)
	{
		if (locale == null)
		{
			locale = CurrentLanguage;
		}
		switch (locale.Identifier.Code)
		{
		default:
			return "English";
		case "en":
			return "English";
		case "fr":
			return "Français";
		case "de":
			return "Deutsch";
		case "es":
			return "Español";
		case "ja":
			if (forceEnglishChars)
			{
				return "Nihongo";
			}
			return "日本語";
		}
	}

	public static string LocaleDisplayNameToFriendlyString(string locTextName, bool forceEnglishChar = false)
	{
		switch (locTextName)
		{
		default:
			return "English";
		case "ENGLISH":
			return "English";
		case "FRANÇAIS":
		case "FRANCAIS":
			return "Français";
		case "DEUTSCH":
			return "Deutsch";
		case "ESPANOL":
		case "ESPAÑOL":
			return "Español";
		case "JAPANESE":
		case "NIHONGO":
		case "日本語":
			if (forceEnglishChar)
			{
				return "Nihongo";
			}
			return "日本語";
		}
	}
}
