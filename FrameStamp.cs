using System;
using UnityEngine;

[Serializable]
public struct FrameStamp
{
	private int _lastFrame;

	public int framesElapsed => Time.frameCount - _lastFrame;

	public static FrameStamp Now()
	{
		FrameStamp result = default(FrameStamp);
		result._lastFrame = Time.frameCount;
		return result;
	}

	public override string ToString()
	{
		return $"{framesElapsed} frames elapsed";
	}

	public override int GetHashCode()
	{
		return StaticHash.Compute(_lastFrame);
	}

	public static implicit operator int(FrameStamp fs)
	{
		return fs.framesElapsed;
	}

	public static implicit operator FrameStamp(int framesElapsed)
	{
		FrameStamp result = default(FrameStamp);
		result._lastFrame = Time.frameCount - framesElapsed;
		return result;
	}
}
