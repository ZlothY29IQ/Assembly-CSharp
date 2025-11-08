using System;
using System.Runtime.CompilerServices;

namespace GorillaTag;

[Serializable]
internal class TickSystemTimer : TickSystemTimerAbstract
{
	public Action callback;

	public TickSystemTimer()
	{
	}

	public TickSystemTimer(float cd)
		: base(cd)
	{
	}

	public TickSystemTimer(float cd, Action cb)
		: base(cd)
	{
		callback = cb;
	}

	public TickSystemTimer(Action cb)
	{
		callback = cb;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override void OnTimedEvent()
	{
		callback?.Invoke();
	}
}
