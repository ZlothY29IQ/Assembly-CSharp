using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class MessageBox : MonoBehaviour
{
	[SerializeField]
	private TMP_Text _headerText;

	[SerializeField]
	private TMP_Text _bodyText;

	[SerializeField]
	private TMP_Text _leftButtonText;

	[SerializeField]
	private TMP_Text _rightButtonText;

	[SerializeField]
	private GameObject _leftButton;

	[SerializeField]
	private GameObject _rightButton;

	[SerializeField]
	private UnityEvent _leftButtonCallback = new UnityEvent();

	[SerializeField]
	private UnityEvent _rightButtonCallback = new UnityEvent();

	public MessageBoxResult Result { get; private set; }

	public string Header
	{
		get
		{
			return _headerText.text;
		}
		set
		{
			_headerText.text = value;
			_headerText.gameObject.SetActive(!string.IsNullOrEmpty(value));
		}
	}

	public string Body
	{
		get
		{
			return _bodyText.text;
		}
		set
		{
			_bodyText.text = value;
		}
	}

	public string LeftButton
	{
		get
		{
			return _leftButtonText.text;
		}
		set
		{
			_leftButtonText.text = value;
			_leftButton.SetActive(!string.IsNullOrEmpty(value));
			if (string.IsNullOrEmpty(value))
			{
				RectTransform component = _rightButton.GetComponent<RectTransform>();
				component.anchorMin = new Vector2(0.5f, 0.5f);
				component.anchorMax = new Vector2(0.5f, 0.5f);
				component.pivot = new Vector2(0.5f, 0.5f);
				component.anchoredPosition = Vector3.zero;
			}
			else
			{
				RectTransform component2 = _rightButton.GetComponent<RectTransform>();
				component2.anchorMin = new Vector2(1f, 0.5f);
				component2.anchorMax = new Vector2(1f, 0.5f);
				component2.pivot = new Vector2(1f, 0.5f);
				component2.anchoredPosition = Vector3.zero;
			}
		}
	}

	public string RightButton
	{
		get
		{
			return _rightButtonText.text;
		}
		set
		{
			_rightButtonText.text = value;
			_rightButton.SetActive(!string.IsNullOrEmpty(value));
			if (string.IsNullOrEmpty(value))
			{
				RectTransform component = _leftButton.GetComponent<RectTransform>();
				component.anchorMin = new Vector2(0.5f, 0.5f);
				component.anchorMax = new Vector2(0.5f, 0.5f);
				component.pivot = new Vector2(0.5f, 0.5f);
				component.anchoredPosition3D = Vector3.zero;
			}
			else
			{
				RectTransform component2 = _leftButton.GetComponent<RectTransform>();
				component2.anchorMin = new Vector2(0f, 0.5f);
				component2.anchorMax = new Vector2(0f, 0.5f);
				component2.pivot = new Vector2(0f, 0.5f);
				component2.anchoredPosition3D = Vector3.zero;
			}
		}
	}

	public UnityEvent LeftButtonCallback => _leftButtonCallback;

	public UnityEvent RightButtonCallback => _rightButtonCallback;

	private void Start()
	{
		Result = MessageBoxResult.None;
	}

	private void Update()
	{
	}

	public void ShowQuitButtonAsPrimary()
	{
		_leftButton.SetActive(value: false);
		RectTransform component = _rightButton.GetComponent<RectTransform>();
		component.anchorMin = new Vector2(0.5f, 0.5f);
		component.anchorMax = new Vector2(0.5f, 0.5f);
		component.pivot = new Vector2(0.5f, 0.5f);
		component.anchoredPosition = Vector3.zero;
	}

	public void OnClickLeftButton()
	{
		Result = MessageBoxResult.Left;
		_leftButtonCallback.Invoke();
	}

	public void OnClickRightButton()
	{
		Result = MessageBoxResult.Right;
		_rightButtonCallback.Invoke();
	}

	public GameObject GetCanvas()
	{
		return GetComponentInChildren<Canvas>(includeInactive: true).gameObject;
	}

	public void OnDisable()
	{
		KIDAudioManager.Instance?.PlaySoundWithDelay(KIDAudioManager.KIDSoundType.PageTransition);
	}
}
