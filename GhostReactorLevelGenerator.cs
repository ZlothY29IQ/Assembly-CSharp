using System;
using System.Collections.Generic;
using GorillaNetworking;
using UnityEngine;

public class GhostReactorLevelGenerator : MonoBehaviourTick
{
	public enum NodeType
	{
		Hub,
		EndCap,
		Blocker
	}

	public class Node
	{
		public NodeType type;

		public int configIndex;

		public int parentAnchorIndex;

		public int attachAnchorIndex;

		public int anchorCount;

		public List<int> anchorOrder;

		public GhostReactorLevelSection sectionInstance;

		public GhostReactorLevelSectionConnector connectorInstance;

		public Node[] children;
	}

	public List<GhostReactorLevelDepthConfig> depthConfigs;

	[SerializeField]
	private GhostReactorLevelSection mainHub = new GhostReactorLevelSection();

	[SerializeField]
	private List<GhostReactorSpawnConfig> mainHubSpawnConfigs;

	[SerializeField]
	private List<Collider> nonOverlapZones = new List<Collider>();

	public int seed = 2343;

	private List<List<Node>> nodeTree = new List<List<Node>>();

	private List<Node> nodeList = new List<Node>();

	private HashSet<string> spawnedHubHashSet = new HashSet<string>();

	private List<int> hubOrder = new List<int>();

	private List<int> connectorOrder = new List<int>();

	private List<int> endCapOrder = new List<int>();

	private List<int> blockerOrder = new List<int>();

	private List<int> entryAnchorOrder = new List<int>();

	private List<Transform> treeParents = new List<Transform>();

	private string generationOutput = "";

	private SRand randomGenerator;

	private BoxCollider testColliderA;

	private BoxCollider testColliderB;

	private GhostReactor reactor;

	[NonSerialized]
	public int depthConfigIndex;

	private Quaternion flip180 = Quaternion.AngleAxis(180f, Vector3.up);

	private const int MAX_VIS_CHECKS_PER_FRAME = 1;

	public int nextVisCheckNodeIndex;

	public List<GhostReactorLevelGeneratorV2.TreeLevelConfig> TreeLevels => GetTreeLevels();

	private void Awake()
	{
		GameObject gameObject = new GameObject("TestColliderA");
		testColliderA = gameObject.AddComponent<BoxCollider>();
		testColliderA.isTrigger = true;
		gameObject.transform.SetParent(base.transform);
		gameObject.gameObject.SetActive(value: false);
		GameObject gameObject2 = new GameObject("TestColliderB");
		testColliderB = gameObject2.AddComponent<BoxCollider>();
		testColliderB.isTrigger = true;
		gameObject2.transform.SetParent(base.transform);
		gameObject2.gameObject.SetActive(value: false);
		nextVisCheckNodeIndex = 0;
	}

	public void Init(GhostReactor reactor)
	{
		this.reactor = reactor;
	}

	public override void Tick()
	{
		Vector3 position = VRRig.LocalRig.transform.position;
		int num = Mathf.Min(1, nodeList.Count);
		for (int i = 0; i < num; i++)
		{
			if (nextVisCheckNodeIndex >= nodeList.Count)
			{
				nextVisCheckNodeIndex = 0;
			}
			if (nodeList[nextVisCheckNodeIndex] == null)
			{
				continue;
			}
			if (nodeList[nextVisCheckNodeIndex].sectionInstance != null)
			{
				nodeList[nextVisCheckNodeIndex].sectionInstance.UpdateDisable(position);
			}
			if (nodeList[nextVisCheckNodeIndex].connectorInstance != null)
			{
				nodeList[nextVisCheckNodeIndex].connectorInstance.UpdateDisable(position);
			}
			Node[] children = nodeList[nextVisCheckNodeIndex].children;
			for (int j = 0; j < children.Length; j++)
			{
				if (children[j] != null)
				{
					if (children[j].sectionInstance != null)
					{
						children[j].sectionInstance.UpdateDisable(position);
					}
					if (children[j].connectorInstance != null)
					{
						children[j].connectorInstance.UpdateDisable(position);
					}
				}
			}
			nextVisCheckNodeIndex++;
		}
	}

