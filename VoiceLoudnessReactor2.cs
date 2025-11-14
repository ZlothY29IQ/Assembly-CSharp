using GorillaTag.Cosmetics;
using UnityEngine;

public class VoiceLoudnessReactor2 : MonoBehaviour, ITickSystemTick
{
	[Tooltip("Multiply the microphone input by this value. A good default is 15.")]
	public float sensitivity = 15f;

	public ContinuousPropertyArray continuousProperties;

	private GorillaSpeakerLoudness gsl;

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
		TickSystem<object>.AddTickCallback(this);
	}

	private void OnDisable()
	{
		TickSystem<object>.RemoveTickCallback(this);
	}

	public void Tick()
	{
		continuousProperties.ApplyAll(Loudness);
	}
}
