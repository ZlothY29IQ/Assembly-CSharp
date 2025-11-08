using System.Collections.Generic;
using UnityEngine;

public class GRBreakable : MonoBehaviour, IGameHittable
{
	public enum BreakableState
	{
		Unbroken,
		Broken
	}

	public GameEntity gameEntity;

	public List<Transform> enableWhenBroken;

	public List<Transform> disableWhenBroken;

	public Collider breakableCollider;

	public bool holdsRandomItem = true;

	public Transform itemSpawnLocation;

	public GRBreakableItemSpawnConfig itemSpawnProbability;

	public AudioSource audioSource;

	public AudioClip breakSound;

	public float breakSoundVolume;

	private bool brokenLocal;

	public bool BrokenLocal => brokenLocal;

	private void OnEnable()
	{
		gameEntity.OnStateChanged += OnEntityStateChanged;
	}

	private void OnDisable()
	{
		if (gameEntity != null)
		{
			gameEntity.OnStateChanged -= OnEntityStateChanged;
		}
	}

	private void OnEntityStateChanged(long prevState, long nextState)
	{
		switch ((BreakableState)nextState)
		{
		case BreakableState.Broken:
			BreakLocal();
			break;
		case BreakableState.Unbroken:
			RestoreLocal();
			break;
		}
	}

	public void BreakLocal()
	{
		if (!brokenLocal)
		{
			brokenLocal = true;
			if (breakableCollider != null)
			{
				breakableCollider.enabled = false;
			}
			for (int i = 0; i < disableWhenBroken.Count; i++)
			{
				disableWhenBroken[i].gameObject.SetActive(value: false);
			}
			for (int j = 0; j < enableWhenBroken.Count; j++)
			{
				enableWhenBroken[j].gameObject.SetActive(value: true);
			}
			if (audioSource != null)
			{
				audioSource.PlayOneShot(breakSound, breakSoundVolume);
			}
			if (gameEntity.IsAuthority() && holdsRandomItem && itemSpawnProbability.TryForRandomItem(gameEntity, out var entity))
			{
				gameEntity.manager.RequestCreateItem(entity.gameObject.name.GetStaticHash(), itemSpawnLocation.position, itemSpawnLocation.rotation, 0L);
			}
		}
	}

	public void RestoreLocal()
	{
		if (brokenLocal)
		{
			brokenLocal = false;
			if (breakableCollider != null)
			{
				breakableCollider.enabled = true;
			}
			for (int i = 0; i < disableWhenBroken.Count; i++)
			{
				disableWhenBroken[i].gameObject.SetActive(value: true);
			}
			for (int j = 0; j < enableWhenBroken.Count; j++)
			{
				enableWhenBroken[j].gameObject.SetActive(value: false);
			}
		}
	}

	public bool IsHitValid(GameHitData hit)
	{
		if (!brokenLocal)
		{
			return hit.hitTypeId == 0;
		}
		return false;
	}

	public void OnHit(GameHitData hit)
	{
		if (hit.hitTypeId == 0 && (int)this.gameEntity.GetState() != 1)
		{
			this.gameEntity.RequestState(this.gameEntity.id, 1L);
			GameEntity gameEntity = this.gameEntity.manager.GetGameEntity(hit.hitByEntityId);
			if (gameEntity != null && gameEntity.IsHeldByLocalPlayer())
			{
				PlayerGameEvents.MiscEvent("GRSmashBreakable");
			}
		}
	}
}
