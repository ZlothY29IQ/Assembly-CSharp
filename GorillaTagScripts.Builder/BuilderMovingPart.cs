using System;
using Photon.Pun;
using UnityEngine;

namespace GorillaTagScripts.Builder;

public class BuilderMovingPart : MonoBehaviour
{
	public enum BuilderMovingPartType
	{
		Translation,
		Rotation
	}

	public BuilderPiece myPiece;

	public BuilderAttachGridPlane[] myGridPlanes;

	[SerializeField]
	private BuilderMovingPartType moveType;

	[SerializeField]
	private float startPercentage = 0.5f;

	[SerializeField]
	private float velocity;

	[SerializeField]
	private bool reverseDirOnCycle = true;

	[SerializeField]
	private bool reverseDir;

	[SerializeField]
	private float cycleDelay = 0.25f;

	[SerializeField]
	protected Transform startXf;

	[SerializeField]
	protected Transform endXf;

	public static int NUM_PAUSE_NODES = 32;

	private AnimationCurve lerpAlpha;

	public bool isMoving;

	private Quaternion initLocalRotation = Quaternion.identity;

	private Vector3 initLocalPos = Vector3.zero;

	private float cycleDuration;

	private float distance;

	private float currT;

	private float percent;

	private bool currForward;

	private float dtSinceServerUpdate;

	private int lastServerTimeStamp;

	private float rotateStartAmt;

	private float rotateAmt;

	private uint startPercentageCycleOffset;

	private void Awake()
	{
		BuilderAttachGridPlane[] array = myGridPlanes;
		foreach (BuilderAttachGridPlane obj in array)
		{
			obj.movesOnPlace = true;
			obj.movingPart = this;
		}
		initLocalPos = base.transform.localPosition;
		initLocalRotation = base.transform.localRotation;
	}

	private long NetworkTimeMs()
	{
		if (PhotonNetwork.InRoom)
		{
			return (uint)(PhotonNetwork.ServerTimestamp - myPiece.activatedTimeStamp + (int)startPercentageCycleOffset + int.MinValue);
		}
		return (long)(Time.time * 1000f);
	}

	private long CycleLengthMs()
	{
		return (long)(cycleDuration * 1000f);
	}

	public double PlatformTime()
	{
		long num = NetworkTimeMs();
		long num2 = CycleLengthMs();
		return (double)(num - num / num2 * num2) / 1000.0;
	}

	public int CycleCount()
	{
		return (int)(NetworkTimeMs() / CycleLengthMs());
	}

	public float CycleCompletionPercent()
	{
		return Mathf.Clamp((float)(PlatformTime() / (double)cycleDuration), 0f, 1f);
	}

	public bool IsEvenCycle()
	{
		return CycleCount() % 2 == 0;
	}

	public void ActivateAtNode(byte node, int timestamp)
	{
		float num = (int)node;
		bool flag = node > NUM_PAUSE_NODES;
		if (flag)
		{
			num -= (float)NUM_PAUSE_NODES;
		}
		num /= (float)NUM_PAUSE_NODES;
		num = Mathf.Clamp(num, 0f, 1f);
		if (num >= startPercentage)
		{
			int num2 = (int)((num - startPercentage) * (float)CycleLengthMs());
			int num3 = timestamp - num2;
			if (flag)
			{
				num3 -= (int)CycleLengthMs();
			}
			myPiece.activatedTimeStamp = num3;
		}
		else
		{
			int num4 = (int)((num + 2f - startPercentage) * (float)CycleLengthMs());
			if (flag)
			{
				num4 -= (int)CycleLengthMs();
			}
			myPiece.activatedTimeStamp = timestamp - num4;
		}
		SetMoving(isMoving: true);
	}

	public int GetTimeOffsetMS()
	{
		int num = PhotonNetwork.ServerTimestamp - myPiece.activatedTimeStamp;
		uint num2 = (uint)(CycleLengthMs() * 2);
		return (int)((uint)num % num2);
	}

	public byte GetNearestNode()
	{
		int num = Mathf.RoundToInt(currT * (float)NUM_PAUSE_NODES);
		if (!IsEvenCycle())
		{
			num += NUM_PAUSE_NODES;
		}
		return (byte)num;
	}

