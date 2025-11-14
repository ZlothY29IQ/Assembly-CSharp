using UnityEngine;

namespace PerformanceSystems;

public class TimeSliceControllerBehaviour : MonoBehaviour
{
	[SerializeField]
	private TimeSliceControllerAsset _timeSliceControllerAsset;

	private void Awake()
	{
		_timeSliceControllerAsset.InitializeReferenceTransformWithMainCam();
	}

	private void Update()
	{
		_timeSliceControllerAsset.Update();
	}
}
