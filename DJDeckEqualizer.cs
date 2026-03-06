using UnityEngine;

public class DJDeckEqualizer : MonoBehaviour
{
	[SerializeField]
	private MeshRenderer display;

	[SerializeField]
	private AnimationCurve[] redTrackCurves;

	[SerializeField]
	private AnimationCurve[] greenTrackCurves;

	[SerializeField]
	private AudioSource[] redTracks;

	[SerializeField]
	private AudioSource[] greenTracks;

	private Material material;

	[SerializeField]
	private string inputColorProperty;

	private ShaderHashId inputColorHash;

	private void Start()
	{
		inputColorHash = inputColorProperty;
		material = display.material;
	}

	private void Update()
	{
		Color value = new Color
		{
			r = 0.25f,
			g = 0.25f,
			b = 0.5f
		};
		for (int i = 0; i < redTracks.Length; i++)
		{
			AudioSource audioSource = redTracks[i];
			if (audioSource.isPlaying)
			{
				value.r = Mathf.Lerp(0.25f, 1f, redTrackCurves[i].Evaluate(audioSource.time));
				break;
			}
		}
		for (int j = 0; j < greenTracks.Length; j++)
		{
			AudioSource audioSource2 = greenTracks[j];
			if (audioSource2.isPlaying)
			{
				value.g = Mathf.Lerp(0.25f, 1f, greenTrackCurves[j].Evaluate(audioSource2.time));
				break;
			}
		}
		material.SetColor(inputColorHash, value);
	}
}
