using System;
using GorillaExtensions;
using Photon.Pun;
using UnityEngine;

public class ElfLauncher : MonoBehaviour
{
	[SerializeField]
	protected TransferrableObject parentHoldable;

	[SerializeField]
	private TransferrableObjectHoldablePart_Crank[] cranks;

	[SerializeField]
	private float crankShootThreshold = 360f;

	[SerializeField]
	private float crankClickThreshold = 30f;

	[SerializeField]
	private Transform muzzle;

	[SerializeField]
	private GameObject elfProjectilePrefab;

	protected int elfProjectileHash;

	[SerializeField]
	protected float muzzleVelocity = 10f;

	[SerializeField]
	private SoundBankPlayer crankClickAudio;

	[SerializeField]
	protected SoundBankPlayer shootAudio;

	[SerializeField]
	private float shootHapticStrength;

	[SerializeField]
	private float shootHapticDuration;

	private RubberDuckEvents _events;

	private float currentShootCrankAmount;

	private float currentClickCrankAmount;

	private NetPlayer m_player;

	private void OnEnable()
	{
		if (_events == null)
		{
			_events = base.gameObject.GetOrAddComponent<RubberDuckEvents>();
			NetPlayer netPlayer = ((parentHoldable.myOnlineRig != null) ? parentHoldable.myOnlineRig.creator : ((!(parentHoldable.myRig != null)) ? null : ((parentHoldable.myRig.creator != null) ? parentHoldable.myRig.creator : NetworkSystem.Instance.LocalPlayer)));
			if (netPlayer != null)
			{
				m_player = netPlayer;
				_events.Init(netPlayer);
			}
			else
			{
				Debug.LogError("Failed to get a reference to the Photon Player needed to hook up the cosmetic event");
			}
		}
		if (_events != null)
		{
			_events.Activate += new Action<int, int, object[], PhotonMessageInfoWrapped>(ShootShared);
		}
	}

	private void OnDisable()
	{
		if (_events != null)
		{
			_events.Activate -= new Action<int, int, object[], PhotonMessageInfoWrapped>(ShootShared);
			_events.Dispose();
			_events = null;
			m_player = null;
		}
	}

	private void Awake()
	{
		_events = GetComponent<RubberDuckEvents>();
		elfProjectileHash = PoolUtils.GameObjHashCode(elfProjectilePrefab);
		TransferrableObjectHoldablePart_Crank[] array = cranks;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].SetOnCrankedCallback(OnCranked);
		}
	}

	private void OnCranked(float deltaAngle)
	{
		currentShootCrankAmount += deltaAngle;
		if (Mathf.Abs(currentShootCrankAmount) > crankShootThreshold)
		{
			currentShootCrankAmount = 0f;
			Shoot();
		}
		currentClickCrankAmount += deltaAngle;
		if (Mathf.Abs(currentClickCrankAmount) > crankClickThreshold)
		{
			currentClickCrankAmount = 0f;
			crankClickAudio.Play();
		}
	}

	private void Shoot()
	{
		if (parentHoldable.IsLocalObject())
		{
			GorillaTagger.Instance.StartVibration(forLeftController: true, shootHapticStrength, shootHapticDuration);
			GorillaTagger.Instance.StartVibration(forLeftController: false, shootHapticStrength, shootHapticDuration);
			if (PhotonNetwork.InRoom)
			{
				_events.Activate.RaiseAll(muzzle.transform.position, muzzle.transform.forward);
			}
			else
			{
				ShootShared(muzzle.transform.position, muzzle.transform.forward);
			}
		}
	}

	private void ShootShared(int sender, int target, object[] args, PhotonMessageInfoWrapped info)
	{
		if (args.Length == 2 && sender == target)
		{
			VRRig ownerRig = parentHoldable.ownerRig;
			if (info.senderID == ownerRig.creator.ActorNumber && args.Length == 2 && args[0] is Vector3 v && args[1] is Vector3 v2 && v.IsValid(10000f) && v2.IsValid(10000f) && FXSystem.CheckCallSpam(ownerRig.fxSettings, 4, info.SentServerTime) && ownerRig.IsPositionInRange(v, 6f))
			{
				ShootShared(v, v2);
			}
		}
	}

	protected virtual void ShootShared(Vector3 origin, Vector3 direction)
	{
		shootAudio.Play();
		Vector3 lossyScale = base.transform.lossyScale;
		GameObject obj = ObjectPools.instance.Instantiate(elfProjectileHash);
		obj.transform.position = origin;
		obj.transform.rotation = Quaternion.LookRotation(direction);
		obj.transform.localScale = lossyScale;
		obj.GetComponent<Rigidbody>().linearVelocity = direction * muzzleVelocity * lossyScale.x;
	}
}
