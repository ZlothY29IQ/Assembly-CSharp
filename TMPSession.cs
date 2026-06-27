using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KID.Model;
using UnityEngine;

public class TMPSession
{
	public readonly Guid SessionId;

	public readonly string Etag;

	public readonly AgeStatusType AgeStatus;

	public readonly Session.StatusEnum KidStatus;

	public readonly Session.ManagedByEnum ManagedBy;

	public readonly DateTime DateOfBirth;

	public readonly string Jurisdiction;

	public readonly string KUID;

	public readonly int Age;

	public readonly bool IsDefault;

	public readonly SessionStatus SessionStatus;

	private Dictionary<EKIDFeatures, Permission> Permissions;

	private HashSet<EKIDFeatures> OptedInPermissions;

	public bool IsValidSession
	{
		get
		{
			if (!IsDefault || Permissions == null || Permissions.Count <= 0)
			{
				if (!IsDefault)
				{
					return SessionId != Guid.Empty;
				}
				return false;
			}
			return true;
		}
	}

	public TMPSession(Session session, KIDDefaultSession defaultSession, SessionStatus status)
	{
		Permissions = new Dictionary<EKIDFeatures, Permission>();
		OptedInPermissions = new HashSet<EKIDFeatures>();
		SessionStatus = status;
		if (session == null && defaultSession == null)
		{
			return;
		}
		if (session == null)
		{
			IsDefault = true;
			AgeStatus = defaultSession.AgeStatus;
			Age = defaultSession.Age;
			InitialiseDefaultPermissionSet(defaultSession);
			return;
		}
		SessionId = session.SessionId;
		Etag = session.Etag;
		AgeStatus = session.AgeStatus;
		KidStatus = session.Status;
		DateOfBirth = session.DateOfBirth;
		KUID = session.Kuid;
		Jurisdiction = session.Jurisdiction;
		ManagedBy = session.ManagedBy;
		Age = GetAgeFromDateOfBirth();
		for (int i = 0; i < session.Permissions.Count; i++)
		{
			EKIDFeatures? eKIDFeatures = KIDFeaturesExtensions.FromString(session.Permissions[i].Name);
			if (eKIDFeatures.HasValue && !Permissions.TryAdd(eKIDFeatures.Value, session.Permissions[i]))
			{
				Debug.LogError("[KID::SESSION] Tried creating new session, but permission for [" + eKIDFeatures.Value.ToStandardisedString() + "] already exists");
			}
		}
	}

	public void SetOptInPermissions(string[] optedInPermissions)
	{
		if (optedInPermissions == null || optedInPermissions.Length == 0)
		{
			Debug.LogWarning("[KID::SESSION] OptedInPermissions is null or empty. Returning without setting.");
			return;
		}
		for (int i = 0; i < optedInPermissions?.Length; i++)
		{
			EKIDFeatures? eKIDFeatures = KIDFeaturesExtensions.FromString(optedInPermissions[i]);
			if (eKIDFeatures.HasValue)
			{
				OptInToPermission(eKIDFeatures.Value, optIn: true);
			}
		}
		Debug.Log($"[KID::SESSION::OptInRefactor] Constructor OptedInPermissions: {GetOptedInPermissions()}");
	}

	public bool TryGetPermission(EKIDFeatures feature, out Permission permission)
	{
		if (!Permissions.ContainsKey(feature))
		{
			Debug.LogError("[KID::SESSION] Tried retreiving permission for [" + feature.ToStandardisedString() + "], but does not exist");
			permission = null;
			return false;
		}
		permission = Permissions[feature];
		return true;
	}

	public List<Permission> GetAllPermissions()
	{
		return Permissions.Values.ToList();
	}

	public bool HasPermissionForFeature(EKIDFeatures feature)
	{
		if (!TryGetPermission(feature, out var permission))
		{
			Debug.LogError("[KID::SESSION] Tried checking for permission but couldn't find [" + feature.ToStandardisedString() + "]. Assuming disabled");
			return false;
		}
		return permission.Enabled;
	}

	public void OptInToPermission(EKIDFeatures feature, bool optIn)
	{
		Debug.Log($"[KID::SESSION::OptInRefactor] Opting in to permission for [{feature.ToStandardisedString()}] with optIn: {optIn}");
		if (optIn && !OptedInPermissions.Contains(feature))
		{
			OptedInPermissions.Add(feature);
		}
		else if (!optIn && OptedInPermissions.Contains(feature))
		{
			OptedInPermissions.Remove(feature);
		}
	}

	public bool HasOptedInToPermission(EKIDFeatures feature)
	{
		return OptedInPermissions.Contains(feature);
	}

	public string[] GetOptedInPermissions()
	{
		if (OptedInPermissions == null || OptedInPermissions.Count == 0)
		{
			Debug.LogWarning("[KID::SESSION] OptedInPermissions is null or empty. Returning empty array.");
			return Array.Empty<string>();
		}
		return OptedInPermissions.Select((EKIDFeatures f) => f.ToStandardisedString()).ToArray();
	}

	public void UpdatePermission(EKIDFeatures feature, Permission newData)
	{
		if (!Permissions.ContainsKey(feature))
		{
			Debug.Log("[KID::SESSION] Trying to update permission, but could not find [" + feature.ToStandardisedString() + "] in dictionary. Will add new one");
			Permissions.Add(feature, null);
		}
		Permissions[feature] = newData;
	}

	private void InitialiseDefaultPermissionSet(KIDDefaultSession defaultSession)
	{
		for (int i = 0; i < defaultSession.Permissions.Count; i++)
		{
			EKIDFeatures? eKIDFeatures = KIDFeaturesExtensions.FromString(defaultSession.Permissions[i].Name);
			if (eKIDFeatures.HasValue && !Permissions.TryAdd(eKIDFeatures.Value, defaultSession.Permissions[i]))
			{
				Debug.LogError("[KID::SESSION] Tried creating new session, but permission for [" + eKIDFeatures.Value.ToStandardisedString() + "] already exists");
			}
		}
	}

	private int GetAgeFromDateOfBirth()
	{
		DateTime today = DateTime.Today;
		int num = today.Year - DateOfBirth.Year;
		int num2 = today.Month - DateOfBirth.Month;
		if (num2 < 0)
		{
			num--;
		}
		else if (num2 == 0 && today.Day - DateOfBirth.Day < 0)
		{
			num--;
		}
		return num;
	}

	public override string ToString()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("New TMPSession]:");
		stringBuilder.AppendLine($"    - Is Default    :   {IsDefault}");
		stringBuilder.AppendLine($"    - Is Valid      :   {IsValidSession}");
		stringBuilder.AppendLine($"    - SessionID     :   {SessionId}");
		stringBuilder.AppendLine($"    - Age           :   {Age}");
		stringBuilder.AppendLine($"    - AgeStatus     :   {AgeStatus}");
		stringBuilder.AppendLine($"    - SessionStatus :   {KidStatus}");
		stringBuilder.AppendLine("    - DoB           :   " + DateOfBirth);
		stringBuilder.AppendLine("    - KUID          :   " + KUID);
		stringBuilder.AppendLine("    - Jurisdiction  :   " + Jurisdiction);
		stringBuilder.AppendLine("    - PERMISSIONS   :");
		if (Permissions != null)
		{
			foreach (Permission value in Permissions.Values)
			{
				stringBuilder.AppendLine($"        - {value.Name} - Enabled: {value.Enabled} - ManagedBy: {value.ManagedBy}");
			}
		}
		return stringBuilder.ToString();
	}
}
