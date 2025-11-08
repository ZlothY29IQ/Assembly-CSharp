using GTMathUtil;
using Photon.Pun;
using UnityEngine;

public class MovingPlatform : BasePlatform
{
	public enum PlatformType
	{
		PointToPoint,
		Arc,
		Rotation,
		Child,
		ContinuousRotation
	}

	public PlatformType platformType;

	public float cycleLength;

	public float smoothingHalflife = 0.1f;

	public float rotateStartAmt;

	public float rotateAmt;

	public bool reverseDirOnCycle = true;

	public bool reverseDir;

	private CriticalSpringDamper springCD = new CriticalSpringDamper();

	private Rigidbody rb;

	public Transform startXf;

	public Transform endXf;

	public Vector3 platformInitLocalPos;

	private Vector3 startPos;

	private Vector3 endPos;

	private Quaternion startRot;

	private Quaternion endRot;

	public float startPercentage;

	public float startDelay;

	public bool startNextCycle;

	public Transform pivot;

	private Quaternion initLocalRotation;

	private Vector3 initOffset;

	private float currT;

	private float percent;

	private float smoothedPercent = -1f;

	private bool currForward;

	private float dtSinceServerUpdate;

	private double lastServerTime;

	public Vector3 currentVelocity;

	public Vector3 rotationalAxis;

	public float angularVelocity;

	public Vector3 rotationPivot;

	public Vector3 lastPos;

	public Quaternion lastRot;

	public Vector3 deltaPosition;

	public bool debugMovement;

	private double lastNT;

	private float lastT;

	public float InitTimeOffset()
	{
		return startPercentage * cycleLength;
	}

	private long InitTimeOffsetMs()
	{
		return (long)(InitTimeOffset() * 1000f);
	}

	private long NetworkTimeMs()
	{
		if (PhotonNetwork.InRoom)
		{
			return (uint)(PhotonNetwork.ServerTimestamp + int.MinValue) + InitTimeOffsetMs();
		}
		return (long)(Time.time * 1000f);
	}

	private long CycleLengthMs()
	{
		return (long)(cycleLength * 1000f);
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
		float value = (float)(PlatformTime() / (double)cycleLength);
		value = Mathf.Clamp(value, 0f, 1f);
		if (startDelay > 0f)
		{
			float num = startDelay / cycleLength;
			value = ((!(value <= num)) ? ((value - num) / (1f - num)) : 0f);
		}
		return value;
	}

	public bool CycleForward()
	{
		return (CycleCount() + (startNextCycle ? 1 : 0)) % 2 == 0;
	}

	private void Awake()
	{
		if (platformType != PlatformType.Child)
		{
			rb = GetComponent<Rigidbody>();
			initLocalRotation = base.transform.localRotation;
			if (pivot != null)
			{
				initOffset = pivot.transform.position - startXf.transform.position;
			}
			startPos = startXf.position;
			endPos = endXf.position;
			startRot = startXf.rotation;
			endRot = endXf.rotation;
			platformInitLocalPos = base.transform.localPosition;
			currT = startPercentage;
		}
	}

	private void OnEnable()
	{
		if (platformType != PlatformType.Child)
		{
			base.transform.localRotation = initLocalRotation;
			startPos = startXf.position;
			endPos = endXf.position;
			startRot = startXf.rotation;
			endRot = endXf.rotation;
			platformInitLocalPos = base.transform.localPosition;
			currT = startPercentage;
		}
	}

	private Vector3 UpdatePointToPoint()
	{
		return Vector3.Lerp(startPos, endPos, smoothedPercent);
	}

	private Vector3 UpdateArc()
	{
		float angle = Mathf.Lerp(rotateStartAmt, rotateStartAmt + rotateAmt, smoothedPercent);
		_ = initLocalRotation;
		Vector3 vector = Quaternion.AngleAxis(angle, Vector3.forward) * initOffset;
		return pivot.transform.position + vector;
	}

	private Quaternion UpdateRotation()
	{
		return Quaternion.Slerp(startRot, endRot, smoothedPercent);
	}

	private Quaternion UpdateContinuousRotation()
	{
		return Quaternion.AngleAxis(smoothedPercent * 360f, Vector3.up) * base.transform.parent.rotation;
	}

	private void SetupContext()
	{
		double time = PhotonNetwork.Time;
		if (lastServerTime == time)
		{
			dtSinceServerUpdate += Time.fixedDeltaTime;
		}
		else
		{
			dtSinceServerUpdate = 0f;
			lastServerTime = time;
		}
		_ = currT;
		currT = CycleCompletionPercent();
		currForward = CycleForward();
		percent = currT;
		if (reverseDirOnCycle)
		{
			percent = (currForward ? currT : (1f - currT));
		}
		if (reverseDir)
		{
			percent = 1f - percent;
		}
		smoothedPercent = percent;
		lastNT = time;
		lastT = Time.time;
	}

	private void Update()
	{
		if (platformType == PlatformType.Child)
		{
			return;
		}
		SetupContext();
		Vector3 vector = base.transform.position;
		Quaternion quaternion = base.transform.rotation;
		bool flag = false;
		switch (platformType)
		{
		case PlatformType.PointToPoint:
			vector = UpdatePointToPoint();
			break;
		case PlatformType.Arc:
			vector = UpdateArc();
			flag = true;
			break;
		case PlatformType.Rotation:
			quaternion = UpdateRotation();
			flag = true;
			break;
		case PlatformType.ContinuousRotation:
			quaternion = UpdateContinuousRotation();
			flag = true;
			break;
		}
		if (!debugMovement)
		{
			lastPos = rb.position;
			lastRot = rb.rotation;
			if (platformType != PlatformType.Rotation)
			{
				rb.MovePosition(vector);
			}
			if (flag)
			{
				rb.MoveRotation(quaternion);
			}
		}
		else
		{
			lastPos = base.transform.position;
			lastRot = base.transform.rotation;
			base.transform.position = vector;
			if (flag)
			{
				base.transform.rotation = quaternion;
			}
		}
		deltaPosition = vector - lastPos;
	}

	public Vector3 ThisFrameMovement()
	{
		return deltaPosition;
	}
}
