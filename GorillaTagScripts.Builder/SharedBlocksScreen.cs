using UnityEngine;

namespace GorillaTagScripts.Builder;

public class SharedBlocksScreen : MonoBehaviour
{
	public SharedBlocksTerminal.ScreenType screenType;

	public SharedBlocksTerminal terminal;

	public virtual void OnUpPressed()
	{
	}

	public virtual void OnDownPressed()
	{
	}

	public virtual void OnSelectPressed()
	{
	}

	public virtual void OnDeletePressed()
	{
	}

	public virtual void OnNumberPressed(int number)
	{
	}

	public virtual void OnLetterPressed(string letter)
	{
	}

	public virtual void Show()
	{
		if (!base.gameObject.activeSelf)
		{
			base.gameObject.SetActive(value: true);
		}
	}

	public virtual void Hide()
	{
		if (base.gameObject.activeSelf)
		{
			base.gameObject.SetActive(value: false);
		}
	}
}
