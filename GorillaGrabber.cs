using GorillaLocomotion;
using GorillaLocomotion.Gameplay;
using UnityEngine;
using UnityEngine.XR;

public class GorillaGrabber : MonoBehaviour
{
	private GTPlayer player;

	[SerializeField]
	private XRNode xrNode = XRNode.LeftHand;

	private AudioSource audioSource;

	private Transform currentGrabbedTransform;

	private Vector3 localGrabbedPosition;

	private IGorillaGrabable currentGrabbable;

	[SerializeField]
	private float grabRadius = 0.015f;

	[SerializeField]
	private float breakDistance = 0.3f;

	[SerializeField]
	private float hapticStrength = 0.2f;

	private float hapticStrengthActual = 0.2f;

	[SerializeField]
	private float hapticDecay;

	[SerializeField]
	private ParticleSystem gripEffects;

	private Collider[] grabCastResults = new Collider[32];

	private float grabTimeStamp;

	[SerializeField]
	private float coyoteTimeDuration = 0.25f;

	public bool isGrabbing => currentGrabbable != null;

	public XRNode XrNode => xrNode;

	public bool IsLeftHand => XrNode == XRNode.LeftHand;

	public bool IsRightHand => XrNode == XRNode.RightHand;

	public GTPlayer Player => player;

	private void Start()
	{
		hapticStrengthActual = hapticStrength;
		audioSource = GetComponent<AudioSource>();
		player = GetComponentInParent<GTPlayer>();
		if (!player)
		{
			Debug.LogWarning("Gorilla Grabber Component has no player in hierarchy. Disabling this Gorilla Grabber");
			GetComponent<GorillaGrabber>().enabled = false;
		}
	}

	public void CheckGrabber(bool initiateGrab)
	{
		bool grabMomentary = ControllerInputPoller.GetGrabMomentary(xrNode);
		bool grabRelease = ControllerInputPoller.GetGrabRelease(xrNode);
		if (currentGrabbable != null && (grabRelease || GrabDistanceOverCheck()))
		{
			Ungrab();
		}
		if (grabMomentary)
		{
			grabTimeStamp = Time.time;
		}
		if (initiateGrab && currentGrabbable == null)
		{
			currentGrabbable = TryGrab(Time.time - grabTimeStamp < coyoteTimeDuration);
		}
		if (currentGrabbable != null && hapticStrengthActual > 0f)
		{
			GorillaTagger.Instance.DoVibration(xrNode, hapticStrengthActual, Time.deltaTime);
			hapticStrengthActual -= hapticDecay * Time.deltaTime;
		}
	}

	private bool GrabDistanceOverCheck()
	{
		if (!(currentGrabbedTransform == null))
		{
			return Vector3.Distance(base.transform.position, currentGrabbedTransform.TransformPoint(localGrabbedPosition)) > breakDistance;
		}
		return true;
	}

	internal void Ungrab(IGorillaGrabable specificGrabbable = null)
	{
		if (specificGrabbable == null || specificGrabbable == currentGrabbable)
		{
			currentGrabbable.OnGrabReleased(this);
			PlayerGameEvents.DroppedObject(currentGrabbable.name);
			currentGrabbable = null;
			gripEffects.Stop();
			hapticStrengthActual = hapticStrength;
		}
	}

	private IGorillaGrabable TryGrab(bool momentary)
	{
		IGorillaGrabable gorillaGrabable = null;
		Debug.DrawRay(base.transform.position, base.transform.forward * (grabRadius * player.scale), Color.blue, 1f);
		int num = Physics.OverlapSphereNonAlloc(base.transform.position, grabRadius * player.scale, grabCastResults);
		float num2 = float.MaxValue;
		for (int i = 0; i < num; i++)
		{
			if (grabCastResults[i].TryGetComponent<IGorillaGrabable>(out var component))
			{
				float num3 = Vector3.Distance(base.transform.position, FindClosestPoint(grabCastResults[i], base.transform.position));
				if (num3 < num2)
				{
					num2 = num3;
					gorillaGrabable = component;
				}
			}
		}
		if (gorillaGrabable != null && (!gorillaGrabable.MomentaryGrabOnly() || momentary) && gorillaGrabable.CanBeGrabbed(this))
		{
			gorillaGrabable.OnGrabbed(this, out currentGrabbedTransform, out localGrabbedPosition);
			PlayerGameEvents.GrabbedObject(gorillaGrabable.name);
		}
		if (gorillaGrabable != null && !gorillaGrabable.CanBeGrabbed(this))
		{
			gorillaGrabable = null;
		}
		return gorillaGrabable;
	}

	private Vector3 FindClosestPoint(Collider collider, Vector3 position)
	{
		if (collider is MeshCollider && !(collider as MeshCollider).convex)
		{
			return position;
		}
		return collider.ClosestPoint(position);
	}

	public void Inject(Transform currentGrabbableTransform, Vector3 localGrabbedPosition)
	{
		if (currentGrabbable != null)
		{
			Ungrab();
		}
		if (currentGrabbableTransform != null)
		{
			currentGrabbable = currentGrabbableTransform.GetComponent<IGorillaGrabable>();
			currentGrabbedTransform = currentGrabbableTransform;
			this.localGrabbedPosition = localGrabbedPosition;
			currentGrabbable.OnGrabbed(this, out currentGrabbedTransform, out localGrabbedPosition);
		}
	}
}
