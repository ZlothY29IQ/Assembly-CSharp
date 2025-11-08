using GT_CustomMapSupportRuntime;
using UnityEngine;

namespace GorillaTagScripts;

[RequireComponent(typeof(Collider))]
public class MovingSurface : MonoBehaviour
{
	[SerializeField]
	private int uniqueId = -1;

	private void Start()
	{
		_ = MovingSurfaceManager.instance == null;
		MovingSurfaceManager.instance.RegisterMovingSurface(this);
	}

	private void OnDestroy()
	{
		if (MovingSurfaceManager.instance != null)
		{
			MovingSurfaceManager.instance.UnregisterMovingSurface(this);
		}
	}

	public int GetID()
	{
		return uniqueId;
	}

	public void CopySettings(MovingSurfaceSettings movingSurfaceSettings)
	{
		uniqueId = movingSurfaceSettings.uniqueId;
	}
}
