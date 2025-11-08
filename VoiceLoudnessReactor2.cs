using GorillaTag.Cosmetics;
using UnityEngine;

public class VoiceLoudnessReactor2 : MonoBehaviour, ITickSystemTick
{
	[Tooltip("How quickly the internal loudness approaches the real loudness. A low value will take a long time to match the true volume but will be more resistant to fluctuations. Note: If the value is too high, you may notice some jerkiness in the output because the underlying GorillaSpeakerLoudness doesn't update every frame.")]
	public float responsiveness = 5f;

	[Tooltip("Multiply the microphone input by this value. A good default is 15.")]
	public float sensitivity = 15f;

	public ContinuousPropertyArray continuousProperties;

	private GorillaSpeakerLoudness gsl;

	private float smoothedLoudness;

	private float Loudness => gsl.Loudness * sensitivity;

	public bool TickRunning { get; set; }

	private void OnEnable()
	{
		if (continuousProperties.Count == 0)
		{
			return;
		}
		if (gsl == null)
		{
			gsl = GetComponentInParent<GorillaSpeakerLoudness>(includeInactive: true);
			if (gsl == null)
			{
				GorillaTagger componentInParent = GetComponentInParent<GorillaTagger>();
				if (componentInParent != null)
				{
					gsl = componentInParent.offlineVRRig.GetComponent<GorillaSpeakerLoudness>();
					if (gsl == null)
					{
						return;
					}
				}
			}
		}
		smoothedLoudness = Loudness;
		TickSystem<object>.AddTickCallback(this);
	}

	private void OnDisable()
	{
		TickSystem<object>.RemoveTickCallback(this);
	}

	public void Tick()
	{
		float t = 1f - Mathf.Exp((0f - responsiveness) * Time.deltaTime);
		smoothedLoudness = Mathf.Lerp(smoothedLoudness, Loudness, t);
		continuousProperties.ApplyAll(smoothedLoudness);
	}
}
