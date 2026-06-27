using UnityEngine;

public abstract class ObservableBehavior : MonoBehaviour, IGorillaSliceableSimple, IBuildValidation
{
	private bool firstFrame = true;

	protected bool observable;

	[SerializeField]
	private ObservableBehaviorRule observableBehaviorRule;

	[SerializeField]
	private RigEventVolume observableVolume;

	private float dist;

	[SerializeField]
	private bool triggerLostObservableIfSpawnedUnobservable;

	public ObservableBehaviorRule ObservableBehaviorRule
	{
		get
		{
			return observableBehaviorRule;
		}
		set
		{
			observableBehaviorRule = value;
			firstFrame = true;
		}
	}

	public float Distance => dist;

	private void OnEnable()
	{
		GorillaSlicerSimpleManager.RegisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.LateUpdate);
		UnityOnEnable();
	}

	private void OnDisable()
	{
		GorillaSlicerSimpleManager.UnregisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.LateUpdate);
		if (observable)
		{
			observable = false;
			OnLostObservable();
		}
		UnityOnDisable();
	}

	private void OnDestroy()
	{
		if (observable)
		{
			observable = false;
			OnLostObservable();
		}
	}

	void IGorillaSliceableSimple.SliceUpdate()
	{
		bool flag = observableVolume != null && observableVolume.LocalRigPresent;
		if (observableVolume == null && observableBehaviorRule != null)
		{
			Transform transform = Camera.main.transform;
			dist = Vector3.Distance(transform.position, base.transform.position);
			float num = ((!observableBehaviorRule.InverseObservable) ? Vector3.Dot((transform.position - base.transform.position).normalized, transform.transform.forward) : Vector3.Dot((base.transform.position - transform.position).normalized, base.transform.forward));
			flag = observableBehaviorRule.ObservableDistanceRange.x <= dist && dist <= observableBehaviorRule.ObservableDistanceRange.y && observableBehaviorRule.ObservableDotRange.x <= num && num <= observableBehaviorRule.ObservableDotRange.y;
		}
		if ((firstFrame && flag) || (observable != flag && flag))
		{
			OnBecameObservable();
		}
		else if ((firstFrame && !flag && triggerLostObservableIfSpawnedUnobservable) || (observable != flag && !flag))
		{
			OnLostObservable();
		}
		firstFrame = false;
		observable = flag;
		if (observable)
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

	public bool BuildValidationCheck()
	{
		if (observableVolume == null && observableBehaviorRule == null)
		{
			Debug.LogError("observableVolume & observableBehaviorRule can't both be null!");
			return false;
		}
		if (observableVolume != null && observableBehaviorRule != null)
		{
			Debug.LogWarning("observableVolume will override the observableBehaviorRule");
		}
		return true;
	}

	public void OnDrawGizmosSelected()
	{
		if (observableBehaviorRule != null)
		{
			if (observableBehaviorRule.ObservableDistanceRange.x > 0f)
			{
				Gizmos.color = Color.red;
				Gizmos.DrawWireSphere(base.transform.position, observableBehaviorRule.ObservableDistanceRange.x);
			}
			Gizmos.color = Color.green;
			Gizmos.DrawWireSphere(base.transform.position, observableBehaviorRule.ObservableDistanceRange.y);
		}
	}
}
