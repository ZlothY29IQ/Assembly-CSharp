using UnityEngine;

public class LerpScale : LerpComponent
{
	[Space]
	public Transform target;

	[Space]
	public Vector3 start = Vector3.one;

	public Vector3 end = Vector3.one;

	public Vector3 current;

	[SerializeField]
	private AnimationCurve scaleCurve = AnimationCurves.EaseInOutBounce;

	protected override void OnLerp(float t)
	{
		current = Vector3.Lerp(start, end, scaleCurve.Evaluate(t));
		if ((bool)target)
		{
			target.localScale = current;
		}
	}
}
