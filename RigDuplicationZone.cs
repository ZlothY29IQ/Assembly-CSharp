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
			SetOtherZone(z);
			z.SetOtherZone(this);
		}
	}

	private void SetOtherZone(RigDuplicationZone z)
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

	public Vector3 GetVisualOffsetForRigs(Vector3 cachedOffset)
	{
		if (otherZone == null)
		{
			Debug.LogError("RigDuplicationZone doesn't have an other zone!", base.gameObject);
			return cachedOffset;
		}
		if (!otherZone.playerInZone)
		{
			return cachedOffset;
		}
		return offsetToOtherZone + cachedOffset;
	}
}