	private List<GhostReactorLevelGeneratorV2.TreeLevelConfig> GetTreeLevels()
	{
		if (depthConfigs == null || depthConfigs.Count == 0)
		{
			return null;
		}
		List<GhostReactorLevelGeneratorV2.TreeLevelConfig> treeLevels = depthConfigs[Mathf.Clamp(reactor.GetDepthLevel(), 0, depthConfigs.Count - 1)].options[reactor.GetDepthConfigIndex()].levelConfig.treeLevels;
		List<GhostReactorLevelGeneratorV2.TreeLevelConfig> list = new List<GhostReactorLevelGeneratorV2.TreeLevelConfig>();
		foreach (GhostReactorLevelGeneratorV2.TreeLevelConfig item in treeLevels)
		{
			if (TreeLevelIsEnabledNow(item))
			{
				list.Add(item);
			}
		}
		return list;
	}

	private static bool TreeLevelIsEnabledNow(GhostReactorLevelGeneratorV2.TreeLevelConfig treeLevel)
	{
		if (string.IsNullOrEmpty(treeLevel.EnableAfterDatetime) && string.IsNullOrEmpty(treeLevel.DisableAfterDatetime))
		{
			return true;
		}
		if (!string.IsNullOrEmpty(treeLevel.EnableAfterDatetime) && !string.IsNullOrEmpty(treeLevel.DisableAfterDatetime))
		{
			throw new ArgumentException("Both enable and disable after datetime are set--this should never happen!");
		}
		DateTime dateTime = GorillaComputer.instance.GetServerTime().ToUniversalTime();
		if (!string.IsNullOrEmpty(treeLevel.EnableAfterDatetime))
		{
			DateTime dateTime2 = DateTime.Parse(treeLevel.EnableAfterDatetime).ToUniversalTime();
			return dateTime > dateTime2;
		}
		DateTime dateTime3 = DateTime.Parse(treeLevel.DisableAfterDatetime).ToUniversalTime();
		return dateTime < dateTime3;
	}

	private bool TestForCollision(GhostReactorLevelSection section, Vector3 position, Quaternion rotation, int selfi, int selfj, int selfk)
	{
		testColliderA.gameObject.SetActive(value: true);
		testColliderB.gameObject.SetActive(value: true);
		testColliderA.transform.position = position + rotation * section.BoundingCollider.transform.localPosition;
		testColliderA.transform.rotation = rotation * section.BoundingCollider.transform.localRotation;
		testColliderA.transform.localScale = section.BoundingCollider.transform.localScale;
		testColliderA.size = section.BoundingCollider.size;
		testColliderA.center = section.BoundingCollider.center;
		for (int i = 0; i < nonOverlapZones.Count; i++)
		{
			if (testColliderA.bounds.Intersects(nonOverlapZones[i].bounds) && Physics.ComputePenetration(testColliderA, testColliderA.transform.position, testColliderA.transform.rotation, nonOverlapZones[i], nonOverlapZones[i].transform.position, nonOverlapZones[i].transform.rotation, out var _, out var _))
			{
				testColliderA.gameObject.SetActive(value: false);
				testColliderB.gameObject.SetActive(value: false);
				return true;
			}
		}
		for (int j = 0; j < nodeTree.Count; j++)
		{
			for (int k = 0; k < nodeTree[j].Count; k++)
			{
				if (j == selfi && k == selfj && selfk == -1)
				{
					continue;
				}
				Node node = nodeTree[j][k];
				for (int l = 0; l < node.children.Length; l++)
				{
					if (j == selfi && k == selfj && l == selfk)
					{
						continue;
					}
					Node node2 = node.children[l];
					if (node2 != null && node2.sectionInstance != null && node2.sectionInstance.BoundingCollider != null && (node2.type == NodeType.Blocker || node2.type == NodeType.EndCap))
					{
						GhostReactorLevelSection sectionInstance = node2.sectionInstance;
						testColliderB.transform.position = sectionInstance.transform.position + sectionInstance.transform.rotation * sectionInstance.BoundingCollider.transform.localPosition;
						testColliderB.transform.rotation = sectionInstance.transform.rotation * sectionInstance.BoundingCollider.transform.localRotation;
						testColliderB.transform.localScale = sectionInstance.BoundingCollider.transform.localScale;
						testColliderB.size = sectionInstance.BoundingCollider.size;
						testColliderB.center = sectionInstance.BoundingCollider.center;
						if (testColliderA.bounds.Intersects(testColliderB.bounds) && Physics.ComputePenetration(testColliderA, testColliderA.transform.position, testColliderA.transform.rotation, testColliderB, testColliderB.transform.position, testColliderB.transform.rotation, out var _, out var _))
						{
							testColliderA.gameObject.SetActive(value: false);
							testColliderB.gameObject.SetActive(value: false);
							return true;
						}
					}
				}
				if ((j != selfi || k != selfj) && node.sectionInstance != null && node.sectionInstance.BoundingCollider != null)
				{
					GhostReactorLevelSection sectionInstance2 = node.sectionInstance;
					testColliderB.transform.position = sectionInstance2.transform.position + sectionInstance2.transform.rotation * sectionInstance2.BoundingCollider.transform.localPosition;
					testColliderB.transform.rotation = sectionInstance2.transform.rotation * sectionInstance2.BoundingCollider.transform.localRotation;
					testColliderB.transform.localScale = sectionInstance2.BoundingCollider.transform.localScale;
					testColliderB.size = sectionInstance2.BoundingCollider.size;
					testColliderB.center = sectionInstance2.BoundingCollider.center;
					if (testColliderA.bounds.Intersects(testColliderB.bounds) && Physics.ComputePenetration(testColliderA, testColliderA.transform.position, testColliderA.transform.rotation, testColliderB, testColliderB.transform.position, testColliderB.transform.rotation, out var _, out var _))
					{
						testColliderA.gameObject.SetActive(value: false);
						testColliderB.gameObject.SetActive(value: false);
						return true;
					}
				}
			}
		}
		testColliderA.gameObject.SetActive(value: false);
		testColliderB.gameObject.SetActive(value: false);
		return false;
	}

