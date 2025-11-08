using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class EyeScannableMono : MonoBehaviour, IEyeScannable
{
	[SerializeField]
	private KeyValuePairSet data;

	private Bounds _bounds;

	private Vector3 _initialPosition;

	int IEyeScannable.scannableId => GetInstanceID();

	Vector3 IEyeScannable.Position => base.transform.position - _initialPosition + _bounds.center;

	Bounds IEyeScannable.Bounds => _bounds;

	IList<KeyValueStringPair> IEyeScannable.Entries => data.Entries;

	public event Action OnDataChange;

	private void Awake()
	{
		RecalculateBounds();
	}

	public void OnEnable()
	{
		RecalculateBoundsLater();
		EyeScannerMono.Register(this);
	}

	public void OnDisable()
	{
		EyeScannerMono.Unregister(this);
	}

	private async void RecalculateBoundsLater()
	{
		await Task.Delay(100);
		RecalculateBounds();
	}

	private void RecalculateBounds()
	{
		_initialPosition = base.transform.position;
		Collider[] componentsInChildren = GetComponentsInChildren<Collider>();
		_bounds = default(Bounds);
		if (componentsInChildren.Length == 0)
		{
			_bounds.center = base.transform.position;
			_bounds.Expand(1f);
			return;
		}
		_bounds = componentsInChildren[0].bounds;
		for (int i = 1; i < componentsInChildren.Length; i++)
		{
			_bounds.Encapsulate(componentsInChildren[i].bounds);
		}
	}
}
