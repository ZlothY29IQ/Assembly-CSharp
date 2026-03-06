using GorillaExtensions;
using UnityEngine;

namespace GorillaTag;

[DefaultExecutionOrder(1250)]
public class HeartRingCosmetic : MonoBehaviour
{
	public GameObject effects;

	[SerializeField]
	private bool isHauntedVoiceChanger;

	[SerializeField]
	private float hauntedVoicePitch = 0.75f;

	[AssignInCorePrefab]
	public float effectActivationRadius = 0.15f;

	private readonly Vector3 headToMouthOffset = new Vector3(0f, 0.0208f, 0.171f);

	private VRRig ownerRig;

	private Transform ownerHead;

	private ParticleSystem particleSystem;

	private AudioSource audioSource;

	private float maxEmissionRate;

	private float maxVolume;

	private const float emissionFadeTime = 0.1f;

	private const float volumeFadeTime = 2f;

	protected void Awake()
	{
		Application.quitting += delegate
		{
			base.enabled = false;
		};
	}

	protected void OnEnable()
	{
		particleSystem = effects.GetComponentInChildren<ParticleSystem>(includeInactive: true);
		audioSource = effects.GetComponentInChildren<AudioSource>(includeInactive: true);
		ownerRig = GetComponentInParent<VRRig>();
		bool flag = (base.enabled = ownerRig != null && ownerRig.head != null && ownerRig.head.rigTarget != null);
		effects.SetActive(flag);
		if (!flag)
		{
			Debug.LogError("Disabling HeartRingCosmetic. Could not find owner head. Scene path: " + base.transform.GetPath(), this);
			return;
		}
		ownerHead = ((ownerRig != null) ? ownerRig.head.rigTarget.transform : base.transform);
		maxEmissionRate = particleSystem.emission.rateOverTime.constant;
		maxVolume = audioSource.volume;
	}

	protected void LateUpdate()
	{
		Transform obj = base.transform;
		Vector3 position = obj.position;
		float x = obj.lossyScale.x;
		float num = effectActivationRadius * effectActivationRadius * x * x;
		bool flag = (ownerHead.TransformPoint(headToMouthOffset) - position).sqrMagnitude < num;
		ParticleSystem.EmissionModule emission = particleSystem.emission;
		emission.rateOverTime = Mathf.Lerp(emission.rateOverTime.constant, flag ? maxEmissionRate : 0f, Time.deltaTime / 0.1f);
		audioSource.volume = Mathf.Lerp(audioSource.volume, flag ? maxVolume : 0f, Time.deltaTime / 2f);
		ownerRig.UsingHauntedRing = isHauntedVoiceChanger && flag;
		if (ownerRig.UsingHauntedRing)
		{
			ownerRig.HauntedRingVoicePitch = hauntedVoicePitch;
		}
	}
}
