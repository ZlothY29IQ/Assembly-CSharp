using TMPro;
using UnityEngine;

namespace GorillaTagScripts.Subscription;

[RequireComponent(typeof(SITouchscreenButtonContainer))]
public class FeatureToggleUI : MonoBehaviour
{
	[SerializeField]
	private TextMeshPro _label;

	[SerializeField]
	private TextMeshPro _unavailable;

	private const float DEBOUNCE_TIME = 0.5f;

	private float _disableUntil = float.MinValue;

	public SITouchscreenButtonContainer ButtonContainer { get; private set; }

	public string LabelText
	{
		get
		{
			return _label.text;
		}
		set
		{
			_label.text = value;
		}
	}

	private void Awake()
	{
		ButtonContainer = base.gameObject.GetComponent<SITouchscreenButtonContainer>();
	}

	public void AttachToFeature(FeatureTogglesScreen.Feature feature)
	{
		ButtonContainer.button.buttonPressed.RemoveAllListeners();
		ButtonContainer.button.buttonToggled.RemoveAllListeners();
		LabelText = feature.DisplayName;
		bool state = SubscriptionManager.GetSubscriptionSettingBool(feature.Value);
		bool num = SubscriptionManager.IsSubscriptionFeatureAvailable(feature.Value);
		bool flag = true;
		if (num && flag)
		{
			ButtonContainer.button.buttonPressed.AddListener(delegate(SITouchscreenButton.SITouchscreenButtonType type, int data, int nr)
			{
				OnPressed(nr, feature);
			});
			ButtonContainer.button.buttonToggled.AddListener(delegate(SITouchscreenButton.SITouchscreenButtonType type, int data, int nr, bool state2)
			{
				OnToggled(nr, feature, state2);
			});
			_unavailable.gameObject.SetActive(value: false);
		}
		else
		{
			state = false;
			_unavailable.gameObject.SetActive(value: true);
			if (!flag)
			{
				_unavailable.text = "ENABLE PERMISSION IN QUEST SETTINGS";
			}
			else
			{
				_unavailable.text = "NOT AVAILABLE ON THIS DEVICE";
			}
		}
		ButtonContainer.button.SetToggleState(state);
		ButtonContainer.UpdateToggleVisual();
	}

	private void OnPressed(int actorNr, FeatureTogglesScreen.Feature feature)
	{
		if (!(Time.time < _disableUntil) && actorNr == NetworkSystem.Instance.LocalPlayer.ActorNumber)
		{
			_disableUntil = Time.time + 0.5f;
			feature.OnPressed.Invoke();
		}
	}

	private void OnToggled(int actorNr, FeatureTogglesScreen.Feature feature, bool state)
	{
		if (!(Time.time < _disableUntil) && actorNr == NetworkSystem.Instance.LocalPlayer.ActorNumber)
		{
			_disableUntil = Time.time + 0.5f;
			feature.OnToggle.Invoke(state);
			ButtonContainer.button.SetToggleState(state);
			ButtonContainer.UpdateToggleVisual();
		}
	}
}
