using GorillaLocomotion;
using UnityEngine;

public class ElderGorilla : MonoBehaviour
{
	private const float MAX_HAND_DIST = 1f;

	private const float COOLDOWN_HAND_DIST = 1f;

	private const float VALID_HAND_DIST = 0.75f;

	private const float TIME_VALID_HEAD_HEIGHT = 1f;

	private Transform tHMD;

	private Transform tLeftHand;

	private Transform tRightHand;

	private int countValidArmDists;

	private float timeLastValidArmDist;

	private bool trackingHeadHeight;

	private float trackedHeadHeight;

	private float timerTrackedHeadHeight;

	private float savedHeadHeight = 1.5f;

	private void Update()
	{
		if (!(GTPlayer.Instance == null) && !GTPlayer.Instance.inOverlay && GTPlayer.Instance.isUserPresent)
		{
			tHMD = GTPlayer.Instance.headCollider.transform;
			tLeftHand = GTPlayer.Instance.GetControllerTransform(isLeftHand: true);
			tRightHand = GTPlayer.Instance.GetControllerTransform(isLeftHand: false);
			if (Time.time - timeLastValidArmDist > 1f)
			{
				CheckHandDistance(tLeftHand);
				CheckHandDistance(tRightHand);
			}
			CheckHeight();
			CheckMicVolume();
		}
	}

	private void CheckHandDistance(Transform hand)
	{
		float num = Vector3.Distance(hand.localPosition, tHMD.localPosition);
		if (!(num >= 1f) && num >= 0.75f)
		{
			countValidArmDists++;
			timeLastValidArmDist = Time.time;
		}
	}

	private void CheckHeight()
	{
		float y = tHMD.localPosition.y;
		if (!trackingHeadHeight)
		{
			trackedHeadHeight = y - 0.05f;
			timerTrackedHeadHeight = 0f;
		}
		else if (trackedHeadHeight < y)
		{
			trackingHeadHeight = false;
		}
		if (trackingHeadHeight)
		{
			if (timerTrackedHeadHeight >= 1f)
			{
				savedHeadHeight = y;
				trackingHeadHeight = false;
			}
			else
			{
				timerTrackedHeadHeight += Time.deltaTime;
			}
		}
	}

	private void CheckMicVolume()
	{
		_ = GorillaTagger.Instance.myRecorder.LevelMeter.CurrentPeakAmp;
		_ = 10f;
	}
}
