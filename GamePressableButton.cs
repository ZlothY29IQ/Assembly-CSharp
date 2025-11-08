using Photon.Pun;
using UnityEngine;
using UnityEngine.Events;

public class GamePressableButton : MonoBehaviour, IClickable
{
	[SerializeField]
	private GameEntity gameEntity;

	[SerializeField]
	private bool requireEquipped;

	[SerializeField]
	private bool activeWhileGrabbed;

	[SerializeField]
	private bool activeWhileSnapped;

	public UnityEvent onPressButton;

	[Header("Button Press")]
	public float debounceTime = 0.25f;

	public int pressButtonSoundIndex = 67;

	private float touchTime;

	public void Click(bool leftHand = false)
	{
		PressButton(leftHand);
	}

	protected void OnTriggerEnter(Collider collider)
	{
		if (base.enabled && touchTime + debounceTime < Time.time)
		{
			GorillaTriggerColliderHandIndicator component = collider.gameObject.GetComponent<GorillaTriggerColliderHandIndicator>();
			if ((bool)component && CheckValidEquippedState(component.isLeftHand))
			{
				PressButton(component.isLeftHand);
			}
		}
	}

	private bool CheckValidEquippedState(bool pressedHandLeft)
	{
		if (!requireEquipped)
		{
			return true;
		}
		int num = -1;
		if (gameEntity.IsHeldByLocalPlayer() && activeWhileGrabbed && GamePlayer.TryGetGamePlayer(gameEntity.heldByActorNumber, out var out_gamePlayer))
		{
			num = out_gamePlayer.FindHandIndex(gameEntity.id);
		}
		if (num == -1 && gameEntity.IsSnappedByLocalPlayer() && activeWhileSnapped && GamePlayer.TryGetGamePlayer(gameEntity.snappedByActorNumber, out var out_gamePlayer2))
		{
			num = out_gamePlayer2.FindSnapIndex(gameEntity.id);
		}
		if (num == -1)
		{
			return false;
		}
		bool flag = GamePlayer.IsLeftHand(num);
		return pressedHandLeft != flag;
	}

	private void PressButton(bool isLeftHand)
	{
		touchTime = Time.time;
		onPressButton?.Invoke();
		GorillaTagger.Instance.offlineVRRig.PlayHandTapLocal(pressButtonSoundIndex, isLeftHand, 0.05f);
		GorillaTagger.Instance.StartVibration(isLeftHand, GorillaTagger.Instance.tapHapticStrength / 2f, GorillaTagger.Instance.tapHapticDuration);
		if (NetworkSystem.Instance.InRoom && GorillaTagger.Instance.myVRRig != null)
		{
			GorillaTagger.Instance.myVRRig.SendRPC("RPC_PlayHandTap", RpcTarget.Others, 67, isLeftHand, 0.05f);
		}
	}
}
