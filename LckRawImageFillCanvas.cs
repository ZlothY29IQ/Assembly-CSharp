using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[ExecuteInEditMode]
public class LckRawImageFillCanvas : UIBehaviour
{
	private enum ScaleType
	{
		Fill,
		Inset,
		Stretch
	}

	[SerializeField]
	private RawImage _rawImage;

	[SerializeField]
	private ScaleType _scaleType;

	private new void OnEnable()
	{
		UpdateSizeDelta();
	}

	private void Update()
	{
		UpdateSizeDelta();
	}

	private void UpdateSizeDelta()
	{
		if (!(_rawImage == null) && !(_rawImage.texture == null))
		{
			RectTransform rectTransform = _rawImage.rectTransform;
			Vector2 sizeDelta = ((RectTransform)rectTransform.parent).sizeDelta;
			Vector2 vector = new Vector2(_rawImage.texture.width, _rawImage.texture.height);
			float num = sizeDelta.x / sizeDelta.y;
			float num2 = vector.x / vector.y;
			float num3 = num / num2;
			Vector2 vector2 = new Vector2(sizeDelta.x, sizeDelta.x / num2);
			Vector2 vector3 = new Vector2(sizeDelta.y * num2, sizeDelta.y);
			switch (_scaleType)
			{
			case ScaleType.Fill:
				rectTransform.sizeDelta = ((num3 > 1f) ? vector2 : vector3);
				break;
			case ScaleType.Inset:
				rectTransform.sizeDelta = ((num3 < 1f) ? vector2 : vector3);
				break;
			case ScaleType.Stretch:
				rectTransform.sizeDelta = sizeDelta;
				break;
			}
		}
	}
}
