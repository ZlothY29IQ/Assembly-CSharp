using UnityEngine;

public class MicrophoneCosmetic : MonoBehaviour
{
	[SerializeField]
	private Transform mouthTransform;

	[SerializeField]
	private Vector2 mouthProximityRampRange = new Vector2(0.6f, 0.3f);

	private AudioSource audioSource;

	private float[] zero = new float[1];

	private void Awake()
	{
		audioSource = GetComponent<AudioSource>();
		if (!Application.isEditor && Application.platform == RuntimePlatform.Android && Microphone.devices.Length != 0)
		{
			audioSource.clip = Microphone.Start(Microphone.devices[0], loop: true, 10, 16000);
		}
		else
		{
			int sampleRate = AudioSettings.GetConfiguration().sampleRate;
			audioSource.clip = Microphone.Start(null, loop: true, 10, sampleRate);
		}
		audioSource.loop = true;
	}

	private void OnEnable()
	{
		int num = ((Application.platform == RuntimePlatform.Android && Microphone.devices.Length != 0) ? Microphone.GetPosition(Microphone.devices[0]) : Microphone.GetPosition(null));
		num -= 10;
		if ((float)num < 0f)
		{
			num = audioSource.clip.samples + num - 1;
		}
		audioSource.GTPlay();
		audioSource.timeSamples = num;
	}

	private void OnDisable()
	{
		audioSource.GTStop();
	}

	private void Update()
	{
		Vector3 vector = mouthTransform.position - base.transform.position;
		float sqrMagnitude = vector.sqrMagnitude;
		float num = 0f;
		if (sqrMagnitude < mouthProximityRampRange.x * mouthProximityRampRange.x)
		{
			float magnitude = vector.magnitude;
			num = Mathf.InverseLerp(mouthProximityRampRange.x, mouthProximityRampRange.y, magnitude);
		}
		if (num != audioSource.volume)
		{
			audioSource.volume = num;
		}
		int num2 = (audioSource.timeSamples -= 10);
		if ((float)num2 < 0f)
		{
			num2 = audioSource.clip.samples + num2 - 1;
		}
		audioSource.clip.SetData(zero, num2);
	}

	private void OnAudioFilterRead(float[] data, int channels)
	{
	}
}
