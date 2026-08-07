using UnityEngine;

public abstract class MonoBehaviourPostTick : MonoBehaviour, ITickSystemPost
{
	public bool PostTickRunning { get; set; }

	public virtual void OnEnable()
	{
		TickSystem<object>.AddPostTickCallback(this);
	}

	public virtual void OnDisable()
	{
		TickSystem<object>.RemovePostTickCallback(this);
	}

	public abstract void PostTick();
}
