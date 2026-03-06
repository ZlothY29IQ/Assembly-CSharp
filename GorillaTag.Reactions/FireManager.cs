using System.Collections.Generic;
using System.Runtime.CompilerServices;
using GorillaTag.Audio;
using UnityEngine;

namespace GorillaTag.Reactions;

public class FireManager : ITickSystemPost
{
	[OnEnterPlay_Clear]
	private static readonly Dictionary<int, FireInstance> _kGObjInstId_to_fire = new Dictionary<int, FireInstance>(256);

	[OnEnterPlay_Clear]
	private static readonly List<FireInstance> _kEnabledReactions = new List<FireInstance>(256);

	[OnEnterPlay_Clear]
	private static readonly List<FireInstance> _kFiresToDespawn = new List<FireInstance>(256);

	[OnEnterPlay_Clear]
	private static readonly Dictionary<Vector3Int, int> _fireSpatialGrid = new Dictionary<Vector3Int, int>(256);

	private const float _kSpatialGridCellSize = 0.2f;

	private const int _kMaxAudioSources = 8;

	[OnEnterPlay_Set(0)]
	private static int _activeAudioSources;

	private static readonly int shaderProp_EmissionColor = ShaderProps._EmissionColor;

	[field: OnEnterPlay_SetNull]
	internal static FireManager instance { get; private set; }

	[field: OnEnterPlay_Set(false)]
	internal static bool hasInstance { get; private set; }

