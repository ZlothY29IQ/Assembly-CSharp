using System;
using Fusion;
using Photon.Pun;
using UnityEngine;

namespace GorillaLocomotion.Gameplay;

[NetworkBehaviourWeaved(1)]
public class TraverseSpline : NetworkComponent
{
	public BezierSpline spline;

	public float duration = 30f;

	public float speedMultiplierWhileHeld = 2f;

	private float currentSpeedMultiplier;

	public float acceleration = 1f;

	public float deceleration = 1f;

	private bool isHeldByLocalPlayer;

	public bool lookForward = true;

	public SplineWalkerMode mode;

	[SerializeField]
	private float SplineProgressOffet;

	private float progress;

	private float progressLerpStart;

	private float progressLerpEnd;

	private const float progressLerpDuration = 1f;

	private float progressLerpStartTime;

	private bool goingForward = true;

	[SerializeField]
	private bool constantVelocity;

	[WeaverGenerated]
	[SerializeField]
	[DefaultForProperty("Data", 0, 1)]
	[DrawIf("IsEditorWritable", true, CompareOperator.Equal, DrawIfMode.ReadOnly)]
	private float _Data;

	[Networked]
	[NetworkedWeaved(0, 1)]
	public unsafe float Data
	{
		get
		{
			if (((NetworkBehaviour)this).Ptr == null)
			{
				throw new InvalidOperationException("Error when accessing TraverseSpline.Data. Networked properties can only be accessed when Spawned() has been called.");
			}
			return *(float*)((byte*)((NetworkBehaviour)this).Ptr + 0);
		}
		set
		{
			if (((NetworkBehaviour)this).Ptr == null)
			{
				throw new InvalidOperationException("Error when accessing TraverseSpline.Data. Networked properties can only be accessed when Spawned() has been called.");
			}
			*(float*)((byte*)((NetworkBehaviour)this).Ptr + 0) = value;
		}
	}

	protected override void Awake()
	{
		base.Awake();
		progress = SplineProgressOffet % 1f;
	}

	protected virtual void FixedUpdate()
	{
		if (!base.IsMine && progressLerpStartTime + 1f > Time.time)
		{
			progress = Mathf.Lerp(progressLerpStart, progressLerpEnd, (Time.time - progressLerpStartTime) / 1f);
		}
		else
		{
			if (isHeldByLocalPlayer)
			{
				currentSpeedMultiplier = Mathf.MoveTowards(currentSpeedMultiplier, speedMultiplierWhileHeld, acceleration * Time.deltaTime);
			}
			else
			{
				currentSpeedMultiplier = Mathf.MoveTowards(currentSpeedMultiplier, 1f, deceleration * Time.deltaTime);
			}
			if (goingForward)
			{
				progress += Time.deltaTime * currentSpeedMultiplier / duration;
				if (progress > 1f)
				{
					if (mode == SplineWalkerMode.Once)
					{
						progress = 1f;
					}
					else if (mode == SplineWalkerMode.Loop)
					{
						progress %= 1f;
					}
					else
					{
						progress = 2f - progress;
						goingForward = false;
					}
				}
			}
			else
			{
				progress -= Time.deltaTime * currentSpeedMultiplier / duration;
				if (progress < 0f)
				{
					progress = 0f - progress;
					goingForward = true;
				}
			}
		}
		Vector3 point = spline.GetPoint(progress, constantVelocity);
		base.transform.position = point;
		if (lookForward)
		{
			base.transform.LookAt(base.transform.position + spline.GetDirection(progress, constantVelocity));
		}
	}

	public override void WriteDataFusion()
	{
		Data = progress + currentSpeedMultiplier * 1f / duration;
	}

	public override void ReadDataFusion()
	{
		progressLerpEnd = Data;
		ReadDataShared();
	}

	protected override void WriteDataPUN(PhotonStream stream, PhotonMessageInfo info)
	{
		stream.SendNext(progress + currentSpeedMultiplier * 1f / duration);
	}

	protected override void ReadDataPUN(PhotonStream stream, PhotonMessageInfo info)
	{
		progressLerpEnd = (float)stream.ReceiveNext();
		ReadDataShared();
	}

	private void ReadDataShared()
	{
		if (float.IsNaN(progressLerpEnd) || float.IsInfinity(progressLerpEnd))
		{
			progressLerpEnd = 1f;
		}
		else
		{
			progressLerpEnd = Mathf.Abs(progressLerpEnd);
			if (progressLerpEnd > 1f)
			{
				progressLerpEnd = (float)((double)progressLerpEnd % 1.0);
			}
		}
		progressLerpStart = ((Mathf.Abs(progressLerpEnd - progress) > Mathf.Abs(progressLerpEnd - (progress - 1f))) ? (progress - 1f) : progress);
		progressLerpStartTime = Time.time;
	}

	protected float GetProgress()
	{
		return progress;
	}

	public float GetCurrentSpeed()
	{
		return currentSpeedMultiplier;
	}

	[WeaverGenerated]
	public override void CopyBackingFieldsToState(bool P_0)
	{
		base.CopyBackingFieldsToState(P_0);
		Data = _Data;
	}

	[WeaverGenerated]
	public override void CopyStateToBackingFields()
	{
		base.CopyStateToBackingFields();
		_Data = Data;
	}
}
