using UnityEngine;
using UnityEngine.Serialization;

namespace GorillaTag.Cosmetics;

public class SimpleTransformAnimatorCosmetic : MonoBehaviour, ITickSystemTick
{
	public enum animatedPropertyChoices
	{
		Position,
		Rotation,
		PositionAndRotation
	}

	public enum animModes
	{
		stepToTargetPos,
		animateBounce,
		animateOneshot
	}

	private animModes animMode;

	[Tooltip("Shapes how the transform will interpolate over the course of the animation.")]
	public AnimationCurve InterpolationCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

	[SerializeField]
	[Tooltip("The object that will animate (blend) between the poses.")]
	private Transform targetTransform;

	[SerializeField]
	[Tooltip("Start pose (blend value 0).")]
	private Transform poseA;

	[SerializeField]
	[Tooltip("End pose (blend value 1).")]
	private Transform poseB;

	[FormerlySerializedAs("transitionTime")]
	[SerializeField]
	[Tooltip("Total time (in seconds) to animate fully between poses.")]
	private float animationDuration = 1f;

	[SerializeField]
	[Tooltip("Controls what aspect of the transform is affected by the blend.")]
	private animatedPropertyChoices animatedProperties = animatedPropertyChoices.PositionAndRotation;

	private bool loopAnim;

	private float posBlendCurrent;

	private float posBlendTarget;

	private bool isAnimating;

	public bool TickRunning { get; set; }

	private void DebugToggle()
	{
		Toggle();
	}

	private void DebugA()
	{
		TogglePoseA();
	}

	private void DebugB()
	{
		TogglePoseB();
	}

	private void OnEnable()
	{
		posBlendCurrent = posBlendTarget;
		UpdateTransform();
	}

	private void OnDisable()
	{
		if (TickRunning)
		{
			TickSystem<object>.RemoveCallbackTarget(this);
			TickRunning = false;
		}
	}

	private void CheckAnimationNeeded()
	{
		bool flag = false;
		bool flag2 = Mathf.Approximately(posBlendCurrent, posBlendTarget);
		switch (animMode)
		{
		case animModes.stepToTargetPos:
			flag = !flag2;
			break;
		case animModes.animateOneshot:
			flag = loopAnim || !flag2;
			break;
		}
		if (flag && !TickRunning)
		{
			TickSystem<object>.AddCallbackTarget(this);
			TickRunning = true;
			isAnimating = true;
		}
		else if (!flag && TickRunning)
		{
			TickSystem<object>.RemoveCallbackTarget(this);
			TickRunning = false;
			isAnimating = false;
		}
	}

	public void Tick()
	{
		float num = 1f / animationDuration;
		posBlendCurrent = Mathf.MoveTowards(posBlendCurrent, posBlendTarget, Time.deltaTime * num);
		switch (animMode)
		{
		}
		UpdateTransform();
		CheckAnimationNeeded();
	}

	private void UpdateTransform()
	{
		Vector3 position = targetTransform.position;
		Quaternion rotation = targetTransform.rotation;
		float t = InterpolationCurve.Evaluate(posBlendCurrent);
		if (animatedProperties == animatedPropertyChoices.Position || animatedProperties == animatedPropertyChoices.PositionAndRotation)
		{
			position = Vector3.Lerp(poseA.position, poseB.position, t);
		}
		if (animatedProperties == animatedPropertyChoices.Rotation || animatedProperties == animatedPropertyChoices.PositionAndRotation)
		{
			rotation = Quaternion.Slerp(poseA.rotation, poseB.rotation, t);
		}
		targetTransform.SetPositionAndRotation(position, rotation);
	}

	public void Toggle()
	{
		animMode = animModes.stepToTargetPos;
		posBlendTarget = ((posBlendTarget < 0.5f) ? 1f : 0f);
		CheckAnimationNeeded();
	}

	public void TogglePoseA()
	{
		animMode = animModes.stepToTargetPos;
		posBlendTarget = 0f;
		CheckAnimationNeeded();
	}

	public void TogglePoseB()
	{
		animMode = animModes.stepToTargetPos;
		posBlendTarget = 1f;
		CheckAnimationNeeded();
	}

	public void playAnimationOneshot()
	{
		animMode = animModes.animateOneshot;
		posBlendCurrent = 0f;
		posBlendTarget = 1f;
		CheckAnimationNeeded();
	}

	private void DebugPlayAnimationOneShot()
	{
		playAnimationOneshot();
	}
}
