using System;
using System.Collections.Generic;
using GorillaTag;
using GorillaTagScripts;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

public class SnowballThrowable : HoldableObject
{
	[GorillaSoundLookup]
	public List<int> matDataIndexes = new List<int> { 32 };

	[Tooltip("prefab to spawn from global object pools when thrown")]
	public GameObject projectilePrefab;

	public SoundBankPlayer pickupSoundBankPlayer;

	public bool isLeftHanded;

	[Tooltip("This needs to match the index of the projectilePrefab on the Local Gorilla Player's BodyDockPositions LeftHandThrowables or RightHandThrowables list\nCheck the array in play mode to find the index")]
	public int throwableMakerIndex;

	[Tooltip("Multiplier is applied to hand speed to get launch speed of the projectile")]
	public float linSpeedMultiplier = 1f;

	[Tooltip("Maximum launch speed of the projectile")]
	public float maxLinSpeed = 12f;

	[Space]
	[FormerlySerializedAs("shouldColorize")]
	public bool randomizeColor;

	public GTColor.HSVRanges randomColorHSVRanges = new GTColor.HSVRanges(0f, 1f, 0.7f, 1f, 1f);

	[Tooltip("Check this part only if we want to randomize the prefab meshes and projectile")]
	public bool randomModelSelection;

	public List<RandomProjectileThrowable> localModels;

	[Tooltip("projectile identifier sent out by the PlayerGameEvents.LaunchedProjectile event. Uses prefab name if empty")]
	public string throwEventName;

	public GorillaVelocityEstimator velocityEstimator;

	protected VRRig targetRig;

	protected bool isOfflineRig;

	private bool awakeHasBeenCalled;

	private bool OnEnableHasBeenCalled;

	private Renderer[] renderers;

	protected int randModelIndex;

	private float destroyTimer = -1f;

	private XformOffset spawnOffset;

	public XformOffset SpawnOffset
	{
		get
		{
			return spawnOffset;
		}
		set
		{
			spawnOffset = value;
		}
	}

	internal int ProjectileHash => PoolUtils.GameObjHashCode((randomModelSelection && localModels != null && randModelIndex >= 0 && randModelIndex <= localModels.Count && localModels[randModelIndex] != null) ? localModels[randModelIndex].GetProjectilePrefab() : projectilePrefab);

	protected virtual void Awake()
	{
		if (awakeHasBeenCalled)
		{
			return;
		}
		awakeHasBeenCalled = true;
		targetRig = GetComponentInParent<VRRig>(includeInactive: true);
		isOfflineRig = targetRig != null && targetRig.isOfflineVRRig;
		renderers = GetComponentsInChildren<Renderer>();
		randModelIndex = -1;
		foreach (RandomProjectileThrowable localModel in localModels)
		{
			if (localModel != null)
			{
				localModel.OnDestroyRandomProjectile = (UnityAction<bool>)Delegate.Combine(localModel.OnDestroyRandomProjectile, new UnityAction<bool>(HandleOnDestroyRandomProjectile));
			}
		}
	}

	public bool IsMine()
	{
		if (targetRig != null)
		{
			return targetRig.isOfflineVRRig;
		}
		return false;
	}

	public virtual void OnEnable()
	{
		if (targetRig == null)
		{
			Debug.LogError("SnowballThrowable: targetRig is null! Deactivating.");
			base.gameObject.SetActive(value: false);
			return;
		}
		if (!targetRig.isOfflineVRRig)
		{
			if (targetRig.netView != null && targetRig.netView.IsMine)
			{
				base.gameObject.SetActive(value: false);
				return;
			}
			Color32 throwableProjectileColor = targetRig.GetThrowableProjectileColor(isLeftHanded);
			ApplyColor(throwableProjectileColor);
			if (randomModelSelection)
			{
				foreach (RandomProjectileThrowable localModel in localModels)
				{
					localModel.gameObject.SetActive(value: false);
				}
				randModelIndex = targetRig.GetRandomThrowableModelIndex();
				EnableRandomModel(randModelIndex, enable: true);
			}
		}
		AnchorToHand();
		OnEnableHasBeenCalled = true;
	}

	public virtual void OnDisable()
	{
	}

	protected new virtual void OnDestroy()
	{
	}

