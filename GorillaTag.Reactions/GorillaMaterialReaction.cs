using System;
using System.Collections.Generic;
using System.Text;
using GorillaExtensions;
using GorillaNetworking;
using UnityEngine;

namespace GorillaTag.Reactions;

public class GorillaMaterialReaction : MonoBehaviour, ITickSystemPost
{
	[Serializable]
	public struct ReactionEntry
	{
		[Tooltip("If any of these statuses are true then this reaction will be executed.")]
		public int[] statusMaterialIndexes;

		public GameObjectStates[] gameObjectStates;
	}

	[Serializable]
	public struct GameObjectStates
	{
		public GameObject gameObject;

		[MomentInState]
		public MomentInStateActiveOption onEnter;

		[MomentInState]
		public MomentInStateActiveOption onStay;

		[MomentInState]
		public MomentInStateActiveOption onExit;
	}

	[Serializable]
	public struct MomentInStateActiveOption
	{
		public bool change;

		public bool activeState;
	}

	public enum EMomentInState
	{
		OnEnter,
		OnStay,
		OnExit
	}

	public class MomentInStateAttribute : Attribute
	{
	}

	[SerializeField]
	private ReactionEntry[] _statusEffectReactions;

	private int _previousMatIndex;

	private EMomentInState _currentMomentInState;

	private double _currentMatIndexStartTime;

	private double _currentMomentDuration;

	private int _reactionsRemaining;

	private int _momentEnumCount;

	private int _matCount;

	private GameObject[][] _mat_x_moment_x_activeBool_to_gObjs;

	private VRRig _ownerVRRig;

	bool ITickSystemPost.PostTickRunning { get; set; }

	public void PopulateRuntimeLookupArrays()
	{
		_momentEnumCount = ((EMomentInState[])Enum.GetValues(typeof(EMomentInState))).Length;
		_matCount = _ownerVRRig.materialsToChangeTo.Length;
		_mat_x_moment_x_activeBool_to_gObjs = new GameObject[_momentEnumCount * _matCount * 2][];
		for (int i = 0; i < _matCount; i++)
		{
			for (int j = 0; j < _momentEnumCount; j++)
			{
				EMomentInState eMomentInState = (EMomentInState)j;
				List<GameObject> list = new List<GameObject>();
				List<GameObject> list2 = new List<GameObject>();
				ReactionEntry[] statusEffectReactions = _statusEffectReactions;
				for (int k = 0; k < statusEffectReactions.Length; k++)
				{
					ReactionEntry reactionEntry = statusEffectReactions[k];
					int[] statusMaterialIndexes = reactionEntry.statusMaterialIndexes;
					for (int l = 0; l < statusMaterialIndexes.Length; l++)
					{
						if (statusMaterialIndexes[l] != i)
						{
							continue;
						}
						GameObjectStates[] gameObjectStates = reactionEntry.gameObjectStates;
						for (int m = 0; m < gameObjectStates.Length; m++)
						{
							GameObjectStates gameObjectStates2 = gameObjectStates[m];
							switch (eMomentInState)
							{
							case EMomentInState.OnEnter:
								if (gameObjectStates2.onEnter.change)
								{
									if (gameObjectStates2.onEnter.activeState)
									{
										list.Add(base.gameObject);
									}
									else
									{
										list2.Add(base.gameObject);
									}
								}
								break;
							case EMomentInState.OnStay:
								if (gameObjectStates2.onStay.change)
								{
									if (gameObjectStates2.onEnter.activeState)
									{
										list.Add(base.gameObject);
									}
									else
									{
										list2.Add(base.gameObject);
									}
								}
								break;
							case EMomentInState.OnExit:
								if (gameObjectStates2.onExit.change)
								{
									if (gameObjectStates2.onEnter.activeState)
									{
										list.Add(base.gameObject);
									}
									else
									{
										list2.Add(base.gameObject);
									}
								}
								break;
							default:
								Debug.LogError(string.Format("Unhandled enum value for {0}: {1}", "EMomentInState", eMomentInState));
								break;
							}
						}
					}
				}
				int num = i * _momentEnumCount * 2 + j * 2;
				_mat_x_moment_x_activeBool_to_gObjs[num] = list2.ToArray();
				_mat_x_moment_x_activeBool_to_gObjs[num + 1] = list.ToArray();
			}
		}
	}

	protected void Awake()
	{
		RemoveAndReportNulls();
		PopulateRuntimeLookupArrays();
	}

