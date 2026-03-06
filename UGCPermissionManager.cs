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
		private Action<bool> setUGCEnabled;

		public PlayFabPermissions(Action<bool> setUGCEnabled)
		{
			this.setUGCEnabled = setUGCEnabled;
		}

		public void Initialize()
		{
			bool safety = PlayFabAuthenticator.instance.GetSafety();
			setUGCEnabled?.Invoke(!safety);
		}

		public void CheckPermissions()
		{
		}
	}

	private class KIDPermissions : IUGCPermissions
	{
		private Action<bool> setUGCEnabled;

		public KIDPermissions(Action<bool> setUGCEnabled)
		{
			this.setUGCEnabled = setUGCEnabled;
		}

		private void SetUGCEnabled(bool enabled)
		{
			setUGCEnabled?.Invoke(enabled);
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
				SetUGCEnabled(enabled: false);
				break;
			case Permission.ManagedByEnum.PLAYER:
				if (isEnabled)
				{
					Debug.Log("[UGCPermissionManager][KID] KID UGC managed by player and enabled - opting in and enabling UGC.");
					if (!hasOptedIn)
					{
						KIDManager.SetFeatureOptIn(EKIDFeatures.Mods, optedIn: true);
					}
					SetUGCEnabled(enabled: true);
				}
				else
				{
					Debug.LogFormat("[UGCPermissionManager][KID] KID UGC managed by player and disabled by default - using opt in status. (opted in: [{0}])", hasOptedIn);
					SetUGCEnabled(hasOptedIn);
				}
				break;
			case Permission.ManagedByEnum.GUARDIAN:
				Debug.LogFormat("[UGCPermissionManager][KID] KID UGC managed by guardian. (opted in: [{0}], enabled: [{1}])", hasOptedIn, isEnabled);
				SetUGCEnabled(isEnabled);
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

	private static bool? isUGCEnabled;

	public static bool IsUGCDisabled => isUGCEnabled != true;

	public static void UsePlayFabSafety()
	{
		permissions = new PlayFabPermissions(SetUGCEnabled);
		permissions.Initialize();
	}

	public static void UseKID()
	{
		permissions = new KIDPermissions(SetUGCEnabled);
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

	private static void SetUGCEnabled(bool enabled)
	{
		if (enabled != isUGCEnabled)
		{
			isUGCEnabled = enabled;
			if (enabled)
			{
				onUGCEnabled?.Invoke();
			}
			else
			{
				onUGCDisabled?.Invoke();
			}
		}
	}
}
