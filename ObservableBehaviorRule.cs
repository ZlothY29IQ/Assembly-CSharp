using UnityEngine;

[CreateAssetMenu(fileName = "ObservableBehaviorRule", menuName = "Utilities/ObservableBehaviorRule")]
public class ObservableBehaviorRule : ScriptableObject
{
	[SerializeField]
	private Vector2 observableDistanceRange = new Vector2(0f, 15f);

	[SerializeField]
	private Vector2 observableDotRange = new Vector2(-1f, 0f);

	[SerializeField]
	private bool inverseObservable;

	public Vector2 ObservableDistanceRange => observableDistanceRange;

	public Vector2 ObservableDotRange => observableDotRange;

	public bool InverseObservable => inverseObservable;
}
