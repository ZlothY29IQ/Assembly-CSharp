using UnityEngine;

public class RigDuplicationZone : MonoBehaviour
{
	public delegate void RigDuplicationZoneAction(RigDuplicationZone z);

	private RigDuplicationZone otherZone;

	[SerializeField]
	private string id;

	private bool playerInZone;

	private Vector3 offsetToOtherZone;

	public string Id => id;

	public Vector3 VisualOffsetForRigs
	{
		get
		{
			if (!otherZone.playerInZone)
			{
				return Vector3.zero;
			}
			return offsetToOtherZone;
		}
	}

	public bool IsApplyingDisplacement => otherZone.playerInZone;

	public static event RigDuplicationZoneAction OnEnabled;

	private void OnEnable()
	{
		OnEnabled += RigDuplicationZone_OnEnabled;
		if (RigDuplicationZone.OnEnabled != null)
		{
			RigDuplicationZone.OnEnabled(this);
		}
	}

	private void OnDisable()
	{
		OnEnabled -= RigDuplicationZone_OnEnabled;
	}

	private void RigDuplicationZone_OnEnabled(RigDuplicationZone z)
	{
		if (!(z == this) && !(z.id != id))
		{
			setOtherZone(z);
			z.setOtherZone(this);
		}
	}

	private void setOtherZone(RigDuplicationZone z)
	{
		otherZone = z;
		offsetToOtherZone = z.transform.position - base.transform.position;
	}

	private void OnTriggerEnter(Collider other)
	{
		VRRig component = other.GetComponent<VRRig>();
		if (!(component == null))
		{
			if (component.isLocal)
			{
				playerInZone = true;
			}
			else
			{
				component.SetDuplicationZone(this);
			}
		}
	}

	private void OnTriggerExit(Collider other)
	{
		VRRig component = other.GetComponent<VRRig>();
		if (!(component == null))
		{
			if (component.isLocal)
			{
				playerInZone = false;
			}
			else
			{
				component.ClearDuplicationZone(this);
			}
		}
	}
}
