using Photon.Pun;
using UnityEngine;

public class MonkeBall : MonoBehaviourTick
{
	public GameBall gameBall;

	public MeshRenderer mainRenderer;

	public Material defaultMaterial;

	public Material[] teamMaterial;

	public double restrictTeamGrabEndTime;

	public bool alreadyDropped;

	private bool _justGrabbed;

	private float _justGrabbedTimer;

	private bool _launchAfterScore;

	private float _droppedTimer;

	private bool _resyncPosition;

	private float _resyncDelay;

	private bool _visualOffset;

	private float _offsetThreshold = 0.05f;

	private float _timeOffset;

	public float maxLerpTime = 0.5f;

	public float offsetLerp = 0.2f;

	private bool _positionFailsafe = true;

	private float _positionFailsafeTimer;

	public Vector3 lastVisiblePosition;

	[SerializeField]
	private Rigidbody _rigidBody;

	private void Start()
	{
		Refresh();
	}

	public override void Tick()
	{
		UpdateVisualOffset();
		if (!PhotonNetwork.IsMasterClient)
		{
			if (_resyncPosition)
			{
				_resyncDelay -= Time.deltaTime;
				if (_resyncDelay <= 0f)
				{
					_resyncPosition = false;
					GameBallManager.Instance.RequestSetBallPosition(gameBall.id);
				}
			}
			if (_positionFailsafe)
			{
				if (base.transform.position.y < -500f || (GameBallManager.Instance.transform.position - base.transform.position).sqrMagnitude > 6400f)
				{
					if (PhotonNetwork.IsConnected)
					{
						GameBallManager.Instance.RequestSetBallPosition(gameBall.id);
					}
					else
					{
						base.transform.position = GameBallManager.Instance.transform.position;
					}
					_positionFailsafe = false;
					_positionFailsafeTimer = 3f;
				}
			}
			else
			{
				_positionFailsafeTimer -= Time.deltaTime;
				if (_positionFailsafeTimer <= 0f)
				{
					_positionFailsafe = true;
				}
			}
			return;
		}
		if (gameBall.onlyGrabTeamId != -1 && Time.timeAsDouble >= restrictTeamGrabEndTime)
		{
			MonkeBallGame.Instance.RequestRestrictBallToTeam(gameBall.id, -1);
		}
		if (AlreadyDropped())
		{
			_droppedTimer += Time.deltaTime;
			if (_droppedTimer >= 7.5f)
			{
				_droppedTimer = 0f;
				GameBallManager.Instance.RequestTeleportBall(gameBall.id, base.transform.position, base.transform.rotation, _rigidBody.linearVelocity, _rigidBody.angularVelocity);
			}
		}
		if (_justGrabbed)
		{
			_justGrabbedTimer -= Time.deltaTime;
			if (_justGrabbedTimer <= 0f)
			{
				_justGrabbed = false;
			}
		}
		if (_resyncPosition)
		{
			_resyncDelay -= Time.deltaTime;
			if (_resyncDelay <= 0f)
			{
				_resyncPosition = false;
				GameBallManager.Instance.RequestTeleportBall(gameBall.id, base.transform.position, base.transform.rotation, _rigidBody.linearVelocity, _rigidBody.angularVelocity);
			}
		}
		if (_positionFailsafe)
		{
			if (base.transform.position.y < -250f || (GameBallManager.Instance.transform.position - base.transform.position).sqrMagnitude > 6400f)
			{
				MonkeBallGame.Instance.LaunchBallNeutral(gameBall.id);
				_positionFailsafe = false;
				_positionFailsafeTimer = 3f;
			}
		}
		else
		{
			_positionFailsafeTimer -= Time.deltaTime;
			if (_positionFailsafeTimer <= 0f)
			{
				_positionFailsafe = true;
			}
		}
	}

