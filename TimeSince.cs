using System;

public struct TimeSince
{
	private DateTime _dt;

	private const double INT32_MAX = 2147483647.0;

	public double secondsElapsed
	{
		get
		{
			double totalSeconds = (DateTime.UtcNow - _dt).TotalSeconds;
			if (!(totalSeconds > 2147483647.0))
			{
				return totalSeconds;
			}
			return 2147483647.0;
		}
	}

	public float secondsElapsedFloat => (float)secondsElapsed;

	public int secondsElapsedInt => (int)secondsElapsed;

	public uint secondsElapsedUint => (uint)secondsElapsed;

	public long secondsElapsedLong => (long)secondsElapsed;

	public TimeSpan secondsElapsedSpan => TimeSpan.FromSeconds(secondsElapsed);

	public TimeSince(DateTime dt)
	{
		_dt = dt;
	}

	public TimeSince(int elapsed)
	{
		_dt = DateTime.UtcNow.AddSeconds(-elapsed);
	}

	public TimeSince(uint elapsed)
	{
		_dt = DateTime.UtcNow.AddSeconds(-1.0 * (double)elapsed);
	}

	public TimeSince(float elapsed)
	{
		_dt = DateTime.UtcNow.AddSeconds(0f - elapsed);
	}

	public TimeSince(double elapsed)
	{
		_dt = DateTime.UtcNow.AddSeconds(0.0 - elapsed);
	}

	public TimeSince(long elapsed)
	{
		_dt = DateTime.UtcNow.AddSeconds(-elapsed);
	}

	public TimeSince(TimeSpan elapsed)
	{
		_dt = DateTime.UtcNow.Add(-elapsed);
	}

	public bool HasElapsed(int seconds)
	{
		return secondsElapsedInt >= seconds;
	}

	public bool HasElapsed(uint seconds)
	{
		return secondsElapsedUint >= seconds;
	}

	public bool HasElapsed(float seconds)
	{
		return secondsElapsedFloat >= seconds;
	}

	public bool HasElapsed(double seconds)
	{
		return secondsElapsed >= seconds;
	}

	public bool HasElapsed(long seconds)
	{
		return secondsElapsedLong >= seconds;
	}

	public bool HasElapsed(TimeSpan seconds)
	{
		return secondsElapsedSpan >= seconds;
	}

	public void Reset()
	{
		_dt = DateTime.UtcNow;
	}

	public bool HasElapsed(int seconds, bool resetOnElapsed)
	{
		if (!resetOnElapsed)
		{
			return secondsElapsedInt >= seconds;
		}
		if (secondsElapsedInt < seconds)
		{
			return false;
		}
		Reset();
		return true;
	}

	public bool HasElapsed(uint seconds, bool resetOnElapsed)
	{
		if (!resetOnElapsed)
		{
			return secondsElapsedUint >= seconds;
		}
		if (secondsElapsedUint < seconds)
		{
			return false;
		}
		Reset();
		return true;
	}

	public bool HasElapsed(float seconds, bool resetOnElapsed)
	{
		if (!resetOnElapsed)
		{
			return secondsElapsedFloat >= seconds;
		}
		if (secondsElapsedFloat < seconds)
		{
			return false;
		}
		Reset();
		return true;
	}

	public bool HasElapsed(double seconds, bool resetOnElapsed)
	{
		if (!resetOnElapsed)
		{
			return secondsElapsed >= seconds;
		}
		if (secondsElapsed < seconds)
		{
			return false;
		}
		Reset();
		return true;
	}

	public bool HasElapsed(long seconds, bool resetOnElapsed)
	{
		if (!resetOnElapsed)
		{
			return secondsElapsedLong >= seconds;
		}
		if (secondsElapsedLong < seconds)
		{
			return false;
		}
		Reset();
		return true;
	}

	public bool HasElapsed(TimeSpan seconds, bool resetOnElapsed)
	{
		if (!resetOnElapsed)
		{
			return secondsElapsedSpan >= seconds;
		}
		if (secondsElapsedSpan < seconds)
		{
			return false;
		}
		Reset();
		return true;
	}

	public override string ToString()
	{
		return $"{secondsElapsed:F3} seconds since {{{_dt:s}";
	}

	public override int GetHashCode()
	{
		return StaticHash.Compute(_dt);
	}

	public static TimeSince Now()
	{
		return new TimeSince(DateTime.UtcNow);
	}

	public static implicit operator long(TimeSince ts)
	{
		return ts.secondsElapsedLong;
	}

	public static implicit operator double(TimeSince ts)
	{
		return ts.secondsElapsed;
	}

	public static implicit operator float(TimeSince ts)
	{
		return ts.secondsElapsedFloat;
	}

	public static implicit operator int(TimeSince ts)
	{
		return ts.secondsElapsedInt;
	}

	public static implicit operator uint(TimeSince ts)
	{
		return ts.secondsElapsedUint;
	}

	public static implicit operator TimeSpan(TimeSince ts)
	{
		return ts.secondsElapsedSpan;
	}

	public static implicit operator TimeSince(int elapsed)
	{
		return new TimeSince(elapsed);
	}

	public static implicit operator TimeSince(uint elapsed)
	{
		return new TimeSince(elapsed);
	}

	public static implicit operator TimeSince(float elapsed)
	{
		return new TimeSince(elapsed);
	}

	public static implicit operator TimeSince(double elapsed)
	{
		return new TimeSince(elapsed);
	}

	public static implicit operator TimeSince(long elapsed)
	{
		return new TimeSince(elapsed);
	}

	public static implicit operator TimeSince(TimeSpan elapsed)
	{
		return new TimeSince(elapsed);
	}

	public static implicit operator TimeSince(DateTime dt)
	{
		return new TimeSince(dt);
	}
}
