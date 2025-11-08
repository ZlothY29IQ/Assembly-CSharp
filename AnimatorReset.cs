using UnityEngine;

public class AnimatorReset : MonoBehaviour
{
	public Animator target;

	public bool onEnable;

	public bool onDisable = true;

	public void Reset()
	{
		if ((bool)target)
		{
			target.Rebind();
			target.Update(0f);
		}
	}

	private void OnEnable()
	{
		if (onEnable)
		{
			Reset();
		}
	}

	private void OnDisable()
	{
		if (onDisable)
		{
			Reset();
		}
	}
}
