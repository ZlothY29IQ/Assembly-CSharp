using TMPro;
using UnityEngine;

public class MonkeBallResetGame : MonoBehaviourTick
{
	[SerializeField]
	private GorillaPressableButton _resetButton;

	public Renderer button;

	public Vector3 buttonPressOffset;

	private Vector3 _buttonOrigin = Vector3.zero;

	[Space]
	public Material[] teamMaterials;

	public Material neutralMaterial;

	public int allowedTeamId = -1;

	[SerializeField]
	private TextMeshPro _resetLabel;

	private bool _cooldown;

	private float _cooldownTimer;

	private void Awake()
	{
		_resetButton.onPressButton.AddListener(OnSelect);
		if (_resetButton == null)
		{
			_buttonOrigin = _resetButton.transform.position;
		}
	}

	public override void Tick()
	{
		if (_cooldown)
		{
			_cooldownTimer -= Time.deltaTime;
			if (_cooldownTimer <= 0f)
			{
				ToggleButton(toggle: false, -1);
				_cooldown = false;
			}
		}
	}

	public void ToggleReset(bool toggle, int teamId, bool force = false)
	{
		if (teamId >= -1 && teamId < teamMaterials.Length)
		{
			if (toggle)
			{
				ToggleButton(toggle: true, teamId);
				_cooldown = false;
			}
			else if (force)
			{
				ToggleButton(toggle: false, -1);
			}
			else
			{
				_cooldown = true;
				_cooldownTimer = 3f;
			}
		}
	}

	private void ToggleButton(bool toggle, int teamId)
	{
		_resetButton.enabled = toggle;
		allowedTeamId = teamId;
		if (!toggle || teamId == -1)
		{
			button.sharedMaterial = neutralMaterial;
		}
		else
		{
			button.sharedMaterial = teamMaterials[teamId];
		}
	}

	private void OnSelect()
	{
		MonkeBallGame.Instance.RequestResetGame();
	}
}
