using UnityEngine;

public class KIDHandReference : MonoBehaviour
{
	[SerializeField]
	private GameObject _leftHand;

	[SerializeField]
	private GameObject _rightHand;

	private static GameObject _leftHandRef;

	private static GameObject _rightHandRef;

	public static GameObject LeftHand => _leftHandRef;

	public static GameObject RightHand => _rightHandRef;

	private void Awake()
	{
		_leftHandRef = _leftHand;
		_rightHandRef = _rightHand;
	}
}
