using GorillaNetworking;
using TMPro;
using UnityEngine;

public class FriendCard : MonoBehaviour
{
	[SerializeField]
	private TextMeshProUGUI nameText;

	[SerializeField]
	private TextMeshProUGUI roomText;

	[SerializeField]
	private TextMeshProUGUI zoneText;

	[SerializeField]
	private Transform removeProgressBar;

	[SerializeField]
	private float width = 0.25f;

	private string emptyString = "";

	private string privateString = "PRIVATE";

	private bool joinable;

	private bool canRemove;

	private GorillaPressableDelayButton _button;

	private TextMeshProUGUI _buttonText;

	private string _friendName;

	private string _friendRoom;

	private string _friendZone;

	private FriendBackendController.Friend currentFriend;

	private FriendDisplay friendDisplay;

	private string[] randomNames = new string[47]
	{
		"Veronica", "Roman", "Janiyah", "Dalton", "Bellamy", "Eithan", "Celeste", "Isaac", "Astrid", "Azariah",
		"Keilani", "Zeke", "Jayleen", "Yosef", "Jaylee", "Bodie", "Greta", "Cain", "Ella", "Everly",
		"Finnley", "Paisley", "Kaison", "Luna", "Nina", "Maison", "Monroe", "Ricardo", "Zariyah", "Travis",
		"Lacey", "Elian", "Frankie", "Otis", "Adele", "Edison", "Amira", "Ivan", "Raelynn", "Eliel",
		"Aliana", "Beckett", "Mylah", "Melvin", "Magdalena", "Leroy", "Madeleine"
	};

	private FriendDisplay.ButtonState _buttonState = (FriendDisplay.ButtonState)(-1);

	private Material[] _buttonDefaultMaterials;

	private Material[] _buttonActiveMaterials;

	private Material[] _buttonAlertMaterials;

	public TextMeshProUGUI NameText => nameText;

	public TextMeshProUGUI RoomText => roomText;

	public TextMeshProUGUI ZoneText => zoneText;

	public float Width => width;

	[field: SerializeField]
	public float Height { get; private set; } = 0.25f;

	private void Awake()
	{
		if ((bool)removeProgressBar)
		{
			removeProgressBar.gameObject.SetActive(value: false);
		}
	}

	private void OnDestroy()
	{
		if ((bool)_button)
		{
			_button.onPressed -= OnButtonPressed;
		}
	}

	public void Init(FriendDisplay owner)
	{
		friendDisplay = owner;
	}

	private void UpdateComponentStates()
	{
		if ((bool)removeProgressBar)
		{
			removeProgressBar.gameObject.SetActive(canRemove);
		}
		if (canRemove)
		{
			SetButtonState((currentFriend != null) ? FriendDisplay.ButtonState.Alert : FriendDisplay.ButtonState.Default);
		}
		else if (joinable)
		{
			SetButtonState(FriendDisplay.ButtonState.Active);
		}
		else
		{
			SetButtonState(FriendDisplay.ButtonState.Default);
		}
	}

	private void SetButtonState(FriendDisplay.ButtonState newState)
	{
		if (!(_button == null) && _buttonState != newState)
		{
			_buttonState = newState;
			MeshRenderer buttonRenderer = _button.buttonRenderer;
			FriendDisplay.ButtonState buttonState = _buttonState;
			Material[] sharedMaterials = default(Material[]);
			switch (buttonState)
			{
			case FriendDisplay.ButtonState.Default:
				sharedMaterials = _buttonDefaultMaterials;
				break;
			case FriendDisplay.ButtonState.Active:
				sharedMaterials = _buttonActiveMaterials;
				break;
			case FriendDisplay.ButtonState.Alert:
				sharedMaterials = _buttonAlertMaterials;
				break;
			default:
				global::_003CPrivateImplementationDetails_003E.ThrowSwitchExpressionException(buttonState);
				break;
			}
			buttonRenderer.sharedMaterials = sharedMaterials;
			_button.delayTime = ((_buttonState == FriendDisplay.ButtonState.Alert) ? 3 : 0);
		}
	}

	public void Populate(FriendBackendController.Friend friend)
	{
		SetEmpty();
		if (friend != null && friend.Presence != null)
		{
			if (friend.Presence.UserName != null)
			{
				SetName(friend.Presence.UserName.ToUpper());
			}
			if (!string.IsNullOrEmpty(friend.Presence.RoomId) && friend.Presence.RoomId.Length > 0)
			{
				bool flag = friend.Presence.IsPublic == true;
				bool flag2 = friend.Presence.RoomId[0] == '@';
				bool flag3 = friend.Presence.RoomId.Equals(NetworkSystem.Instance.RoomName);
				bool flag4 = false;
				if (!flag3 && flag && !friend.Presence.Zone.IsNullOrEmpty())
				{
					string text = friend.Presence.Zone.ToLower();
					foreach (GTZone activeZone in ZoneManagement.instance.activeZones)
					{
						if (text.Contains(activeZone.GetName().ToLower()))
						{
							flag4 = true;
						}
					}
				}
				joinable = !flag2 && !flag3 && (!flag || flag4) && HasKIDPermissionToJoinPrivateRooms();
				if (flag2)
				{
					SetRoom(friend.Presence.RoomId.Substring(1).ToUpper());
					SetZone("CUSTOM");
				}
				else if (!flag)
				{
					SetRoom(friend.Presence.RoomId.ToUpper());
					SetZone("PRIVATE");
				}
				else if (friend.Presence.Zone != null)
				{
					SetRoom(friend.Presence.RoomId.ToUpper());
					SetZone(friend.Presence.Zone.ToUpper());
				}
			}
			else
			{
				joinable = false;
				SetRoom("OFFLINE");
			}
			currentFriend = friend;
		}
		UpdateComponentStates();
	}

