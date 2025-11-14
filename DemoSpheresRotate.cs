using PerformanceSystems;
using UnityEngine;

public class DemoSpheresRotate : TimeSliceLodBehaviour
{
	[SerializeField]
	private TimeSliceControllerAsset[] _timeSliceControllerAssets;

	[SerializeField]
	private float _rotationSpeed = 10f;

	[SerializeField]
	private Material _red;

	[SerializeField]
	private Material _green;

	[SerializeField]
	private Material _black;

	[SerializeField]
	private Renderer _renderer;

	public void OnLod0Enter()
	{
		_renderer.material = _red;
		SwapToTimeSlicer(0);
		base.gameObject.SetActive(value: true);
	}

	public void OnLod1Enter()
	{
		_renderer.material = _green;
		SwapToTimeSlicer(1);
		base.gameObject.SetActive(value: true);
	}

	public void OnLod2Enter()
	{
		_renderer.material = _black;
		SwapToTimeSlicer(2);
		base.gameObject.SetActive(value: true);
	}

	public void OnLodExit()
	{
		base.gameObject.SetActive(value: false);
	}

	public override void SliceUpdate(float deltaTime)
	{
		base.transform.Rotate(Vector3.up * _rotationSpeed * deltaTime);
	}

	private void SwapToTimeSlicer(int index)
	{
		if (!(_timeSliceControllerAssets[index] == _timeSliceControllerAsset))
		{
			_timeSliceControllerAsset.RemoveTimeSliceBehaviour(this);
			_timeSliceControllerAsset = _timeSliceControllerAssets[index];
			_timeSliceControllerAsset.AddTimeSliceBehaviour(this);
		}
	}
}
