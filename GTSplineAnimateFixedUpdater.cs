using System;
using Fusion;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Splines;

[NetworkBehaviourWeaved(1)]
public class GTSplineAnimateFixedUpdater : NetworkComponent
{
	[SerializeField]
	private XSceneRef splineAnimateRef;

	[SerializeField]
	private float Duration;

	private const float progressLerpDuration = 1f;

	private SplineAnimate splineAnimate;

	private bool isSplineLoaded;

	private float progress;

	private float progressLerpStart;

	private float progressLerpEnd;

	private float progressLerpStartTime;

	[WeaverGenerated]
	[SerializeField]
	[DefaultForProperty("Netdata", 0, 1)]
	[DrawIf("IsEditorWritable", true, CompareOperator.Equal, DrawIfMode.ReadOnly)]
	private float _Netdata;

	[Networked]
	[NetworkedWeaved(0, 1)]
	public unsafe float Netdata
	{
		get
		{
			if (((NetworkBehaviour)this).Ptr == null)
			{
				throw new InvalidOperationException("Error when accessing GTSplineAnimateFixedUpdater.Netdata. Networked properties can only be accessed when Spawned() has been called.");
			}
			return *(float*)((byte*)((NetworkBehaviour)this).Ptr + 0);
		}
		set
		{
			if (((NetworkBehaviour)this).Ptr == null)
			{
				throw new InvalidOperationException("Error when accessing GTSplineAnimateFixedUpdater.Netdata. Networked properties can only be accessed when Spawned() has been called.");
			}
			*(float*)((byte*)((NetworkBehaviour)this).Ptr + 0) = value;
		}
	}

	protected override void Awake()
	{
		base.Awake();
		splineAnimateRef.AddCallbackOnLoad(InitSplineAnimate);
		splineAnimateRef.AddCallbackOnUnload(ClearSplineAnimate);
	}

	private void InitSplineAnimate()
	{
		isSplineLoaded = splineAnimateRef.TryResolve(out splineAnimate);
		if (isSplineLoaded && splineAnimate != null)
		{
			splineAnimate.enabled = false;
		}
	}

	private void ClearSplineAnimate()
	{
		splineAnimate = null;
		isSplineLoaded = false;
	}

	private void FixedUpdate()
	{
		if (!base.IsMine && progressLerpStartTime + 1f > Time.time)
		{
			if (isSplineLoaded)
			{
				progress = Mathf.Lerp(progressLerpStart, progressLerpEnd, (Time.time - progressLerpStartTime) / 1f) % Duration;
				splineAnimate.NormalizedTime = progress / Duration;
			}
		}
		else
		{
			progress = (progress + Time.fixedDeltaTime) % Duration;
			if (isSplineLoaded)
			{
				splineAnimate.NormalizedTime = progress / Duration;
			}
		}
	}

	public override void WriteDataFusion()
	{
		Netdata = progress + 1f;
	}

	public override void ReadDataFusion()
	{
		SharedReadData(Netdata);
	}

	protected override void WriteDataPUN(PhotonStream stream, PhotonMessageInfo info)
	{
		if (info.Sender.IsMasterClient)
		{
			stream.SendNext(progress + 1f);
		}
	}

	protected override void ReadDataPUN(PhotonStream stream, PhotonMessageInfo info)
	{
		if (info.Sender.IsMasterClient)
		{
			float incomingValue = (float)stream.ReceiveNext();
			SharedReadData(incomingValue);
		}
	}

	private void SharedReadData(float incomingValue)
	{
		if (float.IsNaN(incomingValue) || incomingValue > Duration + 1f || incomingValue < 0f)
		{
			return;
		}
		progressLerpEnd = incomingValue;
		if (progressLerpEnd < progress)
		{
			if (progress < Duration)
			{
				progressLerpEnd += Duration;
			}
			else
			{
				progress -= Duration;
			}
		}
		progressLerpStart = progress;
		progressLerpStartTime = Time.time;
	}

	[WeaverGenerated]
	public override void CopyBackingFieldsToState(bool P_0)
	{
		base.CopyBackingFieldsToState(P_0);
		Netdata = _Netdata;
	}

	[WeaverGenerated]
	public override void CopyStateToBackingFields()
	{
		base.CopyStateToBackingFields();
		_Netdata = Netdata;
	}
}
