using UnityEngine;

public class GTSignalEmitter : MonoBehaviour
{
	[Space]
	public GTSignalID signal;

	public GTSignal.EmitMode emitMode;

	public virtual void Emit()
	{
		GTSignal.Emit(emitMode, signal);
	}

	public virtual void Emit(int targetActor)
	{
		GTSignal.Emit(targetActor, signal);
	}

	public virtual void Emit(params object[] data)
	{
		GTSignal.Emit(emitMode, signal, data);
	}
}
