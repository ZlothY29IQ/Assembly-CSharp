using System;
using UnityEngine;

[Serializable]
public struct FrameStamp
{
	private int _lastFrame;

	public int framesElapsed => Time.frameCount - _lastFrame;

	public static FrameStamp Now()
	{
		return new FrameStamp
		{
			_lastFrame = Time.frameCount
		};
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
		return new FrameStamp
		{
			_lastFrame = Time.frameCount - framesElapsed
		};
	}
}
