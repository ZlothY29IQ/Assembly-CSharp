using UnityEngine;

public abstract class ObservableBehavior : MonoBehaviour, IGorillaSliceableSimple
{
	private bool firstFrame = true;

	private bool observable = true;

	private void OnEnable()
	{
		GorillaSlicerSimpleManager.RegisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.LateUpdate);
		UnityOnEnable();
	}

	private void OnDisable()
	{
		GorillaSlicerSimpleManager.UnregisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.LateUpdate);
		UnityOnDisable();
	}

	void IGorillaSliceableSimple.SliceUpdate()
	{
		Transform transform = Camera.main.transform;
		Vector3.Distance(transform.position, base.transform.position);
		Vector3.Dot((transform.position - base.transform.position).normalized, transform.transform.forward);
		bool flag = true;
		if (firstFrame || observable != flag)
		{
			if (flag)
			{
				OnBecameObservable();
			}
			else
			{
				OnLostObservable();
			}
		}
		observable = flag;
		firstFrame = false;
		if (flag)
		{
			ObservableSliceUpdate();
		}
	}

	protected virtual void UnityOnEnable()
	{
	}

	protected virtual void UnityOnDisable()
	{
	}

	protected abstract void OnLostObservable();

	protected abstract void OnBecameObservable();

	protected abstract void ObservableSliceUpdate();
}
