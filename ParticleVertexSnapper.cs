using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class ParticleVertexSnapper : MonoBehaviour
{
	[SerializeField]
	private SkinnedMeshRenderer targetSkinnedMesh;

	private ParticleSystem particleSystemComponent;

	private ParticleSystem.Particle[] particles;

	private Mesh bakedMesh;

	private Vector3[] vertexPositions;

	private void Start()
	{
		particleSystemComponent = GetComponent<ParticleSystem>();
		bakedMesh = new Mesh();
		if (targetSkinnedMesh == null)
		{
			Debug.LogError("Please assign a Target Skinned Mesh Renderer!", this);
		}
	}

	private void LateUpdate()
	{
		if (targetSkinnedMesh == null || particleSystemComponent == null)
		{
			return;
		}
		targetSkinnedMesh.BakeMesh(bakedMesh);
		vertexPositions = bakedMesh.vertices;
		if (vertexPositions.Length == 0)
		{
			return;
		}
		int maxParticles = particleSystemComponent.main.maxParticles;
		if (particles == null || particles.Length < maxParticles)
		{
			particles = new ParticleSystem.Particle[maxParticles];
		}
		int num = particleSystemComponent.GetParticles(particles);
		for (int i = 0; i < num; i++)
		{
			int num2 = (int)(particles[i].randomSeed % vertexPositions.Length);
			Vector3 position = targetSkinnedMesh.transform.TransformPoint(vertexPositions[num2]);
			if (particleSystemComponent.main.simulationSpace == ParticleSystemSimulationSpace.Local)
			{
				particles[i].position = base.transform.InverseTransformPoint(position);
			}
			else
			{
				particles[i].position = position;
			}
		}
		particleSystemComponent.SetParticles(particles, num);
	}

	private void OnDestroy()
	{
		if (bakedMesh != null)
		{
			Object.Destroy(bakedMesh);
		}
	}
}
