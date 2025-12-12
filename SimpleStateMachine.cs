using System;
using UnityEngine;

public class SimpleStateMachine<State> where State : Enum
{
	private State currState;

	private double stateStartTime;

	private Action<State> onStateStart;

	private Action<State> onStateEnd;

	private Action<State> onStateUpdate;

	public void Setup(State initialState, Action<State> onStateStart, Action<State> onStateEnd, Action<State> onStateUpdate)
	{
		this.onStateStart = onStateStart;
		this.onStateEnd = onStateEnd;
		this.onStateUpdate = onStateUpdate;
		stateStartTime = Time.timeAsDouble;
		currState = initialState;
		onStateStart?.Invoke(currState);
	}

	public void Update()
	{
		onStateUpdate?.Invoke(currState);
	}

	public void SetState(State state, bool force = false)
	{
		if (force || !state.Equals(currState))
		{
			onStateEnd?.Invoke(currState);
			currState = state;
			stateStartTime = Time.timeAsDouble;
			onStateStart?.Invoke(currState);
		}
	}

	public State GetState()
	{
		return currState;
	}

	public double GetStateStartTime()
	{
		return stateStartTime;
	}

	public bool IsStateFinished(double currTime, float stateDuration)
	{
		return currTime >= stateStartTime + (double)stateDuration;
	}
}
