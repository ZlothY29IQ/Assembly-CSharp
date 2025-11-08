using Unity.XR.CoreUtils;
using UnityEngine;

public class GRBarrierSpectral : MonoBehaviour, IGameEntityComponent, IGameHittable
{
	public GameEntity entity;

	public MeshRenderer visualMesh;

	public Collider collider;

	public AudioSource audioSource;

	public AudioClip onDamageClip;

	public float onDamageVolume;

	public AudioClip onDestroyedClip;

	public float onDestroyedVolume;

	[SerializeField]
	private GameObject hitFx;

	[SerializeField]
	private GameObject destroyedFx;

	public int maxHealth = 3;

	[ReadOnly]
	public int health = 3;

	private int lastVisualUpdateHealth = -1;

	public void Awake()
	{
		hitFx.SetActive(value: false);
		destroyedFx.SetActive(value: false);
	}

	public void OnEntityInit()
	{
		entity.SetState(health);
		Vector3 localScale = BitPackUtils.UnpackWorldPosFromNetwork(entity.createData);
		base.transform.localScale = localScale;
	}

	public void OnEntityDestroy()
	{
	}

	public void OnEntityStateChange(long prevState, long newState)
	{
		int nextHealth = (int)newState;
		ChangeHealth(nextHealth);
	}

	public void OnImpact(GameHitType hitType)
	{
		if (hitType == GameHitType.Flash)
		{
			int nextHealth = Mathf.Max(health - 1, 0);
			ChangeHealth(nextHealth);
			if (entity.IsAuthority())
			{
				entity.RequestState(entity.id, health);
			}
		}
	}

	private void ChangeHealth(int nextHealth)
	{
		if (health != nextHealth)
		{
			health = nextHealth;
			if (health == 0)
			{
				collider.enabled = false;
				visualMesh.enabled = false;
				audioSource.PlayOneShot(onDestroyedClip, onDestroyedVolume);
				destroyedFx.SetActive(value: false);
				destroyedFx.SetActive(value: true);
			}
			else
			{
				audioSource.PlayOneShot(onDamageClip, onDamageVolume);
				hitFx.SetActive(value: false);
				hitFx.SetActive(value: true);
			}
			RefreshVisuals();
		}
	}

	public bool IsHitValid(GameHitData hit)
	{
		return true;
	}

	public void OnHit(GameHitData hit)
	{
		GameHitType hitTypeId = (GameHitType)hit.hitTypeId;
		if (entity.manager.GetGameComponent<GRTool>(hit.hitByEntityId) != null)
		{
			OnImpact(hitTypeId);
		}
	}

	public void RefreshVisuals()
	{
		if (lastVisualUpdateHealth != health)
		{
			lastVisualUpdateHealth = health;
			Color color = visualMesh.material.GetColor("_BaseColor");
			color.a = (float)health / (float)maxHealth;
			visualMesh.material.SetColor("_BaseColor", color);
		}
	}
}
