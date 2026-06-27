using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;

namespace Voxels;

public class ChunkTaskSet
{
	public delegate ChunkTask ChunkTaskDelegate(Chunk chunk, GenerationParameters parameters);

	public List<Chunk> Chunks;

	public GenerationParameters Parameters;

	public Queue<(ChunkTaskDelegate task, Action<Chunk> callback)> Tasks;

	public ChunkTask Current;

	public Action<Chunk> Callback;

	public Chunk Chunk => Chunks[0];

	public bool HasChunks
	{
		get
		{
			if (Chunks == null || Chunks.Count == 0)
			{
				return false;
			}
			foreach (Chunk chunk in Chunks)
			{
				if (chunk == null)
				{
					return false;
				}
			}
			return true;
		}
	}

	public bool IsEmpty
	{
		get
		{
			if (!Current.IsCreated && Callback == null)
			{
				return Tasks.Count == 0;
			}
			return false;
		}
	}

	public ChunkTaskSet(GenerationParameters parameters)
	{
		Chunks = new List<Chunk>();
		Parameters = parameters;
		Tasks = new Queue<(ChunkTaskDelegate, Action<Chunk>)>();
	}

	public ChunkTaskSet(Chunk chunk, GenerationParameters parameters, params (ChunkTaskDelegate task, Action<Chunk> callback)[] tasks)
	{
		Chunks = new List<Chunk> { chunk };
		Parameters = parameters;
		Tasks = new Queue<(ChunkTaskDelegate, Action<Chunk>)>(tasks);
	}

	public ChunkTaskSet(IList<Chunk> chunks, GenerationParameters parameters, params (ChunkTaskDelegate task, Action<Chunk> callback)[] tasks)
	{
		Chunks = new List<Chunk>(chunks);
		Parameters = parameters;
		Tasks = new Queue<(ChunkTaskDelegate, Action<Chunk>)>(tasks);
	}

	public void AddTask(ChunkTaskDelegate task, Action<Chunk> callback = null)
	{
		Tasks.Enqueue((task, callback));
	}

	public void Start()
	{
		if (Current.IsCreated || Callback != null)
		{
			throw new InvalidOperationException("Cannot start a ChunkTaskSet that is already running.");
		}
		StartNext();
		UpdateDirty();
	}

	public void Complete()
	{
		CompleteCurrent();
		while (StartNext())
		{
			CompleteCurrent();
		}
		UpdateDirty();
	}

	public bool CompleteIfReady()
	{
		if (CompleteCurrentIfReady())
		{
			if (Tasks.Count == 0)
			{
				UpdateDirty();
				return true;
			}
			StartNext();
		}
		UpdateDirty();
		return false;
	}

	private bool CompleteCurrentIfReady()
	{
		if (Current.IsCompleted)
		{
			CompleteCurrent();
			return true;
		}
		return false;
	}

	private void CompleteCurrent()
	{
		Current.Complete();
		foreach (Chunk chunk in Chunks)
		{
			Callback?.Invoke(chunk);
		}
		Current = default(ChunkTask);
		Callback = null;
	}

	private bool StartNext()
	{
		if (Tasks.Count == 0)
		{
			return false;
		}
		(ChunkTaskDelegate, Action<Chunk>) task = Tasks.Dequeue();
		(Current, Callback) = CreateTask(task);
		if (Current.IsCompleted)
		{
			CompleteCurrent();
			return StartNext();
		}
		return true;
	}

	private void UpdateDirty()
	{
		if (Current.IsCreated || Callback != null || Tasks.Count > 0)
		{
			foreach (Chunk chunk in Chunks)
			{
				chunk.IsDirty = false;
			}
			return;
		}
		foreach (Chunk chunk2 in Chunks)
		{
			chunk2.IsDirty = chunk2.State < ChunkState.MeshAssigned;
		}
	}

	private (ChunkTask, Action<Chunk>) CreateTask((ChunkTaskDelegate task, Action<Chunk> callback) task)
	{
		return CreateTask(task.task, task.callback);
	}

	private (ChunkTask, Action<Chunk>) CreateTask(ChunkTaskDelegate task, Action<Chunk> callback = null)
	{
		if (Chunks.Count == 1)
		{
			return (task?.Invoke(Chunks[0], Parameters) ?? default(ChunkTask), callback);
		}
		NativeArray<JobHandle> jobs = new NativeArray<JobHandle>(Chunks.Count, Allocator.Temp);
		for (int i = 0; i < Chunks.Count; i++)
		{
			jobs[i] = task?.Invoke(Chunks[i], Parameters).Handle ?? default(JobHandle);
		}
		JobHandle handle = JobHandle.CombineDependencies(jobs);
		jobs.Dispose();
		return (new ChunkTask(Chunks[0], handle), callback);
	}
}
