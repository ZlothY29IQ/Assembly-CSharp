using System;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(GameEntity))]
[RequireComponent(typeof(XSceneRefTarget))]
[RequireComponent(typeof(Rigidbody))]
public class SpawnableEntityDeposit : MonoBehaviour, IGameEntityComponent
{
	[SerializeField]
	protected GameEntity entity;

	[Tooltip("Only SpawnableEntities with a type starting with this value will be accepted.  If this is empty, any SpawnableEntity will be accepted.")]
	[SerializeField]
	private string acceptedType;

	[SerializeField]
	private ParticleSystem fx;

	[Tooltip("This event will trigger any time the count goes up.")]
	[SerializeField]
	private UnityEvent OnCountIncrement;

	[Tooltip("This event will trigger any time the count changes.  Number passed is the new count.")]
	[SerializeField]
	private UnityEvent<int> OnCountChange;

	protected virtual void Reset()
	{
		entity = GetComponent<GameEntity>();
		entity.pickupable = false;
		fx = GetComponentInChildren<ParticleSystem>();
		if ((bool)fx)
		{
			fx.gameObject.SetActive(value: false);
		}
		Rigidbody component = GetComponent<Rigidbody>();
		component.useGravity = false;
		component.isKinematic = true;
		base.gameObject.layer = LayerMask.NameToLayer("Gorilla Trigger");
	}

	protected virtual void OnEnable()
	{
		RoomSystem.LeftRoomEvent += new Action(OnLeftRoom);
	}

	protected virtual void OnDisable()
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
			SpawnableEntity componentInParent = other.GetComponentInParent<SpawnableEntity>();
			if ((object)componentInParent != null && (string.IsNullOrEmpty(acceptedType) || componentInParent.type.StartsWith(acceptedType)))
			{
				entity.manager.RequestDestroyItem(componentInParent.entity.id);
				entity.RequestState(entity.GetState() + 1);
			}
		}
	}

	public virtual void OnEntityInit()
	{
		OnEntityStateChange(entity.GetState(), entity.GetState());
	}

	public virtual void OnEntityDestroy()
	{
	}

	public virtual void OnEntityStateChange(long prevState, long newState)
	{
		if (newState > prevState && newState > 0)
		{
			if ((bool)fx)
			{
				fx.gameObject.SetActive(value: true);
			}
			OnCountIncrement?.Invoke();
		}
		OnCountChange?.Invoke((int)newState);
	}
}