	public byte GetStartNode()
	{
		return (byte)Mathf.RoundToInt(startPercentage * (float)NUM_PAUSE_NODES);
	}

	public void PauseMovement(byte node)
	{
		SetMoving(isMoving: false);
		bool flag = node > NUM_PAUSE_NODES;
		float num = (int)node;
		if (flag)
		{
			num -= (float)NUM_PAUSE_NODES;
		}
		num /= (float)NUM_PAUSE_NODES;
		num = Mathf.Clamp(num, 0f, 1f);
		if (reverseDirOnCycle)
		{
			num = (flag ? (1f - num) : num);
		}
		if (reverseDir)
		{
			num = 1f - num;
		}
		switch (moveType)
		{
		case BuilderMovingPartType.Translation:
			base.transform.localPosition = UpdatePointToPoint(num);
			break;
		case BuilderMovingPartType.Rotation:
			UpdateRotation(num);
			break;
		}
	}

	public void SetMoving(bool isMoving)
	{
		this.isMoving = isMoving;
		BuilderAttachGridPlane[] array = myGridPlanes;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].isMoving = isMoving;
		}
		if (!isMoving)
		{
			ResetMovingGrid();
		}
	}

	public void InitMovingGrid()
	{
		if (moveType == BuilderMovingPartType.Translation)
		{
			distance = Vector3.Distance(endXf.position, startXf.position);
			float num = distance / (velocity * myPiece.GetScale());
			cycleDuration = num + cycleDelay;
			float num2 = cycleDelay / cycleDuration;
			Vector2 vector = new Vector2(num2 / 2f, 0f);
			Vector2 vector2 = new Vector2(1f - num2 / 2f, 1f);
			float num3 = (vector2.y - vector.y) / (vector2.x - vector.x);
			lerpAlpha = new AnimationCurve(new Keyframe(num2 / 2f, 0f, 0f, num3), new Keyframe(1f - num2 / 2f, 1f, num3, 0f));
		}
		else
		{
			cycleDuration = 1f / velocity;
		}
		currT = startPercentage;
		uint num4 = (uint)(cycleDuration * 1000f);
		uint num5 = 2147483648u % num4;
		uint num6 = (uint)(startPercentage * (float)num4);
		if (num6 >= num5)
		{
			startPercentageCycleOffset = num6 - num5;
		}
		else
		{
			startPercentageCycleOffset = num6 + num4 + num4 - num5;
		}
	}

	public void UpdateMovingGrid()
	{
		Progress();
		switch (moveType)
		{
		case BuilderMovingPartType.Translation:
			base.transform.localPosition = UpdatePointToPoint(percent);
			break;
		case BuilderMovingPartType.Rotation:
			UpdateRotation(percent);
			break;
		default:
			throw new ArgumentOutOfRangeException();
		}
	}

	private Vector3 UpdatePointToPoint(float perc)
	{
		float t = lerpAlpha.Evaluate(perc);
		return Vector3.Lerp(startXf.localPosition, endXf.localPosition, t);
	}

	private void UpdateRotation(float perc)
	{
		Quaternion localRotation = Quaternion.AngleAxis(perc * 360f, Vector3.up);
		base.transform.localRotation = localRotation;
	}

	private void ResetMovingGrid()
	{
		base.transform.SetLocalPositionAndRotation(initLocalPos, initLocalRotation);
	}

	private void Progress()
	{
		currT = CycleCompletionPercent();
		currForward = IsEvenCycle();
		percent = currT;
		if (reverseDirOnCycle)
		{
			percent = (currForward ? currT : (1f - currT));
		}
		if (reverseDir)
		{
			percent = 1f - percent;
		}
	}

	public bool IsAnchoredToTable()
	{
		BuilderAttachGridPlane[] array = myGridPlanes;
		foreach (BuilderAttachGridPlane builderAttachGridPlane in array)
		{
			if (builderAttachGridPlane.attachIndex == builderAttachGridPlane.piece.attachIndex)
			{
				return true;
			}
		}
		return false;
	}

	public void OnPieceDestroy()
	{
		ResetMovingGrid();
	}
}
