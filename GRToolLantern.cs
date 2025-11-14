using System;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(GameEntity))]
public class GRToolLantern : MonoBehaviour, IGRSummoningEntity
{
	private enum State
	{
		Off,
		On,
		Count
	}

	public GameEntity gameEntity;

	public GRTool tool;

	public GameLight gameLight;

	public GRAttributes attributes;

	[SerializeField]
	private float timeOnPerEnergyUseDurationSeconds = 2f;

	[SerializeField]
	private int minEnergyPerUse = 1;

	[SerializeField]
	private float turnOnSoundVolume;

	[SerializeField]
	private AudioClip turnOnSound;

	[SerializeField]
	private AudioClip upgrade1TurnOnSound;

	[SerializeField]
	private AudioClip upgrade2TurnOnSound;

	[SerializeField]
	private AudioClip upgrade3TurnOnSound;

	[SerializeField]
	private AudioSource audioSource;

	public List<MeshAndMaterials> meshAndMaterials;

	[Header("Haptic")]
	public AbilityHaptic onHaptic;

	private float timeOnSpentEnergy;

	private float timeLastTurnedOn;

	private float minOnDuration = 0.5f;

	private State state;

	private List<int> trackedEntities;

	private double lastFlareDropTime;

	public double minFlareDropInterval = 1.0;

	public GameEntity lanternFlarePrefab;

	public int maxSpawnedFlares = 10;

	private bool providingXRay;

	public Vector3 flareSpawnoffset = Vector3.zero;

	private void Awake()
	{
		trackedEntities = new List<int>();
		state = State.Off;
		gameEntity.OnStateChanged += OnStateChanged;
		GameEntity obj = gameEntity;
		obj.OnGrabbed = (Action)Delegate.Combine(obj.OnGrabbed, new Action(OnGrabbed));
		GameEntity obj2 = gameEntity;
		obj2.OnReleased = (Action)Delegate.Combine(obj2.OnReleased, new Action(OnReleased));
		if (tool != null)
		{
			tool.onToolUpgraded += OnToolUpgraded;
			OnToolUpgraded(tool);
		}
	}

	private void OnEnable()
	{
		TurnOff();
		state = State.Off;
	}

	private void OnDestroy()
	{
		if (providingXRay && tool.HasUpgradeInstalled(GRToolProgressionManager.ToolParts.LanternIntensity3))
		{
			DisableXRay();
		}
	}

	private void OnToolUpgraded(GRTool tool)
	{
		if (tool.HasUpgradeInstalled(GRToolProgressionManager.ToolParts.LanternIntensity1))
		{
			turnOnSound = upgrade1TurnOnSound;
		}
		else if (tool.HasUpgradeInstalled(GRToolProgressionManager.ToolParts.LanternIntensity2))
		{
			turnOnSound = upgrade2TurnOnSound;
		}
		else if (tool.HasUpgradeInstalled(GRToolProgressionManager.ToolParts.LanternIntensity3))
		{
			turnOnSound = upgrade3TurnOnSound;
		}
	}

	public void OnGrabbed()
	{
	}

	public void OnReleased()
	{
		if (WasLastHeldLocal())
		{
			DisableXRay();
		}
	}

	private void EnableXRay()
	{
		if (!providingXRay && tool.HasUpgradeInstalled(GRToolProgressionManager.ToolParts.LanternIntensity3))
		{
			GRPlayer.GetLocal().xRayVisionRefCount++;
			providingXRay = true;
		}
	}

	private void DisableXRay()
	{
		if (providingXRay && tool.HasUpgradeInstalled(GRToolProgressionManager.ToolParts.LanternIntensity3))
		{
			GRPlayer.GetLocal().xRayVisionRefCount--;
			providingXRay = false;
		}
	}

	public void Update()
	{
		float deltaTime = Time.deltaTime;
		if (IsHeldLocal() || tool.energy > 0)
		{
			OnUpdateAuthority(deltaTime);
		}
		else
		{
			OnUpdateRemote(deltaTime);
		}
	}

	private void OnUpdateAuthority(float dt)
	{
		if (tool.HasUpgradeInstalled(GRToolProgressionManager.ToolParts.LanternIntensity3))
		{
			bool isOn = IsHeld();
			EnableLights(isOn);
		}
		if (tool.HasUpgradeInstalled(GRToolProgressionManager.ToolParts.LanternIntensity2))
		{
			SetState(State.On);
			if (Time.timeAsDouble > lastFlareDropTime + minFlareDropInterval && IsButtonHeld() && tool.HasEnoughEnergy() && trackedEntities.Count < maxSpawnedFlares && lanternFlarePrefab != null)
			{
				if (gameEntity.IsAuthority())
				{
					Vector3 vector = base.transform.rotation * flareSpawnoffset;
					gameEntity.manager.RequestCreateItem(lanternFlarePrefab.name.GetStaticHash(), base.transform.position + vector, base.transform.rotation * Quaternion.Euler(10f, 0f, 10f), gameEntity.GetNetId());
				}
				lastFlareDropTime = Time.timeAsDouble;
				tool.UseEnergy();
				audioSource.PlayOneShot(turnOnSound, turnOnSoundVolume);
			}
			return;
		}
		switch (state)
		{
		case State.Off:
			if (IsButtonHeld() && tool.HasEnoughEnergy())
			{
				SetState(State.On);
				gameEntity.RequestState(gameEntity.id, 1L);
			}
			break;
		case State.On:
			timeOnSpentEnergy -= dt;
			if ((!IsButtonHeld() && timeOnSpentEnergy <= 0f) || tool.energy <= 0)
			{
				SetState(State.Off);
				gameEntity.RequestState(gameEntity.id, 0L);
			}
			else if (IsButtonHeld() && timeOnSpentEnergy <= 0f)
			{
				TryConsumeEnergy();
			}
			break;
		}
	}

