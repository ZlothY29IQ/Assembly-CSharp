using TMPro;
using UnityEngine;

public class MonkeBallShotclock : MonoBehaviourTick
{
	public Renderer backboard;

	public Material[] teamMaterials;

	public Material neutralMaterial;

	public TextMeshPro timeRemainingLabel;

	private float _time;

	private int _timeInt = -1;

	public override void Tick()
	{
		if (_time >= 0f)
		{
			_time -= Time.deltaTime;
			UpdateTimeText(_time);
			if (_time < 0f)
			{
				SetBackboard(neutralMaterial);
			}
		}
	}

	public void SetTime(int teamId, float time)
	{
		_time = time;
		if (teamId == -1)
		{
			_time = 0f;
			SetBackboard(neutralMaterial);
		}
		else if (teamId >= 0 && teamId < teamMaterials.Length)
		{
			SetBackboard(teamMaterials[teamId]);
		}
		UpdateTimeText(time);
	}

	private void SetBackboard(Material teamMaterial)
	{
		if (backboard != null)
		{
			backboard.material = teamMaterial;
		}
	}

	private void UpdateTimeText(float time)
	{
		int num = Mathf.CeilToInt(time);
		if (_timeInt != num)
		{
			_timeInt = num;
			timeRemainingLabel.text = _timeInt.ToString("#00");
		}
	}
}
