using System;
using System.Collections.Generic;
using UnityEngine;

public class GTAnimator : MonoBehaviour, IDelayedExecListener
{
	[Serializable]
	public struct AnimClipAndGObjs
	{
		public AnimationClip animClip;

		public SoundBankPlayer soundBankToPlayOnStart;

		[Tooltip("These GameObjects will be activated when the animation clip finishes playing.")]
		public GameObject[] endStaticGameObjects;
	}

	private const string preLog = "[GTAnimator]  ";

	private const string preErr = "[GTAnimator]  ERROR!!!  ";

	private const string preErrBeta = "[GTAnimator]  ERROR!!!  (beta only log)  ";

	[Tooltip("Assign a unity Animation component (not to be confused with less performant Animator Component).")]
	[SerializeField]
	private Animation m_animationComponent;

	[Tooltip("These will be activated when animation starts playing and deactivated when any anim finishes playing.")]
	[SerializeField]
	private GameObject[] m_animatedGameObjects;

	[Tooltip("If an enum map value is not defined then these will be activated.")]
	[SerializeField]
	private GameObject[] m_defaultStaticGameObjects;

	[Header("Enum To Animation Mapping")]
	[Tooltip("Map an enum's values to specific AnimationClips.")]
	[SerializeField]
	internal GTEnumValueMap<AnimClipAndGObjs> m_animationMap;

	private readonly HashSet<GameObject> _allStaticGobjs = new HashSet<GameObject>();

	private const long _k_invalidState = long.MinValue;

	private long _currentStateAsLong = long.MinValue;

	private int _frameCountWhenLastPlayed;

	private bool _wasInitCalled;

	private long _queuedStateAsLong = long.MinValue;

	public Animation animationComponent => m_animationComponent;

	public bool hasAnimationComponent { get; private set; }

	public bool IsPlaying => m_animationComponent.isPlaying;

	protected void Awake()
	{
		Init();
	}

	public void Init()
	{
		if (_wasInitCalled)
		{
			return;
		}
		_wasInitCalled = true;
		hasAnimationComponent = m_animationComponent != null;
		_ = hasAnimationComponent;
		m_animationMap.Init();
		foreach (AnimClipAndGObjs value in m_animationMap.Values)
		{
			_allStaticGobjs.UnionWith(value.endStaticGameObjects);
		}
	}

	public void OnEnable()
	{
	}

	public void SetState(long enumValueAsLong)
	{
		if (!_wasInitCalled)
		{
			Init();
		}
		if (_currentStateAsLong != enumValueAsLong)
		{
			TryPlay(enumValueAsLong);
		}
	}

	public bool TryPlay(long enumValueAsLong)
	{
		if (!hasAnimationComponent || !m_animationMap.TryGet(enumValueAsLong, out var o))
		{
			return false;
		}
		foreach (GameObject allStaticGobj in _allStaticGobjs)
		{
			allStaticGobj.SetActive(value: false);
		}
		GameObject[] animatedGameObjects = m_animatedGameObjects;
		for (int i = 0; i < animatedGameObjects.Length; i++)
		{
			animatedGameObjects[i].SetActive(value: true);
		}
		_currentStateAsLong = enumValueAsLong;
		m_animationComponent.clip = o.animClip;
		m_animationComponent.Play();
		if ((bool)o.soundBankToPlayOnStart)
		{
			o.soundBankToPlayOnStart.Play();
		}
		if (!o.animClip.isLooping)
		{
			_frameCountWhenLastPlayed = Time.frameCount;
			GTDelayedExec.Add(this, o.animClip.length, _frameCountWhenLastPlayed);
		}
		return true;
	}

	void IDelayedExecListener.OnDelayedAction(int contextId)
	{
		if (!base.enabled || _frameCountWhenLastPlayed != contextId)
		{
			return;
		}
		m_animationComponent.Stop();
		for (int i = 0; i < m_animatedGameObjects.Length; i++)
		{
			if (m_animatedGameObjects[i] != null)
			{
				m_animatedGameObjects[i].SetActive(value: false);
			}
		}
		AnimClipAndGObjs o;
		GameObject[] array = ((!m_animationMap.TryGet(_currentStateAsLong, out o) || o.endStaticGameObjects == null || o.endStaticGameObjects.Length == 0) ? m_defaultStaticGameObjects : o.endStaticGameObjects);
		if (array != null)
		{
			for (int j = 0; j < array.Length; j++)
			{
				if (array[j] != null)
				{
					array[j].SetActive(value: true);
				}
			}
		}
		if (_queuedStateAsLong != long.MinValue)
		{
			long queuedStateAsLong = _queuedStateAsLong;
			_queuedStateAsLong = long.MinValue;
			TryPlay(queuedStateAsLong);
		}
	}

	public void Stop()
	{
		if (m_animationComponent != null)
		{
			m_animationComponent.Stop();
		}
	}

	public void QueueState(long enumValueAsLong)
	{
		if (!_wasInitCalled)
		{
			Init();
		}
		if (_queuedStateAsLong != enumValueAsLong && _currentStateAsLong != enumValueAsLong)
		{
			if (!IsPlaying || _IsCurrentClipLoopable())
			{
				TryPlay(enumValueAsLong);
			}
			else
			{
				_queuedStateAsLong = enumValueAsLong;
			}
		}
	}

	private bool _IsCurrentClipLoopable()
	{
		if (m_animationComponent == null)
		{
			return false;
		}
		AnimationClip clip = m_animationComponent.clip;
		if (clip == null)
		{
			return false;
		}
		WrapMode wrapMode = clip.wrapMode;
		return wrapMode == WrapMode.Loop || wrapMode == WrapMode.PingPong;
	}
}
