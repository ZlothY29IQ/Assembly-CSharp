using System;
using System.Collections.Generic;

namespace GorillaTagScripts.AI;

public class StateMachine
{
	private class Transition
	{
		public Func<bool> Condition { get; }

		public IState To { get; }

		public Transition(IState to, Func<bool> condition)
		{
			To = to;
			Condition = condition;
		}
	}

	private IState _currentState;

	private Dictionary<Type, List<Transition>> _transitions = new Dictionary<Type, List<Transition>>();

	private List<Transition> _currentTransitions = new List<Transition>();

	private List<Transition> _anyTransitions = new List<Transition>();

	private static List<Transition> EmptyTransitions = new List<Transition>(0);

	public void Tick()
	{
		Transition transition = GetTransition();
		if (transition != null)
		{
			SetState(transition.To);
		}
		_currentState?.Tick();
	}

	public void SetState(IState state)
	{
		if (state != _currentState)
		{
			_currentState?.OnExit();
			_currentState = state;
			_transitions.TryGetValue(_currentState.GetType(), out _currentTransitions);
			if (_currentTransitions == null)
			{
				_currentTransitions = EmptyTransitions;
			}
			_currentState.OnEnter();
		}
	}

	public IState GetState()
	{
		return _currentState;
	}

	public void AddTransition(IState from, IState to, Func<bool> predicate)
	{
		if (!_transitions.TryGetValue(from.GetType(), out var value))
		{
			value = new List<Transition>();
			_transitions[from.GetType()] = value;
		}
		value.Add(new Transition(to, predicate));
	}

	public void AddAnyTransition(IState state, Func<bool> predicate)
	{
		_anyTransitions.Add(new Transition(state, predicate));
	}

	private Transition GetTransition()
	{
		foreach (Transition anyTransition in _anyTransitions)
		{
			if (anyTransition.Condition())
			{
				return anyTransition;
			}
		}
		foreach (Transition currentTransition in _currentTransitions)
		{
			if (currentTransition.Condition())
			{
				return currentTransition;
			}
		}
		return null;
	}
}
