using System;
using Unity.Jobs;

namespace Voxels;

public struct ChunkTask
{
	public Chunk Chunk;

	public JobHandle Handle;

	private Action _onJobComplete;

	public bool IsCreated => !Handle.Equals(default(JobHandle));

	public bool IsCompleted => Handle.IsCompleted;

	public bool CompleteIfReady()
	{
		if (Handle.IsCompleted)
		{
			Complete();
			return true;
		}
		return false;
	}

	public void Complete()
	{
		Handle.Complete();
		_onJobComplete?.Invoke();
		_onJobComplete = null;
	}

	public ChunkTask(Chunk chunk, JobHandle handle, Action onComplete = null)
	{
		Chunk = chunk;
		Handle = handle;
		_onJobComplete = onComplete;
	}

	public static ChunkTask CreateCollisionJob(Chunk chunk)
	{
		if (chunk.Mesh == null)
		{
			chunk.IsCollisionBaked = true;
			chunk.IsDirty = true;
			return default(ChunkTask);
		}
		JobHandle handle = new CollisionJob
		{
			MeshId = chunk.Mesh.GetEntityId()
		}.Schedule();
		Action onJobComplete = delegate
		{
			chunk.IsCollisionBaked = true;
			chunk.IsDirty = true;
		};
		return new ChunkTask
		{
			Handle = handle,
			_onJobComplete = onJobComplete,
			Chunk = chunk
		};
	}
}
