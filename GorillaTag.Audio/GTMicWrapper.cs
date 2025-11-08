using System;
using Photon.Voice;
using Photon.Voice.Unity;
using UnityEngine;

namespace GorillaTag.Audio;

public class GTMicWrapper : MicWrapper
{
	private bool _allowPitchAdjustment;

	private float _pitchAdjustment = 1f;

	private bool _allowVolumeAdjustment;

	private float _volumeAdjustment = 1f;

	private static readonly int MaxFrameLength = 16000;

	private readonly float[] InFifo = new float[MaxFrameLength];

	private readonly float[] OutFifo = new float[MaxFrameLength];

	private readonly float[] FfTworksp = new float[2 * MaxFrameLength];

	private readonly float[] LastPhase = new float[MaxFrameLength / 2 + 1];

	private readonly float[] SumPhase = new float[MaxFrameLength / 2 + 1];

	private readonly float[] OutputAccum = new float[2 * MaxFrameLength];

	private readonly float[] AnaFreq = new float[MaxFrameLength];

	private readonly float[] AnaMagn = new float[MaxFrameLength];

	private readonly float[] SynFreq = new float[MaxFrameLength];

	private readonly float[] SynMagn = new float[MaxFrameLength];

	private long _gRover;

	public GTMicWrapper(string device, int suggestedFrequency, bool allowPitchAdjustment, float pitchAdjustment, bool allowVolumeAdjustment, float volumeAdjustment, Photon.Voice.ILogger logger)
		: base(device, suggestedFrequency, logger)
	{
		UpdatePitchAdjustment(allowPitchAdjustment, pitchAdjustment);
		UpdateVolumeAdjustment(allowVolumeAdjustment, volumeAdjustment);
	}

	public void UpdateWrapper(bool allowPitchAdjustment, float pitchAdjustment, bool allowVolumeAdjustment, float volumeAdjustment)
	{
		UpdatePitchAdjustment(allowPitchAdjustment, pitchAdjustment);
		UpdateVolumeAdjustment(allowVolumeAdjustment, volumeAdjustment);
	}

	public void UpdatePitchAdjustment(bool allow, float pitchAdjustment)
	{
		_allowPitchAdjustment = allow;
		_pitchAdjustment = pitchAdjustment;
	}

	public void UpdateVolumeAdjustment(bool allow, float volumeAdjustment)
	{
		_allowVolumeAdjustment = allow;
		_volumeAdjustment = volumeAdjustment;
	}

	public override bool Read(float[] buffer)
	{
		if (base.Error != null)
		{
			return false;
		}
		int position = UnityMicrophone.GetPosition(device);
		if (position < micPrevPos)
		{
			micLoopCnt++;
		}
		micPrevPos = position;
		int num = micLoopCnt * mic.samples + position;
		if (mic.channels == 0)
		{
			base.Error = "Number of channels is 0 in Read()";
			logger.LogError("[PV] MicWrapper: " + base.Error);
			return false;
		}
		int num2 = buffer.Length / mic.channels;
		int num3 = readAbsPos + num2;
		if (num3 < num)
		{
			mic.GetData(buffer, readAbsPos % mic.samples);
			readAbsPos = num3;
			float num4 = Mathf.Clamp(_pitchAdjustment, 0.5f, 2f);
			if (_allowPitchAdjustment && !Mathf.Approximately(num4, 1f))
			{
				PitchShift(num4, num2, base.SamplingRate, buffer);
			}
			if (_allowVolumeAdjustment && !Mathf.Approximately(_volumeAdjustment, 1f))
			{
				for (int i = 0; i < buffer.Length; i++)
				{
					buffer[i] = Mathf.Clamp(buffer[i] * _volumeAdjustment, float.MinValue, float.MaxValue);
				}
			}
			return true;
		}
		return false;
	}

	private void PitchShift(float pitchShift, long numSampsToProcess, float sampleRate, float[] indata)
	{
		PitchShift(pitchShift, numSampsToProcess, 2048L, 10L, sampleRate, indata);
	}

