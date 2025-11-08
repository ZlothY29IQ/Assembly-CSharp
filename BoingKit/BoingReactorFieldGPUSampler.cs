using UnityEngine;

namespace BoingKit;

public class BoingReactorFieldGPUSampler : MonoBehaviour
{
	public BoingReactorField ReactorField;

	[Range(0f, 10f)]
	[Tooltip("Multiplier on positional samples from reactor field.\n1.0 means 100%.")]
	public float PositionSampleMultiplier = 1f;

	[Range(0f, 10f)]
	[Tooltip("Multiplier on rotational samples from reactor field.\n1.0 means 100%.")]
	public float RotationSampleMultiplier = 1f;

	private MaterialPropertyBlock m_matProps;

	private int m_fieldResourceSetId = -1;

	public void OnEnable()
	{
		BoingManager.Register(this);
	}

	public void OnDisable()
	{
		BoingManager.Unregister(this);
	}

	public void Update()
	{
		if (ReactorField == null)
		{
			return;
		}
		BoingReactorField component = ReactorField.GetComponent<BoingReactorField>();
		if (component == null || component.HardwareMode != BoingReactorField.HardwareModeEnum.GPU || m_fieldResourceSetId == component.GpuResourceSetId)
		{
			return;
		}
		if (m_matProps == null)
		{
			m_matProps = new MaterialPropertyBlock();
		}
		if (!component.UpdateShaderConstants(m_matProps, PositionSampleMultiplier, RotationSampleMultiplier))
		{
			return;
		}
		m_fieldResourceSetId = component.GpuResourceSetId;
		Renderer[] array = new Renderer[2]
		{
			GetComponent<MeshRenderer>(),
			GetComponent<SkinnedMeshRenderer>()
		};
		foreach (Renderer renderer in array)
		{
			if (!(renderer == null))
			{
				renderer.SetPropertyBlock(m_matProps);
			}
		}
	}
}
