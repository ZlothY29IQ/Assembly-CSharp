using System;
using UnityEngine;
using UnityEngine.SceneManagement;

[Serializable]
public class GTScene : IEquatable<GTScene>
{
	[SerializeField]
	private string _alias;

	[SerializeField]
	private string _name;

	[SerializeField]
	private string _path;

	[SerializeField]
	private string _guid;

	[SerializeField]
	private int _buildIndex;

	[SerializeField]
	private bool _includeInBuild;

	public string alias => _alias;

	public string name => _name;

	public string path => _path;

	public string guid => _guid;

	public int buildIndex => _buildIndex;

	public bool includeInBuild => _includeInBuild;

	public bool isLoaded => SceneManager.GetSceneByBuildIndex(_buildIndex).isLoaded;

	public bool hasAlias => !string.IsNullOrWhiteSpace(_alias);

	public GTScene(string name, string path, string guid, int buildIndex, bool includeInBuild)
	{
		if (string.IsNullOrWhiteSpace(name))
		{
			throw new ArgumentNullException("name");
		}
		if (string.IsNullOrWhiteSpace(path))
		{
			throw new ArgumentNullException("path");
		}
		if (string.IsNullOrWhiteSpace(guid))
		{
			throw new ArgumentNullException("guid");
		}
		_name = name;
		_path = path;
		_guid = guid;
		_buildIndex = buildIndex;
		_includeInBuild = includeInBuild;
	}

	public override int GetHashCode()
	{
		return _guid.GetHashCode();
	}

	public override string ToString()
	{
		return this.ToJson(indent: false);
	}

	public bool Equals(GTScene other)
	{
		if (_guid.Equals(other._guid) && _name == other._name)
		{
			return _path == other._path;
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj is GTScene other)
		{
			return Equals(other);
		}
		return false;
	}

	public static bool operator ==(GTScene x, GTScene y)
	{
		return x.Equals(y);
	}

	public static bool operator !=(GTScene x, GTScene y)
	{
		return !x.Equals(y);
	}

	public void LoadAsync()
	{
		if (!isLoaded)
		{
			SceneManager.LoadSceneAsync(_buildIndex, LoadSceneMode.Additive);
		}
	}

	public void UnloadAsync()
	{
		if (isLoaded)
		{
			SceneManager.UnloadSceneAsync(_buildIndex, UnloadSceneOptions.UnloadAllEmbeddedSceneObjects);
		}
	}

	public static GTScene FromAsset(object sceneAsset)
	{
		return null;
	}

	public static GTScene From(object editorBuildSettingsScene)
	{
		return null;
	}
}
