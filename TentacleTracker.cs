using System.Collections.Generic;
using GorillaLocomotion;
using GorillaLocomotion.Climbing;
using UnityEngine;

public class TentacleTracker : MonoBehaviour
{
	[SerializeField]
	private Transform anchorPoint;

	[SerializeField]
	private Transform anchorRefPoint;

	[SerializeField]
	private Transform playerRefPoint;

	[SerializeField]
	private Animator animator;

	[SerializeField]
	private GorillaClimbable climbable;

	[SerializeField]
	private string[] testTriggers;

	private List<string> testTriggersRemaining = new List<string>();

	private bool tracking = true;

	private bool currentTargetIsLocal;

	public VRRig currentTargetRig { get; private set; }

	private void OnEnable()
	{
		tracking = true;
		testTriggersRemaining.Clear();
		testTriggersRemaining.AddRange(testTriggers);
		currentTargetRig = null;
		currentTargetIsLocal = false;
	}

	public void BeginGrab(VRRig targetRig, bool isLocalPlayer)
	{
		if (!(targetRig == null))
		{
			base.gameObject.SetActive(value: true);
			tracking = true;
			currentTargetRig = targetRig;
			currentTargetIsLocal = isLocalPlayer;
			testTriggersRemaining.Clear();
			testTriggersRemaining.AddRange(testTriggers);
		}
	}

	public void Anim_OnReachEnded()
	{
		animator.SetTrigger(testTriggersRemaining[0]);
		testTriggersRemaining.RemoveAt(0);
		if (currentTargetIsLocal)
		{
			GTPlayer.Instance.BeginClimbing(climbable, EquipmentInteractor.instance.BodyClimber);
			EquipmentInteractor.instance.BodyClimber.SetCanRelease(canRelease: false);
			tracking = false;
		}
	}

	private void Update()
	{
		if (tracking)
		{
			Vector3 vector = ((currentTargetRig != null && !currentTargetIsLocal) ? currentTargetRig.head.rigTarget.position : GTPlayer.Instance.mainCamera.transform.position);
			Vector3 vector2 = vector - anchorPoint.position;
			float magnitude = vector2.magnitude;
			if (!(magnitude < 0.001f))
			{
				Vector3 forward = vector2 / magnitude;
				Vector3 normalized = (playerRefPoint.localPosition - anchorRefPoint.localPosition).normalized;
				base.transform.rotation = Quaternion.LookRotation(forward) * Quaternion.Inverse(Quaternion.LookRotation(normalized));
				base.transform.position += vector - playerRefPoint.position;
			}
		}
	}

	public void TestDrop()
	{
		if (currentTargetRig == null || currentTargetIsLocal)
		{
			EquipmentInteractor.instance.BodyClimber.SetCanRelease(canRelease: true);
			GTPlayer.Instance.EndClimbing(EquipmentInteractor.instance.BodyClimber, startingNewClimb: false);
		}
		currentTargetRig = null;
		currentTargetIsLocal = false;
		tracking = true;
	}

	public void TestDisappear()
	{
		base.gameObject.SetActive(value: false);
	}
}
