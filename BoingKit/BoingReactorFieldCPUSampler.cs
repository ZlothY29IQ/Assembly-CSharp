using UnityEngine;

namespace BoingKit;

public class BoingReactorFieldCPUSampler : MonoBehaviour
{
	public BoingReactorField ReactorField;

	[Tooltip("Match this mode with how you update your object's transform.\n\nUpdate - Use this mode if you update your object's transform in Update(). This uses variable Time.detalTime. Use FixedUpdate if physics simulation becomes unstable.\n\nFixed Update - Use this mode if you update your object's transform in FixedUpdate(). This uses fixed Time.fixedDeltaTime. Also, use this mode if the game object is affected by Unity physics (i.e. has a rigid body component), which uses fixed updates.")]
	public BoingManager.UpdateMode UpdateMode = BoingManager.UpdateMode.LateUpdate;

	[Range(0f, 10f)]
	[Tooltip("Multiplier on positional samples from reactor field.\n1.0 means 100%.")]
	public float PositionSampleMultiplier = 1f;

	[Range(0f, 10f)]
	[Tooltip("Multiplier on rotational samples from reactor field.\n1.0 means 100%.")]
	public float RotationSampleMultiplier = 1f;

	private Vector3 m_objPosition;

	private Quaternion m_objRotation;

	public void OnEnable()
	{
		BoingManager.Register(this);
	}

	public void OnDisable()
	{
		BoingManager.Unregister(this);
	}

	public void SampleFromField()
	{
		m_objPosition = base.transform.position;
		m_objRotation = base.transform.rotation;
		if (!(ReactorField == null))
		{
			BoingReactorField component = ReactorField.GetComponent<BoingReactorField>();
			if (!(component == null) && component.HardwareMode == BoingReactorField.HardwareModeEnum.CPU && component.SampleCpuGrid(base.transform.position, out var positionOffset, out var rotationOffset))
			{
				base.transform.position = m_objPosition + positionOffset * PositionSampleMultiplier;
				base.transform.rotation = QuaternionUtil.Pow(QuaternionUtil.FromVector4(rotationOffset), RotationSampleMultiplier) * m_objRotation;
			}
		}
	}

	public void Restore()
	{
		base.transform.position = m_objPosition;
		base.transform.rotation = m_objRotation;
	}
}
