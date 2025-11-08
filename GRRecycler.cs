using System;
using UnityEngine;

public class GRRecycler : MonoBehaviourTick
{
	private GameEntity gameEntity;

	public ParticleSystem closeEffects;

	public ParticleSystem openEffects;

	[NonSerialized]
	public GhostReactor reactor;

	public GRRecyclerScanner scanner;

	public Animation anim;

	public float closeDuration = 1f;

	private float timeRemaining;

	private bool closed;

	private bool playedAudio;

	public AudioSource audioSource;

	public AudioClip recyclerRunningAudio;

	public float recyclerRunningAudioVolume = 0.5f;

	public override void Tick()
	{
		if (!closed || anim.isPlaying)
		{
			return;
		}
		if (!playedAudio)
		{
			audioSource.volume = recyclerRunningAudioVolume;
			audioSource.PlayOneShot(recyclerRunningAudio);
			playedAudio = true;
		}
		timeRemaining -= Time.deltaTime;
		if (timeRemaining <= 0f)
		{
			anim.PlayQueued("Recycler_Open", QueueMode.CompleteOthers);
			closed = false;
			if (closeEffects != null && openEffects != null)
			{
				closeEffects.Stop();
				openEffects.Play();
			}
		}
	}

	public void Init(GhostReactor reactor)
	{
		this.reactor = reactor;
	}

	public int GetRecycleValue(GRTool.GRToolType type)
	{
		return reactor.toolProgression.GetRecycleShiftCredit(type);
	}

	public void ScanItem(GRTool.GRToolType toolType)
	{
		scanner.ScanItem(toolType);
	}

	public void RecycleItem()
	{
		if (anim != null)
		{
			anim.Play("Recycler_Close");
		}
		if (closeEffects != null && openEffects != null)
		{
			openEffects.Stop();
			closeEffects.Play();
		}
		closed = true;
		playedAudio = false;
		timeRemaining = closeDuration;
	}

	private void OnTriggerEnter(Collider other)
	{
		if (reactor == null || !reactor.grManager.IsAuthority())
		{
			return;
		}
		int num = 0;
		GRTool.GRToolType gRToolType = GRTool.GRToolType.None;
		GRTool componentInParent = other.gameObject.GetComponentInParent<GRTool>();
		if (componentInParent == null)
		{
			return;
		}
		if (other.gameObject.GetComponentInParent<GRToolClub>() != null)
		{
			gRToolType = GRTool.GRToolType.Club;
		}
		else if (other.gameObject.GetComponentInParent<GRToolCollector>() != null)
		{
			gRToolType = GRTool.GRToolType.Collector;
		}
		else if (other.gameObject.GetComponentInParent<GRToolFlash>() != null)
		{
			gRToolType = GRTool.GRToolType.Flash;
		}
		else if (other.gameObject.GetComponentInParent<GRToolLantern>() != null)
		{
			gRToolType = GRTool.GRToolType.Lantern;
		}
		else if (other.gameObject.GetComponentInParent<GRToolRevive>() != null)
		{
			gRToolType = GRTool.GRToolType.Revive;
		}
		else if (other.gameObject.GetComponentInParent<GRToolShieldGun>() != null)
		{
			gRToolType = GRTool.GRToolType.ShieldGun;
		}
		else if (other.gameObject.GetComponentInParent<GRToolDirectionalShield>() != null)
		{
			gRToolType = GRTool.GRToolType.DirectionalShield;
		}
		else if (componentInParent.toolType == GRTool.GRToolType.HockeyStick)
		{
			gRToolType = componentInParent.toolType;
		}
		else if (componentInParent.toolType == GRTool.GRToolType.DockWrist)
		{
			gRToolType = componentInParent.toolType;
		}
		num = GetRecycleValue(gRToolType);
		if (reactor != null)
		{
			int count = reactor.vrRigs.Count;
			for (int i = 0; i < count; i++)
			{
				GRPlayer gRPlayer = GRPlayer.Get(reactor.vrRigs[i]);
				if (gRPlayer != null)
				{
					gRPlayer.IncrementSynchronizedSessionStat(GRPlayer.SynchronizedSessionStat.EarnedCredits, num);
				}
			}
		}
		if (!(GRPlayer.Get(componentInParent.gameEntity.lastHeldByActorNumber) == null) && gRToolType != 0)
		{
			reactor.grManager.RequestRecycleItem(componentInParent.gameEntity.lastHeldByActorNumber, componentInParent.gameEntity.id, gRToolType);
		}
	}
}
