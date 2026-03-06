using System;
using UnityEngine;
using UnityEngine.Events;

namespace GorillaTagScripts.Subscription;

public class FeatureTogglesScreen : MonoBehaviour, IGorillaSliceableSimple
{
	[Serializable]
	public class Feature
	{
		public string DisplayName = string.Empty;

		public SubscriptionManager.SubscriptionFeatures Value;

		public UnityEvent OnPressed;

		public UnityEvent<bool> OnToggle;

		public string UnavailableMessage = "NOT AVAILABLE ON THIS DEVICE";
	}

	private const int TogglesPerPage = 3;

	[SerializeField]
	private Feature[] _features;

	[SerializeField]
	private SITouchscreenButtonContainer _nextButton;

	[SerializeField]
	private SITouchscreenButtonContainer _backButton;

	[SerializeField]
	private SITouchscreenButtonContainer _exitButton;

	[SerializeField]
	private FeatureToggleUI[] _featureToggleUi;

	private int _currentPage;

	private bool _dirty = true;

	private int NumPages
	{
		get
		{
			if (_features.Length % 3 != 0)
			{
				return _features.Length / 3 + 1;
			}
			return _features.Length / 3;
		}
	}

	private int LastPageIndex => Math.Max(0, NumPages - 1);

	private void Awake()
	{
		_nextButton.button.buttonPressed.AddListener(OnNextButtonPressed);
		_backButton.button.buttonPressed.AddListener(OnBackButtonPressed);
		_exitButton.button.buttonPressed.AddListener(OnExitButtonPressed);
		MarkDirty();
	}

	private void OnNextButtonPressed(SITouchscreenButton.SITouchscreenButtonType type, int data, int actorNr)
	{
		_currentPage++;
		if (_currentPage > LastPageIndex)
		{
			_currentPage = LastPageIndex;
		}
		MarkDirty();
	}

	private void OnBackButtonPressed(SITouchscreenButton.SITouchscreenButtonType type, int data, int actorNr)
	{
		_currentPage--;
		if (_currentPage < 0)
		{
			_currentPage = 0;
		}
		MarkDirty();
	}

	private void OnExitButtonPressed(SITouchscreenButton.SITouchscreenButtonType type, int data, int actorNr)
	{
	}

	public void SliceUpdate()
	{
		if (_dirty)
		{
			_backButton.gameObject.SetActive(_currentPage != 0);
			_nextButton.gameObject.SetActive(_currentPage != LastPageIndex);
			UpdateFeatureToggleUI();
			_dirty = false;
		}
	}

	private void OnEnable()
	{
		GorillaSlicerSimpleManager.RegisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.LateUpdate);
		MarkDirty();
	}

	private void OnDisable()
	{
		GorillaSlicerSimpleManager.UnregisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.LateUpdate);
	}

	private void UpdateFeatureToggleUI()
	{
		for (int i = 0; i < _featureToggleUi.Length; i++)
		{
			FeatureToggleUI featureToggleUI = _featureToggleUi[i];
			int num = _currentPage * 3 + i;
			bool flag = num < _features.Length;
			featureToggleUI.gameObject.SetActive(flag);
			if (flag)
			{
				Feature feature = _features[num];
				featureToggleUI.AttachToFeature(feature);
			}
		}
	}

	public void MarkDirty()
	{
		_dirty = true;
	}
}
