using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using GorillaExtensions;
using GorillaNetworking;
using GorillaTag.Scripts.Utilities;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Events;

namespace GorillaTagScripts.ScavengerHunt;

public class ScavengerManager : MonoBehaviour
{
	[Serializable]
	public class Hunt
	{
		public string Name;

		public bool SendTargetCollectedEventsOnLoad;

		public bool SendHuntCompletedEventsOnLoad;

		public bool Deprecated;

		public string[] TargetNames = new string[0];

		public UnityEvent[] TargetCollected = new UnityEvent[0];

		public UnityEvent<ScavengerTarget>[] TargetCollectedArg = new UnityEvent<ScavengerTarget>[0];

		public UnityEvent[] HuntCompleted = new UnityEvent[0];

		public UnityEvent<Hunt>[] HuntCompletedArg = new UnityEvent<Hunt>[0];

		private List<ScavengerTarget>? _targets;

		private HashSet<string>? _collectedTargetNamesNullable;

		public bool IsCompleted => Targets.Count == CollectedTargetNames.Count;

		public IReadOnlyList<ScavengerTarget> Targets => _targets ?? (_targets = new List<ScavengerTarget>());

		public IReadOnlyCollection<string> CollectedTargetNames => _collectedTargetNamesNullable ?? (_collectedTargetNamesNullable = new HashSet<string>());

		private HashSet<string> _collectedTargetNames => _collectedTargetNamesNullable ?? (_collectedTargetNamesNullable = new HashSet<string>());

		public Hunt(string name)
		{
			Name = name;
		}

		public bool Collect(ScavengerTarget target, bool initialLoad = false)
		{
			if (!Targets.Contains<ScavengerTarget>(target))
			{
				return false;
			}
			if (_collectedTargetNames.Add(target.TargetName))
			{
				if (!initialLoad || SendTargetCollectedEventsOnLoad)
				{
					SendTargetCollectedEvents(target);
				}
				if (IsCompleted && (!initialLoad || SendHuntCompletedEventsOnLoad))
				{
					SendHuntCompletedEvents();
				}
				return true;
			}
			return false;
		}

		public void RegisterTarget(ScavengerTarget target)
		{
			if (!Targets.Contains<ScavengerTarget>(target))
			{
				if (!Enumerable.Contains<string>(TargetNames, target.TargetName))
				{
					Debug.LogError("Scavenger hunt " + Name + " tried to register target " + target.TargetName + " even though it is not defined in the hunt in ScavengerManager.");
				}
				else
				{
					_targets.Add(target);
				}
			}
		}

		private void SendTargetCollectedEvents(ScavengerTarget target)
		{
			if (!Deprecated)
			{
				TargetCollected.InvokeAll();
				TargetCollectedArg.InvokeAll(target);
				target.TargetCollected.InvokeAll();
				target.TargetCollectedArg.InvokeAll(target);
			}
		}

		private void SendHuntCompletedEvents()
		{
			if (!Deprecated)
			{
				HuntCompleted.InvokeAll();
				HuntCompletedArg.InvokeAll(this);
			}
		}

		public bool IsCollected(ScavengerTarget target)
		{
			return _collectedTargetNames.Contains(target.TargetName);
		}

		public void ClearCollectedTargets()
		{
			_collectedTargetNames.Clear();
		}

		public ScavengerTarget? GetTarget(string name)
		{
			foreach (ScavengerTarget target in Targets)
			{
				if (target.TargetName == name)
				{
					return target;
				}
			}
			return null;
		}
	}

	public class ScavengerJson
	{
		public readonly Dictionary<string, string[]> CollectedTargets = new Dictionary<string, string[]>();

		public static ScavengerJson FromManager(ScavengerManager manager)
		{
			ScavengerJson scavengerJson = new ScavengerJson();
			Hunt[] hunts = manager.Hunts;
			foreach (Hunt hunt in hunts)
			{
				string[] value = hunt.CollectedTargetNames.ToArray();
				scavengerJson.CollectedTargets[hunt.Name] = value;
			}
			return scavengerJson;
		}

		public static ScavengerJson FromJson(string json)
		{
			ScavengerJson scavengerJson = new ScavengerJson();
			using TextReader reader = new StringReader(json);
			using JsonReader jsonReader = new JsonTextReader(reader);
			Debug.Log("Scavenger hunt parsing raw json " + json);
			while (jsonReader.Read())
			{
				if (jsonReader.TokenType == JsonToken.PropertyName && (string)jsonReader.Value == "CollectedTargets")
				{
					ReadCollectedTargets(scavengerJson, jsonReader);
				}
			}
			return scavengerJson;
		}

		private static void ReadCollectedTargets(ScavengerJson json, JsonReader reader)
		{
			int num = 0;
			bool flag = false;
			string text = null;
			List<string> list = new List<string>();
			do
			{
				if (!reader.Read())
				{
					throw new Exception("Json read error");
				}
				switch (reader.TokenType)
				{
				case JsonToken.StartObject:
					num++;
					break;
				case JsonToken.EndObject:
					num--;
					break;
				case JsonToken.PropertyName:
					if (text != null)
					{
						throw new Exception("Json read error");
					}
					text = (reader.Value as string) ?? throw new Exception("Json read error");
					break;
				case JsonToken.String:
					if (!flag)
					{
						throw new Exception("Json read error");
					}
					if (!(reader.Value is string item))
					{
						throw new Exception("Json read error");
					}
					list.Add(item);
					break;
				case JsonToken.StartArray:
					if (flag)
					{
						throw new Exception("Json read error");
					}
					flag = true;
					break;
				case JsonToken.EndArray:
					if (!flag)
					{
						throw new Exception("Json read error");
					}
					if (string.IsNullOrEmpty(text))
					{
						throw new Exception("Json read error");
					}
					json.CollectedTargets[text] = list.ToArray();
					text = null;
					list.Clear();
					flag = false;
					break;
				}
			}
			while (num > 0);
		}

