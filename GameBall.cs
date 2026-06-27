using UnityEngine;

public class GameBall : MonoBehaviour
{
	public GameBallId id;

	public float gravityMult = 1f;

	public bool disc;

	public Vector3 localDiscUp;

	public AudioSource audioSource;

	public AudioClip catchSound;

	public float catchSoundVolume;

	private float _catchSoundDecay;

	public AudioClip throwSound;

	public float throwSoundVolume;

	public AudioClip groundSound;

	public float groundSoundVolume;

	[SerializeField]
	private Rigidbody rigidBody;

	[SerializeField]
	private Collider collider;

	public int heldByActorNumber;

	public int lastHeldByActorNumber;

	public int lastHeldByTeamId;

	public int onlyGrabTeamId;

	private bool _launched;

	private float _launchedTimer;

	public MonkeBall _monkeBall;

	public bool IsLaunched => _launched;

	private void Awake()
	{
		id = GameBallId.Invalid;
		if (rigidBody == null)
		{
			rigidBody = GetComponent<Rigidbody>();
		}
		if (collider == null)
		{
			collider = GetComponent<Collider>();
		}
		if (disc && rigidBody != null)
		{
			rigidBody.maxAngularVelocity = 28f;
		}
		heldByActorNumber = -1;
		lastHeldByTeamId = -1;
		onlyGrabTeamId = -1;
		_monkeBall = GetComponent<MonkeBall>();
	}

	private void FixedUpdate()
	{
		if (rigidBody == null)
		{
			return;
		}
		if (_launched)
		{
			_launchedTimer += Time.fixedDeltaTime;
			if (collider.isTrigger && _launchedTimer > 1f && rigidBody.linearVelocity.y <= 0f)
			{
				_launched = false;
				collider.isTrigger = false;
			}
		}
		Vector3 vector = -Physics.gravity * (1f - gravityMult);
		rigidBody.AddForce(vector * rigidBody.mass, ForceMode.Force);
		_catchSoundDecay -= Time.deltaTime;
	}

	public void WasLaunched()
	{
		_launched = true;
		collider.isTrigger = true;
		_launchedTimer = 0f;
	}

	public Vector3 GetVelocity()
	{
		if (rigidBody == null)
		{
			return Vector3.zero;
		}
		return rigidBody.linearVelocity;
	}

	public void SetVelocity(Vector3 velocity)
	{
		rigidBody.linearVelocity = velocity;
	}

	public void PlayCatchFx()
	{
		if (audioSource != null && _catchSoundDecay <= 0f && audioSource.isActiveAndEnabled)
		{
			audioSource.clip = catchSound;
			audioSource.volume = catchSoundVolume;
			audioSource.GTPlay();
			_catchSoundDecay = 0.1f;
		}
	}

	public void PlayThrowFx()
	{
		if (audioSource != null && audioSource.isActiveAndEnabled)
		{
			audioSource.clip = throwSound;
			audioSource.volume = throwSoundVolume;
			audioSource.GTPlay();
		}
	}

	public void PlayBounceFX()
	{
		if (audioSource != null && audioSource.isActiveAndEnabled)
		{
			audioSource.clip = groundSound;
			audioSource.volume = groundSoundVolume;
			audioSource.GTPlay();
		}
	}

	public void SetHeldByTeamId(int teamId)
	{
		lastHeldByTeamId = teamId;
	}

	private bool IsGamePlayer(Collider collider)
	{
		return GameBallPlayer.GetGamePlayer(collider) != null;
	}

	public void SetVisualOffset(bool detach)
	{
		if (_monkeBall != null)
		{
			_monkeBall.SetVisualOffset(detach);
		}
	}
}
