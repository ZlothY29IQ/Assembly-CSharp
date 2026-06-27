using UnityEngine;

public class AnimationScrubber : MonoBehaviour
{
	public bool scrubberActive;

	public float animationPlaybackTime;

	public Animator targetAnimator;

	private void LateUpdate()
	{
		if (scrubberActive)
		{
			AnimatorStateInfo currentAnimatorStateInfo = targetAnimator.GetCurrentAnimatorStateInfo(0);
			AnimatorClipInfo[] currentAnimatorClipInfo = targetAnimator.GetCurrentAnimatorClipInfo(0);
			targetAnimator.Play(currentAnimatorClipInfo[0].clip.name, 0, animationPlaybackTime / currentAnimatorStateInfo.length);
		}
	}
}