	private void DebugGenerate()
	{
		Generate(seed);
	}

	public void Generate(int inputSeed)
	{
		ClearLevelSections();
		if (!Application.isPlaying)
		{
			return;
		}
		seed = inputSeed;
		randomGenerator = new SRand(seed);
		if (TreeLevels.Count < 1)
		{
			return;
		}
		spawnedHubHashSet.Clear();
		for (int i = 0; i < TreeLevels.Count; i++)
		{
			nodeTree.Add(new List<Node>());
			GameObject gameObject = new GameObject($"Tree Level {i}");
			gameObject.transform.parent = base.transform;
			gameObject.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
			treeParents.Add(gameObject.transform);
		}
		Node node = new Node();
		node.type = NodeType.Hub;
		node.configIndex = -1;
		node.attachAnchorIndex = -1;
		node.parentAnchorIndex = -1;
		node.children = new Node[mainHub.Anchors.Count];
		node.sectionInstance = mainHub;
		node.anchorCount = mainHub.Anchors.Count;
		node.anchorOrder = new List<int>();
		RandomizeIndices(ref node.anchorOrder, node.anchorCount);
		nodeTree[0].Add(node);
		nodeList.Add(node);
		for (int j = 0; j < TreeLevels.Count; j++)
		{
			List<GhostReactorLevelSection> hubs = TreeLevels[j].hubs;
			List<GhostReactorLevelSectionConnector> connectors = TreeLevels[j].connectors;
			if (hubs.Count < 1 || connectors.Count < 1)
			{
				continue;
			}
			RandomizeIndices(ref hubOrder, hubs.Count);
			RandomizeIndices(ref connectorOrder, connectors.Count);
			int num = 0;
			int num2 = 0;
			int num3 = Mathf.Max(TreeLevels[j].maxHubs - TreeLevels[j].minHubs, 0);
			int num4 = Mathf.Max(TreeLevels[j].minHubs, 0) + randomGenerator.NextInt(num3 + 1);
			for (int k = 0; k < num4; k++)
			{
				if (j > 0 && nodeTree[j].Count < 1)
				{
					continue;
				}
				int num5 = hubOrder[num % hubOrder.Count];
				num++;
				int index = connectorOrder[num2 % connectorOrder.Count];
				num2++;
				int num6 = ((j == 0) ? (-1) : (k % nodeTree[j].Count));
				Node node2 = ((num6 != -1) ? nodeTree[j][num6] : node);
				for (int l = 0; l < node2.anchorOrder.Count; l++)
				{
					int num7 = node2.anchorOrder[l];
					bool flag = spawnedHubHashSet.Contains(hubs[num5].gameObject.name);
					if (node2.children[num7] != null || node2.attachAnchorIndex == num7 || flag)
					{
						continue;
					}
					Quaternion quaternion = node2.sectionInstance.Anchors[num7].rotation * flip180;
					Vector3 position = node2.sectionInstance.Anchors[num7].position;
					GhostReactorLevelSectionConnector ghostReactorLevelSectionConnector = connectors[index];
					Quaternion quaternion2 = Quaternion.Inverse(ghostReactorLevelSectionConnector.hubAnchor.localRotation) * quaternion;
					Vector3 vector = quaternion2 * -ghostReactorLevelSectionConnector.hubAnchor.localPosition + position;
					Vector3 vector2 = quaternion2 * ghostReactorLevelSectionConnector.sectionAnchor.localPosition + vector;
					Quaternion quaternion3 = quaternion2 * ghostReactorLevelSectionConnector.sectionAnchor.localRotation;
					GhostReactorLevelSection ghostReactorLevelSection = hubs[num5];
					bool flag2 = false;
					if (ghostReactorLevelSection.Anchors.Count > 0)
					{
						RandomizeIndices(ref entryAnchorOrder, ghostReactorLevelSection.Anchors.Count);
						for (int m = 0; m < entryAnchorOrder.Count; m++)
						{
							int num8 = entryAnchorOrder[m];
							Transform transform = ghostReactorLevelSection.Anchors[num8];
							Quaternion quaternion4 = Quaternion.Inverse(transform.localRotation) * quaternion3;
							Vector3 position2 = quaternion4 * -transform.localPosition + vector2;
							if (!TestForCollision(ghostReactorLevelSection, position2, quaternion4, j, k, num7))
							{
								Node node3 = new Node();
								node3.type = NodeType.Hub;
								node3.configIndex = num5;
								node3.children = new Node[ghostReactorLevelSection.Anchors.Count];
								node3.parentAnchorIndex = num7;
								node3.attachAnchorIndex = num8;
								node3.anchorCount = ghostReactorLevelSection.Anchors.Count;
								node3.anchorOrder = new List<int>();
								RandomizeIndices(ref node3.anchorOrder, node3.anchorCount);
								GhostReactorLevelSectionConnector component = UnityEngine.Object.Instantiate(ghostReactorLevelSectionConnector.gameObject, vector, quaternion2, treeParents[j]).GetComponent<GhostReactorLevelSectionConnector>();
								node3.connectorInstance = component;
								GhostReactorLevelSection component2 = UnityEngine.Object.Instantiate(ghostReactorLevelSection.gameObject, position2, quaternion4, treeParents[j]).GetComponent<GhostReactorLevelSection>();
								node3.sectionInstance = component2;
								node2.children[node3.parentAnchorIndex] = node3;
								nodeTree[j + 1].Add(node3);
								nodeList.Add(node3);
								spawnedHubHashSet.Add(ghostReactorLevelSection.gameObject.name);
								flag2 = true;
								break;
							}
						}
					}
					if (flag2)
					{
						break;
					}
				}
			}
		}
		for (int n = 0; n < nodeTree.Count; n++)
		{
			List<GhostReactorLevelSection> endCaps = TreeLevels[n].endCaps;
			List<GhostReactorLevelSection> blockers = TreeLevels[n].blockers;
			RandomizeIndices(ref blockerOrder, blockers.Count);
			RandomizeIndices(ref endCapOrder, endCaps.Count);
			int num9 = 0;
			int num10 = 0;
			for (int num11 = 0; num11 < nodeTree[n].Count; num11++)
			{
				Node node4 = nodeTree[n][num11];
				int num12 = Mathf.Max(TreeLevels[n].maxCaps - TreeLevels[n].minCaps, 0);
				int num13 = Mathf.Max(TreeLevels[n].minCaps, 0) + randomGenerator.NextInt(num12 + 1);
				for (int num14 = 0; num14 < node4.children.Length; num14++)
				{
					if (node4.children[num14] != null || node4.attachAnchorIndex == num14)
					{
						continue;
					}
					bool flag3 = false;
					if (num13 > 0 && endCapOrder.Count > 0)
					{
						int num15 = endCapOrder[num10 % endCapOrder.Count];
						num10++;
						num13--;
						Quaternion quaternion5 = node4.sectionInstance.Anchors[num14].rotation * flip180;
						Vector3 position3 = node4.sectionInstance.Anchors[num14].position;
						GhostReactorLevelSection ghostReactorLevelSection2 = endCaps[num15];
						Quaternion quaternion6 = Quaternion.Inverse(ghostReactorLevelSection2.Anchor.localRotation) * quaternion5;
						Vector3 position4 = quaternion6 * -ghostReactorLevelSection2.Anchor.localPosition + position3;
						if (!TestForCollision(ghostReactorLevelSection2, position4, quaternion6, n, num11, num14))
						{
							Node node5 = new Node();
							node5.type = NodeType.EndCap;
							node5.configIndex = num15;
							node5.parentAnchorIndex = num14;
							GhostReactorLevelSection component3 = UnityEngine.Object.Instantiate(ghostReactorLevelSection2.gameObject, position4, quaternion6, treeParents[n]).GetComponent<GhostReactorLevelSection>();
							node5.sectionInstance = component3;
							node4.children[num14] = node5;
							flag3 = true;
						}
					}
					if (!flag3 && blockerOrder.Count > 0)
					{
						int configIndex = blockerOrder[num9 % blockerOrder.Count];
						num9++;
						Node node6 = new Node();
						node6.type = NodeType.Blocker;
						node6.configIndex = configIndex;
						node6.parentAnchorIndex = num14;
						Quaternion quaternion7 = node4.sectionInstance.Anchors[num14].rotation * flip180;
						Vector3 position5 = node4.sectionInstance.Anchors[num14].position;
						GhostReactorLevelSection ghostReactorLevelSection3 = blockers[node6.configIndex];
						Quaternion quaternion8 = Quaternion.Inverse(ghostReactorLevelSection3.Anchor.localRotation) * quaternion7;
						Vector3 position6 = quaternion8 * -ghostReactorLevelSection3.Anchor.localPosition + position5;
						GhostReactorLevelSection component4 = UnityEngine.Object.Instantiate(ghostReactorLevelSection3.gameObject, position6, quaternion8, treeParents[n]).GetComponent<GhostReactorLevelSection>();
						node6.sectionInstance = component4;
						node4.children[num14] = node6;
					}
				}
			}
		}
		for (int num16 = 0; num16 < nodeList.Count; num16++)
		{
			if (nodeList[num16].connectorInstance != null)
			{
				nodeList[num16].connectorInstance.Init(reactor.grManager);
			}
			nodeList[num16].sectionInstance.InitLevelSection(num16, reactor);
		}
	}

