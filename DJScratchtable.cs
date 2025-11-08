using GorillaExtensions;
using Photon.Pun;
using UnityEngine;

public class DJScratchtable : MonoBehaviour
{
	[SerializeField]
	private bool isLeft;

	[SerializeField]
	private DJScratchSoundPlayer scratchPlayer;

	[SerializeField]
	private float scratchCooldown;

	[SerializeField]
	private float scratchMinAngle;

	[SerializeField]
	private AudioSource[] tracks;

	[SerializeField]
	private CosmeticFan turntableVisual;

	[SerializeField]
	private float trackDuration;

	[SerializeField]
	private float hapticStrength;

	[SerializeField]
	private float hapticDuration;

	private int lastSelectedTrack;

	private bool isPlaying;

	private bool isTouching;

	private Quaternion firstTouchRotation;

	private float lastScratchSoundAngle;

	private float cantForwardScratchUntilTimestamp;

	private float cantBackScratchUntilTimestamp;

	private float pausedUntilTimestamp;

	public void SetPlaying(bool playing)
	{
		isPlaying = playing;
	}

	private void OnTriggerStay(Collider collider)
	{
		if (!base.enabled)
		{
			return;
		}
		GorillaTriggerColliderHandIndicator componentInParent = collider.GetComponentInParent<GorillaTriggerColliderHandIndicator>();
		if (componentInParent == null)
		{
			return;
		}
		Vector3 forward = (base.transform.parent.InverseTransformPoint(collider.transform.position) - base.transform.localPosition).WithY(0f);
		float target = Mathf.Atan2(forward.z, forward.x) * 57.29578f;
		if (isTouching)
		{
			base.transform.localRotation = Quaternion.LookRotation(forward) * firstTouchRotation;
			if (isPlaying)
			{
				float num = Mathf.DeltaAngle(lastScratchSoundAngle, target);
				if (num > scratchMinAngle)
				{
					if (Time.time > cantForwardScratchUntilTimestamp)
					{
						scratchPlayer.Play(ScratchSoundType.Forward, isLeft);
						cantForwardScratchUntilTimestamp = Time.time + scratchCooldown;
						lastScratchSoundAngle = target;
						GorillaTagger.Instance.StartVibration(componentInParent.isLeftHand, hapticStrength, hapticDuration);
					}
				}
				else if (num < 0f - scratchMinAngle && Time.time > cantBackScratchUntilTimestamp)
				{
					scratchPlayer.Play(ScratchSoundType.Back, isLeft);
					cantBackScratchUntilTimestamp = Time.time + scratchCooldown;
					lastScratchSoundAngle = target;
					GorillaTagger.Instance.StartVibration(componentInParent.isLeftHand, hapticStrength, hapticDuration);
				}
			}
		}
		else
		{
			firstTouchRotation = Quaternion.Inverse(Quaternion.LookRotation(base.transform.InverseTransformPoint(collider.transform.position).WithY(0f)));
			if (isPlaying)
			{
				PauseTrack();
				scratchPlayer.Play(ScratchSoundType.Pause, isLeft);
				lastScratchSoundAngle = target;
				cantForwardScratchUntilTimestamp = Time.time + scratchCooldown;
				cantBackScratchUntilTimestamp = Time.time + scratchCooldown;
			}
		}
		isTouching = true;
	}

	private void OnTriggerExit(Collider collider)
	{
		if (base.enabled && !(collider.GetComponentInParent<GorillaTriggerColliderHandIndicator>() == null))
		{
			if (isPlaying)
			{
				ResumeTrack();
				scratchPlayer.Play(ScratchSoundType.Resume, isLeft);
			}
			isTouching = false;
		}
	}

	public void SelectTrack(int track)
	{
		lastSelectedTrack = track;
		if (track == 0)
		{
			turntableVisual.Stop();
			isPlaying = false;
		}
		else
		{
			turntableVisual.Run();
			isPlaying = true;
		}
		int num = track - 1;
		for (int i = 0; i < tracks.Length; i++)
		{
			if (num == i)
			{
				float time = (float)(PhotonNetwork.InRoom ? PhotonNetwork.Time : ((double)Time.time)) % trackDuration;
				tracks[i].Play();
				tracks[i].time = time;
			}
			else
			{
				tracks[i].Stop();
			}
		}
	}

	public void PauseTrack()
	{
		for (int i = 0; i < tracks.Length; i++)
		{
			tracks[i].Stop();
		}
		pausedUntilTimestamp = Time.time + 1f;
	}

	public void ResumeTrack()
	{
		SelectTrack(lastSelectedTrack);
		pausedUntilTimestamp = 0f;
	}
}
