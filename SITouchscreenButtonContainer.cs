using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SITouchscreenButtonContainer : MonoBehaviour
{
	public SITouchscreenButton.SITouchscreenButtonType type;

	public string buttonTextString;

	public int data;

	public RectTransform backGround;

	public RectTransform backgroundShadow;

	public Image foreGround;

	public TextMeshProUGUI buttonText;

	public ITouchScreenStation station;

	[Header("Toggle Visual Settings")]
	public Color toggleOnColor = new Color(0f, 1f, 0.345098f);

	public Color toggleOffColor = new Color(0.5f, 0.5f, 0.5f);

	[Header("Toggle Text Settings")]
	[Tooltip("Text to display when toggle is ON")]
	public string toggleOnText = "ON";

	[Tooltip("Text to display when toggle is OFF")]
	public string toggleOffText = "OFF";

	public SITouchscreenButton button;

	[SerializeField]
	private bool autoConfigure = true;

	[NonSerialized]
	private Color _cachedForegroundColor = new Color(-1f, -1f, -1f);

	public bool isUsable { get; private set; }

	private void Start()
	{
		if (Application.isPlaying && button != null && button.buttonMode == SITouchscreenButton.ButtonMode.Toggle)
		{
			button.buttonToggled.AddListener(OnToggleStateChanged);
			UpdateToggleVisual(button.IsToggledOn);
		}
	}

	private void OnToggleStateChanged(SITouchscreenButton.SITouchscreenButtonType type, int data, int actorNr, bool isToggledOn)
	{
		UpdateToggleVisual(isToggledOn);
	}

	public void UpdateToggleVisual()
	{
		UpdateToggleVisual(button.IsToggledOn);
	}

	private void UpdateToggleVisual(bool isToggledOn)
	{
		if (_cachedForegroundColor.r < 0f)
		{
			_cachedForegroundColor = foreGround.color;
		}
		foreGround.color = (isToggledOn ? toggleOnColor : toggleOffColor);
		buttonText.text = (isToggledOn ? toggleOnText : toggleOffText);
	}

	public void SetUsable(bool newIsUsable)
	{
		if (_cachedForegroundColor.r < 0f)
		{
			_cachedForegroundColor = foreGround.color;
		}
		isUsable = newIsUsable;
		if (button.buttonMode == SITouchscreenButton.ButtonMode.Normal)
		{
			foreGround.color = (newIsUsable ? _cachedForegroundColor : Color.gray);
		}
		button.isUsable = newIsUsable;
	}
}
