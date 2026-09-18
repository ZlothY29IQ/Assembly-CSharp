using Cosmetics;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace GorillaNetworking.Store;

public class BundleStand : MonoBehaviour, IBuildValidation
{
	public BundlePurchaseButton _bundlePurchaseButton;

	[SerializeField]
	public StoreBundleData _bundleDataReference;

	[SerializeField]
	private GameObject creatorCodeProvider;

	public GameObject[] EditorOnlyObjects;

	public Text _bundleDescriptionText;

	public Image _bundleIcon;

	public UnityEvent AlreadyOwnEvent;

	public UnityEvent ErrorHappenedEvent;

	public string playfabBundleID => _bundleDataReference.playfabBundleID;

	[field: SerializeField]
	public GtfcPriceLabel GtfcPriceLabel { get; private set; }

	[field: SerializeField]
	public GameObject[] GtfcObjects { get; private set; }

	bool IBuildValidation.BuildValidationCheck()
	{
		if (creatorCodeProvider == null || !creatorCodeProvider.TryGetComponent<ICreatorCodeProvider>(out var _))
		{
			Debug.LogError(base.name + " has no Creator Code Provider. This will break bundle purchasing.");
			return false;
		}
		return true;
	}

	public void Awake()
	{
		_bundlePurchaseButton.playfabID = playfabBundleID;
		if (_bundleIcon != null && _bundleDataReference != null && _bundleDataReference.bundleImage != null)
		{
			_bundleIcon.sprite = _bundleDataReference.bundleImage;
		}
		_bundlePurchaseButton.codeProvider = creatorCodeProvider.GetComponent<ICreatorCodeProvider>();
	}

	public void InitializeEventListeners()
	{
		AlreadyOwnEvent.AddListener(_bundlePurchaseButton.AlreadyOwn);
		ErrorHappenedEvent.AddListener(_bundlePurchaseButton.ErrorHappened);
	}

	public void NotifyAlreadyOwn()
	{
		AlreadyOwnEvent.Invoke();
	}

	public void ErrorHappened()
	{
		ErrorHappenedEvent.Invoke();
	}

	public void UpdatePurchaseButtonText(string text)
	{
		if (_bundlePurchaseButton != null)
		{
			_bundlePurchaseButton.UpdatePurchaseButtonText(text);
		}
	}

	public void UpdateDescriptionText(string descriptionText)
	{
		if (_bundleDescriptionText != null)
		{
			_bundleDescriptionText.text = descriptionText;
		}
	}
}
