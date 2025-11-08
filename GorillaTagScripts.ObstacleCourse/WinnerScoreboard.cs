using TMPro;
using UnityEngine;

namespace GorillaTagScripts.ObstacleCourse;

public class WinnerScoreboard : MonoBehaviour
{
	public string raceStarted = "RACE STARTED!";

	public string raceLoading = "RACE LOADING...";

	[SerializeField]
	private TextMeshPro output;

	public void UpdateBoard(string winner, ObstacleCourse.RaceState _currentState)
	{
		if ((object)output != null)
		{
			switch (_currentState)
			{
			case ObstacleCourse.RaceState.Started:
				Debug.Log(raceStarted);
				output.text = raceStarted;
				break;
			case ObstacleCourse.RaceState.Waiting:
				Debug.Log(raceLoading);
				output.text = raceLoading;
				break;
			case ObstacleCourse.RaceState.Finished:
				Debug.Log(winner + " WON!!");
				output.text = winner + " WON!!";
				break;
			}
		}
	}
}
