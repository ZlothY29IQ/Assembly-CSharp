using System;
using UnityEngine;

public class GTAnimToStaticSwapper : MonoBehaviour, IGorillaSliceableSimple
{
	[Serializable]
	public struct AnimToStaticMeshSwapInfo
	{
		public AnimationClip animClip;

		[Tooltip("These will be inactive while animation is playing and active when it completes.")]
		public GameObject[] staticGameObjects;
	}

	private const string preLog = "[GT/GTAnimToStaticSwapper]  ";

	private const string preErr = "[GT/GTAnimToStaticSwapper]  ERROR!!!  ";

	private const string preErrBeta = "[GT/GTAnimToStaticSwapper]  ERROR!!!  (beta only log)  ";

	[SerializeField]
	private Animation m_animationComponent;

	[Tooltip("these will be active when the animation is playing and deactivated when the animation completes.")]
	[SerializeField]
	private GameObject[] m_animatedGameObjects;

	[Tooltip("When playing stops, these GameObjects will be activated by default if a matching clip cannot be found when playing stops.")]
	[SerializeField]
	private GameObject[] m_defaultStaticGameObjects;

	[SerializeField]
	private AnimToStaticMeshSwapInfo[] m_animsToGameObjectsSwapInfo;

	private int lastStoppedClipId;

	private int _swapIndexOrState = int.MinValue;

	private const int _k_defaultStaticGameObjs = -1;

	private const int _k_state_animating = -2;

	protected void Awake()
	{
		if (m_animationComponent == null)
		{
			base.enabled = false;
			return;
		}
		if (m_animatedGameObjects == null)
		{
			m_animatedGameObjects = Array.Empty<GameObject>();
		}
		int num = 0;
		for (int i = 0; i < m_animatedGameObjects.Length; i++)
		{
			if (!(m_animatedGameObjects[i] == null))
			{
				m_animatedGameObjects[num] = m_animatedGameObjects[i];
				num++;
			}
		}
		if (num != m_animatedGameObjects.Length)
		{
			Array.Resize(ref m_animatedGameObjects, num);
		}
		SetGameObjectsActive(m_animatedGameObjects, isActive: false);
		SetGameObjectsActive(m_defaultStaticGameObjects, isActive: false);
		for (int j = 0; j < m_animsToGameObjectsSwapInfo.Length; j++)
		{
			SetGameObjectsActive(m_animsToGameObjectsSwapInfo[j].staticGameObjects, isActive: false);
		}
	}

	public void OnEnable()
	{
		((IGorillaSliceableSimple)this).SliceUpdate();
		GorillaSlicerSimpleManager.RegisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.Update);
	}

	public void OnDisable()
	{
		GorillaSlicerSimpleManager.UnregisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.Update);
	}

	void IGorillaSliceableSimple.SliceUpdate()
	{
		int num;
		if (m_animationComponent.isPlaying)
		{
			num = -2;
		}
		else
		{
			AnimationClip clip = m_animationComponent.clip;
			num = -1;
			if (clip != null)
			{
				for (int i = 0; i < m_animsToGameObjectsSwapInfo.Length; i++)
				{
					if (m_animsToGameObjectsSwapInfo[i].animClip == clip)
					{
						num = i;
						break;
					}
				}
			}
		}
		if (_swapIndexOrState == num)
		{
			return;
		}
		switch (_swapIndexOrState)
		{
		case -2:
			SetGameObjectsActive(m_animatedGameObjects, isActive: false);
			break;
		case -1:
			SetGameObjectsActive(m_defaultStaticGameObjects, isActive: false);
			break;
		default:
			if (_swapIndexOrState >= 0 && _swapIndexOrState < m_animsToGameObjectsSwapInfo.Length)
			{
				SetGameObjectsActive(m_animsToGameObjectsSwapInfo[_swapIndexOrState].staticGameObjects, isActive: false);
			}
			break;
		}
		if (num != -2)
		{
			if (num == -1)
			{
				SetGameObjectsActive(m_defaultStaticGameObjects, isActive: true);
			}
			else if (num >= 0 && num < m_animsToGameObjectsSwapInfo.Length)
			{
				SetGameObjectsActive(m_animsToGameObjectsSwapInfo[num].staticGameObjects, isActive: true);
			}
		}
		else
		{
			SetGameObjectsActive(m_animatedGameObjects, isActive: true);
		}
		_swapIndexOrState = num;
	}

	private void SetGameObjectsActive(GameObject[] gameObjects, bool isActive)
	{
		if (gameObjects == null)
		{
			return;
		}
		foreach (GameObject gameObject in gameObjects)
		{
			if (gameObject != null)
			{
				gameObject.SetActive(isActive);
			}
		}
	}
}
