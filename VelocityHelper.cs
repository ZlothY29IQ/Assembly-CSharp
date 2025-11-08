using System;
using UnityEngine;

[Serializable]
public class VelocityHelper
{
	private float[] _samples;

	private int _latest;

	private int _size;

	private bool _initialized;

	public VelocityHelper(int historySize = 12)
	{
		_size = historySize;
		_samples = new float[historySize * 4];
	}

	public void SamplePosition(Transform target, float dt)
	{
		Vector3 position = target.position;
		if (!_initialized)
		{
			_InitSamples(position, dt);
		}
		_SetSample(_latest, position, dt);
		_latest = (_latest + 1) % _size;
	}

	private void _InitSamples(Vector3 position, float dt)
	{
		for (int i = 0; i < _size; i++)
		{
			_SetSample(i, position, dt);
		}
		_initialized = true;
	}

	private void _SetSample(int i, Vector3 position, float dt)
	{
		_samples[i] = position.x;
		_samples[i + 1] = position.y;
		_samples[i + 2] = position.z;
		_samples[i + 3] = dt;
	}
}
