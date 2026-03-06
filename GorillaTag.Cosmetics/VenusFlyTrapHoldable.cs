using System;
using GorillaExtensions;
using Photon.Pun;
using UnityEngine;

namespace GorillaTag.Cosmetics;

[RequireComponent(typeof(TransferrableObject))]
public class VenusFlyTrapHoldable : MonoBehaviour, ITickSystemTick
{
	private enum VenusState
	{
		Closed,
		Open,
		Closing,
		Opening
	}

	[SerializeField]
	private GameObject lipA;

	[SerializeField]
	private GameObject lipB;

	[SerializeField]
	private Vector3 targetRotationA;

	[SerializeField]
	private Vector3 targetRotationB;

	[SerializeField]
	private float closedDuration = 3f;

	[SerializeField]
	private float speed = 2f;

	[SerializeField]
	private UnityLayer layers;

	[SerializeField]
	private TriggerEventNotifier triggerEventNotifier;

	[SerializeField]
	private float hapticStrength = 0.5f;

	[SerializeField]
	private float hapticDuration = 0.1f;

	[SerializeField]
	private GameObject bug;

	[SerializeField]
	private AudioSource audioSource;

	[SerializeField]
	private AudioClip closingAudio;

	[SerializeField]
	private AudioClip openingAudio;

	[SerializeField]
	private AudioClip flyLoopingAudio;

	private CallLimiter callLimiter = new CallLimiter(10, 2f);

	private float closedStartedTime;

	private VenusState state;

	private Quaternion localRotA;

	private Quaternion localRotB;

	private RubberDuckEvents _events;

	private TransferrableObject transferrableObject;

	public bool TickRunning { get; set; }

	private void Awake()
	{
		transferrableObject = GetComponent<TransferrableObject>();
	}

	private void OnEnable()
	{
		TickSystem<object>.AddCallbackTarget(this);
		triggerEventNotifier.TriggerEnterEvent += TriggerEntered;
		state = VenusState.Open;
		localRotA = lipA.transform.localRotation;
		localRotB = lipB.transform.localRotation;
		if (_events == null)
		{
			_events = base.gameObject.GetOrAddComponent<RubberDuckEvents>();
			NetPlayer netPlayer = ((transferrableObject.myOnlineRig != null) ? transferrableObject.myOnlineRig.creator : ((transferrableObject.myRig != null) ? (transferrableObject.myRig.creator ?? NetworkSystem.Instance.LocalPlayer) : null));
			if (netPlayer != null)
			{
				_events.Init(netPlayer);
			}
		}
		if (_events != null)
		{
			_events.Activate += new Action<int, int, object[], PhotonMessageInfoWrapped>(OnTriggerEvent);
		}
	}

	private void OnDisable()
	{
		TickSystem<object>.RemoveCallbackTarget(this);
		triggerEventNotifier.TriggerEnterEvent -= TriggerEntered;
		if (_events != null)
		{
			_events.Activate -= new Action<int, int, object[], PhotonMessageInfoWrapped>(OnTriggerEvent);
			_events.Dispose();
			_events = null;
		}
	}

	public void Tick()
	{
		if (transferrableObject.InHand() && (bool)audioSource && !audioSource.isPlaying && flyLoopingAudio != null)
		{
			audioSource.clip = flyLoopingAudio;
			audioSource.GTPlay();
		}
		if (!transferrableObject.InHand() && (bool)audioSource && audioSource.isPlaying)
		{
			audioSource.GTStop();
		}
		if (state == VenusState.Open)
		{
			return;
		}
		if (state == VenusState.Closed && Time.time - closedStartedTime >= closedDuration)
		{
			UpdateState(VenusState.Opening);
			if ((bool)audioSource && openingAudio != null)
			{
				audioSource.GTPlayOneShot(openingAudio);
			}
		}
		if (state == VenusState.Closing)
		{
			SmoothRotation(isClosing: true);
		}
		else if (state == VenusState.Opening)
		{
			SmoothRotation(isClosing: false);
		}
	}

	private void SmoothRotation(bool isClosing)
	{
		if (isClosing)
		{
			Quaternion quaternion = Quaternion.Euler(targetRotationB);
			lipB.transform.localRotation = Quaternion.Lerp(lipB.transform.localRotation, quaternion, Time.deltaTime * speed);
			Quaternion quaternion2 = Quaternion.Euler(targetRotationA);
			lipA.transform.localRotation = Quaternion.Lerp(lipA.transform.localRotation, quaternion2, Time.deltaTime * speed);
			if (Quaternion.Angle(lipB.transform.localRotation, quaternion) < 1f && Quaternion.Angle(lipA.transform.localRotation, quaternion2) < 1f)
			{
				lipB.transform.localRotation = quaternion;
				lipA.transform.localRotation = quaternion2;
				UpdateState(VenusState.Closed);
			}
		}
		else
		{
			lipB.transform.localRotation = Quaternion.Lerp(lipB.transform.localRotation, localRotB, Time.deltaTime * speed / 2f);
			lipA.transform.localRotation = Quaternion.Lerp(lipA.transform.localRotation, localRotA, Time.deltaTime * speed / 2f);
			if (Quaternion.Angle(lipB.transform.localRotation, localRotB) < 1f && Quaternion.Angle(lipA.transform.localRotation, localRotA) < 1f)
			{
				lipB.transform.localRotation = localRotB;
				lipA.transform.localRotation = localRotA;
				UpdateState(VenusState.Open);
			}
		}
	}

	private void UpdateState(VenusState newState)
	{
		state = newState;
		if (state == VenusState.Closed)
		{
			closedStartedTime = Time.time;
		}
	}

	private void TriggerEntered(TriggerEventNotifier notifier, Collider other)
	{
		if (state == VenusState.Open && other.gameObject.IsOnLayer(layers))
		{
			if (PhotonNetwork.InRoom && _events != null && _events.Activate != null)
			{
				_events.Activate.RaiseOthers();
			}
			OnTriggerLocal();
			GorillaTriggerColliderHandIndicator componentInChildren = other.GetComponentInChildren<GorillaTriggerColliderHandIndicator>();
			if (!(componentInChildren == null))
			{
				GorillaTagger.Instance.StartVibration(componentInChildren.isLeftHand, hapticStrength, hapticDuration);
			}
		}
	}

	private void OnTriggerEvent(int sender, int target, object[] args, PhotonMessageInfoWrapped info)
	{
		if (sender == target)
		{
			MonkeAgent.IncrementRPCCall(info, "OnTriggerEvent");
			if (callLimiter.CheckCallTime(Time.time))
			{
				OnTriggerLocal();
			}
		}
	}

	private void OnTriggerLocal()
	{
		UpdateState(VenusState.Closing);
		if ((bool)audioSource && closingAudio != null)
		{
			audioSource.GTPlayOneShot(closingAudio);
		}
	}
}
