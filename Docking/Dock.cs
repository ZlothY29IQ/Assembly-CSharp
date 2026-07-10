using UnityEngine;
using UnityEngine.Events;

namespace Docking;

public class Dock : MonoBehaviour
{
	[SerializeField]
	protected bool moveable;

	[SerializeField]
	protected float forceUndockTime;

	[SerializeField]
	protected UnityEvent OnDock;

	[SerializeField]
	protected UnityEvent OnUnDock;

	public bool Moveable => moveable;

	public float ForceUndockTime => forceUndockTime;

	public void NotifyDocked()
	{
		OnDock?.Invoke();
	}

	public void NotifyUnDocked()
	{
		OnUnDock?.Invoke();
	}
}
