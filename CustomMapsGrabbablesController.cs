using System;
using GorillaExtensions;
using GT_CustomMapSupportRuntime;
using UnityEngine;

public class CustomMapsGrabbablesController : MonoBehaviour, IGameEntityComponent
{
	public GameEntity entity;

	public short luaAgentID;

	private bool isGrabbed;

	private Transform returnParent;

	private void Awake()
	{
		isGrabbed = false;
		GameEntity gameEntity = entity;
		gameEntity.OnGrabbed = (Action)Delegate.Combine(gameEntity.OnGrabbed, new Action(OnGrabbed));
		GameEntity gameEntity2 = entity;
		gameEntity2.OnReleased = (Action)Delegate.Combine(gameEntity2.OnReleased, new Action(OnReleased));
	}

	private void OnDestroy()
	{
		GameEntity gameEntity = entity;
		gameEntity.OnGrabbed = (Action)Delegate.Remove(gameEntity.OnGrabbed, new Action(OnGrabbed));
		GameEntity gameEntity2 = entity;
		gameEntity2.OnReleased = (Action)Delegate.Remove(gameEntity2.OnReleased, new Action(OnReleased));
	}

	public void OnEntityInit()
	{
		GTDev.Log("CustomMapsGrabbablesController::OnEntityInit");
		if (MapSpawnManager.instance == null)
		{
			return;
		}
		base.transform.parent = MapSpawnManager.instance.transform;
		GrabbableEntity.UnpackCreateData(entity.createData, out var entityTypeID, out luaAgentID);
		if (!MapSpawnManager.instance.SpawnEntity(entityTypeID, out MapEntity newEnemy))
		{
			GTDev.LogError("CustomMapsGrabbablesController::OnEntityInit could not spawn grabbable");
			UnityEngine.Object.Destroy(base.gameObject);
			return;
		}
		GrabbableEntity grabbableEntity = (GrabbableEntity)newEnemy;
		if (!(grabbableEntity == null))
		{
			grabbableEntity.gameObject.SetActive(value: true);
			grabbableEntity.transform.parent = entity.transform;
			grabbableEntity.transform.localPosition = Vector3.zero;
			grabbableEntity.transform.localRotation = Quaternion.identity;
			returnParent = entity.transform.parent;
			entity.audioSource = grabbableEntity.audioSource;
			entity.catchSound = grabbableEntity.catchSound;
			entity.catchSoundVolume = grabbableEntity.catchSoundVolume;
			entity.throwSound = grabbableEntity.throwSound;
			entity.throwSoundVolume = grabbableEntity.throwSoundVolume;
			Collider[] componentsInChildren = base.gameObject.GetComponentsInChildren<Collider>();
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				componentsInChildren[i].gameObject.layer = LayerMask.NameToLayer("Prop");
			}
		}
	}

	public int GetGrabbingActor()
	{
		if (!isGrabbed)
		{
			return -1;
		}
		return entity.heldByActorNumber;
	}

	public void OnEntityDestroy()
	{
	}

	public void OnEntityStateChange(long prevState, long newState)
	{
	}

	private void OnGrabbed()
	{
		isGrabbed = true;
	}

	private void OnReleased()
	{
		isGrabbed = false;
		if (returnParent.IsNotNull())
		{
			entity.transform.parent = returnParent;
		}
	}
}
