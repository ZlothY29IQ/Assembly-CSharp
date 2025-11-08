using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.UI;

[AddComponentMenu("UI/KIDUI Scroll Rect", 37)]
public class KIDUIScrollRectangle : ScrollRect, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler
{
	private bool _isPointerInside;

	private bool _isHolding;

	private PointerEventData _currentPointerData;

	private Vector2 m_PointerStartLocalCursor = Vector2.zero;

	private Camera thirdPersonCamera;

	private XRUIInputModule InputModule => EventSystem.current.currentInputModule as XRUIInputModule;

	protected override void OnEnable()
	{
		base.OnEnable();
		thirdPersonCamera = GorillaTagger.Instance.thirdPersonCamera.GetComponentInChildren<Camera>();
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
		if (_currentPointerData == null || InputModule == null)
		{
			return;
		}
		if (_currentPointerData.hovered.Contains(base.viewport.gameObject) && !_currentPointerData.hovered.Contains(base.verticalScrollbar.gameObject))
		{
			_isPointerInside = true;
		}
		else
		{
			_isPointerInside = false;
		}
		if (!ControllerBehaviour.Instance.TriggerDown)
		{
			_isHolding = false;
			return;
		}
		XRRayInteractor xRRayInteractor = null;
		if (!(InputModule.GetInteractor(_currentPointerData.pointerId) is XRRayInteractor xRRayInteractor2))
		{
			return;
		}
		xRRayInteractor = xRRayInteractor2;
		if (xRRayInteractor.TryGetCurrentUIRaycastResult(out var raycastResult))
		{
			RectTransformUtility.ScreenPointToLocalPointInRectangle(base.viewRect, raycastResult.screenPosition, thirdPersonCamera, out var localPoint);
			if (!_isHolding && _isPointerInside && ControllerBehaviour.Instance.TriggerDown)
			{
				_isHolding = true;
				m_PointerStartLocalCursor = localPoint;
				m_ContentStartPosition = base.content.anchoredPosition;
			}
			if (_isHolding)
			{
				UpdateBounds();
				Vector2 vector = localPoint - m_PointerStartLocalCursor;
				Vector2 contentAnchoredPosition = m_ContentStartPosition + vector;
				SetContentAnchoredPosition(contentAnchoredPosition);
			}
		}
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		if (eventData.hovered.Contains(base.viewport.gameObject))
		{
			_isPointerInside = true;
			_currentPointerData = eventData;
		}
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		_isPointerInside = false;
	}
}
