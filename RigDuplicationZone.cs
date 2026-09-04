using UnityEngine;

public class RigDuplicationZone : RigDisplacementZone
{
	public delegate void RigDuplicationZoneAction(RigDuplicationZone z);

	private RigDuplicationZone otherZone;

	[SerializeField]
	private string id;

	[Tooltip("Leave blank for a regular duplication zone. For a portal effect, set this to the zone from which players looking at this zone should see its contents swapped")]
	[SerializeField]
	private RigDuplicationZone seeSwapFromZone;

	public string Id => id;

	public static event RigDuplicationZoneAction OnEnabled;

	private void OnEnable()
	{
		OnEnabled += RigDuplicationZone_OnEnabled;
		if (RigDuplicationZone.OnEnabled != null)
		{
			RigDuplicationZone.OnEnabled(this);
		}
	}

	protected override void OnDisable()
	{
		base.OnDisable();
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
		if (seeSwapFromZone == null)
		{
			seeSwapFromZone = otherZone;
		}
	}

	public override Vector3 GetDisplacementForRig(VRRig rig, Vector3 undisplacedPosition)
	{
		if (seeSwapFromZone == null)
		{
			Debug.LogError("RigDuplicationZone doesn't have an other zone!", base.gameObject);
			return Vector3.zero;
		}
		if (seeSwapFromZone.localPlayerInZone)
		{
			return seeSwapFromZone.transform.TransformPoint(base.transform.InverseTransformPoint(undisplacedPosition)) - undisplacedPosition;
		}
		return Vector3.zero;
	}

	public override bool IsDisplacingRig(VRRig rig)
	{
		return otherZone.localPlayerInZone;
	}
}
