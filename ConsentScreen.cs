using System;
using System.Collections.Generic;
using System.Text;
using GorillaLocomotion;
using TMPro;
using UnityEngine;

public class ConsentScreen : MonoBehaviour
{
	public struct ConsentCost
	{
		public string DisplayName;

		public int Amount;

		public int CurrentBalance;

		public bool HasBalance;
	}

	private enum PopupState
	{
		Hidden,
		Prompt,
		Processing,
		Result
	}

	[Header("Wiring")]
	[SerializeField]
	private GameObject popupRoot;

	[SerializeField]
	private TextMeshProUGUI promptText;

	[SerializeField]
	private TextMeshProUGUI resultText;

	[SerializeField]
	private ConsentHoldButton yesButton;

	[SerializeField]
	private ConsentHoldButton noButton;

	[Header("Timing")]
	[SerializeField]
	private float resultDisplaySeconds = 3f;

	[Header("Dismissal")]
	[Tooltip("Prompt auto-dismisses (counts as NO) when the player moves this many meters from where it appeared.")]
	[SerializeField]
	private float dismissDistanceMeters = 5f;

	[Header("Popup Cue")]
	[SerializeField]
	private AudioSource appearSound;

	[Tooltip("Haptic buzz on the watch hand when the prompt appears")]
	[SerializeField]
	private float appearHapticScale = 1f;

	[SerializeField]
	private float appearHapticDuration = 0.15f;

	[Header("Placement")]
	[Tooltip("How far above the VStump watch the popup floats, in meters at 1x player scale.")]
	[SerializeField]
	private float hoverHeightMeters = 0.15f;

	[SerializeField]
	private float faceOffsetMeters = 0.08f;

	[Tooltip("Used only when the watch can't be found: popup floats this far in front of the face.")]
	[SerializeField]
	private float fallbackForwardMeters = 0.45f;

	[Tooltip("Used only when the watch can't be found: popup sits this far below eye level.")]
	[SerializeField]
	private float fallbackDownMeters = 0.05f;

	[Header("Text")]
	[SerializeField]
	private string processingText = "Processing...";

	private static ConsentScreen _activeReference;

	private PopupState state;

	private Action<bool, Action<string>> pendingCallback;

	private Vector3 promptOrigin;

	private bool hasPromptOrigin;

	private float resultHideAt;

	private Transform watchAnchor;

	private void Awake()
	{
		if (_activeReference == null)
		{
			_activeReference = this;
		}
		else if (_activeReference != this)
		{
			return;
		}
		if (popupRoot == null || promptText == null || resultText == null || yesButton == null || noButton == null)
		{
			Debug.LogError("[ConsentScreen] Popup references are not wired on the prefab; consent will be denied by default");
			popupRoot = null;
			return;
		}
		yesButton.HoldComplete += delegate
		{
			OnChoice(consented: true);
		};
		noButton.HoldComplete += delegate
		{
			OnChoice(consented: false);
		};
		popupRoot.SetActive(value: false);
	}

	private void OnDestroy()
	{
		if (_activeReference == this)
		{
			_activeReference = null;
		}
	}

	public static void StartConsentFlow(string itemDisplayName, List<ConsentCost> costs, Action<bool, Action<string>> OnConsentChosen)
	{
		if (_activeReference == null || _activeReference.popupRoot == null)
		{
			Debug.LogError("[ConsentScreen] No active consent screen instance; denying consent by default");
			OnConsentChosen?.Invoke(arg1: false, delegate
			{
			});
		}
		else
		{
			_activeReference.ShowPrompt(itemDisplayName, costs, OnConsentChosen);
		}
	}

	private void ShowPrompt(string itemDisplayName, List<ConsentCost> costs, Action<bool, Action<string>> onConsentChosen)
	{
		pendingCallback = onConsentChosen;
		promptText.text = ComposePrompt(itemDisplayName, costs);
		promptText.gameObject.SetActive(value: true);
		resultText.gameObject.SetActive(value: false);
		SetButtonsVisible(visible: true);
		state = PopupState.Prompt;
		popupRoot.SetActive(value: true);
		UpdatePopupTransform();
		Transform transform = ResolveHead();
		hasPromptOrigin = transform != null;
		promptOrigin = (hasPromptOrigin ? transform.position : Vector3.zero);
		PlayAppearCue();
	}

	private void PlayAppearCue()
	{
		if (appearSound != null)
		{
			appearSound.Play();
		}
		if (GorillaTagger.Instance != null)
		{
			GorillaTagger.Instance.StartVibration(forLeftController: true, GorillaTagger.Instance.tapHapticStrength * appearHapticScale, appearHapticDuration);
		}
	}

