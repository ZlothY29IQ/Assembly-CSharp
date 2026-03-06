using System;
using System.Collections.Generic;
using GorillaLocomotion;
using GorillaNetworking;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class FriendDisplay : MonoBehaviour
{
	public enum ButtonState
	{
		Default,
		Active,
		Alert
	}

	[FormerlySerializedAs("gridCenter")]
	[SerializeField]
	private FriendCard[] friendCards = new FriendCard[9];

	[SerializeField]
	private Transform gridRoot;

	[SerializeField]
	private float gridWidth = 2f;

	[SerializeField]
	private float gridHeight = 1f;

	[SerializeField]
	private int gridDimension = 3;

	[SerializeField]
	private TriggerEventNotifier triggerNotifier;

	[FormerlySerializedAs("_joinButtons")]
	[Header("Buttons")]
	[SerializeField]
	private GorillaPressableDelayButton[] _friendCardButtons;

	[SerializeField]
	private TextMeshProUGUI[] _friendCardButtonText;

	[SerializeField]
	private MeshRenderer _localPlayerFullyVisibleButton;

	[SerializeField]
	private MeshRenderer _localPlayerPublicOnlyButton;

	[SerializeField]
	private MeshRenderer _localPlayerFullyHiddenButton;

	[SerializeField]
	private MeshRenderer _removeFriendButton;

	[SerializeField]
	private FriendCard _localPlayerCard;

	[SerializeField]
	private MeshRenderer[] PageButtons;

	[SerializeField]
	private Material[] _buttonDefaultMaterials;

	[SerializeField]
	private Material[] _buttonActiveMaterials;

	[SerializeField]
	private Material[] _buttonAlertMaterials;

	[SerializeField]
	private Material[] _pageButtonDefaultMaterials;

	[SerializeField]
	private Material[] _pageButtonActiveMaterials;

	[SerializeField]
	private Material[] _pageButtonAlerttMaterials;

	private int cardsPerPage = 9;

	private int totalPages = 5;

	[SerializeField]
	private float pageButtonInactiveZPos;

	[SerializeField]
	private float pageButtonActiveZPos;

	private MeshRenderer[] _joinButtonRenderers;

	private bool inRemoveMode;

	private bool localPlayerAtDisplay;

	public bool InRemoveMode => inRemoveMode;

	private void Start()
	{
		InitFriendCards();
		InitLocalPlayerCard();
		UpdateLocalPlayerPrivacyButtons();
		triggerNotifier.TriggerEnterEvent += TriggerEntered;
		triggerNotifier.TriggerExitEvent += TriggerExited;
		NetworkSystem.Instance.OnJoinedRoomEvent += new Action(OnJoinedRoom);
	}

	private void OnDestroy()
	{
		if (NetworkSystem.Instance != null)
		{
			NetworkSystem.Instance.OnJoinedRoomEvent -= new Action(OnJoinedRoom);
		}
		if (triggerNotifier != null)
		{
			triggerNotifier.TriggerEnterEvent -= TriggerEntered;
			triggerNotifier.TriggerExitEvent -= TriggerExited;
		}
	}

	public void TriggerEntered(TriggerEventNotifier notifier, Collider other)
	{
		if (other == GTPlayer.Instance.headCollider)
		{
			FriendSystem.Instance.OnFriendListRefresh += OnGetFriendsReceived;
			FriendSystem.Instance.RefreshFriendsList();
			PopulateLocalPlayerCard();
			localPlayerAtDisplay = true;
			if (InRemoveMode)
			{
				ToggleRemoveFriendMode();
			}
		}
	}

	public void TriggerExited(TriggerEventNotifier notifier, Collider other)
	{
		if (other == GTPlayer.Instance.headCollider)
		{
			FriendSystem.Instance.OnFriendListRefresh -= OnGetFriendsReceived;
			ClearFriendCards();
			ClearLocalPlayerCard();
			ClearPageButtons();
			localPlayerAtDisplay = false;
			if (InRemoveMode)
			{
				ToggleRemoveFriendMode();
			}
		}
	}

	private void OnJoinedRoom()
	{
		Refresh();
	}

	private void Refresh()
	{
		if (localPlayerAtDisplay)
		{
			FriendSystem.Instance.RefreshFriendsList();
			PopulateLocalPlayerCard();
		}
	}

	public void LocalPlayerFullyVisiblePress()
	{
		FriendSystem.Instance.SetLocalPlayerPrivacy(FriendSystem.PlayerPrivacy.Visible);
		UpdateLocalPlayerPrivacyButtons();
		PopulateLocalPlayerCard();
	}

	public void LocalPlayerPublicOnlyPress()
	{
		FriendSystem.Instance.SetLocalPlayerPrivacy(FriendSystem.PlayerPrivacy.PublicOnly);
		UpdateLocalPlayerPrivacyButtons();
		PopulateLocalPlayerCard();
	}

	public void LocalPlayerFullyHiddenPress()
	{
		FriendSystem.Instance.SetLocalPlayerPrivacy(FriendSystem.PlayerPrivacy.Hidden);
		UpdateLocalPlayerPrivacyButtons();
		PopulateLocalPlayerCard();
	}

	private void UpdateLocalPlayerPrivacyButtons()
	{
		FriendSystem.PlayerPrivacy localPlayerPrivacy = FriendSystem.Instance.LocalPlayerPrivacy;
		SetButtonAppearance(_localPlayerFullyVisibleButton, localPlayerPrivacy == FriendSystem.PlayerPrivacy.Visible);
		SetButtonAppearance(_localPlayerPublicOnlyButton, localPlayerPrivacy == FriendSystem.PlayerPrivacy.PublicOnly);
		SetButtonAppearance(_localPlayerFullyHiddenButton, localPlayerPrivacy == FriendSystem.PlayerPrivacy.Hidden);
	}

	private void UpdatePageButtons(int selectedPage)
	{
		for (int i = 0; i < totalPages; i++)
		{
			if (FriendBackendController.Instance.FriendsList.Count > cardsPerPage * Mathf.Max(i, 1))
			{
				SetPageButtonAppearance(PageButtons[i], (i != selectedPage) ? ButtonState.Active : ButtonState.Alert);
			}
			else
			{
				SetPageButtonAppearance(PageButtons[i], active: false);
			}
		}
	}

	private void SetButtonAppearance(MeshRenderer buttonRenderer, bool active)
	{
		SetButtonAppearance(buttonRenderer, active ? ButtonState.Active : ButtonState.Default);
	}

	private void SetButtonAppearance(MeshRenderer buttonRenderer, ButtonState state)
	{
		buttonRenderer.sharedMaterials = state switch
		{
			ButtonState.Default => _buttonDefaultMaterials, 
			ButtonState.Active => _buttonActiveMaterials, 
			ButtonState.Alert => _buttonAlertMaterials, 
			_ => throw new ArgumentOutOfRangeException("state", state, null), 
		};
	}

	private void ClearPageButtons()
	{
		for (int i = 0; i < PageButtons.Length; i++)
		{
			SetPageButtonAppearance(PageButtons[i], active: false);
		}
	}

	private void SetPageButtonAppearance(MeshRenderer buttonRenderer, bool active)
	{
		SetPageButtonAppearance(buttonRenderer, active ? ButtonState.Active : ButtonState.Default);
	}

	private void SetPageButtonAppearance(MeshRenderer buttonRenderer, ButtonState state)
	{
		MeshRenderer meshRenderer = buttonRenderer;
		meshRenderer.enabled = state switch
		{
			ButtonState.Default => false, 
			ButtonState.Active => true, 
			ButtonState.Alert => true, 
			_ => throw new ArgumentOutOfRangeException("state", state, null), 
		};
		meshRenderer = buttonRenderer;
		meshRenderer.sharedMaterials = state switch
		{
			ButtonState.Default => _pageButtonDefaultMaterials, 
			ButtonState.Active => _pageButtonActiveMaterials, 
			ButtonState.Alert => _pageButtonAlerttMaterials, 
			_ => throw new ArgumentOutOfRangeException("state", state, null), 
		};
		Transform transform = buttonRenderer.transform;
		transform.localPosition = state switch
		{
			ButtonState.Default => new Vector3(buttonRenderer.transform.localPosition.x, buttonRenderer.transform.localPosition.y, pageButtonInactiveZPos), 
			ButtonState.Active => new Vector3(buttonRenderer.transform.localPosition.x, buttonRenderer.transform.localPosition.y, pageButtonActiveZPos), 
			ButtonState.Alert => new Vector3(buttonRenderer.transform.localPosition.x, buttonRenderer.transform.localPosition.y, pageButtonActiveZPos), 
			_ => throw new ArgumentOutOfRangeException("state", state, null), 
		};
		BoxCollider component = buttonRenderer.GetComponent<BoxCollider>();
		component.enabled = state switch
		{
			ButtonState.Default => false, 
			ButtonState.Active => true, 
			ButtonState.Alert => true, 
			_ => throw new ArgumentOutOfRangeException("state", state, null), 
		};
	}

	public void ToggleRemoveFriendMode()
	{
		inRemoveMode = !inRemoveMode;
		FriendCard[] array = friendCards;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].SetRemoveEnabled(inRemoveMode);
		}
		SetButtonAppearance(_removeFriendButton, inRemoveMode ? ButtonState.Alert : ButtonState.Default);
	}

	private void InitFriendCards()
	{
		float num = gridWidth / (float)gridDimension;
		float num2 = gridHeight / (float)gridDimension;
		Vector3 right = gridRoot.right;
		Vector3 vector = -gridRoot.up;
		Vector3 vector2 = gridRoot.position - right * (gridWidth * 0.5f - num * 0.5f) - vector * (gridHeight * 0.5f - num2 * 0.5f);
		int num3 = 0;
		int num4 = 0;
		for (int i = 0; i < gridDimension; i++)
		{
			for (int j = 0; j < gridDimension; j++)
			{
				FriendCard friendCard = friendCards[num4];
				friendCard.gameObject.SetActive(value: true);
				friendCard.transform.localScale = Vector3.one * (num / friendCard.Width);
				friendCard.transform.position = vector2 + right * num * j + vector * num2 * i;
				friendCard.transform.rotation = gridRoot.transform.rotation;
				friendCard.Init(this);
				friendCard.SetButton(_friendCardButtons[num3++], _buttonDefaultMaterials, _buttonActiveMaterials, _buttonAlertMaterials, _friendCardButtonText[num4]);
				friendCard.SetEmpty();
				num4++;
			}
		}
	}

	public void RandomizeFriendCards()
	{
		for (int i = 0; i < friendCards.Length; i++)
		{
			friendCards[i].Randomize();
		}
	}

	private void ClearFriendCards()
	{
		for (int i = 0; i < friendCards.Length; i++)
		{
			friendCards[i].SetEmpty();
		}
	}

	public void OnGetFriendsReceived(List<FriendBackendController.Friend> friendsList)
	{
		PopulateFriendCards(friendsList);
		UpdateLocalPlayerPrivacyButtons();
		PopulateLocalPlayerCard();
		UpdatePageButtons(0);
	}

	private void PopulateFriendCards(List<FriendBackendController.Friend> friendsList)
	{
		int num = Mathf.Min(friendCards.Length, friendsList.Count);
		for (int i = 0; i < num && friendsList[i] != null; i++)
		{
			friendCards[i].Populate(friendsList[i]);
		}
	}

	public void GoToFriendPage(int currentPage)
	{
		UpdatePageButtons(currentPage);
		for (int i = 0; i < friendCards.Length; i++)
		{
			friendCards[i].SetEmpty();
		}
		int num = currentPage * cardsPerPage;
		Mathf.Min(num + cardsPerPage, FriendBackendController.Instance.FriendsList.Count);
		int num2 = 0;
		for (int j = 0; j < friendCards.Length; j++)
		{
			if (FriendBackendController.Instance.FriendsList.Count <= num + num2)
			{
				break;
			}
			friendCards[j].Populate(FriendBackendController.Instance.FriendsList[num + num2]);
			num2++;
		}
	}

	private void InitLocalPlayerCard()
	{
		_localPlayerCard.Init(this);
		ClearLocalPlayerCard();
	}

	private void PopulateLocalPlayerCard()
	{
		string zone = PhotonNetworkController.Instance.CurrentRoomZone.GetName().ToUpper();
		_localPlayerCard.SetName(NetworkSystem.Instance.LocalPlayer.NickName.ToUpper());
		if (PhotonNetwork.InRoom && !string.IsNullOrEmpty(NetworkSystem.Instance.RoomName) && NetworkSystem.Instance.RoomName.Length > 0)
		{
			bool flag = NetworkSystem.Instance.RoomName[0] == '@';
			bool flag2 = !NetworkSystem.Instance.SessionIsPrivate;
			if (FriendSystem.Instance.LocalPlayerPrivacy == FriendSystem.PlayerPrivacy.Hidden || (FriendSystem.Instance.LocalPlayerPrivacy == FriendSystem.PlayerPrivacy.PublicOnly && !flag2))
			{
				_localPlayerCard.SetRoom("OFFLINE");
				_localPlayerCard.SetZone("");
			}
			else if (flag)
			{
				_localPlayerCard.SetRoom(NetworkSystem.Instance.RoomName.Substring(1).ToUpper());
				_localPlayerCard.SetZone("CUSTOM");
			}
			else if (!flag2)
			{
				_localPlayerCard.SetRoom(NetworkSystem.Instance.RoomName.ToUpper());
				_localPlayerCard.SetZone("PRIVATE");
			}
			else
			{
				_localPlayerCard.SetRoom(NetworkSystem.Instance.RoomName.ToUpper());
				_localPlayerCard.SetZone(zone);
			}
		}
		else
		{
			_localPlayerCard.SetRoom("OFFLINE");
			_localPlayerCard.SetZone("");
		}
	}

	private void ClearLocalPlayerCard()
	{
		_localPlayerCard.SetEmpty();
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.white;
		float num = gridWidth * 0.5f;
		float num2 = gridHeight * 0.5f;
		float num3 = num;
		float num4 = num2;
		Vector3 a = gridRoot.position + gridRoot.rotation * new Vector3(0f - num3, num4, 0f);
		Vector3 vector = gridRoot.position + gridRoot.rotation * new Vector3(num3, num4, 0f);
		Vector3 vector2 = gridRoot.position + gridRoot.rotation * new Vector3(0f - num3, 0f - num4, 0f);
		Vector3 b = gridRoot.position + gridRoot.rotation * new Vector3(num3, 0f - num4, 0f);
		for (int i = 0; i <= gridDimension; i++)
		{
			float t = (float)i / (float)gridDimension;
			Vector3 vector3 = Vector3.Lerp(a, vector, t);
			Vector3 to = Vector3.Lerp(vector2, b, t);
			Gizmos.DrawLine(vector3, to);
			Vector3 vector4 = Vector3.Lerp(a, vector2, t);
			Vector3 to2 = Vector3.Lerp(vector, b, t);
			Gizmos.DrawLine(vector4, to2);
		}
	}
}
