using System;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Events;

public class IDCardScanner : MonoBehaviour
{
	public delegate void CardSwipeEvent(int actorNumber);

	public UnityEvent onCardSwiped;

	public UnityEvent<int> onCardSwipedByPlayer;

	[Tooltip("Has to be risen externally, by the receiver of the card swipe")]
	public UnityEvent onSucceeded;

	[Tooltip("Has to be risen externally, by the receiver of the card swipe")]
	public UnityEvent onFailed;

	public bool requireSpecificPlayer;

	public bool requireAuthority;

	[NonSerialized]
	public NetPlayer restrictToPlayer;

	public event CardSwipeEvent OnPlayerCardSwipe;

	private void OnTriggerEnter(Collider other)
	{
		if (!(other.GetComponent<ScannableIDCard>() != null))
		{
			return;
		}
		onCardSwiped?.Invoke();
		GameEntity component = other.GetComponent<GameEntity>();
		if (component == null && other.attachedRigidbody != null)
		{
			component = other.attachedRigidbody.GetComponent<GameEntity>();
		}
		if (component != null && component.heldByActorNumber != -1)
		{
			bool num = !requireSpecificPlayer || (restrictToPlayer != null && restrictToPlayer.ActorNumber == component.heldByActorNumber && component.heldByActorNumber == PhotonNetwork.LocalPlayer.ActorNumber);
			bool flag = !requireAuthority || component.manager.IsAuthority();
			if (num && flag)
			{
				onCardSwipedByPlayer?.Invoke(component.heldByActorNumber);
				this.OnPlayerCardSwipe?.Invoke(component.heldByActorNumber);
			}
		}
	}
}