	private void TryConsumeEnergy()
	{
		if (tool.HasEnoughEnergy())
		{
			tool.UseEnergy();
			timeOnSpentEnergy = timeOnPerEnergyUseDurationSeconds * 10f * (float)tool.GetEnergyUseCost() / (float)tool.GetEnergyMax();
		}
	}

	private void OnUpdateRemote(float dt)
	{
		State state = (State)gameEntity.GetState();
		if (state != this.state)
		{
			SetState(state);
		}
	}

	private void SetState(State newState)
	{
		if (state != newState && CanChangeState((long)newState))
		{
			state = newState;
			switch (state)
			{
			case State.On:
				TurnOn();
				break;
			case State.Off:
				TurnOff();
				break;
			}
		}
	}

	private void TurnOn()
	{
		if (tool.HasUpgradeInstalled(GRToolProgressionManager.ToolParts.LanternIntensity3))
		{
			EnableXRay();
		}
		else
		{
			EnableLights(isOn: true);
		}
		audioSource.PlayOneShot(turnOnSound, turnOnSoundVolume);
		onHaptic.PlayIfHeldLocal(gameEntity);
		timeLastTurnedOn = Time.time;
	}

	private void EnableLights(bool isOn)
	{
		if (gameLight.gameObject.activeSelf != isOn)
		{
			if (attributes.HasBeenInitialized())
			{
				gameLight.light.intensity = attributes.CalculateFinalValueForAttribute(GRAttributeType.LightIntensity);
			}
			gameLight.gameObject.SetActive(isOn);
			for (int i = 0; i < meshAndMaterials.Count; i++)
			{
				MaterialUtils.SwapMaterial(meshAndMaterials[i], !isOn);
			}
		}
	}

	private void TurnOff()
	{
		if (tool.HasUpgradeInstalled(GRToolProgressionManager.ToolParts.LanternIntensity3))
		{
			DisableXRay();
		}
		else
		{
			EnableLights(isOn: false);
		}
	}

	private bool IsHeld()
	{
		return gameEntity.IsHeld();
	}

	private bool IsHeldLocal()
	{
		return gameEntity.heldByActorNumber == PhotonNetwork.LocalPlayer.ActorNumber;
	}

	private bool WasLastHeldLocal()
	{
		return gameEntity.lastHeldByActorNumber == PhotonNetwork.LocalPlayer.ActorNumber;
	}

	private bool IsButtonHeld()
	{
		if (!GamePlayer.TryGetGamePlayer(gameEntity.heldByActorNumber, out var out_gamePlayer))
		{
			return false;
		}
		int num = out_gamePlayer.FindHandIndex(gameEntity.id);
		if (num == -1)
		{
			return false;
		}
		if (!GamePlayer.IsLeftHand(num))
		{
			return out_gamePlayer.rig.rightIndex.calcT > 0.25f;
		}
		return out_gamePlayer.rig.leftIndex.calcT > 0.25f;
	}

	private void OnStateChanged(long prevState, long nextState)
	{
	}

	public bool CanChangeState(long newStateIndex)
	{
		if (newStateIndex < 0 || newStateIndex >= 2)
		{
			return false;
		}
		switch ((State)newStateIndex)
		{
		case State.On:
			return tool.energy > 0;
		case State.Off:
			if (!(Time.time > timeLastTurnedOn + minOnDuration))
			{
				return tool.energy <= 0;
			}
			return true;
		default:
			return false;
		}
	}

	public void AddTrackedEntity(GameEntity entityToTrack)
	{
		int netId = entityToTrack.GetNetId();
		trackedEntities.AddIfNew(netId);
	}

	public void RemoveTrackedEntity(GameEntity entityToRemove)
	{
		int netId = entityToRemove.GetNetId();
		if (trackedEntities.Contains(netId))
		{
			trackedEntities.Remove(netId);
		}
	}

	public void OnSummonedEntityInit(GameEntity entity)
	{
		AddTrackedEntity(entity);
	}

	public void OnSummonedEntityDestroy(GameEntity entity)
	{
		RemoveTrackedEntity(entity);
	}
}
