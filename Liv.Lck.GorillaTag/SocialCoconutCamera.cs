using UnityEngine;

namespace Liv.Lck.GorillaTag;

public class SocialCoconutCamera : MonoBehaviour
{
	[SerializeField]
	private GameObject _visuals;

	[SerializeField]
	private MeshRenderer _bodyRenderer;

	private bool _isActive;

	private MaterialPropertyBlock _propertyBlock;

	private string IS_RECORDING = "_Is_Recording";

	private void Awake()
	{
		if (_propertyBlock == null)
		{
			_propertyBlock = new MaterialPropertyBlock();
		}
		_propertyBlock.SetInt(IS_RECORDING, 0);
		_bodyRenderer.SetPropertyBlock(_propertyBlock);
	}

	public void SetVisualsActive(bool active)
	{
		_isActive = active;
		_visuals.SetActive(active);
	}

	public void SetRecordingState(bool isRecording)
	{
		if (_isActive)
		{
			_propertyBlock.SetInt(IS_RECORDING, isRecording ? 1 : 0);
			_bodyRenderer.SetPropertyBlock(_propertyBlock);
		}
	}
}
