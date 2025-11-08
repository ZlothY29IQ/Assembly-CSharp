using System;
using CjLib;
using GorillaLocomotion;
using Photon.Pun;
using UnityEngine;

namespace GorillaTag.Cosmetics;

public class Dreidel : MonoBehaviour
{
	private enum State
	{
		Idle,
		FindingSurface,
		Spinning,
		Falling,
		Fallen
	}

	public enum Side
	{
		Shin,
		Hey,
		Gimel,
		Nun,
		Count
	}

	public enum Variation
	{
		Tumble,
		Smooth,
		Bounce,
		SlowTurn,
		FalseSlowTurn,
		Count
	}

	[Header("References")]
	[SerializeField]
	private Transform spinTransform;

	[SerializeField]
	private MeshCollider dreidelCollider;

	[SerializeField]
	private AudioSource audioSource;

	[SerializeField]
	private AudioClip spinLoopAudio;

	[SerializeField]
	private AudioClip fallSound;

	[SerializeField]
	private AudioClip gimelConfettiSound;

	[SerializeField]
	private ParticleSystem gimelConfetti;

	[Header("Offsets")]
	[SerializeField]
	private Vector3 centerOfMassOffset = Vector3.zero;

	[SerializeField]
	private Vector3 bottomPointOffset = Vector3.zero;

	[SerializeField]
	private Vector2 bodyRect = Vector2.one;

	[SerializeField]
	private float confettiHeight = 0.125f;

	[Header("Surface Detection")]
	[SerializeField]
	private float surfaceCheckDistance = 0.15f;

	[SerializeField]
	private float surfaceUprightThreshold = 0.5f;

	[SerializeField]
	private float surfaceDreidelAngleThreshold = 0.9f;

	[SerializeField]
	private LayerMask surfaceLayers;

	[Header("Spin Paramss")]
	[SerializeField]
	private float spinSpeedStart = 2f;

	[SerializeField]
	private float spinSpeedEnd = 1f;

	[SerializeField]
	private float spinTime = 10f;

	[SerializeField]
	private Vector2 spinTimeRange = new Vector2(7f, 12f);

	[SerializeField]
	private float spinWobbleFrequency = 0.1f;

	[SerializeField]
	private float spinWobbleAmplitude = 0.01f;

	[SerializeField]
	private float spinWobbleAmplitudeEndMin = 0.01f;

	[SerializeField]
	private float tiltFrontBack;

	[SerializeField]
	private float tiltLeftRight;

	[SerializeField]
	private float groundTrackingDampingRatio = 0.9f;

	[SerializeField]
	private float groundTrackingFrequency = 1f;

	[Header("Motion Path")]
	[SerializeField]
	private float pathMoveSpeed = 0.1f;

	[SerializeField]
	private float pathStartTurnRate = 360f;

	[SerializeField]
	private float pathEndTurnRate = 90f;

	[SerializeField]
	private float pathTurnRateSinOffset = 180f;

	[Header("Falling Params")]
	[SerializeField]
	private float spinSpeedStopRate = 1f;

	[SerializeField]
	private float tumbleFallDampingRatio = 0.4f;

	[SerializeField]
	private float tumbleFallFrequency = 6f;

	[SerializeField]
	private float tumbleFallFrontBackDampingRatio = 0.4f;

	[SerializeField]
	private float tumbleFallFrontBackFrequency = 6f;

	[SerializeField]
	private float smoothFallDampingRatio = 0.9f;

	[SerializeField]
	private float smoothFallFrequency = 2f;

	[SerializeField]
	private float slowTurnDampingRatio = 0.9f;

	[SerializeField]
	private float slowTurnFrequency = 2f;

	[SerializeField]
	private float bounceFallSwitchTime = 0.5f;

	[SerializeField]
	private float slowTurnSwitchTime = 0.5f;

	[SerializeField]
	private float respawnTimeAfterLanding = 3f;

	[SerializeField]
	private float fallTimeTumble = 3f;

	[SerializeField]
	private float fallTimeSlowTurn = 5f;

	private State state;

	private double stateStartTime;

	private float spinSpeed;

	private float spinAngle;

	private Vector3 spinAxis = Vector3.up;

	private bool canStartSpin;

	private double spinStartTime = -1.0;

	private float tiltWobble;

	private bool falseTargetReached;

	private bool hasLanded;

	private Vector3 pathOffset = Vector3.zero;

	private Vector3 pathDir = Vector3.forward;

	private Vector3 surfacePlanePoint;

	private Vector3 surfacePlaneNormal;

	private FloatSpring tiltFrontBackSpring;

