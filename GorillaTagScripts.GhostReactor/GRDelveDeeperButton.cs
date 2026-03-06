using System;
using TMPro;
using UnityEngine;

namespace GorillaTagScripts.GhostReactor;

[RequireComponent(typeof(GorillaPressableButton))]
public sealed class GRDelveDeeperButton : MonoBehaviour, IGorillaSliceableSimple
{
	[SerializeField]
	private BoxCollider _drillCollider;

	[SerializeField]
	private GhostReactorShiftManager _shiftManager;

	[SerializeField]
	private TextMeshPro _text;

	private GorillaPressableButton _button;

	private int _numGorillasInDrill;

	private readonly Collider[] _overlapBoxResults = new Collider[200];

	private void Awake()
	{
		CountMonkes();
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.DrawCube(_drillCollider.bounds.center, _drillCollider.bounds.size);
	}

	private void CountMonkes()
	{
		int num = Physics.OverlapBoxNonAlloc(_drillCollider.bounds.center, _drillCollider.bounds.extents, _overlapBoxResults, _drillCollider.transform.rotation, 2048);
		_numGorillasInDrill = 0;
		for (int i = 0; i < num; i++)
		{
			if (_overlapBoxResults[i].GetComponent<VRRig>() != null && _drillCollider.bounds.Contains(_overlapBoxResults[i].transform.position))
			{
				_numGorillasInDrill++;
			}
		}
	}

	private void OnEnable()
	{
		if (_shiftManager == null)
		{
			throw new Exception("_shiftManager unset for GREndShiftButton.");
		}
		GorillaSlicerSimpleManager.RegisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.LateUpdate);
		_button = GetComponent<GorillaPressableButton>();
		UpdateButton();
	}

	private void OnDisable()
	{
		GorillaSlicerSimpleManager.UnregisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.LateUpdate);
	}

	public void SliceUpdate()
	{
		CountMonkes();
		UpdateButton();
	}

	private void UpdateButton()
	{
		if (_shiftManager.authorizedToDelveDeeper && _numGorillasInDrill == _shiftManager.reactor.NumActivePlayers)
		{
			_button.enabled = true;
			_text.text = "DELVE\nNOW";
		}
		else
		{
			_button.enabled = false;
			_text.text = "DISABLED";
		}
	}

	public void DelveDeeper()
	{
		_shiftManager.EndShift();
	}
}
