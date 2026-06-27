using System.Collections;
using GorillaTag;
using GorillaTagScripts.VirtualStumpCustomMaps.UI;
using UnityEngine;

public abstract class CustomMapsScreenTouchPoint : MonoBehaviour, IClickable
{
	public enum TouchPointDirections
	{
		Forward,
		Backward,
		Left,
		Right,
		Up,
		Down
	}

	[SerializeField]
	private CustomMapsTerminalScreen screen;

	[SerializeField]
	private CustomMapKeyboardBinding keyBinding;

	[SerializeField]
	private TouchPointDirections forwardDirection;

	[SerializeField]
	protected SpriteRenderer touchPointRenderer;

	[SerializeField]
	protected ButtonColorSettings buttonColorSettings;

	private static float pressedTime = 0.25f;

	protected static float pressTime;

	private Coroutine colorUpdateCoroutine;

	protected virtual void Awake()
	{
	}

	protected virtual void OnDisable()
	{
		if (colorUpdateCoroutine != null)
		{
			StopCoroutine(colorUpdateCoroutine);
		}
		if (buttonColorSettings != null)
		{
			touchPointRenderer.color = buttonColorSettings.UnpressedColor;
		}
	}

	private void OnTriggerEnter(Collider collider)
	{
		GTDev.Log($"trigger {base.gameObject.name} pressTime={pressTime} time={Time.time}");
		if (Time.time < pressTime + pressedTime || !(collider.GetComponentInParent<GorillaTriggerColliderHandIndicator>() != null))
		{
			return;
		}
		Vector3 rhs = GetForwardDirection();
		if (!(Vector3.Dot((collider.transform.position - base.transform.position).normalized, rhs) < 0f))
		{
			GTDev.Log($"trigger {base.gameObject.name} collider {collider.gameObject.name} postion {collider.transform.position}");
			GorillaTriggerColliderHandIndicator component = collider.GetComponent<GorillaTriggerColliderHandIndicator>();
			pressTime = Time.time;
			OnButtonPressedEvent();
			PressButtonColourUpdate();
			if (screen != null)
			{
				screen.PressButton(keyBinding);
			}
			if (component != null)
			{
				GorillaTagger.Instance.StartVibration(component.isLeftHand, GorillaTagger.Instance.tapHapticStrength / 2f, GorillaTagger.Instance.tapHapticDuration);
			}
		}
	}

	public virtual void PressButtonColourUpdate()
	{
		if (base.gameObject.activeInHierarchy)
		{
			touchPointRenderer.color = buttonColorSettings.PressedColor;
			colorUpdateCoroutine = StartCoroutine(ButtonColorUpdate_Local());
		}
		IEnumerator ButtonColorUpdate_Local()
		{
			yield return new WaitForSeconds(pressedTime);
			if (pressTime != 0f && Time.time > pressedTime + pressTime)
			{
				touchPointRenderer.color = buttonColorSettings.UnpressedColor;
				pressTime = 0f;
			}
		}
	}

	private Vector3 GetForwardDirection()
	{
		return forwardDirection switch
		{
			TouchPointDirections.Forward => base.transform.forward, 
			TouchPointDirections.Backward => -base.transform.forward, 
			TouchPointDirections.Left => -base.transform.right, 
			TouchPointDirections.Right => base.transform.right, 
			TouchPointDirections.Up => base.transform.up, 
			TouchPointDirections.Down => -base.transform.up, 
			_ => base.transform.forward, 
		};
	}

	protected abstract void OnButtonPressedEvent();

	public void Click(bool leftHand = false)
	{
	}
}
