using GorillaExtensions;
using UnityEngine;

[ExecuteAlways]
public class SIPosedTentacleArm : MonoBehaviour
{
	public float LengthFactor = 1.5f;

	public MeshRenderer tentacleRenderer;

	public MeshRenderer tentacleRenderer2;

	public Transform tentacleAnchor;

	public Transform tentacleAnchor2;

	public Material tentacleSharedMaterial;

	private bool _initialized;

	private bool _hasTentacle2;

	private Material _tentacleMat;

	private Material _tentacleMat2;

	private Vector3 _lastPos;

	private Vector3 _lastAnchorPos;

	private ShaderHashId tentacleStartDir_HASH = "_TentacleStartDir";

	private ShaderHashId tentacleEnd_HASH = "_TentacleEndPos";

	private ShaderHashId tentacleEndDir_HASH = "_TentacleEndDir";

	private ShaderHashId tentacleRingOrigin_HASH = "_TentacleRingOrigin";

	public void ConfigureFrom(SIGadgetTentacleArm source, MeshRenderer rend1, MeshRenderer rend2, Transform anchor1, Transform anchor2)
	{
		LengthFactor = source.LengthFactor;
		tentacleRenderer = rend1;
		tentacleRenderer2 = rend2;
		tentacleAnchor = anchor1;
		tentacleAnchor2 = anchor2;
		tentacleSharedMaterial = rend1.sharedMaterial;
	}

	private void Start()
	{
		UpdateTentaclePose();
	}

	private bool CanUpdateTentaclePose()
	{
		return true;
	}

	private void EnsureMaterialsInitialized()
	{
		if (!_initialized)
		{
			_tentacleMat = new Material(tentacleSharedMaterial);
			tentacleRenderer.material = _tentacleMat;
			_hasTentacle2 = tentacleRenderer2;
			if (_hasTentacle2)
			{
				_tentacleMat2 = new Material(tentacleSharedMaterial);
				tentacleRenderer2.material = _tentacleMat2;
			}
			_initialized = true;
		}
	}

	private void UpdateTentaclePose()
	{
		if (CanUpdateTentaclePose())
		{
			EnsureMaterialsInitialized();
			UpdateTentacle(_tentacleMat, tentacleRenderer.transform, tentacleAnchor);
			if (_hasTentacle2)
			{
				UpdateTentacle(_tentacleMat2, tentacleRenderer2.transform, tentacleAnchor2);
			}
		}
	}

	private void UpdateTentacle(Material material, Transform tentacle, Transform anchor)
	{
		Vector3 vector = Vector3.forward * LengthFactor;
		material.SetVector(tentacleStartDir_HASH, vector);
		Vector3 vector2 = tentacle.InverseTransformPoint(anchor.position);
		material.SetVector(tentacleEnd_HASH, vector2);
		Vector3 vector3 = -tentacle.InverseTransformDirection(anchor.forward) * LengthFactor;
		material.SetVector(tentacleEndDir_HASH, vector3);
		Vector3 vector4 = SIGadgetTentacleArm.SplineSample(0.25f, vector, vector2, vector3);
		Vector3 vector5 = SIGadgetTentacleArm.SplineSample(0.26f, vector, vector2, vector3);
		Vector3 vector6 = SIGadgetTentacleArm.SplineSample(0.75f, vector, vector2, vector3);
		Vector3 vector7 = SIGadgetTentacleArm.SplineSample(0.76f, vector, vector2, vector3);
		Vector3 planeIntersection = SIGadgetTentacleArm.GetPlaneIntersection(vector4, (vector5 - vector4).normalized, vector6, (vector7 - vector6).normalized, Quaternion.AngleAxis(90f, Vector3.forward) * vector2.WithZ(0f).normalized);
		material.SetVector(tentacleRingOrigin_HASH, planeIntersection);
	}
}