	private FloatSpring tiltLeftRightSpring;

	private FloatSpring spinSpeedSpring;

	private Vector3Spring groundPointSpring;

	private Vector2[] landingTiltValues = new Vector2[4]
	{
		new Vector2(1f, -1f),
		new Vector2(1f, 0f),
		new Vector2(-1f, 1f),
		new Vector2(-1f, 0f)
	};

	private Vector2 landingTiltLeadingTarget = Vector2.zero;

	private Vector2 landingTiltTarget = Vector2.zero;

	[Header("Debug Params")]
	[SerializeField]
	private Side landingSide;

	[SerializeField]
	private Variation landingVariation;

	[SerializeField]
	private bool spinCounterClockwise;

	[SerializeField]
	private bool debugDraw;

	public bool TrySetIdle()
	{
		if (state == State.Idle || state == State.FindingSurface || state == State.Fallen)
		{
			StartIdle();
			return true;
		}
		return false;
	}

	public bool TryCheckForSurfaces()
	{
		if (state == State.Idle || state == State.FindingSurface)
		{
			StartFindingSurfaces();
			return true;
		}
		return false;
	}

	public void Spin()
	{
		StartSpin();
	}

	public bool TryGetSpinStartData(out Vector3 surfacePoint, out Vector3 surfaceNormal, out float randomDuration, out Side randomSide, out Variation randomVariation, out double startTime)
	{
		if (canStartSpin)
		{
			surfacePoint = surfacePlanePoint;
			surfaceNormal = surfacePlaneNormal;
			randomDuration = UnityEngine.Random.Range(spinTimeRange.x, spinTimeRange.y);
			randomSide = (Side)UnityEngine.Random.Range(0, 4);
			randomVariation = (Variation)UnityEngine.Random.Range(0, 5);
			startTime = (PhotonNetwork.InRoom ? PhotonNetwork.Time : (-1.0));
			return true;
		}
		surfacePoint = Vector3.zero;
		surfaceNormal = Vector3.zero;
		randomDuration = 0f;
		randomSide = Side.Shin;
		randomVariation = Variation.Tumble;
		startTime = -1.0;
		return false;
	}

	public void SetSpinStartData(Vector3 surfacePoint, Vector3 surfaceNormal, float duration, bool counterClockwise, Side side, Variation variation, double startTime)
	{
		surfacePlanePoint = surfacePoint;
		surfacePlaneNormal = surfaceNormal;
		spinTime = duration;
		spinStartTime = startTime;
		spinCounterClockwise = counterClockwise;
		landingSide = side;
		landingVariation = variation;
	}

