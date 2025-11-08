using System;
using GorillaTagScripts.Builder;
using GT_CustomMapSupportRuntime;
using Photon.Pun;
using UnityEngine;

namespace GorillaTagScripts;

public class SurfaceMover : MonoBehaviour
{
	[SerializeField]
	private BuilderMovingPart.BuilderMovingPartType moveType;

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

	[SerializeField]
	public RotationAxis rotationAxis = RotationAxis.Y;

	[SerializeField]
	public float rotationAmount = 360f;

	[SerializeField]
	public bool rotationRelativeToStarting;

	private AnimationCurve lerpAlpha;

	private float cycleDuration;

	private float distance;

	private Vector3 startingRotation;

	private float currT;

	private float percent;

	private bool currForward;

	private float dtSinceServerUpdate;

	private int lastServerTimeStamp;

	private float rotateStartAmt;

	private float rotateAmt;

	private uint startPercentageCycleOffset;

	private void Start()
	{
		_ = MovingSurfaceManager.instance == null;
		MovingSurfaceManager.instance.RegisterSurfaceMover(this);
	}

	private void OnDestroy()
	{
		if (MovingSurfaceManager.instance != null)
		{
			MovingSurfaceManager.instance.UnregisterSurfaceMover(this);
		}
	}

	public void InitMovingSurface()
	{
		if (moveType == BuilderMovingPart.BuilderMovingPartType.Translation)
		{
			distance = Vector3.Distance(endXf.position, startXf.position);
			float num = distance / velocity;
			cycleDuration = num + cycleDelay;
		}
		else
		{
			if (rotationRelativeToStarting)
			{
				startingRotation = base.transform.localRotation.eulerAngles;
			}
			cycleDuration = rotationAmount / 360f / velocity;
			cycleDuration += cycleDelay;
		}
		float num2 = cycleDelay / cycleDuration;
		Vector2 vector = new Vector2(num2 / 2f, 0f);
		Vector2 vector2 = new Vector2(1f - num2 / 2f, 1f);
		float num3 = (vector2.y - vector.y) / (vector2.x - vector.x);
		lerpAlpha = new AnimationCurve(new Keyframe(num2 / 2f, 0f, 0f, num3), new Keyframe(1f - num2 / 2f, 1f, num3, 0f));
		currT = startPercentage;
		uint num4 = (uint)(cycleDuration * 1000f);
		if (num4 == 0)
		{
			num4 = 1u;
		}
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

	private long NetworkTimeMs()
	{
		if (PhotonNetwork.InRoom)
		{
			return (uint)(PhotonNetwork.ServerTimestamp + (int)startPercentageCycleOffset + int.MinValue);
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

	public void Move()
	{
		Progress();
		switch (moveType)
		{
		case BuilderMovingPart.BuilderMovingPartType.Translation:
			base.transform.localPosition = UpdatePointToPoint(percent);
			break;
		case BuilderMovingPart.BuilderMovingPartType.Rotation:
			UpdateRotation(percent);
			break;
		}
	}

	private Vector3 UpdatePointToPoint(float perc)
	{
		float t = lerpAlpha.Evaluate(perc);
		return Vector3.Lerp(startXf.localPosition, endXf.localPosition, t);
	}

	private void UpdateRotation(float perc)
	{
		float num = lerpAlpha.Evaluate(perc) * rotationAmount;
		if (rotationRelativeToStarting)
		{
			Vector3 euler = startingRotation;
			switch (rotationAxis)
			{
			case RotationAxis.X:
				euler.x += num;
				break;
			case RotationAxis.Y:
				euler.y += num;
				break;
			case RotationAxis.Z:
				euler.z += num;
				break;
			}
			base.transform.localRotation = Quaternion.Euler(euler);
		}
		else
		{
			switch (rotationAxis)
			{
			case RotationAxis.X:
				base.transform.localRotation = Quaternion.AngleAxis(num, Vector3.right);
				break;
			case RotationAxis.Y:
				base.transform.localRotation = Quaternion.AngleAxis(num, Vector3.up);
				break;
			case RotationAxis.Z:
				base.transform.localRotation = Quaternion.AngleAxis(num, Vector3.forward);
				break;
			}
		}
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

	public void CopySettings(SurfaceMoverSettings settings)
	{
		moveType = (BuilderMovingPart.BuilderMovingPartType)settings.moveType;
		startPercentage = 0f;
		velocity = Math.Clamp(settings.velocity, 0.001f, Math.Abs(settings.velocity));
		reverseDirOnCycle = settings.reverseDirOnCycle;
		reverseDir = settings.reverseDir;
		cycleDelay = Math.Clamp(settings.cycleDelay, 0f, Math.Abs(settings.cycleDelay));
		startXf = settings.start;
		endXf = settings.end;
		rotationAxis = (RotationAxis)settings.rotationAxis;
		rotationAmount = Math.Clamp(settings.rotationAmount, 0.001f, Math.Abs(settings.rotationAmount));
		rotationRelativeToStarting = settings.rotationRelativeToStarting;
	}
}