	protected void OnEnable()
	{
		if (_ownerVRRig == null)
		{
			_ownerVRRig = GetComponentInParent<VRRig>(includeInactive: true);
		}
		if (_ownerVRRig == null)
		{
			Debug.LogError("GorillaMaterialReaction: Disabling because could not find VRRig! Hierarchy path: " + base.transform.GetPath(), this);
			base.enabled = false;
			return;
		}
		_reactionsRemaining = 0;
		for (int i = 0; i < _statusEffectReactions.Length; i++)
		{
			_reactionsRemaining += _statusEffectReactions[i].gameObjectStates.Length;
		}
		_currentMatIndexStartTime = 0.0;
		TickSystem<object>.AddCallbackTarget(this);
	}

	protected void OnDisable()
	{
		TickSystem<object>.RemoveCallbackTarget(this);
	}

	void ITickSystemPost.PostTick()
	{
		if (!GorillaComputer.hasInstance || _ownerVRRig == null)
		{
			return;
		}
		GorillaComputer instance = GorillaComputer.instance;
		int num = ((!(GorillaGameManager.instance == null)) ? GorillaGameManager.instance.MyMatIndex(_ownerVRRig.creator) : 0);
		if (_previousMatIndex == num && _reactionsRemaining <= 0)
		{
			return;
		}
		double num2 = (double)instance.startupMillis / 1000.0 + Time.realtimeSinceStartupAsDouble;
		bool flag = false;
		if (_currentMomentInState == EMomentInState.OnExit && _previousMatIndex != num)
		{
			_currentMomentInState = EMomentInState.OnEnter;
			flag = true;
			_currentMatIndexStartTime = num2;
			_currentMomentDuration = -1.0;
			GorillaGameManager instance2 = GorillaGameManager.instance;
			if (instance2 != null && instance2 is GorillaTagManager gorillaTagManager)
			{
				_currentMomentDuration = gorillaTagManager.tagCoolDown;
			}
		}
		else if (_currentMomentInState == EMomentInState.OnEnter && _previousMatIndex == num && (_currentMomentDuration < 0.0 || _currentMomentDuration < num2 - _currentMatIndexStartTime))
		{
			_currentMomentInState = EMomentInState.OnStay;
			flag = true;
			_currentMomentDuration = -1.0;
		}
		else if (_currentMomentInState == EMomentInState.OnStay && _previousMatIndex != num)
		{
			_currentMomentInState = EMomentInState.OnExit;
			flag = true;
			_currentMomentDuration = -1.0;
		}
		_previousMatIndex = num;
		if (!flag)
		{
			return;
		}
		for (int i = 0; i < 2; i++)
		{
			GameObject[] array = _mat_x_moment_x_activeBool_to_gObjs[num * _momentEnumCount * 2 + (int)_currentMomentInState * 2 + i];
			for (int j = 0; j < array.Length; j++)
			{
				array[j].SetActive(i == 1);
			}
		}
	}

	private void RemoveAndReportNulls()
	{
		StringBuilder stringBuilder = new StringBuilder(1024);
		if (_statusEffectReactions == null)
		{
			Debug.Log(string.Format("{0}: The array `{1}` is null. ", "GorillaMaterialReaction", _statusEffectReactions) + "(this should never happen)");
			_statusEffectReactions = Array.Empty<ReactionEntry>();
		}
		for (int i = 0; i < _statusEffectReactions.Length; i++)
		{
			GameObjectStates[] gameObjectStates = _statusEffectReactions[i].gameObjectStates;
			if (gameObjectStates == null)
			{
				_statusEffectReactions[i].gameObjectStates = Array.Empty<GameObjectStates>();
				continue;
			}
			int num = 0;
			int[] array = new int[gameObjectStates.Length];
			for (int j = 0; j < gameObjectStates.Length; j++)
			{
				if (gameObjectStates[j].gameObject == null)
				{
					array[num] = j;
					num++;
				}
				else
				{
					array[num] = -1;
				}
			}
			if (num == 0)
			{
				break;
			}
			stringBuilder.Clear();
			stringBuilder.Append("GorillaMaterialReaction");
			stringBuilder.Append(": Removed null references in array `");
			stringBuilder.Append("_statusEffectReactions");
			stringBuilder.Append("[").Append(i).Append("].")
				.Append("gameObjectStates");
			stringBuilder.Append("' at indexes: ");
			stringBuilder.AppendJoin(", ", array);
			stringBuilder.Append(".");
			Debug.LogError(stringBuilder.ToString(), this);
			GameObjectStates[] array2 = new GameObjectStates[gameObjectStates.Length - num];
			int num2 = 0;
			for (int k = 0; k < gameObjectStates.Length; k++)
			{
				if (!(gameObjectStates[k].gameObject == null))
				{
					array2[num2++] = gameObjectStates[k];
				}
			}
			_statusEffectReactions[i].gameObjectStates = array2;
		}
	}
}
