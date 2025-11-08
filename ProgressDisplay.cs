using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ProgressDisplay : MonoBehaviour
{
	[SerializeField]
	private GameObject root;

	[SerializeField]
	private TMP_Text text;

	[SerializeField]
	private Image progressImage;

	[SerializeField]
	private int largestNumberToShow = 99;

	private void Reset()
	{
		root = base.gameObject;
	}

	public void SetVisible(bool visible)
	{
		root.SetActive(visible);
	}

	public void SetProgress(int progress, int total)
	{
		if ((bool)text)
		{
			if (total < largestNumberToShow)
			{
				text.text = ((progress >= total) ? $"{total}" : $"{progress}/{total}");
				SetTextVisible(visible: true);
			}
			else
			{
				SetTextVisible(visible: false);
			}
		}
		progressImage.fillAmount = (float)progress / (float)total;
	}

	public void SetProgress(float progress)
	{
		progressImage.fillAmount = progress;
	}

	private void SetTextVisible(bool visible)
	{
		if (text.gameObject.activeSelf != visible)
		{
			text.gameObject.SetActive(visible);
		}
	}
}
