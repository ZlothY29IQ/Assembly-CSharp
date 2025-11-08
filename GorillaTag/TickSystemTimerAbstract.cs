using System;
using System.Runtime.CompilerServices;

namespace GorillaTag;

[Serializable]
internal abstract class TickSystemTimerAbstract : CoolDownHelper, ITickSystemPre
{
	[NonSerialized]
	internal bool registered;

	bool ITickSystemPre.PreTickRunning
	{
		get
		{
			return registered;
		}
		set
		{
			registered = value;
		}
	}

	public bool Running => registered;

	protected TickSystemTimerAbstract()
	{
	}

	protected TickSystemTimerAbstract(float cd)
		: base(cd)
	{
	}

	public override void Start()
	{
		base.Start();
		TickSystem<object>.AddPreTickCallback(this);
	}

	public override void Stop()
	{
		base.Stop();
		TickSystem<object>.RemovePreTickCallback(this);
	}

	public override void OnCheckPass()
	{
		OnTimedEvent();
	}

	public abstract void OnTimedEvent();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	void ITickSystemPre.PreTick()
	{
		CheckCooldown();
	}
}