	public void PitchShift(float pitchShift, long numSampsToProcess, long fftFrameSize, long osamp, float sampleRate, float[] indata)
	{
		long num = fftFrameSize / 2;
		long num2 = fftFrameSize / osamp;
		double num3 = (double)sampleRate / (double)fftFrameSize;
		double num4 = Math.PI * 2.0 * (double)num2 / (double)fftFrameSize;
		long num5 = fftFrameSize - num2;
		if (_gRover == 0L)
		{
			_gRover = num5;
		}
		for (long num6 = 0L; num6 < numSampsToProcess; num6++)
		{
			InFifo[_gRover] = indata[num6];
			indata[num6] = OutFifo[_gRover - num5];
			_gRover++;
			if (_gRover < fftFrameSize)
			{
				continue;
			}
			_gRover = num5;
			for (long num7 = 0L; num7 < fftFrameSize; num7++)
			{
				double num8 = -0.5 * Math.Cos(Math.PI * 2.0 * (double)num7 / (double)fftFrameSize) + 0.5;
				FfTworksp[2 * num7] = (float)((double)InFifo[num7] * num8);
				FfTworksp[2 * num7 + 1] = 0f;
			}
			ShortTimeFourierTransform(FfTworksp, fftFrameSize, -1L);
			for (long num7 = 0L; num7 <= num; num7++)
			{
				double num9 = FfTworksp[2 * num7];
				double num10 = FfTworksp[2 * num7 + 1];
				double num11 = 2.0 * Math.Sqrt(num9 * num9 + num10 * num10);
				double num12 = Math.Atan2(num10, num9);
				double num13 = num12 - (double)LastPhase[num7];
				LastPhase[num7] = (float)num12;
				num13 -= (double)num7 * num4;
				long num14 = (long)(num13 / Math.PI);
				num14 = ((num14 < 0) ? (num14 - (num14 & 1)) : (num14 + (num14 & 1)));
				num13 -= Math.PI * (double)num14;
				num13 = (double)osamp * num13 / (Math.PI * 2.0);
				num13 = (double)num7 * num3 + num13 * num3;
				AnaMagn[num7] = (float)num11;
				AnaFreq[num7] = (float)num13;
			}
			for (int i = 0; i < fftFrameSize; i++)
			{
				SynMagn[i] = 0f;
				SynFreq[i] = 0f;
			}
			for (long num7 = 0L; num7 <= num; num7++)
			{
				long num15 = (long)((float)num7 * pitchShift);
				if (num15 <= num)
				{
					SynMagn[num15] += AnaMagn[num7];
					SynFreq[num15] = AnaFreq[num7] * pitchShift;
				}
			}
			for (long num7 = 0L; num7 <= num; num7++)
			{
				double num11 = SynMagn[num7];
				double num13 = SynFreq[num7];
				num13 -= (double)num7 * num3;
				num13 /= num3;
				num13 = Math.PI * 2.0 * num13 / (double)osamp;
				num13 += (double)num7 * num4;
				SumPhase[num7] += (float)num13;
				double num12 = SumPhase[num7];
				FfTworksp[2 * num7] = (float)(num11 * Math.Cos(num12));
				FfTworksp[2 * num7 + 1] = (float)(num11 * Math.Sin(num12));
			}
			for (long num7 = fftFrameSize + 2; num7 < 2 * fftFrameSize; num7++)
			{
				FfTworksp[num7] = 0f;
			}
			ShortTimeFourierTransform(FfTworksp, fftFrameSize, 1L);
			for (long num7 = 0L; num7 < fftFrameSize; num7++)
			{
				double num8 = -0.5 * Math.Cos(Math.PI * 2.0 * (double)num7 / (double)fftFrameSize) + 0.5;
				OutputAccum[num7] += (float)(2.0 * num8 * (double)FfTworksp[2 * num7] / (double)(num * osamp));
			}
			for (long num7 = 0L; num7 < num2; num7++)
			{
				OutFifo[num7] = OutputAccum[num7];
			}
			for (long num7 = 0L; num7 < fftFrameSize; num7++)
			{
				OutputAccum[num7] = OutputAccum[num7 + num2];
			}
			for (long num7 = 0L; num7 < num5; num7++)
			{
				InFifo[num7] = InFifo[num7 + num2];
			}
		}
	}

	public void ShortTimeFourierTransform(float[] fftBuffer, long fftFrameSize, long sign)
	{
		for (long num = 2L; num < 2 * fftFrameSize - 2; num += 2)
		{
			long num2 = 2L;
			long num3 = 0L;
			while (num2 < 2 * fftFrameSize)
			{
				if ((num & num2) != 0L)
				{
					num3++;
				}
				num3 <<= 1;
				num2 <<= 1;
			}
			if (num < num3)
			{
				float num4 = fftBuffer[num];
				fftBuffer[num] = fftBuffer[num3];
				fftBuffer[num3] = num4;
				num4 = fftBuffer[num + 1];
				fftBuffer[num + 1] = fftBuffer[num3 + 1];
				fftBuffer[num3 + 1] = num4;
			}
		}
		long num5 = (long)(Math.Log(fftFrameSize) / Math.Log(2.0) + 0.5);
		long num6 = 0L;
		long num7 = 2L;
		for (; num6 < num5; num6++)
		{
			num7 <<= 1;
			long num8 = num7 >> 1;
			float num9 = 1f;
			float num10 = 0f;
			float num11 = MathF.PI / (float)(num8 >> 1);
			float num12 = (float)Math.Cos(num11);
			float num13 = (float)((double)sign * Math.Sin(num11));
			for (long num3 = 0L; num3 < num8; num3 += 2)
			{
				float num14;
				for (long num = num3; num < 2 * fftFrameSize; num += num7)
				{
					num14 = fftBuffer[num + num8] * num9 - fftBuffer[num + num8 + 1] * num10;
					float num15 = fftBuffer[num + num8] * num10 + fftBuffer[num + num8 + 1] * num9;
					fftBuffer[num + num8] = fftBuffer[num] - num14;
					fftBuffer[num + num8 + 1] = fftBuffer[num + 1] - num15;
					fftBuffer[num] += num14;
					fftBuffer[num + 1] += num15;
				}
				num14 = num9 * num12 - num10 * num13;
				num10 = num9 * num13 + num10 * num12;
				num9 = num14;
			}
		}
	}
}
