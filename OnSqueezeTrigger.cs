using UnityEngine;
using UnityEngine.Events;

public class OnSqueezeTrigger : MonoBehaviour
{
	[SerializeField]
	private TransferrableObject myHoldable;

	[SerializeField]
	private UnityEvent onPress;

	[SerializeField]
	private UnityEvent onRelease;

	[SerializeField]
	private UnityEvent updateWhilePressed;

	private VRRig myRig;

	private bool indexFinger = true;

	private bool triggerWasDown;

	private void Start()
	{
		myRig = GetComponentInParent<VRRig>();
	}

	private void Update()
	{
		bool flag = (myHoldable.InLeftHand() ? ((indexFinger ? myRig.leftIndex.calcT : myRig.leftMiddle.calcT) > 0.5f) : (myHoldable.InRightHand() && (indexFinger ? myRig.rightIndex.calcT : myRig.rightMiddle.calcT) > 0.5f));
		if (flag != triggerWasDown)
		{
			if (flag)
			{
				onPress.Invoke();
				updateWhilePressed.Invoke();
			}
			else
			{
				onRelease.Invoke();
			}
		}
		else if (flag)
		{
			updateWhilePressed.Invoke();
		}
		triggerWasDown = flag;
	}
}