	public void SetSnowballActiveLocal(bool enabled)
	{
		if (!awakeHasBeenCalled)
		{
			Awake();
		}
		if (!OnEnableHasBeenCalled)
		{
			OnEnable();
		}
		if (isLeftHanded)
		{
			targetRig.LeftThrowableProjectileIndex = (enabled ? throwableMakerIndex : (-1));
		}
		else
		{
			targetRig.RightThrowableProjectileIndex = (enabled ? throwableMakerIndex : (-1));
		}
		bool num = !base.gameObject.activeSelf && enabled;
		base.gameObject.SetActive(enabled);
		if (num && pickupSoundBankPlayer != null)
		{
			pickupSoundBankPlayer.Play();
		}
		if (randomModelSelection)
		{
			if (enabled)
			{
				EnableRandomModel(GetRandomModelIndex(), enable: true);
			}
			else
			{
				EnableRandomModel(randModelIndex, enable: false);
			}
			targetRig.SetRandomThrowableModelIndex(randModelIndex);
		}
		EquipmentInteractor.instance.UpdateHandEquipment(enabled ? this : null, isLeftHanded);
		if (randomizeColor)
		{
			Color color = (enabled ? GTColor.RandomHSV(randomColorHSVRanges) : Color.white);
			targetRig.SetThrowableProjectileColor(isLeftHanded, color);
			ApplyColor(color);
		}
	}

	private int GetRandomModelIndex()
	{
		if (localModels.Count == 0)
		{
			return -1;
		}
		randModelIndex = UnityEngine.Random.Range(0, localModels.Count);
		if ((float)UnityEngine.Random.Range(1, 100) <= localModels[randModelIndex].spawnChance * 100f)
		{
			return randModelIndex;
		}
		return GetRandomModelIndex();
	}

	private void EnableRandomModel(int index, bool enable)
	{
		if (randModelIndex >= 0 && randModelIndex < localModels.Count)
		{
			localModels[randModelIndex].gameObject.SetActive(enable);
			if (enable && localModels[randModelIndex].autoDestroyAfterSeconds > 0f)
			{
				destroyTimer = 0f;
			}
		}
	}

	protected virtual void LateUpdateLocal()
	{
		if (randomModelSelection && randModelIndex > -1 && localModels[randModelIndex].ForceDestroy)
		{
			localModels[randModelIndex].ForceDestroy = false;
			if (localModels[randModelIndex].gameObject.activeSelf)
			{
				PerformSnowballThrowAuthority();
			}
		}
		if (!randomModelSelection || randModelIndex <= -1 || !(localModels[randModelIndex].autoDestroyAfterSeconds > 0f))
		{
			return;
		}
		destroyTimer += Time.deltaTime;
		if (destroyTimer > localModels[randModelIndex].autoDestroyAfterSeconds)
		{
			if (localModels[randModelIndex].gameObject.activeSelf)
			{
				PerformSnowballThrowAuthority();
			}
			destroyTimer = -1f;
		}
	}

	protected void LateUpdateReplicated()
	{
	}

	protected void LateUpdateShared()
	{
	}

	private Transform Anchor()
	{
		return base.transform.parent;
	}

	private void AnchorToHand()
	{
		BodyDockPositions myBodyDockPositions = targetRig.myBodyDockPositions;
		Transform transform = Anchor();
		if (isLeftHanded)
		{
			transform.parent = myBodyDockPositions.leftHandTransform;
		}
		else
		{
			transform.parent = myBodyDockPositions.rightHandTransform;
		}
		transform.localPosition = Vector3.zero;
		transform.localRotation = Quaternion.identity;
		base.transform.localPosition = spawnOffset.pos;
		base.transform.localRotation = spawnOffset.rot;
	}

	protected void LateUpdate()
	{
		if (IsMine())
		{
			LateUpdateLocal();
		}
		else
		{
			LateUpdateReplicated();
		}
		LateUpdateShared();
	}

	public override bool OnRelease(DropZone zoneReleased, GameObject releasingHand)
	{
		if (!base.OnRelease(zoneReleased, releasingHand))
		{
			return false;
		}
		OnSnowballRelease();
		return true;
	}

	protected virtual void OnSnowballRelease()
	{
		PerformSnowballThrowAuthority();
	}

