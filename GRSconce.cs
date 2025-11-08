using UnityEngine;

public class GRSconce : MonoBehaviour
{
	private enum State
	{
		Off,
		On
	}

	public GameEntity gameEntity;

	public GameLight gameLight;

	public GRTool tool;

	public MeshRenderer meshRenderer;

	public Material offMaterial;

	public Material onMaterial;

	public AudioSource audioSource;

	public AudioClip lightOnSound;

	public float lightOnSoundVolume;

	private State state;

	private void Awake()
	{
		if (tool != null)
		{
			tool.OnEnergyChange += OnEnergyChange;
		}
		if (gameEntity != null)
		{
			gameEntity.OnStateChanged += OnStateChange;
		}
		state = State.Off;
		StopLight();
	}

	private bool IsAuthority()
	{
		return gameEntity.IsAuthority();
	}

	private void SetState(State newState)
	{
		state = newState;
		switch (state)
		{
		case State.Off:
			StopLight();
			break;
		case State.On:
			StartLight();
			break;
		}
		if (IsAuthority())
		{
			gameEntity.RequestState(gameEntity.id, (long)newState);
		}
	}

	private void StartLight()
	{
		gameLight.gameObject.SetActive(value: true);
		audioSource.volume = lightOnSoundVolume;
		audioSource.clip = lightOnSound;
		audioSource.Play();
		meshRenderer.material = onMaterial;
	}

	private void StopLight()
	{
		gameLight.gameObject.SetActive(value: false);
		meshRenderer.material = offMaterial;
	}

	private void OnEnergyChange(GRTool tool, int energy, GameEntityId chargingEntityId)
	{
		if (IsAuthority() && state == State.Off && tool.IsEnergyFull())
		{
			SetState(State.On);
		}
	}

	private void OnStateChange(long prevState, long nextState)
	{
		if (!IsAuthority())
		{
			State state = (State)nextState;
			SetState(state);
		}
	}
}
