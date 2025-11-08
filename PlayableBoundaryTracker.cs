using System.Runtime.CompilerServices;
using UnityEngine;

public class PlayableBoundaryTracker : MonoBehaviour
{
	public float radius = 1f;

	public float signedDistanceToBoundary { get; private set; }

	public float prevSignedDistanceToBoundary { get; private set; }

	public float timeSinceCrossingBorder { get; private set; }

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsInsideZone()
	{
		return Mathf.Sign(signedDistanceToBoundary) < 0f;
	}

	public void UpdateSignedDistanceToBoundary(float newDistance, float elapsed)
	{
		prevSignedDistanceToBoundary = signedDistanceToBoundary;
		signedDistanceToBoundary = newDistance;
		if ((int)Mathf.Sign(prevSignedDistanceToBoundary) != (int)Mathf.Sign(signedDistanceToBoundary))
		{
			timeSinceCrossingBorder = 0f;
		}
		else
		{
			timeSinceCrossingBorder += elapsed;
		}
	}

	internal void ResetValues()
	{
		timeSinceCrossingBorder = 0f;
	}
}
