using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;

public class SITechTreeSO : ScriptableObject
{
	private const string preLog = "[SITechTreeSO]  ";

	private const string preErr = "[SITechTreeSO]  ERROR!!!  ";

	private const int RESOURCE_CAP = 20;

	[SerializeField]
	private SITechTreePage[] treePages;

	private readonly Dictionary<SIUpgradeType, GraphNode<SITechTreeNode>> _nodeLookup = new Dictionary<SIUpgradeType, GraphNode<SITechTreeNode>>();

	private NativeHashMap<int, SIUpgradeType> _upgradeTypeByEntityTypeId;

	private readonly HashSet<int> _spawnableEntityTypeIds = new HashSet<int>();

	private List<GameEntity> _spawnableEntities;

	public List<SITechTreePage> TreePages { get; private set; }

	public int TreePageCount { get; private set; }

	public int[] TreeNodeCounts { get; private set; }

	public List<GraphNode<SITechTreeNode>> AllNodes { get; private set; }

	public bool Initialized { get; private set; }

	public List<GameEntity> SpawnableEntities
	{
		get
		{
			EnsureInitialized();
			return _spawnableEntities;
		}
	}

	public bool TryGetNode(SIUpgradeType upgradeType, out GraphNode<SITechTreeNode> node)
	{
		return _nodeLookup.TryGetValue(upgradeType, out node);
	}

	public bool TryGetUpgradeTypeByEntityTypeId(int entityTypeId, out SIUpgradeType upgradeType)
	{
		return _upgradeTypeByEntityTypeId.TryGetValue(entityTypeId, out upgradeType);
	}

	public bool IsSpawnableEntityTypeId(int entityTypeId)
	{
		EnsureInitialized();
		return _spawnableEntityTypeIds.Contains(entityTypeId);
	}

	public bool IsValidPage(SITechTreePageId id)
	{
		foreach (SITechTreePage treePage in TreePages)
		{
			if (treePage.pageId == id && treePage.IsValid)
			{
				return true;
			}
		}
		return false;
	}

	public SITechTreePage GetTreePage(SITechTreePageId id)
	{
		if (!TryGetTreePage(id, out var treePage))
		{
			return null;
		}
		return treePage;
	}

	public bool TryGetTreePage(SITechTreePageId id, out SITechTreePage treePage)
	{
		foreach (SITechTreePage treePage2 in TreePages)
		{
			if (treePage2.pageId == id && treePage2.IsValid)
			{
				treePage = treePage2;
				return true;
			}
		}
		treePage = null;
		return false;
	}

	public bool IsValidNode(int pageId, int nodeId)
	{
		return IsValidNode(SIUpgradeTypeSystem.GetUpgradeType(pageId, nodeId));
	}

	public bool IsValidNode(SIUpgradeType upgradeType)
	{
		return _nodeLookup.ContainsKey(upgradeType);
	}

	public SITechTreeNode GetTreeNode(int pageId, int nodeId)
	{
		return GetTreeNode(SIUpgradeTypeSystem.GetUpgradeType(pageId, nodeId));
	}

	public SITechTreeNode GetTreeNode(SIUpgradeType upgradeType)
	{
		if (_nodeLookup.TryGetValue(upgradeType, out var value))
		{
			return value.Value;
		}
		return null;
	}

	public void EnsureInitialized()
	{
		if (!Initialized)
		{
			InitTechTree();
		}
	}

	private void InitTechTree()
	{
		Debug.Log("[SI] SITechTreeSO.InitTechTree");
		ClearTechTree();
		TreePages = new List<SITechTreePage>();
		_spawnableEntities = new List<GameEntity>();
		int num = 0;
		SITechTreePage[] array = treePages;
		foreach (SITechTreePage sITechTreePage in array)
		{
			if (!sITechTreePage.IsValid)
			{
				continue;
			}
			sITechTreePage.BuildGraph();
			foreach (GraphNode<SITechTreeNode> root in sITechTreePage.Roots)
			{
				foreach (GraphNode<SITechTreeNode> item in root.TraversePreOrder())
				{
					if (!_nodeLookup.ContainsKey(item.Value.upgradeType))
					{
						_nodeLookup.Add(item.Value.upgradeType, item);
					}
				}
			}
			foreach (SITechTreeNode dispensableGadget in sITechTreePage.DispensableGadgets)
			{
				num++;
				AddSpawnableGadget(dispensableGadget.unlockedGadgetPrefab);
			}
			if (sITechTreePage.Roots.Count > 0)
			{
				TreePages.Add(sITechTreePage);
			}
		}
		if (_upgradeTypeByEntityTypeId.IsCreated)
		{
			_upgradeTypeByEntityTypeId.Clear();
		}
		else
		{
			_upgradeTypeByEntityTypeId = new NativeHashMap<int, SIUpgradeType>(num, Allocator.Persistent);
		}
		array = treePages;
		foreach (SITechTreePage sITechTreePage2 in array)
		{
			if (!sITechTreePage2.IsValid)
			{
				continue;
			}
			foreach (SITechTreeNode dispensableGadget2 in sITechTreePage2.DispensableGadgets)
			{
				int staticHash = dispensableGadget2.unlockedGadgetPrefab.gameObject.name.GetStaticHash();
				_upgradeTypeByEntityTypeId.TryAdd(staticHash, dispensableGadget2.upgradeType);
			}
		}
		AllNodes = new List<GraphNode<SITechTreeNode>>(_nodeLookup.Values);
		TreePageCount = ((SIUpgradeType[])Enum.GetValues(typeof(SIUpgradeType))).Select((SIUpgradeType v) => v.GetPageId()).Max() + 1;
		TreeNodeCounts = new int[TreePageCount];
		SIUpgradeType[] array2 = (SIUpgradeType[])Enum.GetValues(typeof(SIUpgradeType));
		foreach (SIUpgradeType self in array2)
		{
			int pageId = self.GetPageId();
			int nodeId = self.GetNodeId();
			TreeNodeCounts[pageId] = Mathf.Max(TreeNodeCounts[pageId], nodeId + 1);
		}
		Initialized = true;
	}

	private void AddSpawnableGadget(GameEntity entity)
	{
		_spawnableEntities.Add(entity);
		_spawnableEntityTypeIds.Add(entity.gameObject.name.GetStaticHash());
		IPrefabRequirements component = entity.GetComponent<IPrefabRequirements>();
		if (component == null)
		{
			return;
		}
		foreach (GameEntity requiredPrefab in component.RequiredPrefabs)
		{
			_spawnableEntities.Add(requiredPrefab);
			_spawnableEntityTypeIds.Add(requiredPrefab.gameObject.name.GetStaticHash());
		}
	}

	private void ClearTechTree()
	{
		SITechTreePage[] array = treePages;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].ClearGraph();
		}
		_nodeLookup.Clear();
		_spawnableEntityTypeIds.Clear();
		if (_upgradeTypeByEntityTypeId.IsCreated)
		{
			_upgradeTypeByEntityTypeId.Dispose();
		}
		Initialized = false;
	}
}
