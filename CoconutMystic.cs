using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;

public class CoconutMystic : MonoBehaviour
{
	public VRRig rig;

	public GeodeItem geodeItem;

	public SoundBankPlayer soundPlayer;

	public ParticleSystem breakEffect;

	public RandomLocalizedStrings answers;

	public TMP_Text label;

	public bool distinct;

	private static readonly int kUpdateLabelEvent = "CoconutMystic.UpdateLabel".GetStaticHash();

	private void Awake()
	{
		rig = GetComponentInParent<VRRig>();
	}

	private void OnEnable()
	{
		PhotonNetwork.NetworkingClient.EventReceived += OnPhotonEvent;
	}

	private void OnDisable()
	{
		PhotonNetwork.NetworkingClient.EventReceived -= OnPhotonEvent;
	}

	private void OnPhotonEvent(EventData evData)
	{
		if (evData.Code != 176)
		{
			return;
		}
		object[] array = (object[])evData.CustomData;
		if (array[0] is int num && num == kUpdateLabelEvent)
		{
			NetPlayer player = NetworkSystem.Instance.GetPlayer(evData.Sender);
			NetPlayer owningNetPlayer = rig.OwningNetPlayer;
			if (player == owningNetPlayer)
			{
				int index = (int)array[1];
				label.text = answers.GetItem(index).GetLocalizedString();
				soundPlayer.Play();
				breakEffect.Play();
			}
		}
	}

	public void UpdateLabel()
	{
		bool flag = geodeItem.currentState == TransferrableObject.PositionState.InLeftHand;
		label.rectTransform.localRotation = Quaternion.Euler(0f, flag ? 270f : 90f, 0f);
	}

	public void ShowAnswer()
	{
		answers.distinct = distinct;
		label.text = answers.NextItem().GetLocalizedString();
		soundPlayer.Play();
		breakEffect.Play();
		object eventContent = new object[2] { kUpdateLabelEvent, answers.lastItemIndex };
		PhotonNetwork.RaiseEvent(176, eventContent, RaiseEventOptions.Default, SendOptions.SendReliable);
	}
}
