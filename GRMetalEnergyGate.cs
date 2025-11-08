using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GRMetalEnergyGate : MonoBehaviour
{
	public enum State
	{
		Closed,
		Open
	}

	[Serializable]
	public struct DoorParams
	{
		public Transform doorTransform;

		public Transform doorClosedPosition;

		public Transform doorOpenPosition;
	}

	[SerializeField]
	public DoorParams upperDoor;

	[SerializeField]
	public DoorParams lowerDoor;

	[SerializeField]
	private float doorOpenTime = 1.5f;

	[SerializeField]
	private float doorCloseTime = 1.5f;

	[SerializeField]
	private AnimationCurve doorOpenCurve;

	[SerializeField]
	private AnimationCurve doorCloseCurve;

	[SerializeField]
	private AudioClip doorOpenClip;

	[SerializeField]
	private AudioClip doorCloseClip;

	[SerializeField]
	private List<Transform> enableObjectsOnOpen = new List<Transform>();

	[SerializeField]
	private List<Transform> disableObjectsOnOpen = new List<Transform>();

	[SerializeField]
	private GRTool tool;

	[SerializeField]
	private GameEntity gameEntity;

	[SerializeField]
	private AudioSource audioSource;

	public State state;

	private float openProgress;

	private Coroutine doorAnimationCoroutine;

	private void OnEnable()
	{
		tool.OnEnergyChange += OnEnergyChange;
		gameEntity.OnStateChanged += OnEntityStateChanged;
	}

	private void OnDisable()
	{
		if (tool != null)
		{
			tool.OnEnergyChange -= OnEnergyChange;
		}
		if (gameEntity != null)
		{
			gameEntity.OnStateChanged -= OnEntityStateChanged;
		}
	}

	private void OnEnergyChange(GRTool tool, int energyChange, GameEntityId chargingEntityId)
	{
		GameEntity gameEntity = this.gameEntity.manager.GetGameEntity(chargingEntityId);
		GRPlayer gRPlayer = null;
		if (gameEntity != null)
		{
			gRPlayer = GRPlayer.Get(gameEntity.heldByActorNumber);
		}
		if (gRPlayer != null)
		{
			gRPlayer.IncrementCoresSpentPlayer(energyChange);
		}
		if (state == State.Closed && tool.energy >= tool.GetEnergyMax())
		{
			if (gRPlayer != null)
			{
				gRPlayer.IncrementGatesUnlocked(1);
			}
			SetState(State.Open);
			if (this.gameEntity.IsAuthority())
			{
				this.gameEntity.RequestState(this.gameEntity.id, 1L);
			}
		}
	}

	private void OnEntityStateChanged(long prevState, long nextState)
	{
		if (!gameEntity.IsAuthority())
		{
			SetState((State)nextState);
		}
	}

	public void SetState(State newState)
	{
		if (state == newState)
		{
			return;
		}
		state = newState;
		switch (state)
		{
		case State.Open:
		{
			audioSource.PlayOneShot(doorOpenClip);
			for (int k = 0; k < enableObjectsOnOpen.Count; k++)
			{
				enableObjectsOnOpen[k].gameObject.SetActive(value: true);
			}
			for (int l = 0; l < disableObjectsOnOpen.Count; l++)
			{
				disableObjectsOnOpen[l].gameObject.SetActive(value: false);
			}
			break;
		}
		case State.Closed:
		{
			audioSource.PlayOneShot(doorCloseClip);
			for (int i = 0; i < enableObjectsOnOpen.Count; i++)
			{
				enableObjectsOnOpen[i].gameObject.SetActive(value: false);
			}
			for (int j = 0; j < disableObjectsOnOpen.Count; j++)
			{
				disableObjectsOnOpen[j].gameObject.SetActive(value: true);
			}
			break;
		}
		}
		if (doorAnimationCoroutine == null)
		{
			doorAnimationCoroutine = StartCoroutine(UpdateDoorAnimation());
		}
	}

	public void OpenGate()
	{
		SetState(State.Open);
	}

	public void CloseGate()
	{
		SetState(State.Closed);
	}

	private IEnumerator UpdateDoorAnimation()
	{
		while ((state == State.Open && openProgress < 1f) || (state == State.Closed && openProgress > 0f))
		{
			switch (state)
			{
			case State.Open:
			{
				openProgress = Mathf.MoveTowards(openProgress, 1f, Time.deltaTime / doorOpenTime);
				float t2 = doorOpenCurve.Evaluate(openProgress);
				upperDoor.doorTransform.localPosition = Vector3.Lerp(upperDoor.doorClosedPosition.localPosition, upperDoor.doorOpenPosition.localPosition, t2);
				lowerDoor.doorTransform.localPosition = Vector3.Lerp(lowerDoor.doorClosedPosition.localPosition, lowerDoor.doorOpenPosition.localPosition, t2);
				break;
			}
			case State.Closed:
			{
				openProgress = Mathf.MoveTowards(openProgress, 0f, Time.deltaTime / doorOpenTime);
				float t = doorCloseCurve.Evaluate(openProgress);
				upperDoor.doorTransform.localPosition = Vector3.Lerp(upperDoor.doorClosedPosition.localPosition, upperDoor.doorOpenPosition.localPosition, t);
				lowerDoor.doorTransform.localPosition = Vector3.Lerp(lowerDoor.doorClosedPosition.localPosition, lowerDoor.doorOpenPosition.localPosition, t);
				break;
			}
			}
			yield return null;
		}
		doorAnimationCoroutine = null;
	}
}
