using System;
using UnityEngine;

[Serializable]
public class CrittersAnim
{
	public AnimationCurve squashAmount;

	public AnimationCurve forwardOffset;

	public AnimationCurve horizontalOffset;

	public AnimationCurve verticalOffset;

	public float playSpeed;

	public bool IsModified()
	{
		if ((squashAmount == null || squashAmount.length <= 1) && (forwardOffset == null || forwardOffset.length <= 1) && (horizontalOffset == null || horizontalOffset.length <= 1))
		{
			if (verticalOffset != null)
			{
				return verticalOffset.length > 1;
			}
			return false;
		}
		return true;
	}

	public static bool IsModified(CrittersAnim anim)
	{
		return anim?.IsModified() ?? false;
	}
}
