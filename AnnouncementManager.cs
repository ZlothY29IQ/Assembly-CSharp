using GorillaNetworking;
using LitJson;
using PlayFab;
using UnityEngine;

public class AnnouncementManager : MonoBehaviour
{
	private const string ANNOUNCEMENT_ID_PLAYERPREF_PREFIX = "announcement-id-";

	private const string ANNOUNCEMENT_TITLE_DATA_KEY = "AnnouncementData";

	private const string ANNOUNCEMENT_HEADING = "Announcement!";

	private const string ANNOUNCEMENT_BUTTON_TEXT = "Continue";

	[SerializeField]
	private MessageBox _announcementMessageBox;

	private string _announcementString = string.Empty;

	private SAnnouncementData _announcementData;

	private bool _showAnnouncement;

	private static AnnouncementManager _instance;

	private static string _announcementIDPref = "";

	public bool _completedSetup { get; private set; }

	public bool _announcementActive { get; private set; }

	public static AnnouncementManager Instance
	{
		get
		{
			if (_instance == null)
			{
				Debug.LogError("[KID::ANNOUNCEMENT] [_instance] is NULL, does it exist in the scene?");
			}
			return _instance;
		}
	}

	private static string AnnouncementDPlayerPref
	{
		get
		{
			if (string.IsNullOrEmpty(_announcementIDPref))
			{
				_announcementIDPref = "announcement-id-" + PlayFabAuthenticator.instance.GetPlayFabPlayerId();
			}
			return _announcementIDPref;
		}
	}

	public bool ShowAnnouncement()
	{
		return _showAnnouncement;
	}

	private void Awake()
	{
		if (_instance != null)
		{
			Debug.LogError("[KID::ANNOUNCEMENT] [AnnouncementManager] has already been setup, does another already exist in the scene?");
			return;
		}
		_instance = this;
		if (_announcementMessageBox == null)
		{
			Debug.LogError("[ANNOUNCEMENT] Announcement Message Box has not been set. Announcement system will not work without it");
		}
	}

	private void Start()
	{
		if (!(_announcementMessageBox == null))
		{
			_announcementMessageBox.RightButton = "";
			_announcementMessageBox.LeftButton = "Continue";
			PlayFabTitleDataCache.Instance.GetTitleData("AnnouncementData", ConfigureAnnouncement, OnError);
		}
	}

	public void OnContinuePressed()
	{
		HandRayController.Instance.DisableHandRays();
		if (_announcementMessageBox == null)
		{
			Debug.LogError("[ANNOUNCEMENT] Message Box is null, Continue Button cannot work");
			return;
		}
		PrivateUIRoom.RemoveUI(_announcementMessageBox.transform);
		_announcementActive = false;
		PlayerPrefs.SetString(AnnouncementDPlayerPref, _announcementData.AnnouncementID);
		PlayerPrefs.Save();
	}

	private void OnError(PlayFabError error)
	{
		Debug.LogError("[ANNOUNCEMENT] Failed to Get Title Data for key [AnnouncementData]. Error:\n[" + error.ErrorMessage);
		_completedSetup = true;
	}

	private void ConfigureAnnouncement(string data)
	{
		_announcementString = data;
		_announcementData = JsonMapper.ToObject<SAnnouncementData>(_announcementString);
		if (!bool.TryParse(_announcementData.ShowAnnouncement, out _showAnnouncement))
		{
			_completedSetup = true;
			Debug.LogError("[ANNOUNCEMENT] Failed to parse [ShowAnnouncement] with value [" + _announcementData.ShowAnnouncement + "] to a bool, assuming false");
			return;
		}
		if (!ShowAnnouncement())
		{
			_completedSetup = true;
			return;
		}
		if (string.IsNullOrEmpty(_announcementData.AnnouncementID))
		{
			_completedSetup = true;
			Debug.LogError("[ANNOUNCEMENT] Announcement Version is empty or null. Will not show announcement");
			return;
		}
		string text = PlayerPrefs.GetString(AnnouncementDPlayerPref, "");
		if (_announcementData.AnnouncementID == text)
		{
			_completedSetup = true;
			return;
		}
		PrivateUIRoom.ForceStartOverlay(PrivateUIRoom.OverlaySource.KID);
		HandRayController.Instance.EnableHandRays();
		_announcementMessageBox.Header = _announcementData.AnnouncementTitle;
		_announcementMessageBox.Body = _announcementData.Message;
		_announcementActive = true;
		PrivateUIRoom.AddUI(_announcementMessageBox.transform);
		_completedSetup = true;
	}
}