	private void DebugClear()
	{
		ClearLevelSections();
	}

	public void ClearLevelSections()
	{
		for (int i = 0; i < nodeList.Count; i++)
		{
			if (!(nodeList[i].sectionInstance == mainHub))
			{
				if (nodeList[i].connectorInstance != null)
				{
					UnityEngine.Object.Destroy(nodeList[i].connectorInstance.gameObject);
				}
				UnityEngine.Object.Destroy(nodeList[i].sectionInstance.gameObject);
			}
		}
		nodeList.Clear();
		for (int j = 0; j < nodeTree.Count; j++)
		{
			nodeTree[j].Clear();
		}
		nodeTree.Clear();
		for (int k = 0; k < treeParents.Count; k++)
		{
			UnityEngine.Object.Destroy(treeParents[k].gameObject);
		}
		treeParents.Clear();
	}

	public void SpawnEntitiesInEachSection(float respawnCount)
	{
		for (int i = 0; i < nodeTree.Count; i++)
		{
			List<GhostReactorSpawnConfig> spawnConfigs = ((i < 1) ? mainHubSpawnConfigs : TreeLevels[i - 1].sectionSpawnConfigs);
			List<GhostReactorSpawnConfig> endCapSpawnConfigs = TreeLevels[i].endCapSpawnConfigs;
			for (int j = 0; j < nodeTree[i].Count; j++)
			{
				Node node = nodeTree[i][j];
				if (node != null && node.sectionInstance != null && node.type == NodeType.Hub)
				{
					node.sectionInstance.SpawnSectionEntities(ref randomGenerator, reactor.grManager.gameEntityManager, reactor, spawnConfigs, respawnCount);
				}
				for (int k = 0; k < node.children.Length; k++)
				{
					Node node2 = node.children[k];
					if (node2 != null && node2.sectionInstance != null && node2.type == NodeType.EndCap)
					{
						node2.sectionInstance.SpawnSectionEntities(ref randomGenerator, reactor.grManager.gameEntityManager, reactor, endCapSpawnConfigs, respawnCount);
					}
				}
			}
		}
		if (GhostReactorLevelSection.tempCreateEntitiesList.Count > 0)
		{
			reactor.grManager.gameEntityManager.RequestCreateItems(GhostReactorLevelSection.tempCreateEntitiesList);
			GhostReactorLevelSection.tempCreateEntitiesList.Clear();
		}
	}

