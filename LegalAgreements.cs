using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GorillaNetworking;
using Newtonsoft.Json;
using PlayFab;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(1)]
public class LegalAgreements : MonoBehaviour
{
	private static string SCROLL_TO_END_MESSAGE = "<b>Scroll to the bottom</b> to continue.";

	[Header("Scroll Behavior")]
	[SerializeField]
	protected float _minScrollSpeed = 0.02f;

	[SerializeField]
	private float _maxScrollSpeed = 3f;

	[SerializeField]
	private float _scrollInterpTime = 3f;

	[SerializeField]
	private AnimationCurve _scrollInterpCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

	[SerializeField]
	protected Transform uiParent;

	[SerializeField]
	protected TMP_Text tmpBody;

	[SerializeField]
	protected TMP_Text tmpTitle;

	[SerializeField]
	protected Scrollbar scrollBar;

	[SerializeField]
	private LegalAgreementTextAsset[] legalAgreementScreens;

	[SerializeField]
	protected KIDUIButton _pressAndHoldToConfirmButton;

	[SerializeField]
	private TMP_Text _scrollToBottomText;

	[SerializeField]
	private float _stickVibrationStrength = 0.1f;

	[SerializeField]
	private float _stickVibrationDuration = 0.05f;

	protected float stickHeldDuration;

	protected float scrollSpeed;

	private float scrollTime;

	protected bool legalAgreementsStarted;

	protected bool _accepted;

	private string cachedText;

	private int state;

	private bool optIn;

	private bool optional;

	public static LegalAgreements instance { get; private set; }

	protected virtual void Awake()
	{
		if (instance != null)
		{
			Debug.LogError("Trying to set [LegalAgreements] instance but it is not null", this);
			base.gameObject.SetActive(value: false);
			return;
		}
		instance = this;
		stickHeldDuration = 0f;
		scrollSpeed = _minScrollSpeed;
		base.enabled = false;
	}

	private void Update()
	{
		if (!legalAgreementsStarted)
		{
			return;
		}
		float num = Time.deltaTime * scrollSpeed;
		if (ControllerBehaviour.Instance.IsUpStick || ControllerBehaviour.Instance.IsDownStick)
		{
			if (ControllerBehaviour.Instance.IsDownStick)
			{
				num *= -1f;
			}
			scrollBar.value = Mathf.Clamp(scrollBar.value + num, 0f, 1f);
			if (scrollBar.value > 0f && scrollBar.value < 1f)
			{
				HandRayController.Instance.PulseActiveHandray(_stickVibrationStrength, _stickVibrationDuration);
			}
			stickHeldDuration += Time.deltaTime;
			scrollTime = Mathf.Clamp01(stickHeldDuration / _scrollInterpTime);
			scrollSpeed = Mathf.Lerp(_minScrollSpeed, _maxScrollSpeed, _scrollInterpCurve.Evaluate(scrollTime));
			scrollSpeed *= Mathf.Abs(ControllerBehaviour.Instance.StickYValue);
		}
		else
		{
			stickHeldDuration = 0f;
			scrollSpeed = _minScrollSpeed;
		}
		if ((bool)_scrollToBottomText)
		{
			if ((double)scrollBar.value < 0.001)
			{
				_scrollToBottomText.gameObject.SetActive(value: false);
				_pressAndHoldToConfirmButton.gameObject.SetActive(value: true);
			}
			else
			{
				_scrollToBottomText.text = SCROLL_TO_END_MESSAGE;
				_scrollToBottomText.gameObject.SetActive(value: true);
				_pressAndHoldToConfirmButton.gameObject.SetActive(value: false);
			}
		}
	}

	public virtual async Task StartLegalAgreements()
	{
		if (legalAgreementsStarted)
		{
			return;
		}
		legalAgreementsStarted = true;
		while (!PlayFabClientAPI.IsClientLoggedIn())
		{
			if ((bool)PlayFabAuthenticator.instance && PlayFabAuthenticator.instance.loginFailed)
			{
				return;
			}
			await Task.Yield();
		}
		Dictionary<string, string> agreementResults = await GetAcceptedAgreements(legalAgreementScreens);
		LegalAgreementTextAsset[] array = legalAgreementScreens;
		foreach (LegalAgreementTextAsset screen in array)
		{
			string latestVersion = await GetTitleDataAsync(screen.latestVersionKey);
			if (string.IsNullOrEmpty(latestVersion))
			{
				continue;
			}
			string value = string.Empty;
			if ((agreementResults?.TryGetValue(screen.playFabKey, out value) ?? false) && latestVersion == value)
			{
				continue;
			}
			base.enabled = true;
			PrivateUIRoom.ForceStartOverlay(PrivateUIRoom.OverlaySource.KID);
			if (!screen.confirmString.IsNullOrEmpty())
			{
				_pressAndHoldToConfirmButton.SetText(screen.confirmString);
			}
			PrivateUIRoom.AddUI(uiParent);
			HandRayController.Instance.EnableHandRays();
			if (!(await UpdateText(screen, latestVersion)))
			{
				while (true)
				{
					await Task.Yield();
				}
			}
			await WaitForAcknowledgement();
			scrollBar.value = 1f;
			PrivateUIRoom.RemoveUI(uiParent);
			if (agreementResults == null)
			{
				agreementResults = new Dictionary<string, string>();
			}
			agreementResults.AddOrUpdate(screen.playFabKey, latestVersion);
			if (optIn)
			{
				_ = screen.optInAction;
			}
		}
		base.enabled = false;
		await SubmitAcceptedAgreements(agreementResults);
	}

