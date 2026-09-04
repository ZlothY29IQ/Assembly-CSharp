using System;
using TMPro;
using UnityEngine;

namespace Voxels;

[RequireComponent(typeof(GameEntity))]
public class VoxelSpawnableDeposit : MonoBehaviour, IGameEntityComponent
{
	[SerializeField]
	private GameEntity entity;

	[SerializeField]
	private TMP_Text text;

	[SerializeField]
	private ParticleSystem fx;

	private void Reset()
	{
		entity = GetComponent<GameEntity>();
		fx = GetComponentInChildren<ParticleSystem>();
		if ((bool)fx)
		{
			fx.gameObject.SetActive(value: false);
		}
	}

	private void Start()
	{
		SetCounter(0);
	}

	private void OnEnable()
	{
		RoomSystem.LeftRoomEvent += new Action(OnLeftRoom);
	}

	private void OnDisable()
	{
		RoomSystem.LeftRoomEvent -= new Action(OnLeftRoom);
	}

	private void OnLeftRoom()
	{
		entity.SetState(0L);
	}

	private void OnTriggerEnter(Collider other)
	{
		if (entity.IsAuthority())
		{
			VoxelSpawnable componentInParent = other.GetComponentInParent<VoxelSpawnable>();
			if ((object)componentInParent != null)
			{
				entity.manager.RequestDestroyItem(componentInParent.entity.id);
				entity.RequestState(entity.GetState() + 1);
			}
		}
	}

	private void SetCounter(int count)
	{
		text.text = count.ToString();
	}

	public void OnEntityInit()
	{
	}

	public void OnEntityDestroy()
	{
	}

	public void OnEntityStateChange(long prevState, long newState)
	{
		SetCounter(Mathf.Max(0, (int)newState));
		if (newState > prevState && newState > 0 && (bool)fx)
		{
			fx.gameObject.SetActive(value: true);
		}
	}
}
