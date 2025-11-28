using System;
using System.Collections.Generic;
using GorillaExtensions;
using UnityEngine;
using UnityEngine.Audio;

public class GameStateFx : MonoBehaviour, IGameStateReceiver, IDelayedExecListener
{
	[Serializable]
	internal class StateReaction
	{
		[Flags]
		public enum EOptions
		{
			Delay = 1,
			Sound = 2,
			GameObjects = 4,
			Behaviours = 8,
			Renderers = 0x10,
			Materials = 0x20
		}

		[Tooltip("Options for what this reaction should do.")]
		public EOptions options;

		public float delay;

		public SoundEntry soundInfo;

		public GameObjectInfo[] gameObjectInfos;

		public BehaviourInfo[] behaviourInfos;

		public RenderInfo[] renderers;

		public MaterialInfo[] materialInfos;
	}

	[Serializable]
	public struct SoundEntry
	{
		[Flags]
		public enum EOptions
		{
			Source = 1,
			Sound = 2,
			Volume = 4,
			Pitch = 8
		}

		public EOptions options;

		public AudioSource source;

		public AudioResource sound;

		public float volume;

		public float pitch;
	}

	[Serializable]
	internal struct GameObjectInfo
	{
		public bool activate;

		public GameObject gameObject;
	}

	[Serializable]
	internal struct BehaviourInfo
	{
		public bool enable;

		public Behaviour behaviour;
	}

	[Serializable]
	internal struct RenderInfo
	{
		public bool enable;

		public Renderer renderer;
	}

	[Serializable]
	internal struct MaterialInfo
	{
		[Serializable]
		internal struct Entry
		{
			public GTRendererMatSlot slotInfo;

			public Material material;
		}

		public Entry[] entries;
	}

	private const string preLog = "[GT/GameStateFx]  ";

	private const string preErr = "[GT/GameStateFx]  ERROR!!!  ";

	private bool _isValid;

	[SerializeField]
	private MonoBehaviour m_stateProvider;

	private IGameStateProvider _stateProvider;

	[SerializeField]
	private AudioSource m_defaultAudioSource;

	private bool _hasDefaultAudioSource;

	[SerializeField]
	private GTEnumValueMap<StateReaction[]> m_stateMap;

	private int _delayedExecContextFrameNum;

	private Queue<StateReaction> _reactionQueue = new Queue<StateReaction>(4);

	private static readonly List<Material> _g_materialsCache = new List<Material>(8);

	protected void Awake()
	{
		if (!(m_stateProvider is IGameStateProvider stateProvider))
		{
			GTDev.LogError("[GT/GameStateFx]  ERROR!!!  Awake: The supplied State Provider is not type `IGameStateProvider`. Path=" + base.transform.GetPathQ());
			_isValid = false;
			base.enabled = false;
			return;
		}
		_stateProvider = stateProvider;
		if (!_IsAllValid())
		{
			return;
		}
		foreach (StateReaction[] value in m_stateMap.Values)
		{
			if (value != null)
			{
				Array.Sort(value, _DelaySortCompare);
			}
		}
	}

	private static int _DelaySortCompare(StateReaction a, StateReaction b)
	{
		return a.delay.CompareTo(b.delay);
	}

	protected void OnEnable()
	{
		if (!_isValid || ApplicationQuittingState.IsQuitting)
		{
			base.enabled = false;
		}
		else
		{
			_stateProvider.GameStateReceiverRegister(this);
		}
	}

	protected void OnDisable()
	{
		if (_isValid && !ApplicationQuittingState.IsQuitting)
		{
			_stateProvider.GameStateReceiverUnregister(this);
		}
	}

	void IGameStateReceiver.GameStateReceiverOnStateChanged(long oldState, long newState)
	{
		if (!m_stateMap.TryGet(newState, out var o))
		{
			return;
		}
		_delayedExecContextFrameNum = Time.frameCount;
		_reactionQueue.Clear();
		StateReaction[] array = o;
		foreach (StateReaction stateReaction in array)
		{
			if ((stateReaction.options & StateReaction.EOptions.Delay) != 0)
			{
				_reactionQueue.Enqueue(stateReaction);
				GTDelayedExec.Add(this, stateReaction.delay, Time.frameCount);
			}
			else
			{
				_PerformReactions(stateReaction);
			}
		}
	}

	void IDelayedExecListener.OnDelayedAction(int contextFrameNum)
	{
		if (contextFrameNum == _delayedExecContextFrameNum && base.isActiveAndEnabled)
		{
			_PerformReactions(_reactionQueue.Dequeue());
		}
	}

