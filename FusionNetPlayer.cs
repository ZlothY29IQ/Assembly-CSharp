using Fusion;
using UnityEngine;

public class FusionNetPlayer : NetPlayer
{
	private string _defaultName;

	private bool validPlayer;

	public PlayerRef PlayerRef { get; private set; }

	private NetworkRunner runner => ((NetworkSystemFusion)NetworkSystem.Instance).runner;

	public override bool IsValid
	{
		get
		{
			if (validPlayer)
			{
				return PlayerRef.IsRealPlayer;
			}
			return false;
		}
	}

	public override int ActorNumber => PlayerRef.PlayerId;

	public override string UserId => NetworkSystem.Instance.GetUserID(PlayerRef.PlayerId);

	public override bool IsMasterClient
	{
		get
		{
			if (!(runner == null))
			{
				if (!IsLocal || !runner.IsSharedModeMasterClient)
				{
					return NetworkSystem.Instance.MasterClient == this;
				}
				return true;
			}
			return PlayerRef == default(PlayerRef);
		}
	}

	public override bool IsLocal
	{
		get
		{
			if (!(runner == null))
			{
				return PlayerRef == runner.LocalPlayer;
			}
			return PlayerRef == default(PlayerRef);
		}
	}

	public override bool IsNull
	{
		get
		{
			_ = PlayerRef;
			return false;
		}
	}

	public override string NickName => NetworkSystem.Instance.GetNickName(this);

	public override string DefaultName
	{
		get
		{
			if (string.IsNullOrEmpty(_defaultName))
			{
				_defaultName = "gorilla" + Random.Range(0, 9999).ToString().PadLeft(4, '0');
			}
			return _defaultName;
		}
	}

	public override bool InRoom
	{
		get
		{
			foreach (PlayerRef activePlayer in runner.ActivePlayers)
			{
				if (activePlayer == PlayerRef)
				{
					return true;
				}
			}
			return false;
		}
	}

	public FusionNetPlayer()
	{
		PlayerRef = default(PlayerRef);
	}

	public FusionNetPlayer(PlayerRef playerRef)
	{
		PlayerRef = playerRef;
	}

	public override bool Equals(NetPlayer myPlayer, NetPlayer other)
	{
		if (myPlayer == null || other == null)
		{
			return false;
		}
		return ((FusionNetPlayer)myPlayer).PlayerRef.Equals(((FusionNetPlayer)other).PlayerRef);
	}

	public void InitPlayer(PlayerRef player)
	{
		PlayerRef = player;
		validPlayer = true;
	}

	public override void OnReturned()
	{
		base.OnReturned();
		PlayerRef = default(PlayerRef);
		if (PlayerRef.PlayerId != -1)
		{
			Debug.LogError("Returned Player to pool but isnt -1, broken");
		}
	}

	public override void OnTaken()
	{
		base.OnTaken();
	}
}
