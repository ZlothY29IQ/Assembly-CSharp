using UnityEngine;

public class BootscreenPositioner : MonoBehaviour
{
	[SerializeField]
	private Transform _pov;

	[SerializeField]
	private float _distanceThreshold;

	[SerializeField]
	private float _rotationThreshold;

	private void Awake()
	{
		base.transform.position = _pov.position;
		base.transform.rotation = Quaternion.Euler(0f, _pov.rotation.eulerAngles.y, 0f);
	}

	private void LateUpdate()
	{
		if (Vector3.Distance(base.transform.position, _pov.position) > _distanceThreshold)
		{
			base.transform.position = _pov.position;
		}
		if (Mathf.Abs(_pov.rotation.eulerAngles.y - base.transform.rotation.eulerAngles.y) > _rotationThreshold)
		{
			base.transform.rotation = Quaternion.Euler(0f, _pov.rotation.eulerAngles.y, 0f);
		}
	}
}
