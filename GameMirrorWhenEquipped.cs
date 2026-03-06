using System;
using UnityEngine;

public class GameMirrorWhenEquipped : MonoBehaviour
{
	[SerializeField]
	private GameEntity m_gameEntity;

	[SerializeField]
	private Transform[] m_xformsToMirror;

	[SerializeField]
	private bool m_shouldOnlyMirrorWhenSnapped = true;

	[Tooltip("Set the X axis scale to -1 if the gadget is attached (held or snapped) to the selected side.")]
	[SerializeField]
	private EHandedness m_handednessToMirror = EHandedness.Right;

	private void Awake()
	{
		if (m_gameEntity == null)
		{
			m_gameEntity = GetComponent<GameEntity>();
		}
		if (m_xformsToMirror == null)
		{
			m_xformsToMirror = Array.Empty<Transform>();
		}
	}

	protected void OnEnable()
	{
		GameEntity gameEntity = m_gameEntity;
		gameEntity.OnGrabbed = (Action)Delegate.Combine(gameEntity.OnGrabbed, new Action(_HandleGameEntityOnEquipChanged));
		GameEntity gameEntity2 = m_gameEntity;
		gameEntity2.OnSnapped = (Action)Delegate.Combine(gameEntity2.OnSnapped, new Action(_HandleGameEntityOnEquipChanged));
		GameEntity gameEntity3 = m_gameEntity;
		gameEntity3.OnReleased = (Action)Delegate.Combine(gameEntity3.OnReleased, new Action(_HandleGameEntityOnEquipChanged));
		GameEntity gameEntity4 = m_gameEntity;
		gameEntity4.OnUnsnapped = (Action)Delegate.Combine(gameEntity4.OnUnsnapped, new Action(_HandleGameEntityOnEquipChanged));
	}

	protected void OnDisable()
	{
		GameEntity gameEntity = m_gameEntity;
		gameEntity.OnGrabbed = (Action)Delegate.Remove(gameEntity.OnGrabbed, new Action(_HandleGameEntityOnEquipChanged));
		GameEntity gameEntity2 = m_gameEntity;
		gameEntity2.OnSnapped = (Action)Delegate.Remove(gameEntity2.OnSnapped, new Action(_HandleGameEntityOnEquipChanged));
		GameEntity gameEntity3 = m_gameEntity;
		gameEntity3.OnReleased = (Action)Delegate.Remove(gameEntity3.OnReleased, new Action(_HandleGameEntityOnEquipChanged));
		GameEntity gameEntity4 = m_gameEntity;
		gameEntity4.OnUnsnapped = (Action)Delegate.Remove(gameEntity4.OnUnsnapped, new Action(_HandleGameEntityOnEquipChanged));
	}

	private void _HandleGameEntityOnEquipChanged()
	{
		if (!m_shouldOnlyMirrorWhenSnapped || m_gameEntity.snappedJoint != SnapJointType.None)
		{
			Vector3 localScale = ((m_gameEntity.EquippedHandedness == m_handednessToMirror) ? new Vector3(-1f, 1f, 1f) : Vector3.one);
			for (int i = 0; i < m_xformsToMirror.Length; i++)
			{
				m_xformsToMirror[i].localScale = localScale;
			}
		}
	}
}
