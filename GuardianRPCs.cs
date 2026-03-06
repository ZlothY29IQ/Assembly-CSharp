using GorillaExtensions;
using Photon.Pun;
using UnityEngine;

internal class GuardianRPCs : RPCNetworkBase
{
	private GameModeSerializer serializer;

	private GorillaGuardianManager guardianManager;

	private CallLimiter launchCallLimit = new CallLimiter(5, 0.5f);

	private CallLimiter slapFXCallLimit = new CallLimiter(5, 0.5f);

	private CallLimiter slamFXCallLimit = new CallLimiter(5, 0.5f);

	public override void SetClassTarget(IWrappedSerializable target, GorillaWrappedSerializer netHandler)
	{
		guardianManager = (GorillaGuardianManager)target;
		serializer = (GameModeSerializer)netHandler;
	}

	[PunRPC]
	public void GuardianRequestEject(PhotonMessageInfo info)
	{
		MonkeAgent.IncrementRPCCall(info, "GuardianRequestEject");
		PhotonMessageInfoWrapped photonMessageInfoWrapped = new PhotonMessageInfoWrapped(info);
		if (photonMessageInfoWrapped.Sender != null)
		{
			guardianManager.EjectGuardian(photonMessageInfoWrapped.Sender);
		}
	}

	[PunRPC]
	public void GuardianLaunchPlayer(Vector3 velocity, PhotonMessageInfo info)
	{
		MonkeAgent.IncrementRPCCall(info, "GuardianLaunchPlayer");
		PhotonMessageInfoWrapped photonMessageInfoWrapped = new PhotonMessageInfoWrapped(info);
		if (!guardianManager.IsPlayerGuardian(photonMessageInfoWrapped.Sender))
		{
			MonkeAgent.instance.SendReport("Sent LaunchPlayer when not a guardian", photonMessageInfoWrapped.Sender.UserId, photonMessageInfoWrapped.Sender.NickName);
		}
		else if (velocity.IsValid(10000f) && launchCallLimit.CheckCallTime(Time.time))
		{
			guardianManager.LaunchPlayer(photonMessageInfoWrapped.Sender, velocity);
		}
	}

	[PunRPC]
	public void ShowSlapEffects(Vector3 location, Vector3 direction, PhotonMessageInfo info)
	{
		MonkeAgent.IncrementRPCCall(info, "ShowSlapEffects");
		PhotonMessageInfoWrapped photonMessageInfoWrapped = new PhotonMessageInfoWrapped(info);
		if (!guardianManager.IsPlayerGuardian(photonMessageInfoWrapped.Sender))
		{
			MonkeAgent.instance.SendReport("Sent ShowSlapEffects when not a guardian", photonMessageInfoWrapped.Sender.UserId, photonMessageInfoWrapped.Sender.NickName);
		}
		else if (location.IsValid(10000f) && direction.IsValid(10000f) && slapFXCallLimit.CheckCallTime(Time.time))
		{
			guardianManager.PlaySlapEffect(location, direction);
		}
	}

	[PunRPC]
	public void ShowSlamEffect(Vector3 location, Vector3 direction, PhotonMessageInfo info)
	{
		MonkeAgent.IncrementRPCCall(info, "ShowSlamEffect");
		PhotonMessageInfoWrapped photonMessageInfoWrapped = new PhotonMessageInfoWrapped(info);
		if (!guardianManager.IsPlayerGuardian(photonMessageInfoWrapped.Sender))
		{
			MonkeAgent.instance.SendReport("Sent ShowSlamEffect when not a guardian", photonMessageInfoWrapped.Sender.UserId, photonMessageInfoWrapped.Sender.NickName);
		}
		else if (location.IsValid(10000f) && direction.IsValid(10000f) && slamFXCallLimit.CheckCallTime(Time.time))
		{
			guardianManager.PlaySlamEffect(location, direction);
		}
	}
}
