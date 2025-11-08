using System.Collections.Generic;
using UnityEngine;

public class AverageVector3
{
	public struct Sample
	{
		public float timeStamp;

		public Vector3 value;
	}

	private List<Sample> samples = new List<Sample>();

	private float timeWindow = 0.1f;

	public AverageVector3(float averagingWindow = 0.1f)
	{
		timeWindow = averagingWindow;
	}

	public void AddSample(Vector3 sample, float time)
	{
		samples.Add(new Sample
		{
			timeStamp = time,
			value = sample
		});
		RefreshSamples();
	}

	public Vector3 GetAverage()
	{
		RefreshSamples();
		Vector3 zero = Vector3.zero;
		for (int i = 0; i < samples.Count; i++)
		{
			zero += samples[i].value;
		}
		return zero / samples.Count;
	}

	public void Clear()
	{
		samples.Clear();
	}

	private void RefreshSamples()
	{
		float num = Time.time - timeWindow;
		for (int num2 = samples.Count - 1; num2 >= 0; num2--)
		{
			if (samples[num2].timeStamp < num)
			{
				samples.RemoveAt(num2);
			}
		}
	}
}
