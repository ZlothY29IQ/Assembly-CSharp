using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class MenagerieCritter : MonoBehaviour, IHoldableObject, IEyeScannable
{
	public enum MenagerieCritterState
	{
		Donating,
		Displaying
	}

	public CritterVisuals visuals;

	public Collider bodyCollider;

	[Header("Feedback")]
	public CrittersAnim heldAnimation;

	public AudioClip grabbedHaptics;

	public float grabbedHapticsStrength = 1f;

	public GameObject grabbedFX;

	private CrittersAnim _currentAnim;

	private float _currentAnimTime;

	private Transform _animRoot;

	private Vector3 _bodyScale;

	public MenagerieCritterState currentState = MenagerieCritterState.Displaying;

	private CritterConfiguration _critterConfiguration;

	private Menagerie.CritterData _critterData;

	private MenagerieSlot _slot;

	private List<GorillaGrabber> activeGrabbers = new List<GorillaGrabber>();

	private GameObject heldBy;

	private bool isHeld;

	private bool isHeldLeftHand;

	public Action<MenagerieCritter> OnReleased;

	private KeyValueStringPair[] eyeScanData = new KeyValueStringPair[6];

	public Menagerie.CritterData CritterData => _critterData;

	public MenagerieSlot Slot
	{
		get
		{
			return _slot;
		}
		set
		{
			if (!(value == _slot))
			{
				if ((bool)_slot && _slot.critter == this)
				{
					_slot.critter = null;
				}
				_slot = value;
				if ((bool)_slot)
				{
					_slot.critter = this;
				}
			}
		}
	}

	public bool TwoHanded => false;

	int IEyeScannable.scannableId => base.gameObject.GetInstanceID();

	Vector3 IEyeScannable.Position => bodyCollider.bounds.center;

	Bounds IEyeScannable.Bounds => bodyCollider.bounds;

	IList<KeyValueStringPair> IEyeScannable.Entries => BuildEyeScannerData();

	public event Action OnDataChange;

	private void Update()
	{
		UpdateAnimation();
	}

	public void ApplyCritterData(Menagerie.CritterData critterData)
	{
		_critterData = critterData;
		_critterConfiguration = _critterData.GetConfiguration();
		_critterData.instance = this;
		_critterData.GetConfiguration().ApplyVisualsTo(visuals, generateAppearance: false);
		visuals.SetAppearance(_critterData.appearance);
		_animRoot = visuals.bodyRoot;
		_bodyScale = _animRoot.localScale;
		PlayAnimation(heldAnimation, UnityEngine.Random.value);
	}

	private void PlayAnimation(CrittersAnim anim, float time = 0f)
	{
		_currentAnim = anim;
		_currentAnimTime = time;
		if (_currentAnim == null)
		{
			_animRoot.localPosition = Vector3.zero;
			_animRoot.localRotation = Quaternion.identity;
			_animRoot.localScale = _bodyScale;
		}
	}

	private void UpdateAnimation()
	{
		if (_currentAnim != null)
		{
			_currentAnimTime += Time.deltaTime * _currentAnim.playSpeed;
			_currentAnimTime %= 1f;
			float num = _currentAnim.squashAmount.Evaluate(_currentAnimTime);
			float z = _currentAnim.forwardOffset.Evaluate(_currentAnimTime);
			float x = _currentAnim.horizontalOffset.Evaluate(_currentAnimTime);
			float y = _currentAnim.verticalOffset.Evaluate(_currentAnimTime);
			_animRoot.localPosition = Vector3.Scale(_bodyScale, new Vector3(x, y, z));
			float num2 = 1f - num;
			num2 *= 0.5f;
			num2 += 1f;
			_animRoot.localScale = Vector3.Scale(_bodyScale, new Vector3(num2, num, num2));
		}
	}

	public void OnHover(InteractionPoint pointHovered, GameObject hoveringHand)
	{
	}

	public void OnGrab(InteractionPoint pointGrabbed, GameObject grabbingHand)
	{
		isHeld = true;
		isHeldLeftHand = grabbingHand == EquipmentInteractor.instance.leftHand;
		if ((bool)grabbedHaptics)
		{
			CrittersManager.PlayHaptics(grabbedHaptics, grabbedHapticsStrength, isHeldLeftHand);
		}
		if ((bool)grabbedFX)
		{
			grabbedFX.SetActive(value: true);
		}
		EquipmentInteractor.instance.UpdateHandEquipment(this, isHeldLeftHand);
		base.transform.parent = grabbingHand.transform;
		isHeld = true;
		heldBy = grabbingHand;
		this.OnDataChange?.Invoke();
	}

	public bool OnRelease(DropZone zoneReleased, GameObject releasingHand)
	{
		if (EquipmentInteractor.instance.rightHandHeldEquipment == this && releasingHand != EquipmentInteractor.instance.rightHand)
		{
			return false;
		}
		if (EquipmentInteractor.instance.leftHandHeldEquipment == this && releasingHand != EquipmentInteractor.instance.leftHand)
		{
			return false;
		}
		if ((bool)grabbedHaptics)
		{
			CrittersManager.StopHaptics(isHeldLeftHand);
		}
		if ((bool)grabbedFX)
		{
			grabbedFX.SetActive(value: false);
		}
		EquipmentInteractor.instance.UpdateHandEquipment(null, isHeldLeftHand);
		isHeld = false;
		isHeldLeftHand = false;
		OnReleased?.Invoke(this);
		this.OnDataChange?.Invoke();
		ResetToTransform();
		return true;
	}

	public void ResetToTransform()
	{
		base.transform.parent = _slot.transform;
		base.transform.localPosition = Vector3.zero;
		base.transform.localRotation = quaternion.identity;
	}

	public void DropItemCleanup()
	{
	}

	public void OnEnable()
	{
		EyeScannerMono.Register(this);
	}

	public void OnDisable()
	{
		EyeScannerMono.Unregister(this);
	}

	private IList<KeyValueStringPair> BuildEyeScannerData()
	{
		eyeScanData[0] = new KeyValueStringPair("Name", _critterConfiguration.critterName);
		eyeScanData[1] = new KeyValueStringPair("Type", _critterConfiguration.animalType.ToString());
		eyeScanData[2] = new KeyValueStringPair("Temperament", _critterConfiguration.behaviour.temperament);
		eyeScanData[3] = new KeyValueStringPair("Habitat", _critterConfiguration.biome.GetHabitatDescription());
		eyeScanData[4] = new KeyValueStringPair("Size", visuals.Appearance.size.ToString("0.00"));
		eyeScanData[5] = new KeyValueStringPair("State", GetCurrentStateName());
		return eyeScanData;
	}

	private string GetCurrentStateName()
	{
		if (!isHeld)
		{
			return "Content";
		}
		return "Happy";
	}
}
