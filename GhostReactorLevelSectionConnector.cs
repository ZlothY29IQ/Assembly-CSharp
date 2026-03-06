using System.Collections.Generic;
using UnityEngine;

public class GhostReactorLevelSectionConnector : MonoBehaviour
{
	public enum Direction
	{
		Down = -1,
		Forward,
		Up
	}

	public Transform hubAnchor;

	public Transform sectionAnchor;

	public Transform gateSpawnPoint;

	public GameEntity gateEntity;

	public Direction direction;

	public BoxCollider boundingCollider;

	public List<Transform> pathNodes;

	private const float SHOW_DIST = 18f;

	private const float HIDE_DIST = 22f;

	private List<GameEntity> prePlacedGameEntities;

	private List<Renderer> renderers;

	private bool hidden;

	private void Awake()
	{
		prePlacedGameEntities = new List<GameEntity>(128);
		GetComponentsInChildren(prePlacedGameEntities);
		for (int i = 0; i < prePlacedGameEntities.Count; i++)
		{
			prePlacedGameEntities[i].gameObject.SetActive(value: false);
		}
		renderers = new List<Renderer>(512);
		hidden = false;
		GetComponentsInChildren(renderers);
		if (boundingCollider == null)
		{
			Debug.LogWarningFormat("Missing Bounding Collider for section {0}", base.gameObject.name);
		}
	}

	public void Init(GhostReactorManager grManager)
	{
		if (!grManager.IsAuthority())
		{
			return;
		}
		if (gateEntity != null)
		{
			grManager.gameEntityManager.RequestCreateItem(gateEntity.name.GetStaticHash(), gateSpawnPoint.position, gateSpawnPoint.rotation, 0L);
		}
		for (int i = 0; i < prePlacedGameEntities.Count; i++)
		{
			if (!prePlacedGameEntities[i].isBuiltIn)
			{
				int staticHash = prePlacedGameEntities[i].gameObject.name.GetStaticHash();
				if (!grManager.gameEntityManager.FactoryHasEntity(staticHash))
				{
					Debug.LogErrorFormat("Cannot Find Entity in Factory {0} {1}", prePlacedGameEntities[i].gameObject.name, staticHash);
					continue;
				}
				GameEntityCreateData item = new GameEntityCreateData
				{
					entityTypeId = staticHash,
					position = prePlacedGameEntities[i].transform.position,
					rotation = prePlacedGameEntities[i].transform.rotation,
					createData = 0L,
					createdByEntityId = -1,
					slotIndex = -1
				};
				GhostReactorLevelSection.tempCreateEntitiesList.Add(item);
			}
		}
		grManager.gameEntityManager.RequestCreateItems(GhostReactorLevelSection.tempCreateEntitiesList);
		GhostReactorLevelSection.tempCreateEntitiesList.Clear();
	}

	public void Hide(bool hide)
	{
		for (int i = 0; i < renderers.Count; i++)
		{
			if (!(renderers[i] == null))
			{
				renderers[i].enabled = !hide;
			}
		}
	}

	public void UpdateDisable(Vector3 playerPos)
	{
		if (!(boundingCollider == null))
		{
			float sqrMagnitude = (boundingCollider.ClosestPoint(playerPos) - playerPos).sqrMagnitude;
			float num = 324f;
			float num2 = 484f;
			if (hidden && sqrMagnitude < num)
			{
				hidden = false;
				Hide(hide: false);
			}
			else if (!hidden && sqrMagnitude > num2)
			{
				hidden = true;
				Hide(hide: true);
			}
		}
	}
}
