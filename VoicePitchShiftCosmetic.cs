using UnityEngine;

public class VoicePitchShiftCosmetic : MonoBehaviour
{
	private const float MIN = 2f / 3f;

	private const float MAX = 1.5f;

	[Tooltip("If multiple cosmetics are equipped that modify the pitch, their values will be averaged. Has a minimum pitch of 2/3 and a maximum of 1.5.")]
	[Range(2f / 3f, 1.5f)]
	[SerializeField]
	private float pitch = 1f;

	private VRRig myRig;

	public float Pitch
	{
		get
		{
			return pitch;
		}
		set
		{
			value = Mathf.Clamp(value, 2f / 3f, 1.5f);
			if (myRig == null)
			{
				pitch = value;
			}
			else if (!Mathf.Approximately(value, pitch))
			{
				pitch = value;
				myRig.SetPitchShiftCosmeticsDirty();
			}
		}
	}

	private void OnEnable()
	{
		if ((object)myRig == null)
		{
			myRig = GetComponentInParent<VRRig>();
		}
		if (myRig != null)
		{
			myRig.PitchShiftCosmetics.Add(this);
			myRig.SetPitchShiftCosmeticsDirty();
		}
	}

	private void OnDisable()
	{
		if (myRig != null)
		{
			myRig.PitchShiftCosmetics.Remove(this);
			myRig.SetPitchShiftCosmeticsDirty();
		}
	}
}
