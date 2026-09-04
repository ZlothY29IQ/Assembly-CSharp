using System;
using UnityEngine.Events;

[Serializable]
public struct SynthesisUdpCommand
{
	public string commandName;

	public UnityEvent<string> EventReceiver;

	internal string extraArgs;
}