	protected virtual void PerformSnowballThrowAuthority()
	{
		if (!(targetRig != null) || targetRig.creator == null || !targetRig.creator.IsLocal)
		{
			return;
		}
		Vector3 vector = Vector3.zero;
		Rigidbody component = GorillaTagger.Instance.GetComponent<Rigidbody>();
		if (component != null)
		{
			vector = component.linearVelocity;
		}
		Vector3 vector2 = velocityEstimator.linearVelocity - vector;
		float magnitude = vector2.magnitude;
		if (magnitude > 0.001f)
		{
			float num = Mathf.Clamp(magnitude * linSpeedMultiplier, 0f, maxLinSpeed);
			vector2 *= num / magnitude;
		}
		Vector3 velocity = vector2 + vector;
		Color32 throwableProjectileColor = targetRig.GetThrowableProjectileColor(isLeftHanded);
		Transform obj = base.transform;
		Vector3 position = obj.position;
		float x = obj.lossyScale.x;
		SlingshotProjectile slingshotProjectile = LaunchSnowballLocal(position, velocity, x, randomizeColor, throwableProjectileColor);
		SetSnowballActiveLocal(enabled: false);
		if (randModelIndex > -1 && randModelIndex < localModels.Count)
		{
			if (localModels[randModelIndex].ForceDestroy || localModels[randModelIndex].destroyAfterRelease)
			{
				slingshotProjectile.DestroyAfterRelease();
			}
			else if (localModels[randModelIndex].moveOverPassedLifeTime)
			{
				float num2 = Time.time - localModels[randModelIndex].TimeEnabled;
				float remainingLifeTime = slingshotProjectile.GetRemainingLifeTime();
				if (remainingLifeTime > num2)
				{
					float newLifeTime = remainingLifeTime - num2;
					slingshotProjectile.UpdateRemainingLifeTime(newLifeTime);
				}
				else
				{
					slingshotProjectile.UpdateRemainingLifeTime(0f);
				}
			}
		}
		if (NetworkSystem.Instance.InRoom)
		{
			RoomSystem.SendLaunchProjectile(position, velocity, isLeftHanded ? RoomSystem.ProjectileSource.LeftHand : RoomSystem.ProjectileSource.RightHand, slingshotProjectile.myProjectileCount, randomizeColor, throwableProjectileColor.r, throwableProjectileColor.g, throwableProjectileColor.b, throwableProjectileColor.a);
		}
	}

	protected virtual SlingshotProjectile LaunchSnowballLocal(Vector3 location, Vector3 velocity, float scale, bool randomColour, Color colour)
	{
		SlingshotProjectile component = ObjectPools.instance.Instantiate(randomModelSelection ? localModels[randModelIndex].GetProjectilePrefab() : projectilePrefab).GetComponent<SlingshotProjectile>();
		component.Launch(projectileCount: ProjectileTracker.AddAndIncrementLocalProjectile(component, velocity, location, scale), position: location, velocity: velocity, player: NetworkSystem.Instance.LocalPlayer, blueTeam: false, orangeTeam: false, scale: scale, shouldOverrideColor: randomColour, overrideColor: colour);
		GorillaTagger.Instance.StartVibration(isLeftHanded, GorillaTagger.Instance.tapHapticStrength * 0.5f, GorillaTagger.Instance.tapHapticDuration * 0.5f);
		if (string.IsNullOrEmpty(throwEventName))
		{
			PlayerGameEvents.LaunchedProjectile(projectilePrefab.name);
		}
		else
		{
			PlayerGameEvents.LaunchedProjectile(throwEventName);
		}
		component.OnImpact += OnProjectileImpact;
		return component;
	}

	protected virtual SlingshotProjectile SpawnProjectile()
	{
		return ObjectPools.instance.Instantiate(randomModelSelection ? localModels[randModelIndex].GetProjectilePrefab() : projectilePrefab).GetComponent<SlingshotProjectile>();
	}

	protected virtual void OnProjectileImpact(SlingshotProjectile projectile, Vector3 impactPos, NetPlayer hitPlayer)
	{
		if (hitPlayer != null)
		{
			ScienceExperimentManager instance = ScienceExperimentManager.instance;
			if (instance != null && projectilePrefab != null && projectilePrefab == instance.waterBalloonPrefab)
			{
				instance.OnWaterBalloonHitPlayer(hitPlayer);
			}
			if (hitPlayer.IsLocal)
			{
				GorillaTagger.Instance.StartVibration(forLeftController: true, GorillaTagger.Instance.tapHapticStrength * 0.5f, GorillaTagger.Instance.tapHapticDuration * 0.5f);
				GorillaTagger.Instance.StartVibration(forLeftController: true, GorillaTagger.Instance.tapHapticStrength * 0.5f, GorillaTagger.Instance.tapHapticDuration * 0.5f);
			}
		}
	}

	private void ApplyColor(Color newColor)
	{
		Renderer[] array = renderers;
		foreach (Renderer renderer in array)
		{
			if (!renderer)
			{
				continue;
			}
			Material[] materials = renderer.materials;
			foreach (Material material in materials)
			{
				if (!(material == null))
				{
					if (material.HasProperty(ShaderProps._BaseColor))
					{
						material.SetColor(ShaderProps._BaseColor, newColor);
					}
					if (material.HasProperty(ShaderProps._Color))
					{
						material.SetColor(ShaderProps._Color, newColor);
					}
				}
			}
		}
	}

	public override void OnHover(InteractionPoint pointHovered, GameObject hoveringHand)
	{
	}

	public override void OnGrab(InteractionPoint pointGrabbed, GameObject grabbingHand)
	{
	}

	public override void DropItemCleanup()
	{
		if (base.gameObject.activeSelf)
		{
			OnSnowballRelease();
		}
	}

	private void HandleOnDestroyRandomProjectile(bool enable)
	{
		SetSnowballActiveLocal(enable);
	}
}