	bool ITickSystemPost.PostTickRunning { get; set; }

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
	private static void Initialize()
	{
		if (!ApplicationQuittingState.IsQuitting && !hasInstance)
		{
			instance = new FireManager();
			hasInstance = true;
			TickSystem<object>.AddPostTickCallback(instance);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void Register(FireInstance f)
	{
		if (ApplicationQuittingState.IsQuitting)
		{
			return;
		}
		int instanceID = f.gameObject.GetInstanceID();
		if (!_kGObjInstId_to_fire.TryAdd(instanceID, f))
		{
			if (f == null)
			{
				Debug.LogError("FireManager: You tried to register null!", f);
				return;
			}
			Debug.LogError("FireManager: \"" + f.name + "\" was attempted to be registered more than once!", f);
		}
		f.GetComponentAndSetFieldIfNullElseLogAndDisable(ref f._collider, "_collider", "Collider", "Disabling.", "Register");
		f.GetComponentAndSetFieldIfNullElseLogAndDisable(ref f._thermalVolume, "_thermalVolume", "ThermalSourceVolume", "Disabling.", "Register");
		f.GetComponentAndSetFieldIfNullElseLogAndDisable(ref f._particleSystem, "_particleSystem", "ParticleSystem", "Disabling.", "Register");
		f.GetComponentAndSetFieldIfNullElseLogAndDisable(ref f._loopingAudioSource, "_loopingAudioSource", "AudioSource", "Disabling.", "Register");
		f.DisableIfNull(f._extinguishSound.obj, "_extinguishSound", "AudioClip", "Register");
		f.DisableIfNull(f._igniteSound.obj, "_igniteSound", "AudioClip", "Register");
		f._defaultTemperature = f._thermalVolume.celsius;
		f._timeSinceExtinguished = 0f - f._stayExtinguishedDuration;
		f._psEmissionModule = f._particleSystem.emission;
		f._psDefaultEmissionRate = f._psEmissionModule.rateOverTime.constant;
		f._deathStateDuration = 0f;
		if (f._emissiveRenderers != null)
		{
			f._emiRenderers_matPropBlocks = new MaterialPropertyBlock[f._emissiveRenderers.Length];
			f._emiRenderers_defaultColors = new Color[f._emissiveRenderers.Length];
			for (int i = 0; i < f._emissiveRenderers.Length; i++)
			{
				f._emiRenderers_matPropBlocks[i] = new MaterialPropertyBlock();
				f._emissiveRenderers[i].GetPropertyBlock(f._emiRenderers_matPropBlocks[i]);
				f._emiRenderers_defaultColors[i] = f._emiRenderers_matPropBlocks[i].GetColor(shaderProp_EmissionColor);
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void Unregister(FireInstance reactable)
	{
		if (!ApplicationQuittingState.IsQuitting)
		{
			int instanceID = reactable.gameObject.GetInstanceID();
			_kGObjInstId_to_fire.Remove(instanceID);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static Vector3Int GetSpatialGridPos(Vector3 pos)
	{
		Vector3 vector = pos / 0.2f;
		return new Vector3Int((int)vector.x, (int)vector.y, (int)vector.z);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void ResetFireValues(FireInstance f)
	{
		f._timeSinceExtinguished = Mathf.Min(f._timeSinceExtinguished, f._stayExtinguishedDuration);
		f._timeSinceDyingStart = 0f;
		f._isDespawning = false;
		f._timeAlive = 0f;
		f._thermalVolume.celsius = f._defaultTemperature;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void SpawnFire(SinglePool pool, Vector3 pos, Vector3 normal, float scale)
	{
		SpawnFire(pool, pos, normal, scale, null);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void SpawnFire(SinglePool pool, Vector3 pos, Vector3 normal, float scale, Quaternion? rotationOverride)
	{
		if (_fireSpatialGrid.TryGetValue(GetSpatialGridPos(pos), out var value))
		{
			ResetFireValues(_kGObjInstId_to_fire[value]);
			return;
		}
		GameObject gameObject = pool.Instantiate(setActive: false);
		gameObject.transform.position = pos;
		if (rotationOverride.HasValue)
		{
			gameObject.transform.rotation = rotationOverride.Value;
		}
		else
		{
			gameObject.transform.up = normal;
		}
		gameObject.transform.localScale = Vector3.one * scale;
		gameObject.SetActive(value: true);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void OnEnable(FireInstance f)
	{
		if (!ApplicationQuittingState.IsQuitting && !(ObjectPools.instance == null) && ObjectPools.instance.initialized)
		{
			ResetFireValues(f);
			f._spatialGridPosition = GetSpatialGridPos(f.transform.position);
			_fireSpatialGrid.Add(f._spatialGridPosition, f.gameObject.GetInstanceID());
			_kEnabledReactions.Add(f);
			if (GTAudioOneShot.isInitialized && Time.realtimeSinceStartup > 10f)
			{
				GTAudioOneShot.Play(f._igniteSound, f.transform.position, f._igniteSoundVolume);
			}
			if (8 > _activeAudioSources)
			{
				_activeAudioSources++;
				f._loopingAudioSource.enabled = true;
			}
			else
			{
				f._loopingAudioSource.enabled = false;
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void OnDisable(FireInstance f)
	{
		if (!ApplicationQuittingState.IsQuitting)
		{
			_kEnabledReactions.Remove(f);
			_fireSpatialGrid.Remove(f._spatialGridPosition);
			_activeAudioSources = Mathf.Min(_activeAudioSources - (f._loopingAudioSource.enabled ? 1 : 0), 0);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void OnTriggerEnter(FireInstance f, Collider other)
	{
		if (!f._isDespawning && !ApplicationQuittingState.IsQuitting && other.gameObject.layer == 4)
		{
			Extinguish(f.gameObject, float.MaxValue);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void Extinguish(GameObject gObj, float extinguishAmount)
	{
		if (ApplicationQuittingState.IsQuitting || !_kGObjInstId_to_fire.TryGetValue(gObj.GetInstanceID(), out var value))
		{
			return;
		}
		float num = value._thermalVolume.celsius - extinguishAmount;
		if (num <= 0f && value._thermalVolume.celsius > 0.001f)
		{
			value._thermalVolume.celsius = Mathf.Max(num, 0f);
			value._timeSinceExtinguished = 0f;
			GTAudioOneShot.Play(value._extinguishSound, value.transform.position, value._extinguishSoundVolume);
			if (value._despawnOnExtinguish)
			{
				value._isDespawning = true;
				value._timeSinceDyingStart = 0f;
			}
		}
	}

	void ITickSystemPost.PostTick()
	{
		if (ApplicationQuittingState.IsQuitting)
		{
			return;
		}
		foreach (FireInstance kEnabledReaction in _kEnabledReactions)
		{
			kEnabledReaction._timeAlive += Time.unscaledDeltaTime;
			bool flag = kEnabledReaction._timeSinceExtinguished < kEnabledReaction._stayExtinguishedDuration;
			kEnabledReaction._timeSinceExtinguished += Time.unscaledDeltaTime;
			bool flag2 = kEnabledReaction._timeSinceExtinguished < kEnabledReaction._stayExtinguishedDuration;
			if (kEnabledReaction._isDespawning)
			{
				kEnabledReaction._timeSinceDyingStart += Time.unscaledDeltaTime;
				if (kEnabledReaction._timeSinceDyingStart >= kEnabledReaction._deathStateDuration || kEnabledReaction._thermalVolume.celsius < -9999f)
				{
					_kFiresToDespawn.Add(kEnabledReaction);
				}
			}
			if (!kEnabledReaction._isDespawning && kEnabledReaction._despawnOnExtinguish && kEnabledReaction._timeAlive > kEnabledReaction._maxLifetime)
			{
				kEnabledReaction._isDespawning = true;
				kEnabledReaction._timeSinceDyingStart = 0f;
				GTAudioOneShot.Play(kEnabledReaction._extinguishSound, kEnabledReaction.transform.position, kEnabledReaction._extinguishSoundVolume);
			}
			if (!kEnabledReaction._isDespawning && flag != flag2)
			{
				if (flag2)
				{
					if (kEnabledReaction._despawnOnExtinguish)
					{
						kEnabledReaction._isDespawning = true;
						kEnabledReaction._timeSinceDyingStart = 0f;
					}
					GTAudioOneShot.Play(kEnabledReaction._extinguishSound, kEnabledReaction.transform.position, kEnabledReaction._extinguishSoundVolume);
				}
				else
				{
					GTAudioOneShot.Play(kEnabledReaction._igniteSound, kEnabledReaction.transform.position, kEnabledReaction._igniteSoundVolume);
				}
			}
			float num = kEnabledReaction._thermalVolume.celsius + kEnabledReaction._reheatSpeed * Time.unscaledDeltaTime;
			if (kEnabledReaction._isDespawning)
			{
				num = ((!(kEnabledReaction._deathStateDuration <= 0f)) ? Mathf.Lerp(kEnabledReaction._thermalVolume.celsius, 0f, kEnabledReaction._timeSinceDyingStart / kEnabledReaction._deathStateDuration) : 0f);
			}
			num = ((num > kEnabledReaction._defaultTemperature) ? kEnabledReaction._defaultTemperature : num);
			kEnabledReaction._thermalVolume.celsius = num;
			float num2 = num / kEnabledReaction._defaultTemperature;
			kEnabledReaction._loopingAudioSource.volume = num2;
			for (int i = 0; i < kEnabledReaction._emissiveRenderers.Length; i++)
			{
				kEnabledReaction._emiRenderers_matPropBlocks[i].SetColor(shaderProp_EmissionColor, kEnabledReaction._emiRenderers_defaultColors[i] * num2);
			}
		}
		foreach (FireInstance item in _kFiresToDespawn)
		{
			ObjectPools.instance.Destroy(item.gameObject);
		}
		_kFiresToDespawn.Clear();
	}
}
