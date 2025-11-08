using GorillaExtensions;
using UnityEngine;

public class ShadeJumpscare : MonoBehaviour
{
	[SerializeField]
	private Transform shadeTransform;

	[SerializeField]
	private float animationTime;

	[SerializeField]
	private float shadeRotationSpeed = 1f;

	[SerializeField]
	private AnimationCurve shadeHeightFunction;

	[SerializeField]
	private AnimationCurve shadeScaleFunction;

	[SerializeField]
	private AnimationCurve shadeYScaleMultFunction;

	[SerializeField]
	private AnimationCurve soundVolumeFunction;

	[SerializeField]
	private AudioClip[] audioClips;

	private AudioSource audioSource;

	private float startTime;

	private float startAngle;

	private void Awake()
	{
		audioSource = GetComponent<AudioSource>();
	}

	private void OnEnable()
	{
		startTime = Time.time;
		startAngle = Random.value * 360f;
		audioSource.clip = audioClips.GetRandomItem();
		audioSource.GTPlay();
	}

	private void Update()
	{
		float num = Time.time - startTime;
		float time = num / animationTime;
		shadeTransform.SetPositionAndRotation(base.transform.position + new Vector3(0f, shadeHeightFunction.Evaluate(time), 0f), Quaternion.Euler(0f, startAngle + num * shadeRotationSpeed, 0f));
		float num2 = shadeScaleFunction.Evaluate(time);
		shadeTransform.localScale = new Vector3(num2, num2 * shadeYScaleMultFunction.Evaluate(time), num2);
		audioSource.volume = soundVolumeFunction.Evaluate(time);
	}
}
