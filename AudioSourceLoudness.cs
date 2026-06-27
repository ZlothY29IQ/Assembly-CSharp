using UnityEngine;

public class AudioSourceLoudness : MonoBehaviour, ISpeakerLoudness
{
	[SerializeField]
	private AudioSource audioSource;

	[Tooltip("Number of output samples averaged per frame to compute loudness.")]
	[SerializeField]
	private int sampleWindow = 256;

	private float loudness;

	private float[] sampleBuffer;

	public bool IsSpeaking
	{
		get
		{
			if (audioSource != null)
			{
				return audioSource.isPlaying;
			}
			return false;
		}
	}

	public float Loudness => loudness;

	public bool IsMicEnabled => true;

	private void Awake()
	{
		if (audioSource == null)
		{
			audioSource = GetComponent<AudioSource>();
		}
		sampleBuffer = new float[Mathf.Max(1, sampleWindow)];
	}

	private void Update()
	{
		if (audioSource == null || !audioSource.isPlaying)
		{
			loudness = 0f;
			return;
		}
		audioSource.GetOutputData(sampleBuffer, 0);
		float num = 0f;
		for (int i = 0; i < sampleBuffer.Length; i++)
		{
			num += Mathf.Abs(sampleBuffer[i]);
		}
		loudness = num / (float)sampleBuffer.Length;
	}
}
