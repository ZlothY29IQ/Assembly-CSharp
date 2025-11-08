using System.Collections;
using System.Collections.Generic;
using GorillaGameModes;
using GorillaNetworking;
using UnityEngine;

namespace TagEffects;

public class TagEffectsLibrary : MonoBehaviour
{
	public enum EffectType
	{
		FIRST_PERSON,
		THIRD_PERSON,
		HIGH_FIVE,
		FIST_BUMP
	}

	private const int OBJECT_QUEUE_LIMIT = 12;

	[OnEnterPlay_SetNull]
	private static TagEffectsLibrary _instance;

	[SerializeField]
	private float fistBumpSpeedThreshold = 1f;

	[SerializeField]
	private float highFiveSpeedThreshold = 1f;

	[SerializeField]
	private ModeTagEffect[] defaultTagEffects;

	[SerializeField]
	private TagEffectsComboResult[] tagEffectsCombos;

	[SerializeField]
	private bool debugMode;

	private Dictionary<string, Queue<GameObjectOnDisableDispatcher>> tagEffectsPool;

	private Dictionary<TagEffectsCombo, TagEffectPack[]> tagEffectsComboLookUp;

	public static float FistBumpSpeedThreshold => _instance.fistBumpSpeedThreshold;

	public static float HighFiveSpeedThreshold => _instance.highFiveSpeedThreshold;

	public static bool DebugMode => _instance.debugMode;

	private void Awake()
	{
		if (_instance != null)
		{
			Object.Destroy(base.gameObject);
			return;
		}
		_instance = this;
		tagEffectsPool = new Dictionary<string, Queue<GameObjectOnDisableDispatcher>>();
		tagEffectsComboLookUp = new Dictionary<TagEffectsCombo, TagEffectPack[]>();
	}

	public static void PlayEffect(Transform target, bool isLeftHand, float rigScale, EffectType effectType, TagEffectPack playerCosmeticTagEffectPack, TagEffectPack otherPlayerCosmeticTagEffectPack, Quaternion rotation)
	{
		if (_instance == null)
		{
			return;
		}
		ModeTagEffect modeTagEffect = null;
		TagEffectPack tagEffectPack = null;
		GameModeType item = ((GameMode.ActiveGameMode != null) ? GameMode.ActiveGameMode.GameType() : GameModeType.Casual);
		for (int i = 0; i < _instance.defaultTagEffects.Length; i++)
		{
			if (_instance.defaultTagEffects[i] != null && _instance.defaultTagEffects[i].Modes.Contains(item))
			{
				modeTagEffect = _instance.defaultTagEffects[i];
				tagEffectPack = modeTagEffect.tagEffect;
				break;
			}
		}
		if (tagEffectPack == null)
		{
			return;
		}
		GameObject firstPerson = tagEffectPack.firstPerson;
		GameObject thirdPerson = tagEffectPack.thirdPerson;
		GameObject fistBump = tagEffectPack.fistBump;
		GameObject highFive = tagEffectPack.highFive;
		bool firstPersonParentEffect = tagEffectPack.firstPersonParentEffect;
		bool thirdPersonParentEffect = tagEffectPack.thirdPersonParentEffect;
		bool flag = tagEffectPack.fistBumpParentEffect;
		bool highFiveParentEffect = tagEffectPack.highFiveParentEffect;
		if (playerCosmeticTagEffectPack != null)
		{
			TagEffectPack tagEffectPack2 = comboLookup(playerCosmeticTagEffectPack, otherPlayerCosmeticTagEffectPack);
			if (!modeTagEffect.blockFistBumpOverride && playerCosmeticTagEffectPack.fistBump != null)
			{
				fistBump = tagEffectPack2.fistBump;
				flag = tagEffectPack2.firstPersonParentEffect;
			}
			if (!modeTagEffect.blockHiveFiveOverride && playerCosmeticTagEffectPack.highFive != null)
			{
				highFive = tagEffectPack2.highFive;
				highFiveParentEffect = tagEffectPack2.highFiveParentEffect;
			}
		}
		if (otherPlayerCosmeticTagEffectPack != null)
		{
			if (!modeTagEffect.blockTagOverride && otherPlayerCosmeticTagEffectPack.firstPerson != null)
			{
				firstPerson = otherPlayerCosmeticTagEffectPack.firstPerson;
				firstPersonParentEffect = otherPlayerCosmeticTagEffectPack.firstPersonParentEffect;
			}
			if (!modeTagEffect.blockTagOverride && otherPlayerCosmeticTagEffectPack.thirdPerson != null)
			{
				thirdPerson = otherPlayerCosmeticTagEffectPack.thirdPerson;
				thirdPersonParentEffect = otherPlayerCosmeticTagEffectPack.thirdPersonParentEffect;
			}
		}
		switch (effectType)
		{
		case EffectType.FIRST_PERSON:
			placeEffects(firstPerson, target, firstPersonParentEffect ? 1f : rigScale, flipZAxis: false, firstPersonParentEffect, rotation);
			break;
		case EffectType.THIRD_PERSON:
			placeEffects(thirdPerson, target, thirdPersonParentEffect ? 1f : rigScale, flipZAxis: false, thirdPersonParentEffect, rotation);
			break;
		case EffectType.FIST_BUMP:
			placeEffects(fistBump, target, flag ? 1f : rigScale, isLeftHand, flag, rotation);
			break;
		case EffectType.HIGH_FIVE:
			placeEffects(highFive, target, highFiveParentEffect ? 1f : rigScale, isLeftHand, highFiveParentEffect, rotation);
			break;
		}
	}

