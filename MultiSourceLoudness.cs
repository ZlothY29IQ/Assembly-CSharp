using System.Collections.Generic;
using UnityEngine;

public class MultiSourceLoudness : MonoBehaviour, ISpeakerLoudness
{
	[Tooltip("Sources to combine. Each must be a component implementing ISpeakerLoudness (e.g. AudioSourceLoudness or GorillaSpeakerLoudness).")]
	[SerializeField]
	private List<MonoBehaviour> sourceBehaviours = new List<MonoBehaviour>();

	[Tooltip("Scales the combined loudness before it is reported. Use to tune the result onto the scale GorillaMouthFlap's volume thresholds expect.")]
	[SerializeField]
	private float loudnessMultiplier = 1f;

	private readonly List<ISpeakerLoudness> sources = new List<ISpeakerLoudness>();

	public bool IsSpeaking { get; private set; }

	public float Loudness { get; private set; }

	public bool IsMicEnabled { get; private set; }

	private void Awake()
	{
		RebuildSources();
	}

	public void RebuildSources()
	{
		sources.Clear();
		for (int i = 0; i < sourceBehaviours.Count; i++)
		{
			if (sourceBehaviours[i] is ISpeakerLoudness speakerLoudness && speakerLoudness != this)
			{
				sources.Add(speakerLoudness);
			}
		}
	}

	public void AddSource(ISpeakerLoudness source)
	{
		if (source != null && source != this && !sources.Contains(source))
		{
			sources.Add(source);
		}
	}

	public void RemoveSource(ISpeakerLoudness source)
	{
		sources.Remove(source);
	}

	private void Update()
	{
		bool flag = false;
		bool flag2 = false;
		float num = 0f;
		for (int i = 0; i < sources.Count; i++)
		{
			ISpeakerLoudness speakerLoudness = sources[i];
			flag2 |= speakerLoudness.IsMicEnabled;
			if (speakerLoudness.IsSpeaking)
			{
				flag = true;
				if (speakerLoudness.Loudness > num)
				{
					num = speakerLoudness.Loudness;
				}
			}
		}
		IsSpeaking = flag;
		IsMicEnabled = flag2;
		Loudness = (flag ? (num * loudnessMultiplier) : 0f);
	}
}
