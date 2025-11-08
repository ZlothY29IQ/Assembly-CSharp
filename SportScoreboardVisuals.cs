using GorillaTag.Sports;
using UnityEngine;

public class SportScoreboardVisuals : MonoBehaviour
{
	[SerializeField]
	public MaterialUVOffsetListSetter score1s;

	[SerializeField]
	public MaterialUVOffsetListSetter score10s;

	[SerializeField]
	private int TeamIndex;

	private void Awake()
	{
		SportScoreboard.Instance.RegisterTeamVisual(TeamIndex, this);
	}
}
