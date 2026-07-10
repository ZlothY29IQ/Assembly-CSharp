using UnityEngine;
using UnityEngine.Events;

public class TappableGeneric : Tappable
{
	[Tooltip("Invoked when this object is tapped. Fires on every client by default; if localOnly is true, fires only on the tapping player's client.")]
	[SerializeField]
	protected UnityEvent OnTapped;

	public override void OnTapLocal(float tapStrength, float tapTime, PhotonMessageInfoWrapped info)
	{
		OnTapped?.Invoke();
	}
}
