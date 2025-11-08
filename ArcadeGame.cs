using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Photon.Pun;
using UnityEngine;

public abstract class ArcadeGame : MonoBehaviour
{
	[SerializeField]
	public Vector2 Scale = new Vector2(1f, 1f);

	private ArcadeButtons[] playerInputs = new ArcadeButtons[4];

	public AudioClip[] audioClips;

	private ArcadeMachine machine;

	protected static int NetStateBufferSize = 512;

	protected byte[] netStateBuffer = new byte[NetStateBufferSize];

	protected byte[] netStateBufferAlt = new byte[NetStateBufferSize];

	protected MemoryStream netStateMemStream;

	protected MemoryStream netStateMemStreamAlt;

	public bool memoryStreamsInitialized;

	protected virtual void Awake()
	{
		InitializeMemoryStreams();
	}

	public void InitializeMemoryStreams()
	{
		if (!memoryStreamsInitialized)
		{
			netStateMemStream = new MemoryStream(netStateBuffer, writable: true);
			netStateMemStreamAlt = new MemoryStream(netStateBufferAlt, writable: true);
			memoryStreamsInitialized = true;
		}
	}

	public void SetMachine(ArcadeMachine machine)
	{
		this.machine = machine;
	}

	protected bool getButtonState(int player, ArcadeButtons button)
	{
		return playerInputs[player].HasFlag(button);
	}

	public void OnInputStateChange(int player, ArcadeButtons buttons)
	{
		for (int i = 1; i < 256; i += i)
		{
			ArcadeButtons arcadeButtons = (ArcadeButtons)i;
			bool flag = buttons.HasFlag(arcadeButtons);
			bool flag2 = playerInputs[player].HasFlag(arcadeButtons);
			if (flag != flag2)
			{
				if (flag)
				{
					ButtonDown(player, arcadeButtons);
				}
				else
				{
					ButtonUp(player, arcadeButtons);
				}
			}
		}
		playerInputs[player] = buttons;
	}

	public abstract byte[] GetNetworkState();

	public abstract void SetNetworkState(byte[] obj);

	protected static void WrapNetState(object ns, MemoryStream stream)
	{
		if (stream == null)
		{
			Debug.LogWarning("Null MemoryStream passed to WrapNetState");
			return;
		}
		BinaryFormatter binaryFormatter = new BinaryFormatter();
		stream.SetLength(0L);
		stream.Position = 0L;
		binaryFormatter.Serialize(stream, ns);
	}

	protected static object UnwrapNetState(byte[] b)
	{
		BinaryFormatter binaryFormatter = new BinaryFormatter();
		MemoryStream memoryStream = new MemoryStream();
		memoryStream.Write(b);
		memoryStream.Position = 0L;
		object result = binaryFormatter.Deserialize(memoryStream);
		memoryStream.Close();
		return result;
	}

	protected void SwapNetStateBuffersAndStreams()
	{
		byte[] array = netStateBufferAlt;
		byte[] array2 = netStateBuffer;
		netStateBuffer = array;
		netStateBufferAlt = array2;
		MemoryStream memoryStream = netStateMemStreamAlt;
		MemoryStream memoryStream2 = netStateMemStream;
		netStateMemStream = memoryStream;
		netStateMemStreamAlt = memoryStream2;
	}

	protected void PlaySound(int clipId, int prio = 3)
	{
		machine.PlaySound(clipId, prio);
	}

	protected bool IsPlayerLocallyControlled(int player)
	{
		return machine.IsPlayerLocallyControlled(player);
	}

	protected abstract void ButtonUp(int player, ArcadeButtons button);

	protected abstract void ButtonDown(int player, ArcadeButtons button);

	public abstract void OnTimeout();

	public virtual void ReadPlayerDataPUN(int player, PhotonStream stream, PhotonMessageInfo info)
	{
	}

	public virtual void WritePlayerDataPUN(int player, PhotonStream stream, PhotonMessageInfo info)
	{
	}
}
