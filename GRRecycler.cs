using System;
using GorillaTagScripts.GhostReactor;
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

	public void ScanItem(GameEntityId id)
	{
		scanner.ScanItem(id);
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
		if (reactor == null)
		{
			Debug.LogFormat("GRRecycler reactor is null?");
			return;
		}
		if (!reactor.grManager.IsAuthority())
		{
			Debug.LogFormat("GRRecycler is not authority.");
			return;
		}
		int num = 0;
		GRTool componentInParent = other.gameObject.GetComponentInParent<GRTool>();
		if (componentInParent == null)
		{
			Debug.LogFormat("GRRecycler Colliding Object is not a GRTool.");
			return;
		}
		GRTool.GRToolType toolType = other.gameObject.GetToolType();
		num = GetRecycleValue(toolType);
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
		Debug.LogFormat("GRRecycler Recycle Value is {0}", num);
		if (GRPlayer.Get(componentInParent.gameEntity.lastHeldByActorNumber) == null)
		{
			Debug.LogFormat("GRRecycler Tool Not last held by a player (?), can't recycle.");
			return;
		}
		Debug.LogFormat("GRRecycler Refunding player {0} {1} Currency and Destroying Tool.", componentInParent.gameEntity.lastHeldByActorNumber, num);
		if (toolType != GRTool.GRToolType.None)
		{
			reactor.grManager.RequestRecycleItem(componentInParent.gameEntity.lastHeldByActorNumber, componentInParent.gameEntity.id, toolType);
		}
	}
}
