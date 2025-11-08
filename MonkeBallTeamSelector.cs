using UnityEngine;

public class MonkeBallTeamSelector : MonoBehaviour
{
	public int teamId;

	[SerializeField]
	private GorillaPressableButton _setTeamButton;

	public void Awake()
	{
		_setTeamButton.onPressButton.AddListener(OnSelect);
	}

	public void OnDestroy()
	{
		_setTeamButton.onPressButton.RemoveListener(OnSelect);
	}

	private void OnSelect()
	{
		MonkeBallGame.Instance.RequestSetTeam(teamId);
	}
}
