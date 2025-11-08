using System;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class GRToolUpgradePiece : MonoBehaviour, IGameEntityComponent
{
	public GameEntity gameEntity;

	public GRToolProgressionManager.ToolParts matchingUpgrade;

	private int gameEntityListCheckIndex;

	private GameEntity currentMagnetizingTool;

	public AnimationCurve visualDistanceCurve;

	public float shakeMaxAmount = 10f;

	public float shakeFrequency = 100f;

	public Transform childVisualTransform;

	public AudioSource humAudioSource;

	public AudioSource audioSource;

	public AudioClip snapAudioClip;

	public MeshCollider meshCollider;

	public ParticleSystem attractParticleSystem;

	public ParticleSystemForceField forceField;

	public float minDistToStartMagnetize = 0.5f;

	public float minDistToSnap;

	public float magnetizingLoopMinVolume = 0.2f;

	public float magnetizingLoopMaxVolume = 1f;

	public float snapAudioVolume = 1f;

	private int toolSearchesPerFrame = 5;

	private float shakePhase;

	private void Start()
	{
		MeshFilter componentInChildren = GetComponentInChildren<MeshFilter>();
		if (componentInChildren != null)
		{
			meshCollider.sharedMesh = componentInChildren.sharedMesh;
		}
	}

	private void EnableProcAnimLoop()
	{
		GameEntity obj = gameEntity;
		obj.OnTick = (Action)Delegate.Combine(obj.OnTick, new Action(Tick));
		if (!humAudioSource.isPlaying)
		{
			humAudioSource.volume = 0f;
			humAudioSource.GTPlay();
		}
	}

	private void DisableProcAnimLoop()
	{
		GameEntity obj = gameEntity;
		obj.OnTick = (Action)Delegate.Remove(obj.OnTick, new Action(Tick));
		SwitchMagnetizedTarget(null);
		childVisualTransform.localPosition = Vector3.zero;
		childVisualTransform.localRotation = Quaternion.identity;
		childVisualTransform.localScale = Vector3.one;
		humAudioSource.Stop();
		if (attractParticleSystem != null)
		{
			attractParticleSystem.Stop();
		}
	}

	private void SwitchMagnetizedTarget(GameEntity entity)
	{
		currentMagnetizingTool = entity;
	}

	private void Tick()
	{
		Vector3 position = base.transform.position;
		List<GameEntity> gameEntities = this.gameEntity.manager.GetGameEntities();
		int num = gameEntityListCheckIndex;
		int num2 = ((toolSearchesPerFrame < gameEntities.Count) ? toolSearchesPerFrame : gameEntities.Count);
		GRTool gRTool = ((currentMagnetizingTool != null) ? currentMagnetizingTool.GetComponent<GRTool>() : null);
		GRTool.Upgrade upgrade = ((gRTool != null) ? gRTool.FindMatchingUpgrade(matchingUpgrade) : null);
		float num3 = ((gRTool != null) ? gRTool.GetPointDistanceToUpgrade(position, upgrade) : 1E+10f);
		if (num3 > minDistToStartMagnetize)
		{
			SwitchMagnetizedTarget(null);
			gRTool = null;
			upgrade = null;
			num3 = 1E+10f;
		}
		for (int i = 0; i < num2; i++)
		{
			num = (num + 1) % gameEntities.Count;
			GameEntity gameEntity = gameEntities[num];
			if (gameEntity == null)
			{
				continue;
			}
			GRTool component = gameEntity.GetComponent<GRTool>();
			if (!(component != null) || gameEntity.heldByActorNumber == -1)
			{
				continue;
			}
			GRTool.Upgrade upgrade2 = component.FindMatchingUpgrade(matchingUpgrade);
			if (upgrade2 != null)
			{
				float pointDistanceToUpgrade = component.GetPointDistanceToUpgrade(position, upgrade2);
				if (pointDistanceToUpgrade > 0f && pointDistanceToUpgrade < num3 && pointDistanceToUpgrade < minDistToStartMagnetize)
				{
					SwitchMagnetizedTarget(gameEntity);
					gRTool = component;
					upgrade = upgrade2;
					num3 = pointDistanceToUpgrade;
				}
			}
		}
		gameEntityListCheckIndex = num;
		if (gRTool != null)
		{
			Transform upgradeAttachTransform = gRTool.GetUpgradeAttachTransform(upgrade);
			if (num3 < minDistToSnap)
			{
				humAudioSource.volume = 0f;
				if (attractParticleSystem != null)
				{
					attractParticleSystem.Stop();
				}
				childVisualTransform.position = upgradeAttachTransform.position;
				childVisualTransform.rotation = upgradeAttachTransform.rotation;
				childVisualTransform.localScale = new Vector3(upgradeAttachTransform.localScale.x / base.transform.localScale.x, upgradeAttachTransform.localScale.y / base.transform.localScale.y, upgradeAttachTransform.localScale.z / base.transform.localScale.z);
				if (currentMagnetizingTool != null)
				{
					GhostReactor instance = GhostReactor.instance;
					if (instance != null)
					{
						instance.grManager.ToolSnapRequestUpgrade(this.gameEntity.GetNetId(), matchingUpgrade, currentMagnetizingTool.GetComponent<GameEntity>().GetNetId());
					}
				}
				return;
			}
			float num4 = Mathf.Clamp01(num3 / minDistToStartMagnetize);
			humAudioSource.volume = Mathf.Lerp(magnetizingLoopMaxVolume, magnetizingLoopMinVolume, num4);
			float num5 = shakeMaxAmount * (1f - num4);
			float t = Mathf.Clamp01((visualDistanceCurve != null) ? visualDistanceCurve.Evaluate(num4) : num4);
			shakePhase += Time.deltaTime * shakeFrequency;
			if (shakePhase > MathF.PI * 2f)
			{
				shakePhase -= MathF.PI * 2f;
			}
			Transform transform = base.transform;
			if (childVisualTransform != null)
			{
				Vector3 position2 = Vector3.Lerp(upgradeAttachTransform.position, transform.position, t);
				Quaternion rotation = Quaternion.Slerp(upgradeAttachTransform.rotation, transform.rotation, t);
				Vector3 localScale = Vector3.Lerp(upgradeAttachTransform.localScale, transform.localScale, t);
				localScale.x /= transform.localScale.x;
				localScale.y /= transform.localScale.y;
				localScale.z /= transform.localScale.y;
				rotation *= Quaternion.Euler(new Vector3(num5 * Mathf.Sin(shakePhase), num5 * Mathf.Cos(shakePhase), 0f));
				childVisualTransform.position = position2;
				childVisualTransform.rotation = rotation;
				childVisualTransform.localScale = localScale;
			}
			if (attractParticleSystem != null)
			{
				if (!attractParticleSystem.isPlaying)
				{
					attractParticleSystem.Play();
				}
				ParticleSystem.EmissionModule emission = attractParticleSystem.emission;
				emission.enabled = true;
			}
			forceField.transform.position = upgradeAttachTransform.position;
		}
		else
		{
			if (attractParticleSystem != null)
			{
				ParticleSystem.EmissionModule emission2 = attractParticleSystem.emission;
				emission2.enabled = false;
			}
			humAudioSource.volume = 0f;
		}
	}

	private void OnEnable()
	{
		GameEntity obj = gameEntity;
		obj.OnGrabbed = (Action)Delegate.Combine(obj.OnGrabbed, new Action(GrabbedByPlayer));
		GameEntity obj2 = gameEntity;
		obj2.OnReleased = (Action)Delegate.Combine(obj2.OnReleased, new Action(ReleasedByPlayer));
	}

	private void OnDisable()
	{
		GameEntity obj = gameEntity;
		obj.OnGrabbed = (Action)Delegate.Remove(obj.OnGrabbed, new Action(GrabbedByPlayer));
		GameEntity obj2 = gameEntity;
		obj2.OnReleased = (Action)Delegate.Remove(obj2.OnReleased, new Action(ReleasedByPlayer));
	}

	public void GrabbedByPlayer()
	{
		if (gameEntity.heldByActorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
		{
			GRPlayer gRPlayer = GRPlayer.Get(gameEntity.heldByActorNumber);
			if ((bool)gRPlayer)
			{
				gRPlayer.GrabbedItem(gameEntity.id, base.gameObject.name);
			}
		}
		EnableProcAnimLoop();
	}

	public void ReleasedByPlayer()
	{
		DisableProcAnimLoop();
	}

	public void OnEntityInit()
	{
		GhostReactor.ToolEntityCreateData toolEntityCreateData = GhostReactor.ToolEntityCreateData.Unpack(gameEntity.createData);
		GhostReactorManager ghostReactorManager = GhostReactorManager.Get(gameEntity);
		if (ghostReactorManager != null)
		{
			GRToolUpgradePurchaseStationFull toolUpgradeStationFullForIndex = ghostReactorManager.GetToolUpgradeStationFullForIndex(toolEntityCreateData.stationIndex);
			if (toolUpgradeStationFullForIndex != null)
			{
				toolUpgradeStationFullForIndex.InitLinkedEntity(gameEntity);
			}
		}
	}

	public void OnEntityDestroy()
	{
	}

	public void OnEntityStateChange(long prevState, long nextState)
	{
	}
}