	private void LateUpdate()
	{
		float deltaTime = Time.deltaTime;
		double num = (PhotonNetwork.InRoom ? PhotonNetwork.Time : ((double)Time.time));
		canStartSpin = false;
		switch (state)
		{
		default:
			base.transform.localPosition = Vector3.zero;
			base.transform.localRotation = Quaternion.identity;
			spinTransform.localRotation = Quaternion.identity;
			spinTransform.localPosition = Vector3.zero;
			break;
		case State.FindingSurface:
		{
			float num12 = ((GTPlayer.Instance != null) ? GTPlayer.Instance.scale : 1f);
			Vector3 down = Vector3.down;
			Vector3 origin = base.transform.parent.position - down * 2f * surfaceCheckDistance * num12;
			float maxDistance = (3f * surfaceCheckDistance + (0f - bottomPointOffset.y)) * num12;
			if (Physics.Raycast(origin, down, out var hitInfo, maxDistance, surfaceLayers.value, QueryTriggerInteraction.Ignore) && Vector3.Dot(hitInfo.normal, Vector3.up) > surfaceUprightThreshold && Vector3.Dot(hitInfo.normal, spinTransform.up) > surfaceDreidelAngleThreshold)
			{
				canStartSpin = true;
				surfacePlanePoint = hitInfo.point;
				surfacePlaneNormal = hitInfo.normal;
				AlignToSurfacePlane();
				groundPointSpring.Reset(GetGroundContactPoint(), Vector3.zero);
				UpdateSpinTransform();
			}
			else
			{
				canStartSpin = false;
				base.transform.localPosition = Vector3.zero;
				base.transform.localRotation = Quaternion.identity;
				spinTransform.localRotation = Quaternion.identity;
				spinTransform.localPosition = Vector3.zero;
			}
			break;
		}
		case State.Spinning:
		{
			float num7 = Mathf.Clamp01((float)(num - stateStartTime) / spinTime);
			spinSpeed = Mathf.Lerp(spinSpeedStart, spinSpeedEnd, num7);
			float num8 = (spinCounterClockwise ? (-1f) : 1f);
			spinAngle += num8 * spinSpeed * 360f * deltaTime;
			float num9 = tiltWobble;
			float num10 = Mathf.Sin(spinWobbleFrequency * 2f * MathF.PI * (float)(num - stateStartTime));
			float t = 0.5f * num10 + 0.5f;
			tiltWobble = Mathf.Lerp(spinWobbleAmplitudeEndMin * num7, spinWobbleAmplitude * num7, t);
			if (landingTiltTarget.y == 0f)
			{
				if (landingVariation == Variation.Tumble || landingVariation == Variation.Smooth)
				{
					tiltFrontBack = Mathf.Sign(landingTiltTarget.x) * tiltWobble;
				}
				else
				{
					tiltFrontBack = Mathf.Sign(landingTiltLeadingTarget.x) * tiltWobble;
				}
			}
			else if (landingVariation == Variation.Tumble || landingVariation == Variation.Smooth)
			{
				tiltLeftRight = Mathf.Sign(landingTiltTarget.y) * tiltWobble;
			}
			else
			{
				tiltLeftRight = Mathf.Sign(landingTiltLeadingTarget.y) * tiltWobble;
			}
			float num11 = Mathf.Lerp(pathStartTurnRate, pathEndTurnRate, num7) + num10 * pathTurnRateSinOffset;
			if (spinCounterClockwise)
			{
				pathDir = Vector3.ProjectOnPlane(Quaternion.AngleAxis((0f - num11) * deltaTime, Vector3.up) * pathDir, Vector3.up);
				pathDir.Normalize();
			}
			else
			{
				pathDir = Vector3.ProjectOnPlane(Quaternion.AngleAxis((0f - num11) * deltaTime, Vector3.up) * pathDir, Vector3.up);
				pathDir.Normalize();
			}
			pathOffset += pathDir * pathMoveSpeed * deltaTime;
			AlignToSurfacePlane();
			UpdateSpinTransform();
			if (num7 - Mathf.Epsilon >= 1f && tiltWobble > 0.9f * spinWobbleAmplitude && num9 < tiltWobble)
			{
				StartFall();
			}
			break;
		}
		case State.Falling:
		{
			float num2 = fallTimeTumble;
			Variation variation = landingVariation;
			if ((uint)variation <= 1u || (uint)(variation - 2) > 2u)
			{
				spinSpeed = Mathf.MoveTowards(spinSpeed, 0f, spinSpeedStopRate * deltaTime);
				float num3 = (spinCounterClockwise ? (-1f) : 1f);
				spinAngle += num3 * spinSpeed * 360f * deltaTime;
				float angularFrequency = ((landingVariation == Variation.Smooth) ? smoothFallFrequency : tumbleFallFrontBackFrequency);
				float dampingRatio = ((landingVariation == Variation.Smooth) ? smoothFallDampingRatio : tumbleFallFrontBackDampingRatio);
				float angularFrequency2 = ((landingVariation == Variation.Smooth) ? smoothFallFrequency : tumbleFallFrequency);
				float dampingRatio2 = ((landingVariation == Variation.Smooth) ? smoothFallDampingRatio : tumbleFallDampingRatio);
				tiltFrontBack = tiltFrontBackSpring.TrackDampingRatio(landingTiltTarget.x, angularFrequency, dampingRatio, deltaTime);
				tiltLeftRight = tiltLeftRightSpring.TrackDampingRatio(landingTiltTarget.y, angularFrequency2, dampingRatio2, deltaTime);
			}
			else
			{
				bool flag = landingVariation != Variation.Bounce;
				bool flag2 = landingVariation == Variation.FalseSlowTurn;
				float num4 = (flag ? slowTurnSwitchTime : bounceFallSwitchTime);
				if (flag)
				{
					num2 = fallTimeSlowTurn;
				}
				if (num - stateStartTime < (double)num4)
				{
					tiltFrontBack = tiltFrontBackSpring.TrackDampingRatio(landingTiltLeadingTarget.x, tumbleFallFrontBackFrequency, tumbleFallFrontBackDampingRatio, deltaTime);
					tiltLeftRight = tiltLeftRightSpring.TrackDampingRatio(landingTiltLeadingTarget.y, tumbleFallFrequency, tumbleFallDampingRatio, deltaTime);
				}
				else
				{
					tiltFrontBack = tiltFrontBackSpring.TrackDampingRatio(landingTiltTarget.x, tumbleFallFrontBackFrequency, tumbleFallFrontBackDampingRatio, deltaTime);
					if (flag2)
					{
						if (!falseTargetReached && Mathf.Abs(landingTiltTarget.y - tiltLeftRight) > 0.49f)
						{
							tiltLeftRight = tiltLeftRightSpring.TrackDampingRatio(landingTiltTarget.y, slowTurnFrequency, slowTurnDampingRatio, deltaTime);
						}
						else
						{
							falseTargetReached = true;
							tiltLeftRight = tiltLeftRightSpring.TrackDampingRatio(landingTiltLeadingTarget.y, tumbleFallFrequency, tumbleFallDampingRatio, deltaTime);
						}
					}
					else if (flag && Mathf.Abs(landingTiltTarget.y - tiltLeftRight) > 0.45f)
					{
						tiltLeftRight = tiltLeftRightSpring.TrackDampingRatio(landingTiltTarget.y, slowTurnFrequency, slowTurnDampingRatio, deltaTime);
					}
					else
					{
						tiltLeftRight = tiltLeftRightSpring.TrackDampingRatio(landingTiltTarget.y, tumbleFallFrequency, tumbleFallDampingRatio, deltaTime);
					}
				}
				spinSpeed = Mathf.MoveTowards(spinSpeed, 0f, spinSpeedStopRate * deltaTime);
				float num5 = (spinCounterClockwise ? (-1f) : 1f);
				spinAngle += num5 * spinSpeed * 360f * deltaTime;
			}
			AlignToSurfacePlane();
			UpdateSpinTransform();
			float num6 = (float)(num - stateStartTime);
			if (!(num6 > num2))
			{
				break;
			}
			if (!hasLanded)
			{
				hasLanded = true;
				if (landingSide == Side.Gimel)
				{
					gimelConfetti.transform.position = spinTransform.position + Vector3.up * confettiHeight;
					gimelConfetti.gameObject.SetActive(value: true);
					audioSource.GTPlayOneShot(gimelConfettiSound);
				}
			}
			if (num6 > num2 + respawnTimeAfterLanding)
			{
				StartIdle();
			}
			break;
		}
		case State.Fallen:
			break;
		}
	}

