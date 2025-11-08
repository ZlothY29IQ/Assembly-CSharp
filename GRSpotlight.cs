using UnityEngine;

public class GRSpotlight : MonoBehaviourTick
{
	public float yAmplitude = 75f;

	public float xAmplitude = 40f;

	public float yFrequency = 0.2f;

	public float xFrequency = 0.3f;

	private float yStart;

	private float xStart;

	private float timeOffset;

	private void Awake()
	{
		yStart = base.transform.rotation.eulerAngles.y;
		xStart = base.transform.rotation.eulerAngles.x;
		timeOffset = Random.value * 360f;
		yFrequency += Random.value / 100f;
		xFrequency += Random.value / 100f;
	}

	public override void Tick()
	{
		base.transform.eulerAngles = new Vector3(xStart + xAmplitude * Mathf.Sin(Time.time * xFrequency), yStart + yAmplitude * Mathf.Cos(Time.time * yFrequency), 0f);
	}
}
