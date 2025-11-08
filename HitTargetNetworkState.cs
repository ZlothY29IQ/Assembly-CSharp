using System;
using System.Collections;
using Fusion;
using GorillaTag;
using Photon.Pun;
using UnityEngine;

[NetworkBehaviourWeaved(1)]
public class HitTargetNetworkState : NetworkComponent
{
	[SerializeField]
	private WatchableIntSO networkedScore;

	[SerializeField]
	private int hitCooldownTime = 1;

	[SerializeField]
	private bool testPress;

	[SerializeField]
	private AudioClip[] audioClips;

	[SerializeField]
	private bool scoreIsDistance;

	[SerializeField]
	private float resetAfterDuration;

	private AudioSource audioPlayer;

	private float nextHittableTimestamp;

	private float resetAtTimestamp;

	private Coroutine resetCoroutine;

	[WeaverGenerated]
	[SerializeField]
	[DefaultForProperty("Data", 0, 1)]
	[DrawIf("IsEditorWritable", true, CompareOperator.Equal, DrawIfMode.ReadOnly)]
	private int _Data;

	[Networked]
	[NetworkedWeaved(0, 1)]
	public unsafe int Data
	{
		get
		{
			if (((NetworkBehaviour)this).Ptr == null)
			{
				throw new InvalidOperationException("Error when accessing HitTargetNetworkState.Data. Networked properties can only be accessed when Spawned() has been called.");
			}
			return *(int*)((byte*)((NetworkBehaviour)this).Ptr + 0);
		}
		set
		{
			if (((NetworkBehaviour)this).Ptr == null)
			{
				throw new InvalidOperationException("Error when accessing HitTargetNetworkState.Data. Networked properties can only be accessed when Spawned() has been called.");
			}
			*(int*)((byte*)((NetworkBehaviour)this).Ptr + 0) = value;
		}
	}

	protected override void Awake()
	{
		base.Awake();
		audioPlayer = GetComponent<AudioSource>();
		SlingshotProjectileHitNotifier component = GetComponent<SlingshotProjectileHitNotifier>();
		if (component != null)
		{
			component.OnProjectileHit += ProjectileHitReciever;
			component.OnProjectileCollisionStay += ProjectileHitReciever;
		}
		else
		{
			Debug.LogError("Needs SlingshotProjectileHitNotifier added to this GameObject to increment score");
		}
	}

	protected override void Start()
	{
		base.Start();
		RoomSystem.LeftRoomEvent += new Action(OnLeftRoom);
	}

	private void SetInitialState()
	{
		networkedScore.Value = 0;
		nextHittableTimestamp = 0f;
		audioPlayer.GTStop();
	}

	public void OnLeftRoom()
	{
		SetInitialState();
	}

	internal override void OnEnable()
	{
		NetworkBehaviourUtils.InternalOnEnable(this);
		base.OnEnable();
		if (Application.isEditor)
		{
			StartCoroutine(TestPressCheck());
		}
		SetInitialState();
	}

	private IEnumerator TestPressCheck()
	{
		while (true)
		{
			if (testPress)
			{
				testPress = false;
				TargetHit(Vector3.zero, Vector3.one);
			}
			yield return new WaitForSeconds(1f);
		}
	}

	private void ProjectileHitReciever(SlingshotProjectile projectile, Collision collision)
	{
		TargetHit(projectile.launchPosition, collision.contacts[0].point);
	}

	public void TargetHit(Vector3 launchPoint, Vector3 impactPoint)
	{
		if (!NetworkSystem.Instance.IsMasterClient || Time.time <= nextHittableTimestamp)
		{
			return;
		}
		int value = networkedScore.Value;
		if (scoreIsDistance)
		{
			int num = Mathf.RoundToInt((launchPoint - impactPoint).magnitude * 3.28f);
			if (num <= value)
			{
				return;
			}
			value = num;
		}
		else
		{
			value++;
			if (value >= 1000)
			{
				value = 0;
			}
		}
		if (resetAfterDuration > 0f && resetCoroutine == null)
		{
			resetAtTimestamp = Time.time + resetAfterDuration;
			resetCoroutine = StartCoroutine(ResetCo());
		}
		PlayAudio(networkedScore.Value, value);
		networkedScore.Value = value;
		nextHittableTimestamp = Time.time + (float)hitCooldownTime;
	}

	public override void WriteDataFusion()
	{
		Data = networkedScore.Value;
	}

	public override void ReadDataFusion()
	{
		int data = Data;
		if (data != networkedScore.Value)
		{
			PlayAudio(networkedScore.Value, data);
		}
		networkedScore.Value = data;
	}

	protected override void WriteDataPUN(PhotonStream stream, PhotonMessageInfo info)
	{
		if (info.Sender.IsMasterClient)
		{
			stream.SendNext(networkedScore.Value);
		}
	}

	protected override void ReadDataPUN(PhotonStream stream, PhotonMessageInfo info)
	{
		if (info.Sender.IsMasterClient)
		{
			int num = (int)stream.ReceiveNext();
			if (num != networkedScore.Value)
			{
				PlayAudio(networkedScore.Value, num);
			}
			networkedScore.Value = num;
		}
	}

	public void PlayAudio(int oldScore, int newScore)
	{
		if (oldScore > newScore && !scoreIsDistance)
		{
			audioPlayer.GTPlayOneShot(audioClips[1]);
		}
		else
		{
			audioPlayer.GTPlayOneShot(audioClips[0]);
		}
	}

	private IEnumerator ResetCo()
	{
		while (Time.time < resetAtTimestamp)
		{
			yield return new WaitForSeconds(resetAtTimestamp - Time.time);
		}
		networkedScore.Value = 0;
		PlayAudio(networkedScore.Value, 0);
		resetCoroutine = null;
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
