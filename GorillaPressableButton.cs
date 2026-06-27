using System;
using GorillaExtensions;
using GorillaNetworking;
using GorillaTagScripts;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization;
using UnityEngine.UI;

public class GorillaPressableButton : MonoBehaviour, IClickable
{
	public Material pressedMaterial;

	public Material unpressedMaterial;

	public MeshRenderer buttonRenderer;

	public int pressButtonSoundIndex = 67;

	public bool isOn;

	public float debounceTime = 0.25f;

	public float touchTime;

	public bool testPress;

	public bool testHandLeft;

	[SerializeField]
	private bool _useOnOffText = true;

	[TextArea]
	public string offText;

	[SerializeField]
	private LocalizedString _offLocalizedText;

	[TextArea]
	public string onText;

	[SerializeField]
	private LocalizedString _onLocalizedText;

	[SerializeField]
	[Tooltip("Use this one when you can. Don't use MyText if you can help it!")]
	public TMP_Text myTmpText;

	[SerializeField]
	[Tooltip("Use this one when you can. Don't use MyText if you can help it!")]
	public TMP_Text myTmpText2;

	public Text myText;

	public bool isSubscriberOnlyButton;

	public Material nonSubscriberMaterial;

	protected bool _localPlayerSubscribed;

	private bool _subscriptionChecked;

	[Tooltip("For buttons on cosmetics: when true, only the player wearing this cosmetic can press the button.Leave off for world/UI buttons.")]
	public bool isOwnerOnlyButton;

	[Space]
	public UnityEvent onPressButton;

	protected bool _myTxtSet;

	protected bool _myTmpTxtSet;

	protected bool _myTmpTxt2Set;

	public event Action<GorillaPressableButton, bool> onPressed;

	public virtual void Start()
	{
	}

	protected virtual void OnEnable()
	{
		LocalisationManager.RegisterOnLanguageChanged(RefreshText);
		if (isSubscriberOnlyButton)
		{
			SubscriptionManager.OnLocalSubscriptionData = (Action)Delegate.Combine(SubscriptionManager.OnLocalSubscriptionData, new Action(CheckSubscription));
			CheckSubscription();
		}
		RefreshText();
	}

	protected virtual void OnDisable()
	{
		LocalisationManager.UnregisterOnLanguageChanged(RefreshText);
		if (isSubscriberOnlyButton)
		{
			SubscriptionManager.OnLocalSubscriptionData = (Action)Delegate.Remove(SubscriptionManager.OnLocalSubscriptionData, new Action(CheckSubscription));
		}
	}

	private void CheckSubscription()
	{
		bool flag = SubscriptionManager.IsLocalSubscribed();
		if (!_subscriptionChecked || flag != _localPlayerSubscribed)
		{
			UpdateSubscriptionState(flag);
		}
	}

	private void UpdateSubscriptionState(bool subscribed)
	{
		_localPlayerSubscribed = subscribed;
		UpdateColor();
		_subscriptionChecked = true;
	}

	protected virtual void RefreshText()
	{
		if (_offLocalizedText == null || _offLocalizedText.IsEmpty || _onLocalizedText == null || _onLocalizedText.IsEmpty || !_useOnOffText)
		{
			return;
		}
		string text = "";
		if (!isOn)
		{
			text = offText;
			text = _offLocalizedText.GetLocalizedString();
			if (string.IsNullOrEmpty(text))
			{
				Debug.LogError("[LOCALIZATION::GORILLA_PRESSABLE_BUTTON] Null or empty string returned for OFF localized text", this);
				text = offText;
			}
		}
		else
		{
			text = onText;
			text = _onLocalizedText.GetLocalizedString();
			if (string.IsNullOrEmpty(text))
			{
				Debug.LogError("[LOCALIZATION::GORILLA_PRESSABLE_BUTTON] Null or empty string returned for ON localized text", this);
				text = onText;
			}
		}
		if (_myTxtSet || myText.IsNotNull())
		{
			myText.text = text;
		}
		if (_myTmpTxtSet || myTmpText.IsNotNull())
		{
			myTmpText.text = text;
		}
		if (_myTmpTxt2Set || myTmpText2.IsNotNull())
		{
			myTmpText2.text = text;
		}
	}

	protected virtual void SetOffText(bool setMyText, bool setMyTmpText = false, bool setMyTmpText2 = false)
	{
		if (!_useOnOffText)
		{
			return;
		}
		string localizedString = offText;
		if (_offLocalizedText != null && !_offLocalizedText.IsEmpty)
		{
			localizedString = _offLocalizedText.GetLocalizedString();
			if (string.IsNullOrEmpty(localizedString))
			{
				Debug.LogError("[LOCALIZATION::GORILLA_PRESSABLE_BUTTON] Null or empty string returned for OFF localized text", this);
				localizedString = offText;
			}
		}
		_myTxtSet = setMyText;
		_myTmpTxtSet = setMyTmpText;
		_myTmpTxt2Set = setMyTmpText2;
		if (setMyText)
		{
			myText.text = localizedString;
		}
		if (setMyTmpText)
		{
			myTmpText.text = localizedString;
		}
		if (setMyTmpText2)
		{
			myTmpText2.text = localizedString;
		}
	}