	private void StartIdle()
	{
		state = State.Idle;
		stateStartTime = (PhotonNetwork.InRoom ? PhotonNetwork.Time : ((double)Time.time));
		canStartSpin = false;
		spinAngle = 0f;
		base.transform.localPosition = Vector3.zero;
		base.transform.localRotation = Quaternion.identity;
		spinTransform.localRotation = Quaternion.identity;
		spinTransform.localPosition = Vector3.zero;
		tiltFrontBack = 0f;
		tiltLeftRight = 0f;
		pathOffset = Vector3.zero;
		pathDir = Vector3.forward;
		gimelConfetti.gameObject.SetActive(value: false);
		groundPointSpring.Reset(GetGroundContactPoint(), Vector3.zero);
		UpdateSpinTransform();
	}

	private void StartFindingSurfaces()
	{
		state = State.FindingSurface;
		stateStartTime = (PhotonNetwork.InRoom ? PhotonNetwork.Time : ((double)Time.time));
		canStartSpin = false;
		spinAngle = 0f;
		base.transform.localPosition = Vector3.zero;
		base.transform.localRotation = Quaternion.identity;
		spinTransform.localRotation = Quaternion.identity;
		spinTransform.localPosition = Vector3.zero;
		tiltFrontBack = 0f;
		tiltLeftRight = 0f;
		pathOffset = Vector3.zero;
		pathDir = Vector3.forward;
		gimelConfetti.gameObject.SetActive(value: false);
		groundPointSpring.Reset(GetGroundContactPoint(), Vector3.zero);
		UpdateSpinTransform();
	}

	private void StartSpin()
	{
		state = State.Spinning;
		stateStartTime = ((spinStartTime > 0.0) ? spinStartTime : ((double)Time.time));
		canStartSpin = false;
		spinSpeed = spinSpeedStart;
		tiltWobble = 0f;
		audioSource.loop = true;
		audioSource.clip = spinLoopAudio;
		audioSource.GTPlay();
		gimelConfetti.gameObject.SetActive(value: false);
		AlignToSurfacePlane();
		groundPointSpring.Reset(GetGroundContactPoint(), Vector3.zero);
		UpdateSpinTransform();
		pathOffset = Vector3.zero;
		pathDir = Vector3.forward;
	}