		public string Write()
		{
			JsonSerializer jsonSerializer = new JsonSerializer();
			using TextWriter textWriter = new StringWriterWithEncoding(Encoding.UTF8);
			using JsonWriter jsonWriter = new JsonTextWriter(textWriter);
			jsonSerializer.Serialize(jsonWriter, this);
			return textWriter.ToString();
		}
	}

	public const string MothershipKey = "ScavengerHunt";

	public Hunt[] Hunts = new Hunt[0];

	public static ScavengerManager? Instance { get; private set; }

	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
			return;
		}
		throw new Exception("Too ScavengerManagers exist at once, this should never happen.");
	}

	private void Start()
	{
		StartCoroutine(ImportMothershipUserData());
	}

	private IEnumerator ImportMothershipUserData()
	{
		while (!MothershipClientContext.IsClientLoggedIn())
		{
			if (PlayFabAuthenticator.instance?.loginFailed ?? false)
			{
				Debug.LogError("ScavengerManager critical error, could not log into Mothership.");
				yield break;
			}
			yield return new WaitForSecondsRealtime(0.5f);
		}
		MothershipClientApiUnity.GetUserDataValue("ScavengerHunt", OnGetUserDataSuccess, OnGetUserDataFailure);
	}

	private void OnGetUserDataSuccess(MothershipUserData data)
	{
		Debug.Log("Successfully read scavenger hunt data from Mothership.");
		byte[] bytes = Convert.FromBase64String(data.value);
		string json = Encoding.UTF8.GetString(bytes);
		FromJson(json);
	}

	private void OnGetUserDataFailure(MothershipError error, int responseCode)
	{
		Debug.LogError($"Failed to read scavenger hunt user data (error {error.Name} / {responseCode}): {error.Message}");
	}

	private void OnDestroy()
	{
		Instance = null;
	}

	public Hunt? GetHunt(string huntName)
	{
		Hunt[] hunts = Hunts;
		foreach (Hunt hunt in hunts)
		{
			if (hunt.Name == huntName)
			{
				return hunt;
			}
		}
		return null;
	}

	public void RegisterTarget(ScavengerTarget target)
	{
		Hunt hunt = GetHunt(target.HuntName);
		if (hunt == null)
		{
			throw new Exception("No hunt found with name " + target.HuntName + ".");
		}
		if (!hunt.Targets.Contains<ScavengerTarget>(target))
		{
			hunt.RegisterTarget(target);
		}
	}

	public bool IsCollected(ScavengerTarget target)
	{
		return GetHunt(target.HuntName)?.IsCollected(target) ?? false;
	}

	public void Collect(ScavengerTarget target)
	{
		Hunt hunt = GetHunt(target.HuntName);
		if (hunt == null)
		{
			throw new Exception("Cannot collect scavenger hunt " + target.TargetName + ", hunt " + target.HuntName + " does not exist.");
		}
		if (!hunt.Collect(target))
		{
			Debug.Log("Did not collect scavenger hunt " + target.TargetName + ". This is normally because the user already collected it.");
			return;
		}
		Debug.Log("Collected " + target.HuntName + "." + target.TargetName);
		string value = ToJson().Write();
		MothershipClientApiUnity.SetUserDataValue("ScavengerHunt", value, OnSetUserDataSuccess, OnSetUserDataFailure);
	}

	private void OnSetUserDataSuccess(SetUserDataResponse response)
	{
		Debug.Log("Successfully wrote scavenger hunt data for user " + response.user_id + " on Mothership key " + response.key_name);
	}

	private void OnSetUserDataFailure(MothershipError error, int statusCode)
	{
		Debug.LogError($"Failed to write scavenger hunt data to Mothership (error {error.Name} / {statusCode}): {error.Message}");
	}

	public ScavengerJson ToJson()
	{
		return ScavengerJson.FromManager(this);
	}

	public void FromJson(string json)
	{
		FromJson(ScavengerJson.FromJson(json));
	}

	public void FromJson(ScavengerJson json)
	{
		Hunt[] hunts = Hunts;
		for (int i = 0; i < hunts.Length; i++)
		{
			hunts[i].ClearCollectedTargets();
		}
		foreach (KeyValuePair<string, string[]> collectedTarget in json.CollectedTargets)
		{
			Hunt hunt = GetHunt(collectedTarget.Key);
			if (hunt == null)
			{
				throw new Exception("Cannot import scavenger data, no hunt by name " + collectedTarget.Key + ".");
			}
			string[] value = collectedTarget.Value;
			foreach (string text in value)
			{
				ScavengerTarget target = hunt.GetTarget(text);
				if ((object)target == null)
				{
					if (!hunt.Deprecated)
					{
						throw new Exception("Cannot import scavenger data, no hunt/target by name " + collectedTarget.Key + "." + text);
					}
				}
				else
				{
					hunt.Collect(target, initialLoad: true);
				}
			}
		}
		int num = Hunts.Sum((Hunt hunt2) => hunt2.Targets.Count);
		Debug.Log($"Imported {num} targets from {Hunts.Length} scavenger hunts.");
	}
}
