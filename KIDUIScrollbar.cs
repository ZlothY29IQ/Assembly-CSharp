using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.UI;

[AddComponentMenu("UI/KIDUI Scrollbar", 37)]
public class KIDUIScrollbar : Scrollbar, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler
{
	private enum Axis
	{
		Horizontal,
		Vertical
	}

	private float _highlightedVibrationStrength = 0.1f;

	private float _highlightedVibrationDuration = 0.1f;

	private RectTransform containerRect;

	private bool _isPointerInside;

	private bool _isHolding;

	private PointerEventData _currentPointerData;

	private Camera thirdPersonCamera;

	private XRUIInputModule InputModule => EventSystem.current.currentInputModule as XRUIInputModule;

	private Axis axis
	{
		get
		{
			if (base.direction != Direction.LeftToRight && base.direction != Direction.RightToLeft)
			{
				return Axis.Vertical;
			}
			return Axis.Horizontal;
		}
	}

	protected override void OnEnable()
	{
		base.OnEnable();
		containerRect = base.handleRect.parent.GetComponent<RectTransform>();
		if ((bool)GorillaTagger.Instance)
		{
			thirdPersonCamera = GorillaTagger.Instance.thirdPersonCamera.GetComponentInChildren<Camera>();
		}
		if (ControllerBehaviour.Instance != null)
		{
			ControllerBehaviour.Instance.OnAction += PostUpdate;
		}
	}

	protected override void OnDisable()
	{
		base.OnDisable();
		if (ControllerBehaviour.Instance != null)
		{
			ControllerBehaviour.Instance.OnAction -= PostUpdate;
		}
		_isPointerInside = false;
		_currentPointerData = null;
	}

	private void PostUpdate()
	{
		if (!_isPointerInside && !ControllerBehaviour.Instance.TriggerDown)
		{
			_isHolding = false;
		}
		else
		{
			if (!base.interactable || !ControllerBehaviour.Instance.TriggerDown || _currentPointerData == null)
			{
				return;
			}
			if (!_isHolding && _isPointerInside && ControllerBehaviour.Instance.TriggerDown)
			{
				_isHolding = true;
			}
			if (_isHolding && IsInteractable() && !(InputModule == null) && InputModule.GetInteractor(_currentPointerData.pointerId) is XRRayInteractor xRRayInteractor && xRRayInteractor.TryGetCurrentUIRaycastResult(out var raycastResult))
			{
				RectTransformUtility.ScreenPointToLocalPointInRectangle(containerRect, raycastResult.screenPosition, thirdPersonCamera, out var localPoint);
				Vector2 zero = Vector2.zero;
				Vector2 handleCorner = localPoint - zero - containerRect.rect.position - (base.handleRect.rect.size - base.handleRect.sizeDelta) * 0.5f;
				float num = ((axis == Axis.Horizontal) ? containerRect.rect.width : containerRect.rect.height) * (1f - base.size);
				if (!(num <= 0f))
				{
					UpdateDrag(handleCorner, num);
				}
			}
		}
	}

	private void UpdateDrag(Vector2 handleCorner, float remainingSize)
	{
		switch (base.direction)
		{
		case Direction.LeftToRight:
			base.value = Mathf.Clamp01(handleCorner.x / remainingSize);
			break;
		case Direction.RightToLeft:
			base.value = Mathf.Clamp01(1f - handleCorner.x / remainingSize);
			break;
		case Direction.BottomToTop:
			base.value = Mathf.Clamp01(handleCorner.y / remainingSize);
			break;
		case Direction.TopToBottom:
			base.value = Mathf.Clamp01(1f - handleCorner.y / remainingSize);
			break;
		}
	}

	public override void OnPointerEnter(PointerEventData eventData)
	{
		base.OnPointerEnter(eventData);
		_isPointerInside = true;
		_currentPointerData = eventData;
		if (IsInteractable() && InputModule != null && InputModule.GetInteractor(eventData.pointerId) is XRRayInteractor xRRayInteractor)
		{
			xRRayInteractor.xrController.SendHapticImpulse(_highlightedVibrationStrength, _highlightedVibrationDuration);
		}
	}

	public override void OnPointerExit(PointerEventData eventData)
	{
		base.OnPointerExit(eventData);
		_isPointerInside = false;
	}
}
