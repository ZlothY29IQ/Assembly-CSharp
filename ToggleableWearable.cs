using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

public class ToggleableWearable : MonoBehaviour
{
	public Renderer[] renderers;

	public Animator[] animators;

	public float animationTransitionDuration = 1f;

	[Tooltip("Whether the wearable state is toggled on by default.")]
	public bool startOn;

	[Tooltip("AudioSource to play toggle sounds.")]
	public AudioSource audioSource;

	[Tooltip("Sound to play when toggled on.")]
	public AudioClip toggleOnSound;

	[Tooltip("Sound to play when toggled off.")]
	public AudioClip toggleOffSound;

	[Tooltip("Layer to check for trigger sphere collisions.")]
	public LayerMask layerMask;

	[Tooltip("Radius of the trigger sphere.")]
	public float triggerRadius = 0.2f;

	[Tooltip("Position in local space to move the trigger sphere.")]
	public Vector3 triggerOffset = Vector3.zero;

	[Tooltip("This is to determine what bit to change in VRRig.WearablesPackedStates.")]
	public VRRig.WearablePackedStateSlots assignedSlot;

	[Header("Vibration")]
	public float turnOnVibrationDuration = 0.05f;

	public float turnOnVibrationStrength = 0.2f;

	public float turnOffVibrationDuration = 0.05f;

	public float turnOffVibrationStrength = 0.2f;

	private VRRig ownerRig;

	private bool ownerIsLocal;

	private bool isOn;

	[SerializeField]
	private Vector2 toggleCooldownRange = new Vector2(0.2f, 0.2f);

	private bool hasAudioSource;

	private readonly Collider[] colliders = new Collider[1];

	private int framesSinceCooldownAndExitingVolume;

	private float toggleCooldownTimer;

	private int assignedSlotBitIndex;

	private static readonly int animParam_Progress = Animator.StringToHash("Progress");

	private float progress;

	[SerializeField]
	private bool oneShot;

	[SerializeField]
	[Tooltip("Seconds before reverting to its default state, as defined by 'Start On.' A value of 0 or less means never.")]
	private float resetTimer;

	private float toggleTimer;

	protected void Awake()
	{
		ownerRig = GetComponentInParent<VRRig>();
		if (ownerRig == null)
		{
			GorillaTagger componentInParent = GetComponentInParent<GorillaTagger>();
			if (componentInParent != null)
			{
				ownerRig = componentInParent.offlineVRRig;
				ownerIsLocal = ownerRig != null;
			}
		}
		if (ownerRig == null)
		{
			Debug.LogError("TriggerToggler: Disabling cannot find VRRig.");
			base.enabled = false;
			return;
		}
		Renderer[] array = renderers;
		foreach (Renderer renderer in array)
		{
			if (renderer == null)
			{
				Debug.LogError("TriggerToggler: Disabling because a renderer is null.");
				base.enabled = false;
				break;
			}
			renderer.enabled = startOn;
		}
		hasAudioSource = audioSource != null;
		assignedSlotBitIndex = (int)assignedSlot;
		if (oneShot)
		{
			toggleCooldownRange.x += animationTransitionDuration;
			toggleCooldownRange.y += animationTransitionDuration;
		}
	}

	protected void LateUpdate()
	{
		if (ownerIsLocal)
		{
			toggleCooldownTimer -= Time.deltaTime;
			Transform transform = base.transform;
			if (Physics.OverlapSphereNonAlloc(transform.TransformPoint(triggerOffset), triggerRadius * transform.lossyScale.x, colliders, layerMask) > 0 && toggleCooldownTimer < 0f)
			{
				XRController componentInParent = colliders[0].GetComponentInParent<XRController>();
				if (componentInParent != null)
				{
					LocalToggle(componentInParent.controllerNode == XRNode.LeftHand, playAudio: true, playHaptics: true);
				}
				toggleCooldownTimer = Random.Range(toggleCooldownRange.x, toggleCooldownRange.y);
				toggleTimer = 0f;
			}
			if (resetTimer > 0f)
			{
				toggleTimer += Time.deltaTime;
				if (toggleTimer > resetTimer && startOn != isOn)
				{
					LocalToggle(isLeftHand: false, playAudio: true, playHaptics: false);
					toggleTimer = 0f;
				}
			}
		}
		else
		{
			bool flag = (ownerRig.WearablePackedStates & (1 << assignedSlotBitIndex)) != 0;
			if (isOn != flag)
			{
				SharedSetState(flag, playAudio: true);
			}
		}
		if (oneShot)
		{
			if (isOn)
			{
				progress = Mathf.MoveTowards(progress, 1f, Time.deltaTime / animationTransitionDuration);
				if (progress == 1f)
				{
					if (ownerIsLocal)
					{
						LocalToggle(isLeftHand: false, playAudio: false, playHaptics: false);
					}
					else
					{
						SharedSetState(state: false, playAudio: false);
					}
					progress = 0f;
				}
			}
		}
		else
		{
			progress = Mathf.MoveTowards(progress, isOn ? 1f : 0f, Time.deltaTime / animationTransitionDuration);
		}
		Animator[] array = animators;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].SetFloat(animParam_Progress, progress);
		}
	}

	private void LocalToggle(bool isLeftHand, bool playAudio, bool playHaptics)
	{
		ownerRig.WearablePackedStates ^= 1 << assignedSlotBitIndex;
		SharedSetState((ownerRig.WearablePackedStates & (1 << assignedSlotBitIndex)) != 0, playAudio);
		if (playHaptics && (bool)GorillaTagger.Instance)
		{
			GorillaTagger.Instance.StartVibration(isLeftHand, isOn ? turnOnVibrationDuration : turnOffVibrationDuration, isOn ? turnOnVibrationStrength : turnOffVibrationStrength);
		}
	}

	private void SharedSetState(bool state, bool playAudio)
	{
		isOn = state;
		Renderer[] array = renderers;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].enabled = isOn;
		}
		if (!playAudio || !hasAudioSource)
		{
			return;
		}
		AudioClip audioClip = (isOn ? toggleOnSound : toggleOffSound);
		if (!(audioClip == null))
		{
			if (oneShot)
			{
				audioSource.clip = audioClip;
				audioSource.GTPlay();
			}
			else
			{
				audioSource.GTPlayOneShot(audioClip);
			}
		}
	}
}
