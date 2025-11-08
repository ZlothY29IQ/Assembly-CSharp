using UnityEngine;

public class DebugTestGrabber : MonoBehaviour
{
	public bool isGrabbing;

	public bool setIsGrabbing;

	public bool setRelease;

	public Collider[] colliders = new Collider[50];

	public bool isLeft;

	public float grabRadius = 0.05f;

	public Transform transformToFollow;

	public GorillaVelocityEstimator estimator;

	public CrittersGrabber grabber;

	public CrittersActorGrabber otherHand;

	private bool isHandGrabbingDisabled;

	private float grabDuration = 0.3f;

	private float remainingGrabDuration;

	private void Awake()
	{
		if (grabber == null)
		{
			grabber = GetComponentInChildren<CrittersGrabber>();
		}
	}

	private void LateUpdate()
	{
		if (transformToFollow != null)
		{
			base.transform.rotation = transformToFollow.rotation;
			base.transform.position = transformToFollow.position;
		}
		if (!(grabber == null))
		{
			if (!isGrabbing && setIsGrabbing)
			{
				setIsGrabbing = false;
				isGrabbing = true;
				remainingGrabDuration = grabDuration;
			}
			else if (isGrabbing && setRelease)
			{
				setRelease = false;
				isGrabbing = false;
				DoRelease();
			}
			if (isGrabbing && remainingGrabDuration > 0f)
			{
				remainingGrabDuration -= Time.deltaTime;
				DoGrab();
			}
		}
	}

	private void DoGrab()
	{
		grabber.grabbing = true;
		int num = Physics.OverlapSphereNonAlloc(base.transform.position, grabRadius, colliders, LayerMask.GetMask("GorillaInteractable"));
		if (num <= 0)
		{
			return;
		}
		for (int i = 0; i < num; i++)
		{
			CrittersActor componentInParent = colliders[i].GetComponentInParent<CrittersActor>();
			if (!(componentInParent == null) && componentInParent.usesRB && componentInParent.CanBeGrabbed(grabber))
			{
				isHandGrabbingDisabled = true;
				if (componentInParent.equipmentStorable)
				{
					componentInParent.localCanStore = true;
				}
				componentInParent.GrabbedBy(grabber);
				grabber.grabbedActors.Add(componentInParent);
				remainingGrabDuration = 0f;
				break;
			}
		}
	}

	private void DoRelease()
	{
		grabber.grabbing = false;
		for (int num = grabber.grabbedActors.Count - 1; num >= 0; num--)
		{
			CrittersActor crittersActor = grabber.grabbedActors[num];
			crittersActor.Released(keepWorldPosition: true, crittersActor.transform.rotation, crittersActor.transform.position, estimator.linearVelocity);
			if (num < grabber.grabbedActors.Count)
			{
				grabber.grabbedActors.RemoveAt(num);
			}
		}
		if (isHandGrabbingDisabled)
		{
			isHandGrabbingDisabled = false;
		}
	}
}
