using UnityEngine;

public class LifeCycleLogger : MonoBehaviour
{
	private void Awake()
	{
		PersistLog.Log($"[AC][F{Time.frameCount}] {base.name} Awake");
	}

	private void Start()
	{
		PersistLog.Log($"[AC][F{Time.frameCount}] {base.name} Start");
	}

	private void OnEnable()
	{
		PersistLog.Log($"[AC][F{Time.frameCount}] {base.name} Enable");
	}

	private void OnDisable()
	{
		PersistLog.Log($"[AC][F{Time.frameCount}] {base.name} Disable");
	}

	private void OnDestroy()
	{
		PersistLog.Log($"[AC][F{Time.frameCount}] {base.name} OnDestroy");
	}
}
