using BoingKit;
using UnityEngine;

public class RotationStepper : MonoBehaviour
{
	public enum ModeEnum
	{
		Fixed,
		Random
	}

	public ModeEnum Mode;

	[ConditionalField("Mode", ModeEnum.Fixed, null, null, null, null, null)]
	public float Angle = 25f;

	public float Frequency;

	private float m_phase;

	public void OnEnable()
	{
		m_phase = 0f;
		Random.InitState(0);
	}

	public void Update()
	{
		m_phase += Frequency * Time.deltaTime;
		switch (Mode)
		{
		default:
			return;
		case ModeEnum.Fixed:
			base.transform.rotation = Quaternion.Euler(0f, 0f, (Mathf.Repeat(m_phase, 2f) < 1f) ? (-25f) : 25f);
			return;
		case ModeEnum.Random:
			break;
		}
		while (m_phase >= 1f)
		{
			Random.InitState(Time.frameCount);
			base.transform.rotation = Random.rotationUniform;
			m_phase -= 1f;
		}
	}
}
