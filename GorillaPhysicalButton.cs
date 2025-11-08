using System;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class GorillaPhysicalButton : MonoBehaviour
{
	public Material pressedMaterial;

	public Material unpressedMaterial;

	public MeshRenderer buttonRenderer;

	public int pressButtonSoundIndex = 67;

	[SerializeField]
	public bool canToggleOn;

	public bool canToggleOff;

	private bool waitingForReleaseAfterStateChange;

	public bool isOn;

	public bool testPress;

	public bool testHandLeft;

	[SerializeField]
	protected float buttonPushDepth = 0.0125f;

	[SerializeField]
	protected float buttonDepthForTrigger = 0.01f;

	[SerializeField]
	public List<Transform> moveableChildren;

	[NonSerialized]
	public List<Vector3> moveableChildrenStartPositions;

	private Vector3 startButtonPosition;

	[TextArea]
	public string offText = "OFF";

	[TextArea]
	public string onText = "ON";

	[SerializeField]
	public TMP_Text textField;

	[Space]
	public UnityEvent onPressButtonOn;

	public UnityEvent onPressButtonToggleOff;

	private Collider recentFingerCollider;

	protected float currentButtonDepthFromPressing;

	private Coroutine buttonTestCoroutine;

	public event Action<GorillaPhysicalButton, bool> onPressedOn;

	public event Action<GorillaPhysicalButton, bool> onToggledOff;

	public virtual void Start()
	{
		if (moveableChildren != null)
		{
			moveableChildrenStartPositions = new List<Vector3>(moveableChildren.Count);
			for (int i = 0; i < moveableChildren.Count; i++)
			{
				moveableChildrenStartPositions.Add(moveableChildren[i].position);
			}
		}
		startButtonPosition = base.transform.position;
		base.enabled = true;
	}

	private void OnEnable()
	{
	}

	private void OnDisable()
	{
	}

	private float GetSurfaceDistanceFromKeyToCollider(Collider collider)
	{
		if (collider == null)
		{
			return 1f;
		}
		SphereCollider sphereCollider = collider as SphereCollider;
		float num = (sphereCollider ? sphereCollider.radius : 0f);
		float num2 = base.transform.localScale.z * 0.5f;
		if (Vector3.Distance(collider.transform.position, base.transform.position) > (base.transform.localScale.magnitude * 0.5f + num) * 1.5f)
		{
			return 1f;
		}
		return Vector3.Dot(base.transform.position - collider.transform.position, -base.transform.forward) - num - num2;
	}

	protected void OnTriggerEnter(Collider other)
	{
		if (base.enabled && !(other.GetComponentInParent<GorillaTriggerColliderHandIndicator>() == null))
		{
			recentFingerCollider = other;
			if (buttonTestCoroutine == null)
			{
				buttonTestCoroutine = StartCoroutine(ButtonUpdate());
			}
		}
	}

	protected IEnumerator ButtonUpdate()
	{
		while (true)
		{
			UpdateButtonFromCollider();
			if (!base.enabled || recentFingerCollider == null)
			{
				break;
			}
			yield return null;
		}
		buttonTestCoroutine = null;
	}

	protected void UpdateButtonFromCollider()
	{
		if (recentFingerCollider != null)
		{
			float surfaceDistanceFromKeyToCollider = GetSurfaceDistanceFromKeyToCollider(recentFingerCollider);
			currentButtonDepthFromPressing -= surfaceDistanceFromKeyToCollider;
			currentButtonDepthFromPressing = Mathf.Clamp(currentButtonDepthFromPressing, 0f, buttonPushDepth);
		}
		else
		{
			currentButtonDepthFromPressing = 0f;
		}
		if (currentButtonDepthFromPressing == 0f)
		{
			if (!canToggleOn && !canToggleOff)
			{
				isOn = false;
			}
			recentFingerCollider = null;
			waitingForReleaseAfterStateChange = false;
		}
		TestForButtonStateChange();
		UpdateButtonVisuals();
	}

	protected void TestForButtonStateChange()
	{
		if (waitingForReleaseAfterStateChange)
		{
			return;
		}
		if (currentButtonDepthFromPressing > buttonDepthForTrigger && !isOn && recentFingerCollider != null)
		{
			isOn = true;
			waitingForReleaseAfterStateChange = true;
			GorillaTriggerColliderHandIndicator component = recentFingerCollider.GetComponent<GorillaTriggerColliderHandIndicator>();
			if (!(component == null))
			{
				onPressButtonOn?.Invoke();
				this.onPressedOn?.Invoke(this, component.isLeftHand);
				ButtonPressedOn();
				ButtonPressedOnWithHand(component.isLeftHand);
				GorillaTagger.Instance.offlineVRRig.PlayHandTapLocal(pressButtonSoundIndex, component.isLeftHand, 0.05f);
				GorillaTagger.Instance.StartVibration(component.isLeftHand, GorillaTagger.Instance.tapHapticStrength / 2f, GorillaTagger.Instance.tapHapticDuration);
				if (NetworkSystem.Instance.InRoom && GorillaTagger.Instance.myVRRig != null)
				{
					GorillaTagger.Instance.myVRRig.SendRPC("RPC_PlayHandTap", RpcTarget.Others, 67, component.isLeftHand, 0.05f);
				}
			}
		}
		else
		{
			if (!(currentButtonDepthFromPressing > buttonDepthForTrigger) || !canToggleOff || !isOn || !(recentFingerCollider != null))
			{
				return;
			}
			isOn = false;
			waitingForReleaseAfterStateChange = true;
			GorillaTriggerColliderHandIndicator component2 = recentFingerCollider.GetComponent<GorillaTriggerColliderHandIndicator>();
			if (!(component2 == null))
			{
				onPressButtonToggleOff?.Invoke();
				this.onToggledOff?.Invoke(this, component2.isLeftHand);
				ButtonToggledOff();
				ButtonToggledOffWithHand(component2.isLeftHand);
				GorillaTagger.Instance.offlineVRRig.PlayHandTapLocal(pressButtonSoundIndex, component2.isLeftHand, 0.05f);
				GorillaTagger.Instance.StartVibration(component2.isLeftHand, GorillaTagger.Instance.tapHapticStrength / 2f, GorillaTagger.Instance.tapHapticDuration);
				if (NetworkSystem.Instance.InRoom && GorillaTagger.Instance.myVRRig != null)
				{
					GorillaTagger.Instance.myVRRig.SendRPC("RPC_PlayHandTap", RpcTarget.Others, 67, component2.isLeftHand, 0.05f);
				}
			}
		}
	}

	protected void UpdateButtonVisuals()
	{
		float num = currentButtonDepthFromPressing;
		if ((canToggleOff || canToggleOn) && isOn)
		{
			num = Mathf.Max(buttonDepthForTrigger, num);
		}
		base.transform.position = startButtonPosition - base.transform.forward * num;
		if (moveableChildren != null)
		{
			for (int i = 0; i < moveableChildren.Count; i++)
			{
				moveableChildren[i].position = moveableChildrenStartPositions[i] - base.transform.forward * num;
			}
		}
		UpdateColorWithState(isOn);
	}

	protected void UpdateColorWithState(bool state)
	{
		if (state)
		{
			buttonRenderer.material = pressedMaterial;
			if ((!string.IsNullOrEmpty(onText) || !string.IsNullOrEmpty(offText)) && textField != null)
			{
				textField.text = onText;
			}
		}
		else
		{
			buttonRenderer.material = unpressedMaterial;
			if ((!string.IsNullOrEmpty(offText) || !string.IsNullOrEmpty(onText)) && textField != null)
			{
				textField.text = offText;
			}
		}
	}

	public virtual void ButtonPressedOn()
	{
	}

	public virtual void ButtonPressedOnWithHand(bool isLeftHand)
	{
	}

	public virtual void ButtonToggledOff()
	{
	}

	public virtual void ButtonToggledOffWithHand(bool isLeftHand)
	{
	}

	public virtual void ResetState()
	{
		isOn = false;
		currentButtonDepthFromPressing = 0f;
		waitingForReleaseAfterStateChange = false;
		UpdateButtonVisuals();
	}

	public void SetText(string newText)
	{
		if (textField != null)
		{
			textField.text = offText;
		}
	}

	public virtual void SetButtonState(bool setToOn)
	{
		if (!canToggleOn && !canToggleOff)
		{
			return;
		}
		if (isOn != setToOn)
		{
			isOn = setToOn;
			if (isOn)
			{
				onPressButtonOn?.Invoke();
				ButtonPressedOn();
			}
			else
			{
				onPressButtonToggleOff?.Invoke();
				ButtonToggledOff();
			}
		}
		UpdateButtonVisuals();
	}
}