	public void RespawnEntity(int entityId, long entityCreateData, GameEntityId createdByEntityId)
	{
		int sectionIndex = GhostReactor.EnemyEntityCreateData.Unpack(entityCreateData).sectionIndex;
		if (sectionIndex >= 0 && sectionIndex < nodeList.Count)
		{
			nodeList[sectionIndex].sectionInstance.RespawnEntity(ref randomGenerator, reactor.grManager.gameEntityManager, entityId, entityCreateData, createdByEntityId);
		}
	}

	public GRPatrolPath GetPatrolPath(long createData)
	{
		GhostReactor.EnemyEntityCreateData enemyEntityCreateData = GhostReactor.EnemyEntityCreateData.Unpack(createData);
		int sectionIndex = enemyEntityCreateData.sectionIndex;
		int patrolIndex = enemyEntityCreateData.patrolIndex;
		if (sectionIndex < 0 || sectionIndex >= nodeList.Count)
		{
			return null;
		}
		return nodeList[sectionIndex].sectionInstance.GetPatrolPath(patrolIndex);
	}

	private void RandomizeIndices(ref List<int> list, int count)
	{
		list.Clear();
		for (int i = 0; i < count; i++)
		{
			list.Add(i);
		}
		randomGenerator.Shuffle(list);
	}

