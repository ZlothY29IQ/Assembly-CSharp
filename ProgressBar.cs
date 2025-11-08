using UnityEngine;
using UnityEngine.UI;

public class ProgressBar : MonoBehaviour
{
	[SerializeField]
	private Image fillImage;

	[SerializeField]
	private bool useColors;

	[SerializeField]
	private Color underCapacity = Color.green;

	[SerializeField]
	private Color overCapacity = Color.red;

	[SerializeField]
	private Color atCapacity = Color.yellow;

	private float _fillAmount;

	public void UpdateProgress(float newFill)
	{
		bool flag = newFill > 1f;
		_fillAmount = Mathf.Clamp(newFill, 0f, 1f);
		fillImage.fillAmount = _fillAmount;
		if (useColors)
		{
			if (flag)
			{
				fillImage.color = overCapacity;
			}
			else if (Mathf.Approximately(_fillAmount, 1f))
			{
				fillImage.color = atCapacity;
			}
			else
			{
				fillImage.color = underCapacity;
			}
		}
	}
}
