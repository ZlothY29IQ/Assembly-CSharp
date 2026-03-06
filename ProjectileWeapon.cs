using GorillaExtensions;
using UnityEngine;

public abstract class ProjectileWeapon : TransferrableObject
{
	[SerializeField]
	protected GameObject projectilePrefab;

	[SerializeField]
	private GameObject projectileTrail;

	public AudioClip[] shootSfxClips;

	public AudioSource shootSfx;

	protected abstract Vector3 GetLaunchPosition();

	protected abstract Vector3 GetLaunchVelocity();

	internal override void OnEnable()
	{
		base.OnEnable();
		if (base.myOnlineRig != null)
		{
			base.myOnlineRig.projectileWeapon = this;
		}
		if (base.myRig != null)
		{
			base.myRig.projectileWeapon = this;
		}
	}

	protected void LaunchProjectile()
	{
		int hash = PoolUtils.GameObjHashCode(projectilePrefab);
		int trailHash = PoolUtils.GameObjHashCode(projectileTrail);
		GameObject gameObject = ObjectPools.instance.Instantiate(hash);
		float num = Mathf.Abs(base.transform.lossyScale.x);
		gameObject.transform.localScale = Vector3.one * num;
		Vector3 launchPosition = GetLaunchPosition();
		Vector3 launchVelocity = GetLaunchVelocity();
		GetIsOnTeams(out var blueTeam, out var orangeTeam, out var shouldUsePlayerColor);
		AttachTrail(trailHash, gameObject, launchPosition, blueTeam, orangeTeam, shouldUsePlayerColor && (bool)targetRig, targetRig ? targetRig.playerColor : default(Color));
		SlingshotProjectile component = gameObject.GetComponent<SlingshotProjectile>();
		if (NetworkSystem.Instance.InRoom)
		{
			int projectileCount = ProjectileTracker.AddAndIncrementLocalProjectile(component, launchVelocity, launchPosition, num);
			component.Launch(launchPosition, launchVelocity, NetworkSystem.Instance.LocalPlayer, blueTeam, orangeTeam, projectileCount, num, shouldUsePlayerColor, base.myRig.playerColor);
			_ = currentState;
			RoomSystem.SendLaunchProjectile(launchPosition, launchVelocity, RoomSystem.ProjectileSource.ProjectileWeapon, projectileCount, randomColour: false, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue);
			PlayLaunchSfx();
		}
		else
		{
			component.Launch(launchPosition, launchVelocity, NetworkSystem.Instance.LocalPlayer, blueTeam, orangeTeam, 0, num, shouldUsePlayerColor, base.myRig.playerColor);
			PlayLaunchSfx();
		}
		PlayerGameEvents.LaunchedProjectile(projectilePrefab.name);
	}

	internal virtual SlingshotProjectile LaunchNetworkedProjectile(Vector3 location, Vector3 velocity, RoomSystem.ProjectileSource projectileSource, int projectileCounter, float scale, bool shouldOverrideColor, Color color, PhotonMessageInfoWrapped info)
	{
		GameObject gameObject = null;
		SlingshotProjectile slingshotProjectile = null;
		NetPlayer player = NetworkSystem.Instance.GetPlayer(info.senderID);
		try
		{
			int hash = -1;
			int num = -1;
			if (projectileSource == RoomSystem.ProjectileSource.ProjectileWeapon)
			{
				if (currentState == PositionState.OnChest || currentState == PositionState.None)
				{
					return null;
				}
				hash = PoolUtils.GameObjHashCode(projectilePrefab);
				num = PoolUtils.GameObjHashCode(projectileTrail);
			}
			gameObject = ObjectPools.instance.Instantiate(hash);
			slingshotProjectile = gameObject.GetComponent<SlingshotProjectile>();
			GetIsOnTeams(out var blueTeam, out var orangeTeam, out var shouldUsePlayerColor);
			if (shouldUsePlayerColor && !shouldOverrideColor && (bool)targetRig)
			{
				shouldOverrideColor = true;
				color = targetRig.playerColor;
			}
			if (num != -1)
			{
				AttachTrail(num, slingshotProjectile.gameObject, location, blueTeam, orangeTeam, shouldOverrideColor, color);
			}
			slingshotProjectile.Launch(location, velocity, player, blueTeam, orangeTeam, projectileCounter, scale, shouldOverrideColor, color);
			PlayLaunchSfx();
		}
		catch
		{
			MonkeAgent.instance.SendReport("projectile error", player.UserId, player.NickName);
			if ((object)slingshotProjectile != null && (bool)slingshotProjectile)
			{
				slingshotProjectile.transform.position = Vector3.zero;
				slingshotProjectile.Deactivate();
				slingshotProjectile = null;
			}
			else if (gameObject.IsNotNull())
			{
				ObjectPools.instance.Destroy(gameObject);
			}
		}
		return slingshotProjectile;
	}

	protected void GetIsOnTeams(out bool blueTeam, out bool orangeTeam, out bool shouldUsePlayerColor)
	{
		NetPlayer player = OwningPlayer();
		blueTeam = false;
		orangeTeam = false;
		shouldUsePlayerColor = false;
		if (GorillaGameManager.instance != null)
		{
			GorillaPaintbrawlManager component = GorillaGameManager.instance.GetComponent<GorillaPaintbrawlManager>();
			if (component != null)
			{
				blueTeam = component.OnBlueTeam(player);
				orangeTeam = component.OnRedTeam(player);
				shouldUsePlayerColor = !blueTeam && !orangeTeam;
			}
		}
	}

	private void AttachTrail(int trailHash, GameObject newProjectile, Vector3 location, bool blueTeam, bool orangeTeam, bool shouldOverrideColor = false, Color overrideColor = default(Color))
	{
		GameObject gameObject = ObjectPools.instance.Instantiate(trailHash);
		SlingshotProjectileTrail component = gameObject.GetComponent<SlingshotProjectileTrail>();
		if (component.IsNull())
		{
			ObjectPools.instance.Destroy(gameObject);
		}
		newProjectile.transform.position = location;
		component.AttachTrail(newProjectile, blueTeam, orangeTeam, shouldOverrideColor, overrideColor);
	}

	private void PlayLaunchSfx()
	{
		if (shootSfx != null && shootSfxClips != null && shootSfxClips.Length != 0)
		{
			shootSfx.GTPlayOneShot(shootSfxClips[Random.Range(0, shootSfxClips.Length)]);
		}
	}
}
