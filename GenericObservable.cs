using UnityEngine.Events;

public class GenericObservable : ObservableBehavior
{
	public UnityEvent OnObservable;

	public UnityEvent OnUnobservable;

	protected override void ObservableSliceUpdate()
	{
	}

	protected override void OnBecameObservable()
	{
		OnObservable?.Invoke();
	}

	protected override void OnLostObservable()
	{
		OnUnobservable?.Invoke();
	}
}
