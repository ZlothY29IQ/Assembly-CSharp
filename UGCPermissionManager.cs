using System;
using GorillaNetworking;
using KID.Model;
using UnityEngine;

internal class UGCPermissionManager : MonoBehaviour
{
	private interface IUGCPermissions
	{
		void Initialize();

		void CheckPermissions();
	}

	private class PlayFabPermissions : IUGCPermissions
	{
		private Action<UGCAccessLevel> setAccessLevel;

		public PlayFabPermissions(Action<UGCAccessLevel> setAccessLevel)
		{
			this.setAccessLevel = setAccessLevel;
		}

		public void Initialize()
		{
			bool safety = PlayFabAuthenticator.instance.GetSafety();
			setAccessLevel?.Invoke((!safety) ? UGCAccessLevel.Full : UGCAccessLevel.Disabled);
		}

		public void CheckPermissions()
		{
		}
	}

	private class KIDPermissions : IUGCPermissions
	{
		private Action<UGCAccessLevel> setAccessLevel;

		public KIDPermissions(Action<UGCAccessLevel> setAccessLevel)
		{
			this.setAccessLevel = setAccessLevel;
		}

		private void SetAccessLevel(UGCAccessLevel level)
		{
			setAccessLevel?.Invoke(level);
		}

		public void Initialize()
		{
			Debug.Log("[UGCPermissionManager][KID] Initializing with KID");
			CheckPermissions();
			KIDManager.RegisterSessionUpdatedCallback_UGC(OnKIDSessionUpdate);
		}

		public void CheckPermissions()
		{
			Permission permissionDataByFeature = KIDManager.GetPermissionDataByFeature(EKIDFeatures.Mods);
			bool item = KIDManager.CheckFeatureOptIn(EKIDFeatures.Mods).hasOptedInPreviously;
			ProcessPermissionKID(item, permissionDataByFeature.Enabled, permissionDataByFeature.ManagedBy);
		}

		private void OnKIDSessionUpdate(bool isEnabled, Permission.ManagedByEnum managedBy)
		{
			Debug.Log("[UGCPermissionManager][KID] KID session update.");
			bool item = KIDManager.CheckFeatureOptIn(EKIDFeatures.Mods).hasOptedInPreviously;
			ProcessPermissionKID(item, isEnabled, managedBy);
		}

		private void ProcessPermissionKID(bool hasOptedIn, bool isEnabled, Permission.ManagedByEnum managedBy)
		{
			Debug.LogFormat("[UGCPermissionManager][KID] Process KID permissions - opted in: [{0}], enabled: [{1}], managedBy: [{2}].", hasOptedIn, isEnabled, managedBy);
			switch (managedBy)
			{
			case Permission.ManagedByEnum.PROHIBITED:
				Debug.Log("[UGCPermissionManager][KID] KID UGC prohibited.");
				SetAccessLevel(UGCAccessLevel.Disabled);
				break;
			case Permission.ManagedByEnum.PLAYER:
				if (isEnabled)
				{
					Debug.Log("[UGCPermissionManager][KID] KID UGC managed by player and enabled - opting in and enabling UGC.");
					if (!hasOptedIn)
					{
						KIDManager.SetFeatureOptIn(EKIDFeatures.Mods, optedIn: true);
					}
					SetAccessLevel(UGCAccessLevel.Full);
				}
				else
				{
					Debug.LogFormat("[UGCPermissionManager][KID] KID UGC managed by player and disabled by default - using opt in status. (opted in: [{0}])", hasOptedIn);
					SetAccessLevel((!hasOptedIn) ? UGCAccessLevel.FeaturedMapsOnly : UGCAccessLevel.Full);
				}
				break;
			case Permission.ManagedByEnum.GUARDIAN:
				Debug.LogFormat("[UGCPermissionManager][KID] KID UGC managed by guardian. (opted in: [{0}], enabled: [{1}])", hasOptedIn, isEnabled);
				SetAccessLevel((!isEnabled) ? UGCAccessLevel.FeaturedMapsOnly : UGCAccessLevel.Full);
				break;
			}
		}
	}

	[OnEnterPlay_SetNull]
	private static IUGCPermissions permissions;

	[OnEnterPlay_SetNull]
	private static Action onUGCEnabled;

	[OnEnterPlay_SetNull]
	private static Action onUGCDisabled;

	[OnEnterPlay_SetNull]
	private static Action onVirtualStumpEnabled;

	[OnEnterPlay_SetNull]
	private static Action onVirtualStumpDisabled;

	private static UGCAccessLevel? accessLevel;

	public static bool IsUGCDisabled => accessLevel != UGCAccessLevel.Full;

	public static bool FeaturedMapsOnly => accessLevel == UGCAccessLevel.FeaturedMapsOnly;

	public static bool HasNoMapAccess => accessLevel.GetValueOrDefault() == UGCAccessLevel.Disabled;

	public static void UsePlayFabSafety()
	{
		permissions = new PlayFabPermissions(SetAccessLevel);
		permissions.Initialize();
	}

	public static void UseKID()
	{
		permissions = new KIDPermissions(SetAccessLevel);
		permissions.Initialize();
	}

	public static void CheckPermissions()
	{
		permissions?.CheckPermissions();
	}

	public static void SubscribeToUGCEnabled(Action callback)
	{
		onUGCEnabled = (Action)Delegate.Combine(onUGCEnabled, callback);
	}

	public static void UnsubscribeFromUGCEnabled(Action callback)
	{
		onUGCEnabled = (Action)Delegate.Remove(onUGCEnabled, callback);
	}

	public static void SubscribeToUGCDisabled(Action callback)
	{
		onUGCDisabled = (Action)Delegate.Combine(onUGCDisabled, callback);
	}

	public static void UnsubscribeFromUGCDisabled(Action callback)
	{
		onUGCDisabled = (Action)Delegate.Remove(onUGCDisabled, callback);
	}

	public static void SubscribeToVirtualStumpEnabled(Action callback)
	{
		onVirtualStumpEnabled = (Action)Delegate.Combine(onVirtualStumpEnabled, callback);
	}

	public static void UnsubscribeFromVirtualStumpEnabled(Action callback)
	{
		onVirtualStumpEnabled = (Action)Delegate.Remove(onVirtualStumpEnabled, callback);
	}

	public static void SubscribeToVirtualStumpDisabled(Action callback)
	{
		onVirtualStumpDisabled = (Action)Delegate.Combine(onVirtualStumpDisabled, callback);
	}

	public static void UnsubscribeFromVirtualStumpDisabled(Action callback)
	{
		onVirtualStumpDisabled = (Action)Delegate.Remove(onVirtualStumpDisabled, callback);
	}

	private static void SetAccessLevel(UGCAccessLevel level)
	{
		if (level == accessLevel)
		{
			return;
		}
		bool hasValue = accessLevel.HasValue;
		bool flag = accessLevel == UGCAccessLevel.Full;
		bool flag2 = accessLevel.HasValue && accessLevel != UGCAccessLevel.Disabled;
		accessLevel = level;
		bool flag3 = level == UGCAccessLevel.Full;
		bool flag4 = level != UGCAccessLevel.Disabled;
		if (!hasValue || flag != flag3)
		{
			if (flag3)
			{
				onUGCEnabled?.Invoke();
			}
			else
			{
				onUGCDisabled?.Invoke();
			}
		}
		if (!hasValue || flag2 != flag4)
		{
			if (flag4)
			{
				onVirtualStumpEnabled?.Invoke();
			}
			else
			{
				onVirtualStumpDisabled?.Invoke();
			}
		}
	}
}
