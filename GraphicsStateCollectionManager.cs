using System.Collections;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.SceneManagement;

public class GraphicsStateCollectionManager : MonoBehaviour
{
	public enum Mode
	{
		Tracing,
		WarmUp
	}

	public Mode mode;

	public static GraphicsStateCollectionManager Instance;

	public GraphicsStateCollection[] collections;

	private const string k_CollectionFolderPath = "SharedAssets/GraphicsStateCollections/";

	private string m_OutputCollectionName;

	private GraphicsStateCollection m_GraphicsStateCollection;

	private Coroutine _autoSaveRoutine;

	private GraphicsStateCollection FindExistingCollection()
	{
		for (int i = 0; i < collections.Length; i++)
		{
			if (collections[i] != null && collections[i].runtimePlatform == Application.platform && collections[i].graphicsDeviceType == SystemInfo.graphicsDeviceType && collections[i].qualityLevelName == QualitySettings.names[QualitySettings.GetQualityLevel()])
			{
				return collections[i];
			}
		}
		return null;
	}

	private void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Debug.LogError("Only one instance of GraphicsStateCollectionManager is allowed!");
			Object.Destroy(base.gameObject);
		}
		else
		{
			Instance = this;
			Object.DontDestroyOnLoad(base.gameObject);
		}
	}

	private void Start()
	{
		if (mode == Mode.Tracing)
		{
			m_GraphicsStateCollection = FindExistingCollection();
			if (m_GraphicsStateCollection != null)
			{
				m_OutputCollectionName = "SharedAssets/GraphicsStateCollections/" + m_GraphicsStateCollection.name;
			}
			else
			{
				int qualityLevel = QualitySettings.GetQualityLevel();
				string text = QualitySettings.names[qualityLevel];
				text = text.Replace(" ", "");
				m_OutputCollectionName = string.Concat("SharedAssets/GraphicsStateCollections/", "GfxState_", Application.platform, "_", SystemInfo.graphicsDeviceType.ToString(), "_", text);
				m_GraphicsStateCollection = new GraphicsStateCollection();
			}
			Debug.Log("Tracing started for GraphicsStateCollection by Scene '" + SceneManager.GetActiveScene().name + "'.");
			m_GraphicsStateCollection.BeginTrace();
			_autoSaveRoutine = StartCoroutine(AutoSaveRoutine());
		}
		else
		{
			GraphicsStateCollection graphicsStateCollection = FindExistingCollection();
			if (graphicsStateCollection != null)
			{
				Scene activeScene = SceneManager.GetActiveScene();
				Debug.Log("Scene '" + activeScene.name + "' started warming up " + graphicsStateCollection.totalGraphicsStateCount + " GraphicsState entries.");
				graphicsStateCollection.WarmUp();
			}
		}
	}

	private void OnApplicationFocus(bool focus)
	{
		if (!focus && mode == Mode.Tracing && m_GraphicsStateCollection != null)
		{
			Debug.Log("Focus changed. Sending collection to Editor with " + m_GraphicsStateCollection.totalGraphicsStateCount + " GraphicsState entries.");
			m_GraphicsStateCollection.SendToEditor(m_OutputCollectionName);
		}
	}

	private void OnDestroy()
	{
		if (_autoSaveRoutine != null)
		{
			StopCoroutine(_autoSaveRoutine);
		}
		if (mode == Mode.Tracing && m_GraphicsStateCollection != null)
		{
			m_GraphicsStateCollection.EndTrace();
			Debug.Log("Sending collection to Editor with " + m_GraphicsStateCollection.totalGraphicsStateCount + " GraphicsState entries.");
			m_GraphicsStateCollection.SendToEditor(m_OutputCollectionName);
		}
	}

	private IEnumerator AutoSaveRoutine()
	{
		while (true)
		{
			yield return new WaitForSeconds(5f);
			if (mode == Mode.Tracing && m_GraphicsStateCollection != null)
			{
				Debug.Log("Auto-saving collection with " + m_GraphicsStateCollection.totalGraphicsStateCount + " GraphicsState entries.");
				m_GraphicsStateCollection.SendToEditor(m_OutputCollectionName);
			}
		}
	}
}
