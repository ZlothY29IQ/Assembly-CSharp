using System.Collections.Generic;
using UnityEngine;

namespace GorillaTagScripts;

public class MovingSurfaceManager : MonoBehaviour
{
	private List<SurfaceMover> surfaceMovers = new List<SurfaceMover>(5);

	private Dictionary<int, MovingSurface> movingSurfaces = new Dictionary<int, MovingSurface>(10);

	public static MovingSurfaceManager instance;

	private void Awake()
	{
		if (instance != null && instance != this)
		{
			GTDev.LogWarning("Instance of MovingSurfaceManager already exists. Destroying.");
			Object.Destroy(this);
		}
		else if (instance == null)
		{
			instance = this;
		}
	}

	public void RegisterMovingSurface(MovingSurface ms)
	{
		movingSurfaces.TryAdd(ms.GetID(), ms);
	}

	public void UnregisterMovingSurface(MovingSurface ms)
	{
		movingSurfaces.Remove(ms.GetID());
	}

	public void RegisterSurfaceMover(SurfaceMover sm)
	{
		if (!surfaceMovers.Contains(sm))
		{
			surfaceMovers.Add(sm);
			sm.InitMovingSurface();
		}
	}

	public void UnregisterSurfaceMover(SurfaceMover sm)
	{
		surfaceMovers.Remove(sm);
	}

	public bool TryGetMovingSurface(int id, out MovingSurface result)
	{
		if (movingSurfaces.TryGetValue(id, out result))
		{
			return result != null;
		}
		return false;
	}

	private void FixedUpdate()
	{
		foreach (SurfaceMover surfaceMover in surfaceMovers)
		{
			if (surfaceMover.isActiveAndEnabled)
			{
				surfaceMover.Move();
			}
		}
	}
}