	protected virtual void SetOnText(bool setMyText, bool setMyTmpText = false, bool setMyTmpText2 = false)
	{
		if (!_useOnOffText)
		{
			return;
		}
		string localizedString = onText;
		if (_onLocalizedText != null && !_onLocalizedText.IsEmpty)
		{
			localizedString = _onLocalizedText.GetLocalizedString();
			if (string.IsNullOrEmpty(localizedString))
			{
				Debug.LogError("[LOCALIZATION::GORILLA_PRESSABLE_BUTTON] Null or empty string returned for ON localized text", this);
				localizedString = onText;
			}
		}
		_myTxtSet = setMyText;
		_myTmpTxtSet = setMyTmpText;
		_myTmpTxt2Set = setMyTmpText2;
		if (setMyText)
		{
			myText.text = localizedString;
		}
		if (setMyTmpText)
		{
			myTmpText.text = localizedString;
		}
		if (setMyTmpText2)
		{
			myTmpText2.text = localizedString;
		}
	}

	protected void OnTriggerEnter(Collider collider)
	{
		if (base.enabled && touchTime + debounceTime < Time.time)
		{
			GorillaTriggerColliderHandIndicator component = collider.gameObject.GetComponent<GorillaTriggerColliderHandIndicator>();
			if ((bool)component)
			{
				PressButton(component.isLeftHand);
			}
		}
	}

	private void PressButton(bool isLeftHand)
	{
		if ((!isSubscriberOnlyButton || _localPlayerSubscribed) && (!isOwnerOnlyButton || IsOwnedByLocalPlayer()))
		{
			touchTime = Time.time;
			onPressButton?.Invoke();
			this.onPressed?.Invoke(this, isLeftHand);
			ButtonActivation();
			ButtonActivationWithHand(isLeftHand);
			GorillaTagger.Instance.offlineVRRig.PlayHandTapLocal(pressButtonSoundIndex, isLeftHand, 0.05f);
			GorillaTagger.Instance.StartVibration(isLeftHand, GorillaTagger.Instance.tapHapticStrength / 2f, GorillaTagger.Instance.tapHapticDuration);
			if (NetworkSystem.Instance.InRoom && GorillaTagger.Instance.myVRRig != null)
			{
				GorillaTagger.Instance.myVRRig.SendRPC("RPC_PlayHandTap", RpcTarget.Others, 67, isLeftHand, 0.05f);
			}
		}
	}

	public void Click(bool leftHand = false)
	{
		PressButton(leftHand);
	}

	private bool IsOwnedByLocalPlayer()
	{
		VRRig componentInParent = GetComponentInParent<VRRig>(includeInactive: true);
		if (componentInParent != null)
		{
			return componentInParent.isLocal;
		}
		CosmeticCollectionDisplay componentInParent2 = GetComponentInParent<CosmeticCollectionDisplay>(includeInactive: true);
		if (componentInParent2 != null)
		{
			return componentInParent2.IsLocal;
		}
		return true;
	}

	public virtual void UpdateColor()
	{
		UpdateColorWithState(isOn);
	}

	protected void UpdateColorWithState(bool state)
	{
		if (isSubscriberOnlyButton && !_localPlayerSubscribed)
		{
			SetUnsubscribedMaterial();
			SetOffText(myText.IsNotNull(), myTmpText.IsNotNull(), myTmpText2.IsNotNull());
		}
		else if (state)
		{
			SetPressedMaterial();
			SetOnText(myText.IsNotNull(), myTmpText.IsNotNull(), myTmpText2.IsNotNull());
		}
		else
		{
			SetUnpressedMaterial();
			SetOffText(myText.IsNotNull(), myTmpText.IsNotNull(), myTmpText2.IsNotNull());
		}
	}

	public void SetRendererMaterial(Material mat)
	{
		if ((bool)buttonRenderer)
		{
			buttonRenderer.material = mat;
		}
	}

	public void SetPressedMaterial()
	{
		SetRendererMaterial(pressedMaterial);
	}

	public void SetUnpressedMaterial()
	{
		SetRendererMaterial(unpressedMaterial);
	}

	public void SetUnsubscribedMaterial()
	{
		SetRendererMaterial(nonSubscriberMaterial ? nonSubscriberMaterial : unpressedMaterial);
	}

	public virtual void ButtonActivation()
	{
	}

	public virtual void ButtonActivationWithHand(bool isLeftHand)
	{
	}

	public virtual void ResetState()
	{
		isOn = false;
		UpdateColor();
	}

	public void SetText(string newText)
	{
		if (myTmpText != null)
		{
			myTmpText.text = newText;
		}
		if (myTmpText2 != null)
		{
			myTmpText2.text = newText;
		}
		if (myText != null)
		{
			myText.text = newText;
		}
	}
}
