using UnityEngine;
using UnityEngine.Events;

public class OnTapHandler : Tappable
{
	[SerializeField]
	private UnityEvent OnTapEvents;

	[SerializeField]
	private UnityEvent OnGrabEvents;

	[SerializeField]
	private UnityEvent OnReleaseEvents;

	public override void OnTapLocal(float tapStrength, float tapTime, PhotonMessageInfoWrapped sender)
	{
		OnTapEvents?.Invoke();
	}

	public override void OnGrabLocal(float tapTime, PhotonMessageInfoWrapped sender)
	{
		OnGrabEvents?.Invoke();
	}

	public override void OnReleaseLocal(float tapTime, PhotonMessageInfoWrapped sender)
	{
		OnReleaseEvents?.Invoke();
	}
}
