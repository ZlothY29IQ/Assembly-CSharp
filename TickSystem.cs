using System.Collections.Generic;
using System.Runtime.CompilerServices;
using GorillaTag;
using UnityEngine;

[DefaultExecutionOrder(0)]
internal abstract class TickSystem<T> : MonoBehaviour
{
	private abstract class TickCallbackWrapper<U> : ObjectPoolEvents, ICallBack where U : class
	{
		protected U m_target;

		public U target
		{
			get
			{
				return m_target;
			}
			set
			{
				m_target = value;
			}
		}

		public abstract void CallBack();

		public void OnTaken()
		{
		}

		public void OnReturned()
		{
			target = null;
		}
	}

	private class TickCallbackWrapperPre : TickCallbackWrapper<ITickSystemPre>
	{
		public override void CallBack()
		{
			m_target.PreTick();
		}
	}

	private class TickCallbackWrapperTick : TickCallbackWrapper<ITickSystemTick>
	{
		public override void CallBack()
		{
			m_target.Tick();
		}
	}

	private class TickCallbackWrapperPost : TickCallbackWrapper<ITickSystemPost>
	{
		public override void CallBack()
		{
			m_target.PostTick();
		}
	}

	private static readonly ObjectPool<TickCallbackWrapperPre> preTickWrapperPool;

	private static readonly CallbackContainer<TickCallbackWrapperPre> preTickCallbacks;

	private static readonly Dictionary<ITickSystemPre, TickCallbackWrapperPre> preTickWrapperTable;

	private static readonly ObjectPool<TickCallbackWrapperTick> tickWrapperPool;

	private static readonly CallbackContainer<TickCallbackWrapperTick> tickCallbacks;

	private static readonly Dictionary<ITickSystemTick, TickCallbackWrapperTick> tickWrapperTable;

	private static readonly ObjectPool<TickCallbackWrapperPost> postTickWrapperPool;

	private static readonly CallbackContainer<TickCallbackWrapperPost> postTickCallbacks;

	private static readonly Dictionary<ITickSystemPost, TickCallbackWrapperPost> postTickWrapperTable;

	private void Awake()
	{
		base.transform.SetParent(null, worldPositionStays: true);
		Object.DontDestroyOnLoad(this);
	}

	private void Update()
	{
		if (!ApplicationQuittingState.IsQuitting)
		{
			preTickCallbacks.TryRunCallbacks();
			tickCallbacks.TryRunCallbacks();
		}
	}

	private void LateUpdate()
	{
		if (!ApplicationQuittingState.IsQuitting)
		{
			postTickCallbacks.TryRunCallbacks();
		}
	}

	static TickSystem()
	{
		preTickCallbacks = new CallbackContainer<TickCallbackWrapperPre>();
		preTickWrapperTable = new Dictionary<ITickSystemPre, TickCallbackWrapperPre>(100);
		tickCallbacks = new CallbackContainer<TickCallbackWrapperTick>();
		tickWrapperTable = new Dictionary<ITickSystemTick, TickCallbackWrapperTick>(100);
		postTickCallbacks = new CallbackContainer<TickCallbackWrapperPost>();
		postTickWrapperTable = new Dictionary<ITickSystemPost, TickCallbackWrapperPost>(100);
		preTickWrapperPool = new ObjectPool<TickCallbackWrapperPre>(100);
		tickWrapperPool = new ObjectPool<TickCallbackWrapperTick>(100);
		postTickWrapperPool = new ObjectPool<TickCallbackWrapperPost>(100);
	}

	private static void OnEnterPlay()
	{
		preTickCallbacks.Clear();
		preTickWrapperTable.Clear();
		tickCallbacks.Clear();
		tickWrapperTable.Clear();
		postTickCallbacks.Clear();
		postTickWrapperTable.Clear();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void AddPreTickCallback(ITickSystemPre callback)
	{
		if (!callback.PreTickRunning)
		{
			TickCallbackWrapperPre item = preTickWrapperPool.Take();
			item.target = callback;
			preTickWrapperTable[callback] = item;
			preTickCallbacks.Add(in item);
			callback.PreTickRunning = true;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void AddTickCallback(ITickSystemTick callback)
	{
		if (!callback.TickRunning)
		{
			TickCallbackWrapperTick item = tickWrapperPool.Take();
			item.target = callback;
			tickWrapperTable[callback] = item;
			tickCallbacks.Add(in item);
			callback.TickRunning = true;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void AddPostTickCallback(ITickSystemPost callback)
	{
		if (!callback.PostTickRunning)
		{
			TickCallbackWrapperPost item = postTickWrapperPool.Take();
			item.target = callback;
			postTickWrapperTable[callback] = item;
			postTickCallbacks.Add(in item);
			callback.PostTickRunning = true;
		}
	}

	public static void AddTickSystemCallBack(ITickSystem callback)
	{
		AddPreTickCallback(callback);
		AddTickCallback(callback);
		AddPostTickCallback(callback);
	}

	public static void AddCallbackTarget(object target)
	{
		if (target is ITickSystem callback)
		{
			AddTickSystemCallBack(callback);
			return;
		}
		if (target is ITickSystemPre callback2)
		{
			AddPreTickCallback(callback2);
		}
		if (target is ITickSystemTick callback3)
		{
			AddTickCallback(callback3);
		}
		if (target is ITickSystemPost callback4)
		{
			AddPostTickCallback(callback4);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void RemovePreTickCallback(ITickSystemPre callback)
	{
		if (callback.PreTickRunning && preTickWrapperTable.TryGetValue(callback, out var value))
		{
			preTickCallbacks.Remove(in value);
			callback.PreTickRunning = false;
			preTickWrapperPool.Return(value);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void RemoveTickCallback(ITickSystemTick callback)
	{
		if (callback.TickRunning && tickWrapperTable.TryGetValue(callback, out var value))
		{
			tickCallbacks.Remove(in value);
			callback.TickRunning = false;
			tickWrapperPool.Return(value);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void RemovePostTickCallback(ITickSystemPost callback)
	{
		if (callback.PostTickRunning && postTickWrapperTable.TryGetValue(callback, out var value))
		{
			postTickCallbacks.Remove(in value);
			callback.PostTickRunning = false;
			postTickWrapperPool.Return(value);
		}
	}

	public static void RemoveTickSystemCallback(ITickSystem callback)
	{
		RemovePreTickCallback(callback);
		RemoveTickCallback(callback);
		RemovePostTickCallback(callback);
	}

	public static void RemoveCallbackTarget(object target)
	{
		if (target is ITickSystem callback)
		{
			RemoveTickSystemCallback(callback);
			return;
		}
		if (target is ITickSystemPre callback2)
		{
			RemovePreTickCallback(callback2);
		}
		if (target is ITickSystemTick callback3)
		{
			RemoveTickCallback(callback3);
		}
		if (target is ITickSystemPost callback4)
		{
			RemovePostTickCallback(callback4);
		}
	}
}
internal class TickSystem : TickSystem<object>
{
}
