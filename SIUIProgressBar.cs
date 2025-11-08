using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SIUIProgressBar : MonoBehaviour
{
	public Image backgroundImage;

	public Image progressImage;

	public float borderPercent;

	public TextMeshProUGUI progressText;

	public void UpdateFillPercent(float percentFull)
	{
		float num = backgroundImage.rectTransform.sizeDelta.x * (1f - 2f * borderPercent / 100f);
		float num2 = num * Mathf.Min(1f, percentFull);
		float x = (0f - (num - num2)) / 2f * progressImage.rectTransform.localScale.x;
		progressImage.rectTransform.sizeDelta = new Vector2(num2, progressImage.rectTransform.sizeDelta.y);
		progressImage.rectTransform.localPosition = new Vector3(x, progressImage.rectTransform.localPosition.y, progressImage.rectTransform.localPosition.z);
	}
}