	private static TagEffectPack comboLookup(TagEffectPack playerCosmeticTagEffectPack, TagEffectPack otherPlayerCosmeticTagEffectPack)
	{
		if (otherPlayerCosmeticTagEffectPack == null)
		{
			return playerCosmeticTagEffectPack;
		}
		TagEffectsCombo tagEffectsCombo = new TagEffectsCombo();
		tagEffectsCombo.inputA = playerCosmeticTagEffectPack;
		tagEffectsCombo.inputB = otherPlayerCosmeticTagEffectPack;
		if (!_instance.tagEffectsComboLookUp.TryGetValue(tagEffectsCombo, out var value))
		{
			return playerCosmeticTagEffectPack;
		}
		int num = 0;
		if (GorillaComputer.instance != null)
		{
			num = GorillaComputer.instance.GetServerTime().Second;
		}
		return value[num % value.Length];
	}

	public static void placeEffects(GameObject prefab, Transform target, float scale, bool flipZAxis, bool parentEffect, Quaternion rotation)
	{
		if (prefab == null)
		{
			return;
		}
		if (!_instance.tagEffectsPool.TryGetValue(prefab.name, out var value))
		{
			value = new Queue<GameObjectOnDisableDispatcher>();
			_instance.tagEffectsPool.Add(prefab.name, value);
		}
		if (value.Count == 0 || (value.Peek().gameObject.activeInHierarchy && value.Count < 12))
		{
			GameObject gameObject = Object.Instantiate(prefab, target.transform.position, rotation, parentEffect ? target : _instance.transform);
			gameObject.name = prefab.name;
			gameObject.transform.localScale = (flipZAxis ? new Vector3(scale, scale, 0f - scale) : (Vector3.one * scale));
			if (!gameObject.TryGetComponent<GameObjectOnDisableDispatcher>(out var component))
			{
				component = gameObject.AddComponent<GameObjectOnDisableDispatcher>();
			}
			component.OnDisabled += NewGameObjectOnDisableDispatcher_OnDisabled;
			gameObject.SetActive(value: true);
			value.Enqueue(component);
		}
		else
		{
			GameObjectOnDisableDispatcher recycledGameObject = value.Dequeue();
			_instance.StartCoroutine(_instance.RecycleGameObject(recycledGameObject, target, scale, flipZAxis, parentEffect));
		}
	}

	private static void NewGameObjectOnDisableDispatcher_OnDisabled(GameObjectOnDisableDispatcher goodd)
	{
		_instance.StartCoroutine(_instance.ReclaimDisabled(goodd.transform));
	}

	private IEnumerator RecycleGameObject(GameObjectOnDisableDispatcher recycledGameObject, Transform target, float scale, bool flipZAxis, bool parentEffect)
	{
		if (recycledGameObject.gameObject.activeInHierarchy)
		{
			recycledGameObject.gameObject.SetActive(value: false);
			recycledGameObject.OnDisabled -= NewGameObjectOnDisableDispatcher_OnDisabled;
			yield return null;
		}
		recycledGameObject.transform.position = target.transform.position;
		recycledGameObject.transform.rotation = target.transform.rotation;
		recycledGameObject.transform.localScale = (flipZAxis ? new Vector3(scale, scale, 0f - scale) : (Vector3.one * scale));
		recycledGameObject.transform.parent = (parentEffect ? target : _instance.transform);
		if (_instance.tagEffectsPool.TryGetValue(recycledGameObject.gameObject.name, out var value))
		{
			recycledGameObject.gameObject.SetActive(value: true);
			value.Enqueue(recycledGameObject);
		}
	}

	private IEnumerator ReclaimDisabled(Transform transform)
	{
		yield return null;
		transform.parent = _instance.transform;
	}
}
