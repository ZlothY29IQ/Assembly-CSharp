using UnityEngine;

public class FingerFlagTwirlTest : MonoBehaviour
{
	public Vector3 rotAnimDurations = new Vector3(0.2f, 0.1f, 0.5f);

	public Vector3 rotAnimAmplitudes = Vector3.one * 360f;

	public AnimationCurve rotXAnimCurve;

	public AnimationCurve rotYAnimCurve;

	public AnimationCurve rotZAnimCurve;

	private Vector3 animTimes = Vector3.zero;

	protected void FixedUpdate()
	{
		animTimes += Time.deltaTime * rotAnimDurations;
		animTimes.x %= 1f;
		animTimes.y %= 1f;
		animTimes.z %= 1f;
		base.transform.localRotation = Quaternion.Euler(rotXAnimCurve.Evaluate(animTimes.x) * rotAnimAmplitudes.x, rotYAnimCurve.Evaluate(animTimes.y) * rotAnimAmplitudes.y, rotZAnimCurve.Evaluate(animTimes.z) * rotAnimAmplitudes.z);
	}
}
