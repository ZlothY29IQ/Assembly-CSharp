using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class ParticleSystemEventShortcut : MonoBehaviour
{
	private bool initialized;

	private ParticleSystem ps;

	private ParticleSystem.ShapeModule shape;

	private bool poolExists;

	private void InitIfNeeded()
	{
		if (!initialized)
		{
			initialized = true;
			ps = GetComponent<ParticleSystem>();
			shape = ps.shape;
			poolExists = ObjectPools.instance.DoesPoolExist(base.gameObject);
		}
	}

	public void StopAndClear()
	{
		InitIfNeeded();
		ps.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmittingAndClear);
	}

	public void ClearAndPlay()
	{
		InitIfNeeded();
		ps.Clear();
		ps.Play();
	}

	public void PlayFromMesh(MeshRenderer mesh)
	{
		InitIfNeeded();
		shape.shapeType = ParticleSystemShapeType.MeshRenderer;
		shape.meshRenderer = mesh;
		ps.Play();
	}

	public void PlayFromSkin(SkinnedMeshRenderer skin)
	{
		InitIfNeeded();
		shape.shapeType = ParticleSystemShapeType.SkinnedMeshRenderer;
		shape.skinnedMeshRenderer = skin;
		ps.Play();
	}

	public void ReturnToPool()
	{
		InitIfNeeded();
		if (poolExists)
		{
			ObjectPools.instance.Destroy(base.gameObject);
		}
	}

	private void OnParticleSystemStopped()
	{
		ReturnToPool();
	}
}
