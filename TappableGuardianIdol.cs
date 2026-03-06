using System;
using System.Collections;
using GorillaExtensions;
using Photon.Pun;
using UnityEngine;

[DisallowMultipleComponent]
public class TappableGuardianIdol : Tappable
{
	[Serializable]
	public struct IdolActivationSound
	{
		public AudioClip activation;

		public AudioClip loop;
	}

	[Serializable]
	public struct StageActivatedObject
	{
		public GameObject[] objects;

		public int min;

		public int max;

		public void UpdateActiveState(int stage)
		{
			bool active = stage >= min && stage <= max;
			GameObject[] array = objects;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].SetActive(active);
			}
		}
	}

	[SerializeField]
	private GorillaGuardianZoneManager zoneManager;

	[SerializeField]
	private float floatDuration = 2f;

	[SerializeField]
	private float fallDuration = 1.5f;

	[SerializeField]
	private float inactiveDuration = 2f;

	[SerializeField]
	private float activationDuration = 1f;

	[SerializeField]
	private float activeHeight = 1f;

	[SerializeField]
	private bool knockbackOnTrigger;

	[SerializeField]
	private bool knockbackOnLand = true;

	[SerializeField]
	private bool knockbackOnActivate;

	[SerializeField]
	private Vector3 fallStartOffset = new Vector3(3f, 20f, 3f);

	[SerializeField]
	private ParticleSystem trailFX;

	[SerializeField]
	private ParticleSystem tapFX;

	[SerializeField]
	private GameObject explodeFX;

	[SerializeField]
	private GameObject startFallFX;

	[SerializeField]
	private GameObject landedFX;

	[SerializeField]
	private GameObject activatedFX;

	[SerializeField]
	private SphereCollider tapCollision;

	[SerializeField]
	private GameObject idolVisualRoot;

	[SerializeField]
	private GameObject idolMeshRoot;

	[SerializeField]
	private AnimationCurve bulgeCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.5f, 1f), new Keyframe(1f, 0f));

	[SerializeField]
	private float bulgeScale = 1.1f;

	[SerializeField]
	private AudioSource _audio;

	[SerializeField]
	private AudioClip[] _descentSound;

	[SerializeField]
	private AudioClip[] _activateSound;

	[SerializeField]
	private IdolActivationSound[] _activationStageSounds;

	[SerializeField]
	private StageActivatedObject[] _stageActivatedObjects;

	[Header("Look Around")]
	[SerializeField]
	private Transform _lookRoot;

	[SerializeField]
	private float _lookInterval = 10f;

	[SerializeField]
	private float _baseLookRate = 1f;

	[SerializeField]
	private float _randomLookChance = 0.25f;

	private Coroutine _lookRoutine;

	private Vector3 transitionPos;

	private Vector3 finalPos;

	private int _activationState;

	private Coroutine _activationRoutine;

	private float _colliderBaseRadius;

	private bool _zoneIsActive = true;

	public bool isActivationReady;

	private float requiredTapDistance = 3f;

	public bool isChangingPositions { get; private set; }

	protected override void OnEnable()
	{
		base.OnEnable();
		_colliderBaseRadius = tapCollision.radius;
	}

	protected override void OnDisable()
	{
		base.OnDisable();
		isChangingPositions = false;
		_activationState = -1;
		isActivationReady = true;
		tapCollision.radius = _colliderBaseRadius;
	}

	public void OnZoneActiveStateChanged(bool zoneActive)
	{
		_zoneIsActive = zoneActive;
		idolVisualRoot.SetActive(_zoneIsActive);
	}

	public override void OnTapLocal(float tapStrength, float tapTime, PhotonMessageInfoWrapped info)
	{
		if (info.Sender.IsLocal)
		{
			zoneManager.SetScaleCenterPoint(base.transform);
		}
		if (isChangingPositions || !zoneManager.IsZoneValid())
		{
			return;
		}
		if (PhotonNetwork.LocalPlayer.IsMasterClient && VRRigCache.Instance.TryGetVrrig(info.Sender, out var playerRig))
		{
			if (Vector3.Magnitude(playerRig.Rig.transform.position - base.transform.position) > requiredTapDistance + Mathf.Epsilon)
			{
				return;
			}
			zoneManager.IdolWasTapped(info.Sender);
		}
		if (!zoneManager.IsPlayerGuardian(info.Sender))
		{
			tapFX.Play();
		}
	}

	public void SetPosition(Vector3 position)
	{
		base.transform.position = position + new Vector3(0f, activeHeight, 0f);
		UpdateStageActivatedObjects();
		_audio.GTPlayOneShot(_activateSound, _audio.volume);
		StartCoroutine(Unshrink());
		IEnumerator Unshrink()
		{
			float lerpVal = 0f;
			float growDuration = 0.5f;
			while (lerpVal < 1f)
			{
				lerpVal += Time.deltaTime / growDuration;
				float num = Mathf.Lerp(0f, 1f, AnimationCurves.EaseOutQuad.Evaluate(lerpVal));
				idolMeshRoot.transform.localScale = Vector3.one * num;
				tapCollision.radius = _colliderBaseRadius * num;
				yield return null;
			}
		}
	}

	public void MovePositions(Vector3 finalPosition)
	{
		if (!isChangingPositions)
		{
			transitionPos = finalPosition + fallStartOffset;
			finalPos = finalPosition;
			StartCoroutine(TransitionToNextIdol());
		}
	}

	public void UpdateActivationProgress(float rawProgress, bool progressing)
	{
		isActivationReady = !progressing;
		if (rawProgress <= 0f && !progressing)
		{
			if (_activationState >= 0)
			{
				if (_activationRoutine != null)
				{
					StopCoroutine(_activationRoutine);
					_activationRoutine = null;
				}
				idolMeshRoot.transform.localScale = Vector3.one;
			}
			_activationState = -1;
			UpdateStageActivatedObjects();
			_audio.GTStop();
			return;
		}
		int num = (int)rawProgress;
		progressing &= _activationStageSounds.Length > num;
		if (_activationState != num && progressing)
		{
			if (_activationRoutine != null)
			{
				StopCoroutine(_activationRoutine);
			}
			_activationRoutine = StartCoroutine(ShowActivationEffect());
			_activationState = num;
			UpdateStageActivatedObjects();
			IdolActivationSound idolActivationSound = _activationStageSounds[num];
			_audio.GTPlayOneShot(idolActivationSound.activation, _audio.volume);
			_audio.clip = idolActivationSound.loop;
			_audio.loop = true;
			_audio.GTPlay();
		}
	}

	public void StartLookingAround()
	{
		if (_lookRoutine != null)
		{
			StopCoroutine(_lookRoutine);
		}
		_lookRoutine = StartCoroutine(DoLookingAround());
	}

	public void StopLookingAround()
	{
		if (_lookRoutine != null)
		{
			StopCoroutine(_lookRoutine);
			_lookRoot.localRotation = Quaternion.identity;
			_lookRoutine = null;
		}
	}

	private IEnumerator DoLookingAround()
	{
		float nextLookTime = Time.time;
		Quaternion _lookDirection = _lookRoot.rotation;
		yield return null;
		while (true)
		{
			if (Time.time >= nextLookTime)
			{
				PickLookTarget();
			}
			_lookRoot.rotation = Quaternion.Slerp(_lookRoot.rotation, _lookDirection, Time.deltaTime * Mathf.Max(1f, (float)_activationState * _baseLookRate));
			yield return null;
		}
		Transform GetClosestPlayerPosition()
		{
			if (UnityEngine.Random.value < _randomLookChance)
			{
				return null;
			}
			Vector3 position = base.transform.position;
			float num = float.MaxValue;
			Transform result = null;
			foreach (RigContainer activeRigContainer in VRRigCache.ActiveRigContainers)
			{
				if (!activeRigContainer.IsNull())
				{
					bool flag = activeRigContainer.Creator == zoneManager.CurrentGuardian;
					float num2 = Vector3.SqrMagnitude(activeRigContainer.transform.position - position) * (float)((!flag) ? 1 : 100);
					if (num2 < num)
					{
						num = num2;
						result = activeRigContainer.transform;
					}
				}
			}
			return result;
		}
		void PickLookTarget()
		{
			Transform transform = GetClosestPlayerPosition();
			_lookDirection = (transform ? Quaternion.LookRotation(transform.position - _lookRoot.position) : Quaternion.Euler(UnityEngine.Random.Range(-15, 15), _lookRoot.rotation.eulerAngles.y + (float)UnityEngine.Random.Range(-45, 45), 0f));
			SetLookTime();
		}
		void SetLookTime()
		{
			nextLookTime = Time.time + _lookInterval / (float)_activationState * 0.5f + UnityEngine.Random.value;
		}
	}

	private void UpdateStageActivatedObjects()
	{
		StageActivatedObject[] stageActivatedObjects = _stageActivatedObjects;
		foreach (StageActivatedObject stageActivatedObject in stageActivatedObjects)
		{
			stageActivatedObject.UpdateActiveState(_activationState);
		}
	}

	private IEnumerator ShowActivationEffect()
	{
		float bulgeDuration = 1f;
		float lerpVal = 0f;
		while (lerpVal < 1f)
		{
			lerpVal += Time.deltaTime / bulgeDuration;
			float num = Mathf.Lerp(1f, bulgeScale, bulgeCurve.Evaluate(lerpVal));
			idolMeshRoot.transform.localScale = Vector3.one * num;
			tapCollision.radius = _colliderBaseRadius * num;
			yield return null;
		}
		_activationRoutine = null;
	}

	private IEnumerator TransitionToNextIdol()
	{
		isChangingPositions = true;
		_audio.GTStop();
		if (knockbackOnTrigger)
		{
			zoneManager.TriggerIdolKnockback();
		}
		if ((bool)explodeFX)
		{
			ObjectPools.instance.Instantiate(explodeFX, base.transform.position);
		}
		UpdateActivationProgress(-1f, progressing: false);
		idolMeshRoot.SetActive(value: false);
		tapCollision.enabled = false;
		base.transform.position = transitionPos;
		yield return new WaitForSeconds(floatDuration);
		idolMeshRoot.SetActive(value: true);
		tapCollision.enabled = true;
		if ((bool)startFallFX)
		{
			ObjectPools.instance.Instantiate(startFallFX, transitionPos);
		}
		_audio.GTPlayOneShot(_descentSound);
		trailFX.Play();
		float fall = 0f;
		Vector3 startPos = transitionPos;
		Vector3 destinationPos = finalPos;
		while (fall < fallDuration)
		{
			fall += Time.deltaTime;
			base.transform.position = Vector3.Lerp(startPos, destinationPos, fall / fallDuration);
			yield return null;
		}
		base.transform.position = destinationPos;
		trailFX.Stop();
		if ((bool)landedFX)
		{
			ObjectPools.instance.Instantiate(landedFX, destinationPos);
		}
		if (knockbackOnLand)
		{
			zoneManager.TriggerIdolKnockback();
		}
		yield return new WaitForSeconds(inactiveDuration);
		_audio.GTPlayOneShot(_activateSound, _audio.volume);
		float activateLerp = 0f;
		startPos = finalPos;
		destinationPos = finalPos + new Vector3(0f, activeHeight, 0f);
		AnimationCurve animCurve = AnimationCurves.EaseInOutQuad;
		while (activateLerp < 1f)
		{
			activateLerp = Mathf.Clamp01(activateLerp + Time.deltaTime / activationDuration);
			base.transform.position = Vector3.Lerp(startPos, destinationPos, animCurve.Evaluate(activateLerp));
			yield return null;
		}
		if ((bool)activatedFX)
		{
			ObjectPools.instance.Instantiate(activatedFX, base.transform.position);
		}
		if (knockbackOnActivate)
		{
			zoneManager.TriggerIdolKnockback();
		}
		isChangingPositions = false;
	}

	private float EaseInOut(float input)
	{
		if (!(input < 0.5f))
		{
			return 1f - Mathf.Pow(-2f * input + 2f, 3f) / 2f;
		}
		return 4f * input * input * input;
	}
}
