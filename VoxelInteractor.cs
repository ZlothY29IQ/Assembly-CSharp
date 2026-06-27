using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Voxels;

public class VoxelInteractor : MonoBehaviour
{
	[SerializeField]
	private LayerMask layerMask = 1;

	[SerializeField]
	private float rayLength = 0.1f;

	[SerializeField]
	private float cooldown = 0.25f;

	[SerializeField]
	private VoxelAction action = new VoxelAction
	{
		strength = 0.5f,
		radius = 0.5f,
		operation = OperationType.Subtract
	};

	private bool _active;

	private Coroutine _actionRoutine;

	private float _nextActionTime;

	[OnEnterPlay_SetNull]
	private static List<VoxelWorld> _hitWorlds;

	[OnEnterPlay_SetNull]
	private static Collider[] _hitColliders;

	private void OnDisable()
	{
		StopOngoingAction();
	}

	public void StartOngoingAction()
	{
		if (!_active)
		{
			if (_actionRoutine != null)
			{
				StopCoroutine(_actionRoutine);
			}
			_active = true;
			_actionRoutine = StartCoroutine(DoContinuousAction());
		}
	}

	public void StopOngoingAction()
	{
		if (_actionRoutine != null)
		{
			StopCoroutine(_actionRoutine);
			_actionRoutine = null;
		}
		_active = false;
	}

	public void PerformAction()
	{
		if (Time.time < _nextActionTime)
		{
			return;
		}
		if (Physics.Linecast(base.transform.position, base.transform.position + base.transform.forward * rayLength, out var hitInfo, layerMask, QueryTriggerInteraction.Ignore))
		{
			ChunkComponent component = hitInfo.collider.GetComponent<ChunkComponent>();
			if ((bool)component)
			{
				component.World.Mine(hitInfo, action);
			}
		}
		_nextActionTime = Time.time + cooldown;
	}

	public void PerformActionOmnidirectional()
	{
		if (Time.time < _nextActionTime)
		{
			return;
		}
		if (_hitWorlds == null)
		{
			_hitWorlds = new List<VoxelWorld>();
		}
		if (_hitColliders == null)
		{
			_hitColliders = new Collider[5];
		}
		_hitWorlds.Clear();
		int num = Physics.OverlapSphereNonAlloc(base.transform.position, action.radius, _hitColliders, layerMask);
		if (num == _hitColliders.Length)
		{
			Array.Resize(ref _hitColliders, _hitColliders.Length * 2);
		}
		for (int i = 0; i < num; i++)
		{
			if (_hitColliders[i].TryGetComponent<ChunkComponent>(out var component) && !_hitWorlds.Contains(component.World))
			{
				_hitWorlds.Add(component.World);
				component.World.PerformAction(base.transform.position, action);
			}
		}
		_nextActionTime = Time.time + cooldown;
	}

	private IEnumerator DoContinuousAction()
	{
		while (_active)
		{
			while (Time.time < _nextActionTime)
			{
				yield return null;
			}
			if (_active)
			{
				PerformAction();
			}
		}
		_actionRoutine = null;
	}

	public bool ApplyVoxelAction(Collision collision)
	{
		ChunkComponent component = collision.gameObject.GetComponent<ChunkComponent>();
		if ((bool)component)
		{
			component.World.Mine(collision, action);
		}
		return component;
	}

	public bool ApplyVoxelAction(RaycastHit hit)
	{
		ChunkComponent component = hit.collider.GetComponent<ChunkComponent>();
		if ((bool)component)
		{
			component.World.Mine(hit, action);
		}
		return component;
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.green;
		Vector3 vector = base.transform.position + base.transform.forward * rayLength;
		Gizmos.DrawLine(base.transform.position, vector);
		Gizmos.DrawWireSphere(base.transform.position, 0.01f);
		Gizmos.DrawWireSphere(vector, 0.01f);
	}
}
