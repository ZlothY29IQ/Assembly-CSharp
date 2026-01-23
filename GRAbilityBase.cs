using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.AI;

public class GRAbilityBase
{
	protected GameAgent agent;

	protected GameEntity entity;

	protected Animation anim;

	protected Animator animator;

	protected Transform root;

	protected Transform head;

	protected AudioSource audioSource;

	protected GRSenseLineOfSight lineOfSight;

	protected Rigidbody rb;

	protected GRAttributes attributes;

	[ReadOnly]
	public double startTime;

	[ReadOnly]
	public double stopTime;

	protected int walkableArea = -1;

	protected virtual void OnStart()
	{
	}

	protected virtual void OnStop()
	{
	}

	protected virtual void OnThink(float dt)
	{
	}

	protected virtual void OnUpdateShared(float dt)
	{
	}

	protected virtual void OnUpdateRemote(float dt)
	{
	}

	protected virtual void OnUpdateAuthority(float dt)
	{
	}

	public virtual bool IsCoolDownOver()
	{
		return true;
	}

	public virtual void Setup(GameAgent agent, Animation anim, AudioSource audioSource, Transform root, Transform head, GRSenseLineOfSight lineOfSight)
	{
		this.root = root;
		this.anim = anim;
		if (anim == null)
		{
			animator = null;
		}
		this.agent = agent;
		this.head = head;
		this.audioSource = audioSource;
		this.lineOfSight = lineOfSight;
		rb = agent.GetComponent<Rigidbody>();
		entity = agent.GetComponent<GameEntity>();
		attributes = agent.GetComponent<GRAttributes>();
		walkableArea = NavMesh.GetAreaFromName("walkable");
	}

	public void Start()
	{
		startTime = Time.timeAsDouble;
		OnStart();
	}

	public void Stop()
	{
		stopTime = Time.timeAsDouble;
		OnStop();
	}

	public float GetAbilityTime(double currTime)
	{
		return (float)(currTime - startTime);
	}

	public virtual bool IsDone()
	{
		return false;
	}

	public void Think(float dt)
	{
		OnThink(dt);
	}

	public void UpdateAuthority(float dt)
	{
		OnUpdateShared(dt);
		OnUpdateAuthority(dt);
	}

	public void UpdateRemote(float dt)
	{
		OnUpdateShared(dt);
		OnUpdateRemote(dt);
	}

	protected virtual void PlayAnim(string animName, float blendTime, float speed)
	{
		if (anim != null && !string.IsNullOrEmpty(animName))
		{
			if (anim.GetClip(animName) == null)
			{
				Debug.LogErrorFormat("Anim Clip {0} does not exist in (1)", animName, anim);
			}
			else
			{
				anim[animName].speed = speed;
				anim.CrossFade(animName, blendTime);
			}
		}
	}

	public bool IsCoolDownOver(float coolDown)
	{
		return (float)(Time.timeAsDouble - stopTime) > coolDown;
	}

	public virtual float GetRange()
	{
		return 0f;
	}
}
