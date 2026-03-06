using UnityEngine;
using UnityEngine.Serialization;

public class ZoneDef : MonoBehaviour
{
	public GTZone zoneId;

	[FormerlySerializedAs("subZoneType")]
	[FormerlySerializedAs("subZone")]
	public GTSubZone subZoneId;

	public GroupJoinZoneA groupZone;

	public GroupJoinZoneB groupZoneB;

	public int trackStayIntervalSec = 30;

	[Space]
	public bool trackEnter = true;

	public bool trackExit;

	public bool trackStay = true;

	public GroupJoinZoneAB groupZoneAB => new GroupJoinZoneAB
	{
		a = groupZone,
		b = groupZoneB
	};

	public bool IsSameZone(ZoneDef other)
	{
		if (other == null)
		{
			return false;
		}
		if (zoneId == other.zoneId)
		{
			return subZoneId == other.subZoneId;
		}
		return false;
	}
}