	private static string ComposePrompt(string itemDisplayName, List<ConsentCost> costs)
	{
		StringBuilder stringBuilder = new StringBuilder();
		if (!string.IsNullOrEmpty(itemDisplayName))
		{
			stringBuilder.Append("<b>Purchase " + itemDisplayName + "?</b>\n\n");
		}
		if (costs != null)
		{
			foreach (ConsentCost cost in costs)
			{
				string arg = (string.IsNullOrEmpty(cost.DisplayName) ? "credits" : cost.DisplayName);
				stringBuilder.Append($"<b>You are about to spend {cost.Amount} {arg}.</b>\n\n");
				if (cost.HasBalance)
				{
					stringBuilder.Append($"This will leave you with {cost.CurrentBalance - cost.Amount} {arg}.\n\n");
				}
			}
		}
		stringBuilder.Append("<b>Are you sure?</b>");
		return stringBuilder.ToString();
	}

	private void OnChoice(bool consented)
	{
		if (state == PopupState.Prompt)
		{
			Action<bool, Action<string>> action = pendingCallback;
			pendingCallback = null;
			if (consented)
			{
				SetButtonsVisible(visible: false);
				ShowResultText(processingText);
				state = PopupState.Processing;
			}
			else
			{
				Hide();
			}
			action?.Invoke(consented, OnAsyncWorkComplete);
		}
	}

	private async void OnAsyncWorkComplete(string result)
	{
		await Awaitable.MainThreadAsync();
		if (this == null || popupRoot == null)
		{
			return;
		}
		if (string.IsNullOrEmpty(result))
		{
			if (state == PopupState.Processing)
			{
				Hide();
			}
		}
		else if (state == PopupState.Processing)
		{
			SetButtonsVisible(visible: false);
			ShowResultText(result);
			resultHideAt = Time.time + resultDisplaySeconds;
			state = PopupState.Result;
			popupRoot.SetActive(value: true);
		}
	}

	private void Hide()
	{
		state = PopupState.Hidden;
		pendingCallback = null;
		popupRoot.SetActive(value: false);
	}

	private void SetButtonsVisible(bool visible)
	{
		yesButton.gameObject.SetActive(visible);
		noButton.gameObject.SetActive(visible);
		if (visible)
		{
			yesButton.ResetHold();
			noButton.ResetHold();
		}
	}

	private void ShowResultText(string message)
	{
		promptText.gameObject.SetActive(value: false);
		resultText.gameObject.SetActive(value: true);
		resultText.text = message;
	}

	private void Update()
	{
		switch (state)
		{
		case PopupState.Prompt:
		{
			Transform transform = ResolveHead();
			if (transform != null)
			{
				if (!hasPromptOrigin)
				{
					promptOrigin = transform.position;
					hasPromptOrigin = true;
				}
				else if (Vector3.Distance(transform.position, promptOrigin) >= dismissDistanceMeters)
				{
					OnChoice(consented: false);
				}
			}
			break;
		}
		case PopupState.Result:
			if (Time.time >= resultHideAt)
			{
				Hide();
			}
			break;
		}
	}

	private void LateUpdate()
	{
		if (state != PopupState.Hidden)
		{
			UpdatePopupTransform();
		}
	}

	private void UpdatePopupTransform()
	{
		Transform transform = ResolveHead();
		Transform transform2 = ResolveWatchAnchor();
		float num = ResolvePlayerScale();
		Vector3 vector;
		if (transform2 != null)
		{
			vector = transform2.position + Vector3.up * (hoverHeightMeters * num);
			if (transform != null)
			{
				Vector3 vector2 = transform.position - vector;
				if (vector2.sqrMagnitude > 0.0001f)
				{
					vector += vector2.normalized * (faceOffsetMeters * num);
				}
			}
		}
		else
		{
			if (!(transform != null))
			{
				return;
			}
			vector = transform.position + transform.forward * (fallbackForwardMeters * num) - Vector3.up * (fallbackDownMeters * num);
		}
		popupRoot.transform.position = vector;
		popupRoot.transform.localScale = Vector3.one * num;
		if (transform != null)
		{
			Vector3 forward = vector - transform.position;
			if (forward.sqrMagnitude > 0.0001f)
			{
				popupRoot.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
			}
		}
	}

	private static Transform ResolveHead()
	{
		if (GorillaTagger.Instance != null && GorillaTagger.Instance.mainCamera != null)
		{
			return GorillaTagger.Instance.mainCamera.transform;
		}
		return null;
	}

	private Transform ResolveWatchAnchor()
	{
		if (watchAnchor != null)
		{
			return watchAnchor;
		}
		if (VRRig.LocalRig != null && VRRig.LocalRig.vStumpReturnWatch != null)
		{
			watchAnchor = VRRig.LocalRig.vStumpReturnWatch.transform;
		}
		return watchAnchor;
	}

	private static float ResolvePlayerScale()
	{
		if (VRRig.LocalRig != null)
		{
			return VRRig.LocalRig.scaleFactor;
		}
		if (GTPlayer.Instance != null)
		{
			return GTPlayer.Instance.scale;
		}
		return 1f;
	}
}
