using System;
using UnityEngine;

[Serializable]
public class AdvancedItemState
{
	[Serializable]
	public class PreData
	{
		public float distAlongLine;

		public PointType pointType;
	}

	public enum PointType
	{
		Standard,
		DistanceBased
	}

	private int _encodedValue;

	public Vector2 angleVectorWhereUpIsStandard;

	public Quaternion deltaRotation;

	public int index;

	public PreData preData;

	public LimitAxis limitAxis;

	public bool reverseGrip;

	public float angle;

	private float EncodedDeltaRotation => GetEncodedDeltaRotation();

	public void Encode()
	{
		_encodedValue = EncodeData();
	}

	public void Decode()
	{
		AdvancedItemState advancedItemState = DecodeData(_encodedValue);
		index = advancedItemState.index;
		preData = advancedItemState.preData;
		limitAxis = advancedItemState.limitAxis;
		reverseGrip = advancedItemState.reverseGrip;
		angle = advancedItemState.angle;
	}

	public Quaternion GetQuaternion()
	{
		_ = Vector3.one;
		if (reverseGrip)
		{
			switch (limitAxis)
			{
			case LimitAxis.NoMovement:
				return Quaternion.identity;
			case LimitAxis.YAxis:
				return Quaternion.identity;
			default:
				throw new ArgumentOutOfRangeException();
			case LimitAxis.XAxis:
			case LimitAxis.ZAxis:
				break;
			}
		}
		return Quaternion.identity;
	}

	public (int grabPointIndex, float YRotation, float XRotation, float ZRotation) DecodeAdvancedItemState(int encodedValue)
	{
		int item = (encodedValue >> 21) & 0xFF;
		float item2 = (float)((encodedValue >> 14) & 0x7F) / 128f * 360f;
		float item3 = (float)((encodedValue >> 7) & 0x7F) / 128f * 360f;
		float item4 = (float)(encodedValue & 0x7F) / 128f * 360f;
		return (grabPointIndex: item, YRotation: item2, XRotation: item3, ZRotation: item4);
	}

	public float GetEncodedDeltaRotation()
	{
		return Mathf.Abs(Mathf.Atan2(angleVectorWhereUpIsStandard.x, angleVectorWhereUpIsStandard.y)) / MathF.PI;
	}

	public void DecodeDeltaRotation(float encodedDelta, bool isFlipped)
	{
		float f = encodedDelta * MathF.PI;
		if (isFlipped)
		{
			angleVectorWhereUpIsStandard = new Vector2(0f - Mathf.Sin(f), Mathf.Cos(f));
		}
		else
		{
			angleVectorWhereUpIsStandard = new Vector2(Mathf.Sin(f), Mathf.Cos(f));
		}
		switch (limitAxis)
		{
		case LimitAxis.YAxis:
		{
			Vector3 forward = new Vector3(angleVectorWhereUpIsStandard.x, 0f, angleVectorWhereUpIsStandard.y);
			Vector3 upwards = (reverseGrip ? Vector3.down : Vector3.up);
			deltaRotation = Quaternion.LookRotation(forward, upwards);
			break;
		}
		default:
			throw new ArgumentOutOfRangeException();
		case LimitAxis.NoMovement:
		case LimitAxis.XAxis:
		case LimitAxis.ZAxis:
			break;
		}
	}

	public int EncodeData()
	{
		int num = 0;
		if ((index >= 32) | (index < 0))
		{
			throw new ArgumentOutOfRangeException($"Index is invalid {index}");
		}
		num |= index << 25;
		PointType pointType = preData.pointType;
		num |= (int)(pointType & (PointType)7) << 22;
		num |= (int)limitAxis << 19;
		num |= (reverseGrip ? 1 : 0) << 18;
		bool flag = angleVectorWhereUpIsStandard.x < 0f;
		switch (pointType)
		{
		case PointType.Standard:
		{
			int num4 = (int)(GetEncodedDeltaRotation() * 65536f) & 0xFFFF;
			num |= (flag ? 1 : 0) << 17;
			return num | (num4 << 1);
		}
		case PointType.DistanceBased:
		{
			int num2 = (int)(GetEncodedDeltaRotation() * 512f) & 0x1FF;
			num |= (flag ? 1 : 0) << 17;
			num |= num2 << 9;
			int num3 = (int)(preData.distAlongLine * 256f) & 0xFF;
			return num | num3;
		}
		default:
			throw new ArgumentOutOfRangeException();
		}
	}

	public AdvancedItemState DecodeData(int encoded)
	{
		AdvancedItemState advancedItemState = new AdvancedItemState();
		advancedItemState.index = (encoded >> 25) & 0x1F;
		advancedItemState.limitAxis = (LimitAxis)((encoded >> 19) & 7);
		advancedItemState.reverseGrip = ((encoded >> 18) & 1) == 1;
		PointType pointType = (PointType)((encoded >> 22) & 7);
		switch (pointType)
		{
		case PointType.Standard:
			advancedItemState.preData = new PreData
			{
				pointType = pointType
			};
			DecodeDeltaRotation((float)((encoded >> 1) & 0xFFFF) / 65536f, ((encoded >> 17) & 1) > 0);
			break;
		case PointType.DistanceBased:
			advancedItemState.preData = new PreData
			{
				pointType = pointType,
				distAlongLine = (float)(encoded & 0xFF) / 256f
			};
			DecodeDeltaRotation((float)((encoded >> 9) & 0x1FF) / 512f, ((encoded >> 17) & 1) > 0);
			break;
		default:
			throw new ArgumentOutOfRangeException();
		}
		return advancedItemState;
	}
}