	public void OnCollisionEnter(Collision collision)
	{
		if (AlreadyDropped() || _justGrabbed || IsGamePlayer(collision.collider))
		{
			return;
		}
		alreadyDropped = true;
		_droppedTimer = 0f;
		gameBall.PlayBounceFX();
		if (PhotonNetwork.IsMasterClient)
		{
			if (_rigidBody.linearVelocity.sqrMagnitude > 1f)
			{
				_resyncPosition = true;
				_resyncDelay = 0.5f;
			}
			if (_launchAfterScore)
			{
				_launchAfterScore = false;
				MonkeBallGame.Instance.RequestRestrictBallToTeamOnScore(gameBall.id, MonkeBallGame.Instance.GetOtherTeam(gameBall.lastHeldByTeamId));
			}
			else
			{
				MonkeBallGame.Instance.RequestRestrictBallToTeam(gameBall.id, MonkeBallGame.Instance.GetOtherTeam(gameBall.lastHeldByTeamId));
			}
		}
		else
		{
			if (_rigidBody.linearVelocity.sqrMagnitude > 1f)
			{
				_resyncPosition = true;
				_resyncDelay = 1.5f;
			}
			_ = gameBall.lastHeldByActorNumber;
			_ = PhotonNetwork.LocalPlayer.ActorNumber;
		}
	}

	public void TriggerDelayedResync()
	{
		_resyncPosition = true;
		if (PhotonNetwork.IsMasterClient)
		{
			_resyncDelay = 0.5f;
		}
		else
		{
			_resyncDelay = 1.5f;
		}
	}

	public void SetRigidbodyDiscrete()
	{
		_rigidBody.collisionDetectionMode = CollisionDetectionMode.Discrete;
	}

	public void SetRigidbodyContinuous()
	{
		_rigidBody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
	}

	public static MonkeBall Get(GameBall ball)
	{
		if (ball == null)
		{
			return null;
		}
		return ball.GetComponent<MonkeBall>();
	}

	public bool AlreadyDropped()
	{
		return alreadyDropped;
	}

	public void OnGrabbed()
	{
		alreadyDropped = false;
		_justGrabbed = true;
		_justGrabbedTimer = 0.1f;
		_resyncPosition = false;
	}

	public void OnSwitchHeldByTeam(int teamId)
	{
		if (PhotonNetwork.IsMasterClient)
		{
			MonkeBallGame.Instance.RequestRestrictBallToTeam(gameBall.id, teamId);
		}
	}

	public void ClearCannotGrabTeamId()
	{
		gameBall.onlyGrabTeamId = -1;
		restrictTeamGrabEndTime = -1.0;
		Refresh();
	}

	public bool RestrictBallToTeam(int teamId, float duration)
	{
		if (teamId == gameBall.onlyGrabTeamId && Time.timeAsDouble + (double)duration < restrictTeamGrabEndTime)
		{
			return false;
		}
		gameBall.onlyGrabTeamId = teamId;
		restrictTeamGrabEndTime = Time.timeAsDouble + (double)duration;
		Refresh();
		return true;
	}

	private void Refresh()
	{
		if (gameBall.onlyGrabTeamId == -1)
		{
			mainRenderer.material = defaultMaterial;
		}
		else
		{
			mainRenderer.material = teamMaterial[gameBall.onlyGrabTeamId];
		}
	}

	private static bool IsGamePlayer(Collider collider)
	{
		return GameBallPlayer.GetGamePlayer(collider) != null;
	}

	public void SetVisualOffset(bool detach)
	{
		if (detach)
		{
			lastVisiblePosition = mainRenderer.transform.position;
			_visualOffset = true;
			_timeOffset = Time.time;
			mainRenderer.transform.SetParent(null, worldPositionStays: true);
		}
		else
		{
			ReattachVisuals();
		}
	}

	private void ReattachVisuals()
	{
		if (_visualOffset)
		{
			mainRenderer.transform.SetParent(base.transform);
			mainRenderer.transform.localPosition = Vector3.zero;
			mainRenderer.transform.localRotation = Quaternion.identity;
			_visualOffset = false;
		}
	}

	private void UpdateVisualOffset()
	{
		if (_visualOffset)
		{
			mainRenderer.transform.position = Vector3.Lerp(mainRenderer.transform.position, _rigidBody.position, Mathf.Clamp((Time.time - _timeOffset) / maxLerpTime, offsetLerp, 1f));
			if ((mainRenderer.transform.position - _rigidBody.position).sqrMagnitude < _offsetThreshold)
			{
				ReattachVisuals();
			}
		}
	}
}