	private static void _PerformReactions(StateReaction reaction)
	{
		if ((reaction.options & StateReaction.EOptions.Sound) != 0)
		{
			if ((reaction.soundInfo.options & SoundEntry.EOptions.Sound) != 0)
			{
				reaction.soundInfo.source.resource = reaction.soundInfo.sound;
			}
			if ((reaction.soundInfo.options & SoundEntry.EOptions.Volume) != 0)
			{
				reaction.soundInfo.source.volume = reaction.soundInfo.volume;
			}
			if ((reaction.soundInfo.options & SoundEntry.EOptions.Pitch) != 0)
			{
				reaction.soundInfo.source.pitch = reaction.soundInfo.pitch;
			}
			reaction.soundInfo.source.GTPlay();
		}
		if ((reaction.options & StateReaction.EOptions.GameObjects) != 0)
		{
			GameObjectInfo[] gameObjectInfos = reaction.gameObjectInfos;
			for (int i = 0; i < gameObjectInfos.Length; i++)
			{
				GameObjectInfo gameObjectInfo = gameObjectInfos[i];
				gameObjectInfo.gameObject.SetActive(gameObjectInfo.activate);
			}
		}
		if ((reaction.options & StateReaction.EOptions.Behaviours) != 0)
		{
			BehaviourInfo[] behaviourInfos = reaction.behaviourInfos;
			for (int i = 0; i < behaviourInfos.Length; i++)
			{
				BehaviourInfo behaviourInfo = behaviourInfos[i];
				behaviourInfo.behaviour.enabled = behaviourInfo.enable;
			}
		}
		if ((reaction.options & StateReaction.EOptions.Renderers) != 0)
		{
			RenderInfo[] renderers = reaction.renderers;
			for (int i = 0; i < renderers.Length; i++)
			{
				RenderInfo renderInfo = renderers[i];
				renderInfo.renderer.enabled = renderInfo.enable;
			}
		}
		if ((reaction.options & StateReaction.EOptions.Materials) == 0)
		{
			return;
		}
		MaterialInfo[] materialInfos = reaction.materialInfos;
		for (int i = 0; i < materialInfos.Length; i++)
		{
			MaterialInfo.Entry[] entries = materialInfos[i].entries;
			for (int j = 0; j < entries.Length; j++)
			{
				MaterialInfo.Entry entry = entries[j];
				entry.slotInfo.renderer.GetSharedMaterials(_g_materialsCache);
				if (entry.slotInfo.slot >= 0 && entry.slotInfo.slot < _g_materialsCache.Count)
				{
					_g_materialsCache[entry.slotInfo.slot] = entry.material;
					entry.slotInfo.renderer.SetSharedMaterials(_g_materialsCache);
				}
			}
		}
	}

	private bool _IsAllValid()
	{
		_isValid = true;
		bool flag = false;
		_hasDefaultAudioSource = m_defaultAudioSource != null;
		foreach (StateReaction[] value in m_stateMap.Values)
		{
			foreach (StateReaction stateReaction in value)
			{
				if ((stateReaction.options & StateReaction.EOptions.Sound) != 0)
				{
					if ((stateReaction.soundInfo.options & SoundEntry.EOptions.Source) != 0)
					{
						if (!_IsOneValid(stateReaction.soundInfo.source != null, "an AudioSource is unassigned."))
						{
							return false;
						}
					}
					else
					{
						flag = true;
						stateReaction.soundInfo.source = m_defaultAudioSource;
					}
					if (!_IsOneValid(stateReaction.soundInfo.sound != null, "A sound is unassigned."))
					{
						return false;
					}
				}
				if ((stateReaction.options & StateReaction.EOptions.GameObjects) != 0)
				{
					GameObjectInfo[] gameObjectInfos = stateReaction.gameObjectInfos;
					for (int j = 0; j < gameObjectInfos.Length; j++)
					{
						GameObjectInfo gameObjectInfo = gameObjectInfos[j];
						if (!_IsOneValid(gameObjectInfo.gameObject != null, "A GameObject is unassigned."))
						{
							return false;
						}
					}
				}
				if ((stateReaction.options & StateReaction.EOptions.Behaviours) != 0)
				{
					BehaviourInfo[] behaviourInfos = stateReaction.behaviourInfos;
					for (int j = 0; j < behaviourInfos.Length; j++)
					{
						BehaviourInfo behaviourInfo = behaviourInfos[j];
						if (!_IsOneValid(behaviourInfo.behaviour != null, "A Behaviour is unassigned."))
						{
							return false;
						}
					}
				}
				if ((stateReaction.options & StateReaction.EOptions.Renderers) != 0)
				{
					RenderInfo[] renderers = stateReaction.renderers;
					for (int j = 0; j < renderers.Length; j++)
					{
						RenderInfo renderInfo = renderers[j];
						if (!_IsOneValid(renderInfo.renderer != null, "A Renderer is unassigned."))
						{
							return false;
						}
					}
				}
				if ((stateReaction.options & StateReaction.EOptions.Materials) == 0)
				{
					continue;
				}
				MaterialInfo[] materialInfos = stateReaction.materialInfos;
				for (int j = 0; j < materialInfos.Length; j++)
				{
					MaterialInfo.Entry[] entries = materialInfos[j].entries;
					for (int k = 0; k < entries.Length; k++)
					{
						MaterialInfo.Entry entry = entries[k];
						if (!_IsOneValid(entry.slotInfo.renderer != null, "A mat swap Renderer is unassigned"))
						{
							return false;
						}
					}
				}
			}
		}
		if (flag && !_hasDefaultAudioSource)
		{
			base.enabled = false;
			_isValid = false;
			return false;
		}
		return true;
	}

	private bool _IsOneValid(bool isValidCondition, string msgFailReason)
	{
		if (isValidCondition)
		{
			return true;
		}
		_isValid = false;
		base.enabled = false;
		return false;
	}
}
