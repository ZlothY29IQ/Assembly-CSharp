public interface ISpeakerLoudness
{
	bool IsSpeaking { get; }

	float Loudness { get; }

	bool IsMicEnabled { get; }
}
