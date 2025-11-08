using System;
using System.Collections;
using GorillaExtensions;
using GorillaTag;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Events;

public abstract class GorillaKeyButton<TBinding> : MonoBehaviour where TBinding : Enum
{
	public string characterString;

	public TBinding Binding;

	public bool functionKey;

	public Renderer ButtonRenderer;

	public ButtonColorSettings ButtonColorSettings;

	[Tooltip("These GameObjects will be Activated/Deactivated when this button is Activated/Deactivated")]
	public GameObject[] linkedObjects;

	[Tooltip("Intended for use with GorillaKeyWrapper")]
	public UnityEvent<TBinding> OnKeyButtonPressed = new UnityEvent<TBinding>();

	public bool testClick;

	public bool repeatTestClick;

	public float repeatCooldown = 2f;

	private float pressTime;

	private float lastTestClick;

	protected MaterialPropertyBlock propBlock;

	private void Awake()
	{
		if (ButtonRenderer == null)
		{
			ButtonRenderer = GetComponent<Renderer>();
		}
		propBlock = new MaterialPropertyBlock();
		pressTime = 0f;
	}

	private void OnEnable()
	{
		for (int i = 0; i < linkedObjects.Length; i++)
		{
			if (linkedObjects[i].IsNotNull())
			{
				linkedObjects[i].SetActive(value: true);
			}
		}
		OnEnableEvents();
	}

	private void OnDisable()
	{
		for (int i = 0; i < linkedObjects.Length; i++)
		{
			if (linkedObjects[i].IsNotNull())
			{
				linkedObjects[i].SetActive(value: false);
			}
		}
		OnDisableEvents();
	}

	private void OnTriggerEnter(Collider collider)
	{
		GorillaTriggerColliderHandIndicator componentInParent = collider.GetComponentInParent<GorillaTriggerColliderHandIndicator>();
		if ((bool)componentInParent)
		{
			PressButton(componentInParent.isLeftHand);
		}
	}

	private void PressButton(bool isLeftHand)
	{
		OnButtonPressedEvent();
		OnKeyButtonPressed?.Invoke(Binding);
		PressButtonColourUpdate();
		GorillaTagger.Instance.StartVibration(isLeftHand, GorillaTagger.Instance.tapHapticStrength / 2f, GorillaTagger.Instance.tapHapticDuration);
		GorillaTagger.Instance.offlineVRRig.PlayHandTapLocal(66, isLeftHand, 0.1f);
		if (NetworkSystem.Instance.InRoom && GorillaTagger.Instance.myVRRig != null)
		{
			GorillaTagger.Instance.myVRRig.SendRPC("RPC_PlayHandTap", RpcTarget.Others, 66, isLeftHand, 0.1f);
		}
	}

	protected virtual void OnEnableEvents()
	{
	}

	protected virtual void OnDisableEvents()
	{
	}

	public void Click(bool leftHand = false)
	{
		PressButton(leftHand);
	}

	public virtual void PressButtonColourUpdate()
	{
		if (base.gameObject.activeInHierarchy)
		{
			propBlock.SetColor(ShaderProps._BaseColor, ButtonColorSettings.PressedColor);
			propBlock.SetColor(ShaderProps._Color, ButtonColorSettings.PressedColor);
			ButtonRenderer.SetPropertyBlock(propBlock);
			pressTime = Time.time;
			StartCoroutine(ButtonColorUpdate_Local());
		}
		IEnumerator ButtonColorUpdate_Local()
		{
			yield return new WaitForSeconds(ButtonColorSettings.PressedTime);
			if (pressTime != 0f && Time.time > ButtonColorSettings.PressedTime + pressTime)
			{
				propBlock.SetColor(ShaderProps._BaseColor, ButtonColorSettings.UnpressedColor);
				propBlock.SetColor(ShaderProps._Color, ButtonColorSettings.UnpressedColor);
				ButtonRenderer.SetPropertyBlock(propBlock);
				pressTime = 0f;
			}
		}
	}

	protected abstract void OnButtonPressedEvent();
}
