using UnityEngine;

public static class AnimatorUtils
{
	public static void ResetToEntryState(this Animator a)
	{
		if (!(a == null))
		{
			a.Rebind();
			a.Update(0f);
		}
	}
}