	private void StartFall()
	{
		state = State.Falling;
		stateStartTime = (PhotonNetwork.InRoom ? PhotonNetwork.Time : ((double)Time.time));
		canStartSpin = false;
		falseTargetReached = false;
		hasLanded = false;
		if (landingVariation == Variation.FalseSlowTurn)
		{
			if (spinCounterClockwise)
			{
				GetTiltVectorsForSideWithPrev(landingSide, out landingTiltLeadingTarget, out landingTiltTarget);
			}
			else
			{
				GetTiltVectorsForSideWithNext(landingSide, out landingTiltLeadingTarget, out landingTiltTarget);
			}
		}
		else if (spinCounterClockwise)
		{
			GetTiltVectorsForSideWithNext(landingSide, out landingTiltTarget, out landingTiltLeadingTarget);
		}
		else
		{
			GetTiltVectorsForSideWithPrev(landingSide, out landingTiltTarget, out landingTiltLeadingTarget);
		}
		spinSpeedSpring.Reset(spinSpeed, 0f);
		tiltFrontBackSpring.Reset(tiltFrontBack, 0f);
		tiltLeftRightSpring.Reset(tiltLeftRight, 0f);
		groundPointSpring.Reset(GetGroundContactPoint(), Vector3.zero);
		audioSource.loop = false;
		audioSource.GTPlayOneShot(fallSound);
		gimelConfetti.gameObject.SetActive(value: false);
	}

	private Vector3 GetGroundContactPoint()
	{
		Vector3 position = spinTransform.position;
		dreidelCollider.enabled = true;
		Vector3 vector = dreidelCollider.ClosestPoint(position - base.transform.up);
		dreidelCollider.enabled = false;
		float num = Vector3.Dot(vector - position, spinTransform.up);
		if (num > 0f)
		{
			vector -= num * spinTransform.up;
		}
		return spinTransform.InverseTransformPoint(vector);
	}

	private void GetTiltVectorsForSideWithPrev(Side side, out Vector2 sideTilt, out Vector2 prevSideTilt)
	{
		int num = (int)((side <= Side.Shin) ? Side.Nun : (side - 1));
		if (side == Side.Hey || side == Side.Nun)
		{
			sideTilt = landingTiltValues[(int)side];
			prevSideTilt = landingTiltValues[num];
			prevSideTilt.x = sideTilt.x;
		}
		else
		{
			prevSideTilt = landingTiltValues[num];
			sideTilt = landingTiltValues[(int)side];
			sideTilt.x = prevSideTilt.x;
		}
	}

	private void GetTiltVectorsForSideWithNext(Side side, out Vector2 sideTilt, out Vector2 nextSideTilt)
	{
		int num = (int)(side + 1) % 4;
		if (side == Side.Hey || side == Side.Nun)
		{
			sideTilt = landingTiltValues[(int)side];
			nextSideTilt = landingTiltValues[num];
			nextSideTilt.x = sideTilt.x;
		}
		else
		{
			nextSideTilt = landingTiltValues[num];
			sideTilt = landingTiltValues[(int)side];
			sideTilt.x = nextSideTilt.x;
		}
	}

	private void AlignToSurfacePlane()
	{
		Vector3 forward = Vector3.forward;
		if (Vector3.Dot(Vector3.up, surfacePlaneNormal) < 0.9999f)
		{
			Vector3 axis = Vector3.Cross(surfacePlaneNormal, Vector3.up);
			forward = Quaternion.AngleAxis(90f, axis) * surfacePlaneNormal;
		}
		Quaternion rotation = Quaternion.LookRotation(forward, surfacePlaneNormal);
		base.transform.position = surfacePlanePoint;
		base.transform.rotation = rotation;
	}

	private void UpdateSpinTransform()
	{
		Vector3 position = spinTransform.position;
		Vector3 groundContactPoint = GetGroundContactPoint();
		Vector3 position2 = groundPointSpring.TrackDampingRatio(groundContactPoint, groundTrackingFrequency, groundTrackingDampingRatio, Time.deltaTime);
		Vector3 vector = spinTransform.TransformPoint(position2);
		Quaternion quaternion = Quaternion.AngleAxis(90f * tiltLeftRight, Vector3.forward) * Quaternion.AngleAxis(90f * tiltFrontBack, Vector3.right);
		spinAxis = base.transform.InverseTransformDirection(base.transform.up);
		Quaternion quaternion2 = Quaternion.AngleAxis(spinAngle, spinAxis);
		spinTransform.localRotation = quaternion2 * quaternion;
		Vector3 vector2 = base.transform.InverseTransformVector(Vector3.Dot(position - vector, base.transform.up) * base.transform.up);
		spinTransform.localPosition = vector2 + pathOffset;
		spinTransform.TransformPoint(bottomPointOffset);
	}
}