	public void SetName(string friendName)
	{
		nameText.text = (_friendName = friendName);
	}

	public void SetRoom(string friendRoom)
	{
		roomText.text = (_friendRoom = friendRoom);
	}

	public void SetZone(string friendZone)
	{
		zoneText.text = (_friendZone = friendZone);
	}

	public void Randomize()
	{
		SetEmpty();
		int num = Random.Range(0, randomNames.Length);
		SetName(randomNames[num].ToUpper());
		SetRoom($"{(char)Random.Range(65, 91)}{(char)Random.Range(65, 91)}{(char)Random.Range(65, 91)}{(char)Random.Range(65, 91)}");
		bool flag = Random.Range(0f, 1f) > 0.5f;
		joinable = flag && Random.Range(0f, 1f) > 0.5f;
		if (flag)
		{
			int num2 = Random.Range(0, 17);
			GTZone gTZone = (GTZone)num2;
			SetZone(gTZone.ToString().ToUpper());
		}
		else
		{
			SetZone(privateString);
		}
		UpdateComponentStates();
	}

	public void SetEmpty()
	{
		SetName(emptyString);
		SetRoom(emptyString);
		SetZone(emptyString);
		joinable = false;
		currentFriend = null;
		UpdateComponentStates();
	}

	public void SetRemoveEnabled(bool enabled)
	{
		canRemove = enabled;
		UpdateComponentStates();
	}

	private void JoinButtonPressed()
	{
		if (joinable && currentFriend != null && currentFriend.Presence != null)
		{
			JoinType roomJoinType = ((currentFriend.Presence.IsPublic == true) ? JoinType.FriendStationPublic : JoinType.FriendStationPrivate);
			GorillaComputer.instance.roomToJoin = _friendRoom;
			PhotonNetworkController.Instance.AttemptToJoinSpecificRoom(_friendRoom, roomJoinType);
			joinable = false;
			UpdateComponentStates();
		}
	}

	private void RemoveFriendButtonPressed()
	{
		if (friendDisplay.InRemoveMode)
		{
			FriendSystem.Instance.RemoveFriend(currentFriend);
			SetEmpty();
		}
	}

	private void OnDrawGizmosSelected()
	{
		float num = width * 0.5f * base.transform.lossyScale.x;
		float num2 = Height * 0.5f * base.transform.lossyScale.y;
		float num3 = num;
		float num4 = num2;
		Vector3 vector = base.transform.position + base.transform.rotation * new Vector3(0f - num3, num4, 0f);
		Vector3 vector2 = base.transform.position + base.transform.rotation * new Vector3(num3, num4, 0f);
		Vector3 vector3 = base.transform.position + base.transform.rotation * new Vector3(0f - num3, 0f - num4, 0f);
		Vector3 vector4 = base.transform.position + base.transform.rotation * new Vector3(num3, 0f - num4, 0f);
		Gizmos.color = Color.white;
		Gizmos.DrawLine(vector, vector2);
		Gizmos.DrawLine(vector2, vector4);
		Gizmos.DrawLine(vector4, vector3);
		Gizmos.DrawLine(vector3, vector);
	}

	public void SetButton(GorillaPressableDelayButton friendCardButton, Material[] normalMaterials, Material[] activeMaterials, Material[] alertMaterials, TextMeshProUGUI buttonText)
	{
		_button = friendCardButton;
		_button.SetFillBar(removeProgressBar);
		_button.onPressBegin += OnButtonPressBegin;
		_button.onPressAbort += OnButtonPressAbort;
		_button.onPressed += OnButtonPressed;
		_buttonDefaultMaterials = normalMaterials;
		_buttonActiveMaterials = activeMaterials;
		_buttonAlertMaterials = alertMaterials;
		_buttonText = buttonText;
		SetButtonState(FriendDisplay.ButtonState.Default);
	}

	private void OnRemoveFriendBegin()
	{
		nameText.text = "REMOVING";
		roomText.text = "FRIEND";
		zoneText.text = emptyString;
	}

	private void OnRemoveFriendEnd()
	{
		nameText.text = _friendName;
		roomText.text = _friendRoom;
		zoneText.text = _friendZone;
	}

	private void OnButtonPressBegin()
	{
		switch (_buttonState)
		{
		case FriendDisplay.ButtonState.Alert:
			OnRemoveFriendBegin();
			break;
		case FriendDisplay.ButtonState.Default:
		case FriendDisplay.ButtonState.Active:
			break;
		}
	}

	private void OnButtonPressAbort()
	{
		switch (_buttonState)
		{
		case FriendDisplay.ButtonState.Alert:
			OnRemoveFriendEnd();
			break;
		case FriendDisplay.ButtonState.Default:
		case FriendDisplay.ButtonState.Active:
			break;
		}
	}

	private void OnButtonPressed(GorillaPressableButton button, bool isLeftHand)
	{
		switch (_buttonState)
		{
		case FriendDisplay.ButtonState.Active:
			JoinButtonPressed();
			break;
		case FriendDisplay.ButtonState.Alert:
			RemoveFriendButtonPressed();
			break;
		case FriendDisplay.ButtonState.Default:
			break;
		}
	}

	private bool HasKIDPermissionToJoinPrivateRooms()
	{
		if (!KIDManager.KidEnabled)
		{
			return true;
		}
		if (KIDManager.HasPermissionToUseFeature(EKIDFeatures.Groups))
		{
			return KIDManager.HasPermissionToUseFeature(EKIDFeatures.Multiplayer);
		}
		return false;
	}
}
