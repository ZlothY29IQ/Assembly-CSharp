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

	public SITouchscreenButton button;

	[SerializeField]
	private bool autoConfigure = true;

	[NonSerialized]
	private Color _cachedForegroundColor = new Color(-1f, -1f, -1f);

	public bool isUsable { get; private set; }

	public void SetUsable(bool newIsUsable)
	{
		if (_cachedForegroundColor.r < 0f)
		{
			_cachedForegroundColor = foreGround.color;
		}
		isUsable = newIsUsable;
		foreGround.color = (newIsUsable ? _cachedForegroundColor : Color.gray);
		button.isUsable = newIsUsable;
	}
}
