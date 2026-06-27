using System;
using System.Collections.Generic;
using GorillaLocomotion;
using GorillaNetworking;
using GorillaTagScripts;
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

	public const int PageCapacity = 9;

	public const int VIMPageCapacity = 9;

	[SerializeField]
	private int freeExtraPageCount = 1;

	[SerializeField]
	private int vimPageCount;

	private int cardsPerPage = 9;

	[SerializeField]
	private float pageButtonInactiveZPos;

	[SerializeField]
	private float pageButtonActiveZPos;

	private MeshRenderer[] _joinButtonRenderers;

	private bool inRemoveMode;

	private bool localPlayerAtDisplay;

	private int _currentPage;

	public static int ConfiguredVimPageCount { get; private set; } = 0;

	public static int ConfiguredFreeExtraPageCount { get; private set; } = 1;

	private int totalPages => 1 + freeExtraPageCount + vimPageCount;

	public int TotalCapacity => 9 + (freeExtraPageCount + vimPageCount) * 9;

	public int VIMTotalCapacity => vimPageCount * 9;

	public int FreeExtraTotalCapacity => freeExtraPageCount * 9;

	public int VimPageCount => vimPageCount;

	public bool InRemoveMode => inRemoveMode;

	private void Awake()
	{
		ConfiguredVimPageCount = vimPageCount;
		ConfiguredFreeExtraPageCount = freeExtraPageCount;
	}

	private void Start()
	{
		InitFriendCards();
		InitLocalPlayerCard();
		UpdateLocalPlayerPrivacyButtons();
		triggerNotifier.TriggerEnterEvent += TriggerEntered;
		triggerNotifier.TriggerExitEvent += TriggerExited;
		NetworkSystem.Instance.OnJoinedRoomEvent += new Action(OnJoinedRoom);
		SubscriptionManager.OnLocalSubscriptionData = (Action)Delegate.Combine(SubscriptionManager.OnLocalSubscriptionData, new Action(OnLocalSubscriptionChanged));
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
		SubscriptionManager.OnLocalSubscriptionData = (Action)Delegate.Remove(SubscriptionManager.OnLocalSubscriptionData, new Action(OnLocalSubscriptionChanged));
	}

	private void OnLocalSubscriptionChanged()
	{
		if (localPlayerAtDisplay)
		{
			GoToFriendPage(_currentPage);
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
		int count = FriendBackendController.Instance.FriendsList.Count;
		bool flag = SubscriptionManager.IsLocalSubscribed();
		int num = 1 + freeExtraPageCount;
		int num2 = 9 + freeExtraPageCount * 9;
		bool flag2 = freeExtraPageCount > 0;
		int num3 = Mathf.Min(totalPages, PageButtons.Length);
		if (!flag2)
		{
			for (int i = num; i < num3; i++)
			{
				int num4 = i - num;
				bool flag3 = count > num2 + num4 * 9;
				if (flag || flag3)
				{
					flag2 = true;
					break;
				}
			}
		}
		for (int j = 0; j < PageButtons.Length; j++)
		{
			bool flag4;
			if (j >= num3)
			{
				flag4 = false;
			}
			else if (j == 0)
			{
				flag4 = flag2;
			}
			else if (j < num)
			{
				flag4 = true;
			}
			else
			{
				int num5 = j - num;
				bool flag5 = count > num2 + num5 * 9;
				flag4 = flag || flag5;
			}
			if (flag4)
			{
				SetPageButtonAppearance(PageButtons[j], (j == selectedPage) ? ButtonState.Active : ButtonState.Default);
			}
			else
			{
				HidePageButton(PageButtons[j]);
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
			HidePageButton(PageButtons[i]);
		}
	}

	private void HidePageButton(MeshRenderer buttonRenderer)
	{
		buttonRenderer.enabled = false;
		buttonRenderer.GetComponent<BoxCollider>().enabled = false;
		buttonRenderer.transform.localPosition = new Vector3(buttonRenderer.transform.localPosition.x, buttonRenderer.transform.localPosition.y, pageButtonInactiveZPos);
	}

	private void SetPageButtonAppearance(MeshRenderer buttonRenderer, ButtonState state)
	{
		buttonRenderer.enabled = true;
		buttonRenderer.GetComponent<BoxCollider>().enabled = true;
		buttonRenderer.sharedMaterials = state switch
		{
			ButtonState.Default => _pageButtonDefaultMaterials, 
			ButtonState.Active => _pageButtonActiveMaterials, 
			ButtonState.Alert => _pageButtonAlerttMaterials, 
			_ => throw new ArgumentOutOfRangeException("state", state, null), 
		};
		Vector3 localPosition = buttonRenderer.transform.localPosition;
		buttonRenderer.transform.localPosition = new Vector3(localPosition.x, localPosition.y, pageButtonActiveZPos);
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
		UpdateLocalPlayerPrivacyButtons();
		PopulateLocalPlayerCard();
		GoToFriendPage(_currentPage);
	}

	public void GoToFriendPage(int currentPage)
	{
		int num = Mathf.Min(totalPages, PageButtons.Length);
		if (currentPage < 0 || currentPage >= num)
		{
			currentPage = 0;
		}
		_currentPage = currentPage;
		UpdatePageButtons(currentPage);
		for (int i = 0; i < friendCards.Length; i++)
		{
			friendCards[i].SetEmpty();
		}
		List<FriendBackendController.Friend> friendsList = FriendBackendController.Instance.FriendsList;
		int num2 = currentPage * cardsPerPage;
		int num3 = 9 + freeExtraPageCount * 9;
		for (int j = 0; j < friendCards.Length; j++)
		{
			int num4 = num2 + j;
			bool flag = num4 >= num3;
			if (num4 < friendsList.Count)
			{
				friendCards[j].Populate(friendsList[num4], flag);
			}
			else
			{
				friendCards[j].SetEmpty(flag);
			}
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