	public bool GetExitFromCurrentSection(Vector3 pos, out Vector3 exitPos, out Quaternion exitRot, List<Vector3> connectorCorners)
	{
		exitPos = Vector3.zero;
		exitRot = Quaternion.identity;
		Node currentNode = GetCurrentNode(pos);
		if (currentNode == null || currentNode.parentAnchorIndex < 0)
		{
			return false;
		}
		Transform anchor = currentNode.sectionInstance.GetAnchor(currentNode.attachAnchorIndex);
		exitPos = anchor.transform.position;
		exitRot = anchor.transform.rotation;
		GRLevelAnchor component = anchor.GetComponent<GRLevelAnchor>();
		if (component != null && component.navigablePoint != null)
		{
			exitPos = component.navigablePoint.position;
			exitRot = component.navigablePoint.rotation;
		}
		connectorCorners.Clear();
		if (currentNode.connectorInstance != null)
		{
			for (int i = 0; i < currentNode.connectorInstance.pathNodes.Count; i++)
			{
				connectorCorners.Add(currentNode.connectorInstance.pathNodes[i].position);
			}
		}
		return true;
	}

	private Node GetCurrentNode(Vector3 pos)
	{
		float num = float.MaxValue;
		Node result = null;
		for (int i = 0; i < nodeTree.Count; i++)
		{
			List<Node> list = nodeTree[i];
			for (int j = 0; j < list.Count; j++)
			{
				Node node = list[j];
				if (!(node.sectionInstance == null))
				{
					float distSq = node.sectionInstance.GetDistSq(pos);
					if (distSq < num)
					{
						num = distSq;
						result = node;
					}
				}
			}
		}
		return result;
	}
}
