using UnityEngine;

public static class GTAudioClipExtensions
{
	public static float GetPeakMagnitude(this AudioClip audioClip)
	{
		if (audioClip == null)
		{
			return 0f;
		}
		float num = float.NegativeInfinity;
		float[] array = new float[audioClip.samples];
		audioClip.GetData(array, 0);
		float[] array2 = array;
		foreach (float f in array2)
		{
			num = Mathf.Max(num, Mathf.Abs(f));
		}
		return num;
	}

	public static float GetRMSMagnitude(this AudioClip audioClip)
	{
		if (audioClip == null)
		{
			return 0f;
		}
		float num = 0f;
		float[] array = new float[audioClip.samples];
		audioClip.GetData(array, 0);
		float[] array2 = array;
		foreach (float num2 in array2)
		{
			num += num2 * num2;
		}
		return Mathf.Sqrt(num / (float)array.Length);
	}
}
