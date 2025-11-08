using UnityEngine;

namespace GorillaTagScripts.ObstacleCourse;

public class ObstacleCourse : MonoBehaviour
{
	public enum RaceState
	{
		Started,
		Waiting,
		Finished
	}

	public WinnerScoreboard scoreboard;

	private RigContainer winnerRig;

	public ObstacleCourseZoneTrigger[] zoneTriggers;

	[HideInInspector]
	public RaceState currentState;

	[SerializeField]
	private ParticleSystem confettiParticle;

	[SerializeField]
	private Renderer bannerRenderer;

	[SerializeField]
	private TappableBell TappableBell;

	[SerializeField]
	private AudioSource audioSource;

	[SerializeField]
	private float cooldownTime = 20f;

	public GameObject leftGate;

	public GameObject rightGate;

	private int numPlayersOnCourse;

	private float startTime;

	public int winnerActorNumber { get; private set; }

	private void Awake()
	{
		numPlayersOnCourse = 0;
		for (int i = 0; i < zoneTriggers.Length; i++)
		{
			ObstacleCourseZoneTrigger obstacleCourseZoneTrigger = zoneTriggers[i];
			if (!(obstacleCourseZoneTrigger == null))
			{
				obstacleCourseZoneTrigger.OnPlayerTriggerEnter += OnPlayerEnterZone;
				obstacleCourseZoneTrigger.OnPlayerTriggerExit += OnPlayerExitZone;
			}
		}
		TappableBell.OnTapped += OnEndLineTrigger;
	}

	private void OnDestroy()
	{
		for (int i = 0; i < zoneTriggers.Length; i++)
		{
			ObstacleCourseZoneTrigger obstacleCourseZoneTrigger = zoneTriggers[i];
			if (!(obstacleCourseZoneTrigger == null))
			{
				obstacleCourseZoneTrigger.OnPlayerTriggerEnter -= OnPlayerEnterZone;
				obstacleCourseZoneTrigger.OnPlayerTriggerExit -= OnPlayerExitZone;
			}
		}
		TappableBell.OnTapped -= OnEndLineTrigger;
	}

	private void Start()
	{
		RestartTimer(playFx: false);
	}

	public void InvokeUpdate()
	{
		if (NetworkSystem.Instance.InRoom && ObstacleCourseManager.Instance.IsMine && currentState == RaceState.Finished && Time.time - startTime >= cooldownTime)
		{
			RestartTimer();
		}
	}

	public void OnPlayerEnterZone(Collider other)
	{
		if (ObstacleCourseManager.Instance.IsMine)
		{
			numPlayersOnCourse++;
		}
	}

	public void OnPlayerExitZone(Collider other)
	{
		if (ObstacleCourseManager.Instance.IsMine)
		{
			numPlayersOnCourse--;
		}
	}

	private void RestartTimer(bool playFx = true)
	{
		UpdateState(RaceState.Started, playFx);
	}

	private void EndRace()
	{
		UpdateState(RaceState.Finished);
		startTime = Time.time;
	}

	public void PlayWinningEffects()
	{
		if ((bool)confettiParticle)
		{
			confettiParticle.Play();
		}
		if ((bool)bannerRenderer)
		{
			UberShader.BaseColor.SetValue(bannerRenderer.material, winnerRig?.Rig.playerColor);
		}
		audioSource.GTPlay();
	}

	public void OnEndLineTrigger(VRRig rig)
	{
		if (ObstacleCourseManager.Instance.IsMine && currentState == RaceState.Started)
		{
			winnerActorNumber = rig.creator.ActorNumber;
			winnerRig = rig.rigContainer;
			EndRace();
		}
	}

	public void Deserialize(int _winnerActorNumber, RaceState _currentState)
	{
		if (!ObstacleCourseManager.Instance.IsMine)
		{
			winnerActorNumber = _winnerActorNumber;
			VRRigCache.Instance.TryGetVrrig(NetworkSystem.Instance.GetPlayer(winnerActorNumber), out winnerRig);
			UpdateState(_currentState);
		}
	}

	private void UpdateState(RaceState state, bool playFX = true)
	{
		currentState = state;
		scoreboard.UpdateBoard(winnerRig?.Rig.playerNameVisible, currentState);
		if (currentState == RaceState.Finished)
		{
			PlayWinningEffects();
		}
		else if (currentState == RaceState.Started && (bool)bannerRenderer)
		{
			UberShader.BaseColor.SetValue(bannerRenderer.material, Color.white);
		}
		UpdateStartingGate();
	}

	private void UpdateStartingGate()
	{
		if (currentState == RaceState.Finished)
		{
			leftGate.transform.RotateAround(leftGate.transform.position, Vector3.up, 90f);
			rightGate.transform.RotateAround(rightGate.transform.position, Vector3.up, -90f);
		}
		else if (currentState == RaceState.Started)
		{
			leftGate.transform.RotateAround(leftGate.transform.position, Vector3.up, -90f);
			rightGate.transform.RotateAround(rightGate.transform.position, Vector3.up, 90f);
		}
	}
}
