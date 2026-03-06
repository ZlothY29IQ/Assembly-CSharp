using System.Collections.Generic;
using GorillaLocomotion;
using Photon.Pun;
using UnityEngine;

public class GameHitter : MonoBehaviour, IGameEntityComponent
{
	public GameEntity gameEntity;

	public GameHitType hitType;

	public GRAttributeType damageAttribute = GRAttributeType.BatonDamage;

	public GRAttributeType flashDamageAttribute = GRAttributeType.FlashDamage;

	public GRAttributeType shieldDamageAttribute = GRAttributeType.BatonDamage;

	public float minSwingSpeed = 1.5f;

	public GameHitFx hitFx;

	private GRAttributes attributes;

	public float knockbackMultiplier = 1f;

	public float maxImpulseSpeed = 4.5f;

	private List<IGameHitter> components;

	private double hitCooldownEnd;

	public bool hitOnCollision = true;

	private void Awake()
	{
		components = new List<IGameHitter>(1);
		GetComponentsInChildren(components);
		attributes = GetComponent<GRAttributes>();
	}

	public void OnEntityInit()
	{
		GRTool component = GetComponent<GRTool>();
		if (component != null)
		{
			component.onToolUpgraded += OnToolUpgraded;
			OnToolUpgraded(component);
		}
	}

	public void OnEntityDestroy()
	{
	}

	public void OnEntityStateChange(long prevState, long nextState)
	{
	}

	private void OnToolUpgraded(GRTool tool)
	{
		if (attributes.HasValueForAttribute(GRAttributeType.KnockbackMultiplier))
		{
			knockbackMultiplier = attributes.CalculateFinalFloatValueForAttribute(GRAttributeType.KnockbackMultiplier);
		}
	}

	public void ApplyHit(GameHitData hitData)
	{
		if (hitFx.hitSound != null)
		{
			hitFx.hitSound.Play(null);
		}
		if (hitFx.hitEffect != null)
		{
			hitFx.hitEffect.Stop();
			hitFx.hitEffect.Play();
		}
		for (int i = 0; i < components.Count; i++)
		{
			components[i].OnSuccessfulHit(hitData);
		}
		if (gameEntity.IsHeldByLocalPlayer())
		{
			PlayVibration(GorillaTagger.Instance.tapHapticStrength, 0.2f);
			GamePlayer gamePlayer = GamePlayer.GetGamePlayer(gameEntity.heldByActorNumber);
			if (gamePlayer != null)
			{
				int num = gamePlayer.FindHandIndex(gameEntity.id);
				if (num != -1)
				{
					GTPlayer.Instance.TempFreezeHand(GamePlayer.IsLeftHand(num), 0.15f);
				}
			}
		}
		if (GRNoiseEventManager.instance != null)
		{
			GRNoiseEventManager.instance.AddNoiseEvent(hitData.hitPosition);
		}
	}

	public void ApplyHitToPlayer(GRPlayer player, Vector3 hitPosition)
	{
		hitFx.hitSound.Play(null);
		if (hitFx.hitEffect != null)
		{
			hitFx.hitEffect.Play();
		}
		for (int i = 0; i < components.Count; i++)
		{
			components[i].OnSuccessfulHitPlayer(player, hitPosition);
		}
	}

	private void PlayVibration(float strength, float duration)
	{
		if (!gameEntity.IsHeldByLocalPlayer())
		{
			return;
		}
		GamePlayer gamePlayer = GamePlayer.GetGamePlayer(gameEntity.heldByActorNumber);
		if (!(gamePlayer == null))
		{
			int num = gamePlayer.FindHandIndex(gameEntity.id);
			if (num != -1)
			{
				GorillaTagger.Instance.StartVibration(GamePlayer.IsLeftHand(num), strength, duration);
			}
		}
	}

	private T GetParentEnemy<T>(Collider collider) where T : MonoBehaviour
	{
		Transform parent = collider.transform;
		while (parent != null)
		{
			T component = parent.GetComponent<T>();
			if (component != null)
			{
				return component;
			}
			parent = parent.parent;
		}
		return null;
	}

	public int CalcHitAmount(GameHitType hitType, GameHittable hittable, GameEntity hitByEntity)
	{
		int result = 0;
		if (hitByEntity != null)
		{
			GRAttributes component = hitByEntity.GetComponent<GRAttributes>();
			if (component != null)
			{
				switch (hitType)
				{
				case GameHitType.Club:
					result = component.CalculateFinalValueForAttribute(damageAttribute);
					break;
				case GameHitType.Flash:
					result = component.CalculateFinalValueForAttribute(flashDamageAttribute);
					break;
				case GameHitType.Shield:
					result = component.CalculateFinalValueForAttribute(shieldDamageAttribute);
					break;
				}
			}
		}
		return result;
	}

	private void OnCollisionEnter(Collision collision)
	{
		if (!hitOnCollision)
		{
			return;
		}
		float num = gameEntity.GetVelocity().sqrMagnitude;
		if (gameEntity.lastHeldByActorNumber != PhotonNetwork.LocalPlayer.ActorNumber)
		{
			return;
		}
		bool flag = false;
		GamePlayer gamePlayer = GamePlayer.GetGamePlayer(gameEntity.heldByActorNumber);
		if (gamePlayer != null)
		{
			float handSpeed = GamePlayerLocal.instance.GetHandSpeed(gamePlayer.FindHandIndex(gameEntity.id));
			num = handSpeed * handSpeed;
		}
		if (num < minSwingSpeed * minSwingSpeed)
		{
			return;
		}
		double timeAsDouble = Time.timeAsDouble;
		if (timeAsDouble < hitCooldownEnd)
		{
			return;
		}
		Collider collider = collision.collider;
		GameHittable parentEnemy = GetParentEnemy<GameHittable>(collider);
		if (parentEnemy != null && parentEnemy.IsColliderValid(collision.collider))
		{
			Vector3 vector = parentEnemy.transform.position - base.transform.position;
			vector.Normalize();
			if (!flag && gamePlayer != null)
			{
				vector = GamePlayerLocal.instance.GetHandVelocity(gamePlayer.FindHandIndex(gameEntity.id)).normalized;
			}
			float a = Mathf.Sqrt(num);
			a = Mathf.Min(a, maxImpulseSpeed);
			vector *= a;
			Vector3 position = parentEnemy.transform.position;
			GameHitData hitData = new GameHitData
			{
				hitTypeId = (int)hitType,
				hitEntityId = parentEnemy.gameEntity.id,
				hitByEntityId = gameEntity.id,
				hitEntityPosition = position,
				hitImpulse = vector * knockbackMultiplier,
				hitPosition = collision.GetContact(0).point,
				hitAmount = CalcHitAmount(hitType, parentEnemy, gameEntity),
				hittablePoint = parentEnemy.FindHittablePoint(collider)
			};
			if (parentEnemy.IsHitValid(hitData))
			{
				parentEnemy.RequestHit(hitData);
				hitCooldownEnd = timeAsDouble + 0.25;
			}
		}
	}
}