	public void OnAccepted(int currentAge)
	{
		_accepted = true;
	}

	protected async Task WaitForAcknowledgement()
	{
		_accepted = false;
		while (!_accepted)
		{
			await Task.Yield();
		}
		_accepted = false;
	}

	private async Task<bool> UpdateText(LegalAgreementTextAsset asset, string version)
	{
		optional = asset.optional;
		tmpTitle.text = asset.title;
		bool num = await UpdateTextFromPlayFabTitleData(asset.playFabKey, version, tmpBody);
		if (!num)
		{
			tmpBody.text = asset.errorMessage + "\n\nPlease restart the game and try again.";
			scrollBar.value = 0f;
			scrollBar.size = 1f;
		}
		return num;
	}

	public async Task<bool> UpdateTextFromPlayFabTitleData(string key, string version, TMP_Text target)
	{
		string text = key + "_" + version;
		state = 0;
		PlayFabTitleDataCache.Instance.GetTitleData(text, OnTitleDataReceived, OnPlayFabError);
		while (state == 0)
		{
			await Task.Yield();
		}
		if (state == 1)
		{
			string text2 = Regex.Unescape(cachedText.Substring(1, cachedText.Length - 2));
			try
			{
				if (string.IsNullOrEmpty(text2))
				{
					Debug.LogError("[LOCALIZATION] TItle Data for Legal Agreements is NULL or Empty. Unable to deserialize or proceed.");
					return false;
				}
				text2 = JsonConvert.DeserializeObject<TitleDataLocalization>(text2).GetLocalizedText();
			}
			catch (Exception)
			{
				if (text2.StartsWith('{') && text2.EndsWith('}'))
				{
					Debug.LogError("[LOCALIZATION] TItle Data for Legal Agreements is likely in JSON format, but failed to deserialize into [TitleDataLocalization]");
					return false;
				}
			}
			target.text = text2;
			return true;
		}
		return false;
	}

	private void OnPlayFabError(PlayFabError error)
	{
		state = -1;
	}

	private void OnTitleDataReceived(string obj)
	{
		cachedText = obj;
		state = 1;
	}

	private async Task<string> GetTitleDataAsync(string key)
	{
		int state = 0;
		string result = null;
		PlayFabTitleDataCache.Instance.GetTitleData(key, delegate(string res)
		{
			result = res;
			state = 1;
		}, delegate(PlayFabError err)
		{
			result = null;
			state = -1;
			Debug.LogError("[GT/LegalAgreements]  ERROR!!!  GetTitleDataAsync: Encountered error while getting title data: " + err.ErrorMessage);
		});
		while (state == 0)
		{
			await Task.Yield();
		}
		return (state == 1) ? result : null;
	}

	private async Task<Dictionary<string, string>> GetAcceptedAgreements(LegalAgreementTextAsset[] agreements)
	{
		int state = 0;
		Dictionary<string, string> returnValue = new Dictionary<string, string>();
		string[] agreementKeys = agreements.Select((LegalAgreementTextAsset x) => x.playFabKey).ToArray();
		GorillaServer.Instance.GetAcceptedAgreements(new GetAcceptedAgreementsRequest
		{
			AgreementKeys = agreementKeys
		}, delegate(Dictionary<string, string> result)
		{
			state = 1;
			returnValue = result;
		}, delegate(PlayFabError error)
		{
			Debug.LogError(error.ErrorMessage);
			state = -1;
		});
		while (state == 0)
		{
			await Task.Yield();
		}
		return returnValue;
	}

	private async Task SubmitAcceptedAgreements(Dictionary<string, string> agreements)
	{
		int state = 0;
		GorillaServer.Instance.SubmitAcceptedAgreements(new SubmitAcceptedAgreementsRequest
		{
			Agreements = agreements
		}, delegate
		{
			state = 1;
		}, delegate
		{
			state = -1;
		});
		while (state == 0)
		{
			await Task.Yield();
		}
	}

	public void OnDisable()
	{
		KIDAudioManager.Instance?.PlaySoundWithDelay(KIDAudioManager.KIDSoundType.PageTransition);
	}
}
