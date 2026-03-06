using UnityEngine;

public class VoiceShiftCosmetic : MonoBehaviour
{
	private const float PITCH_MIN = 2f / 3f;

	private const float PITCH_MAX = 1.5f;

	private const float VOLUME_MIN = 0f;

	private const float VOLUME_MAX = 1f;

	[SerializeField]
	private bool modifyPitch = true;

	[SerializeField]
	private bool modifyVolume = true;

	[Range(2f / 3f, 1.5f)]
	[SerializeField]
	private float shiftedPitch = 1.5f;

	[Range(0f, 1f)]
	[SerializeField]
	private float shiftedVolume = 1f;

	private float pitch = 1f;

	private float volume = 1f;

	private bool isShifted;

	private VRRig myRig;

	public bool ModifyPitch => modifyPitch;

	public bool ModifyVolume => modifyVolume;

	public bool IsShifted => isShifted;

	public float Pitch
	{
		get
		{
			return pitch;
		}
		set
		{
			if (modifyPitch)
			{
				float num = Mathf.Clamp(value, 2f / 3f, 1.5f);
				pitch = num;
				myRig?.SetVoiceShiftCosmeticsDirty();
			}
		}
	}

	public float Volume
	{
		get
		{
			return volume;
		}
		set
		{
			if (modifyVolume)
			{
				float num = Mathf.Clamp(value, 0f, 1f);
				volume = num;
				myRig?.SetVoiceShiftCosmeticsDirty();
			}
		}
	}

	private void OnEnable()
	{
		if ((object)myRig == null)
		{
			myRig = GetComponentInParent<VRRig>();
		}
		if (!(myRig == null))
		{
			myRig.VoiceShiftCosmetics.Add(this);
			myRig.SetVoiceShiftCosmeticsDirty();
		}
	}

	private void OnDisable()
	{
		if (!(myRig == null))
		{
			myRig.VoiceShiftCosmetics.Remove(this);
			myRig.SetVoiceShiftCosmeticsDirty();
		}
	}

	public void StartVoiceShift()
	{
		if (!isShifted)
		{
			isShifted = true;
			if (modifyPitch)
			{
				Pitch = shiftedPitch;
			}
			if (modifyVolume)
			{
				Volume = shiftedVolume;
			}
		}
	}

	public void StopVoiceShift()
	{
		if (isShifted)
		{
			isShifted = false;
			myRig?.SetVoiceShiftCosmeticsDirty();
		}
	}

	public void ToggleVoiceShift()
	{
		if (isShifted)
		{
			StopVoiceShift();
		}
		else
		{
			StartVoiceShift();
		}
	}
}
