using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace GorillaTagScripts;

public class BuilderFactory : MonoBehaviour
{
	public Transform spawnLocation;

	private List<int> pieceTypes;

	public List<GameObject> itemList;

	[HideInInspector]
	public List<BuilderPiece> pieceList;

	public BuilderOptionButton buildItemButton;

	public TextMeshPro itemLabel;

	public BuilderOptionButton prevItemButton;

	public BuilderOptionButton nextItemButton;

	public TextMeshPro materialLabel;

	public BuilderOptionButton prevMaterialButton;

	public BuilderOptionButton nextMaterialButton;

	public AudioSource audioSource;

	public AudioClip buildPieceSound;

	public Transform previewMarker;

	public List<BuilderUIResource> resourceCostUIs;

	private BuilderPiece previewPiece;

	private int currPieceTypeIndex;

	private int currPieceMaterialIndex;

	private Dictionary<int, int> pieceTypeToIndex;

	private BuilderTable table;

	private bool initialized;

	private void Awake()
	{
		InitIfNeeded();
	}

	public void InitIfNeeded()
	{
		if (initialized)
		{
			return;
		}
		buildItemButton.Setup(OnBuildItem);
		currPieceTypeIndex = 0;
		prevItemButton.Setup(OnPrevItem);
		nextItemButton.Setup(OnNextItem);
		currPieceMaterialIndex = 0;
		prevMaterialButton.Setup(OnPrevMaterial);
		nextMaterialButton.Setup(OnNextMaterial);
		pieceTypeToIndex = new Dictionary<int, int>(256);
		initialized = true;
		if (resourceCostUIs == null)
		{
			return;
		}
		for (int i = 0; i < resourceCostUIs.Count; i++)
		{
			if (resourceCostUIs[i] != null)
			{
				resourceCostUIs[i].gameObject.SetActive(value: false);
			}
		}
	}

	public void Setup(BuilderTable tableOwner)
	{
		table = tableOwner;
		InitIfNeeded();
		List<BuilderPiece> list = pieceList;
		pieceTypes = new List<int>(list.Count);
		for (int i = 0; i < list.Count; i++)
		{
			string text = list[i].name;
			int staticHash = text.GetStaticHash();
			int value;
			if (pieceTypeToIndex.TryAdd(staticHash, i))
			{
				pieceTypes.Add(staticHash);
			}
			else if (pieceTypeToIndex.TryGetValue(staticHash, out value))
			{
				string text2 = "BuilderFactory: ERROR!! " + $"Could not add pieceType \"{text}\" with hash {staticHash} " + "to 'pieceTypeToIndex' Dictionary because because it was already added!";
				if (value < 0 || value >= list.Count)
				{
					text2 += " Also the index to the conflicting piece is out of range of the pieceList!";
				}
				else
				{
					BuilderPiece builderPiece = list[value];
					text2 = ((!(builderPiece != null)) ? (text2 + "And (should never happen) the piece at that slot is null!") : ((!(text == builderPiece.name)) ? (text2 + "Also the conflicting pieceType has the same hash but different name \"" + builderPiece.name + "\"!") : (text2 + "The conflicting piece has the same name (as expected).")));
				}
				Debug.LogError(text2, this);
			}
		}
		int num = pieceTypes.Count;
		foreach (BuilderPieceSet allPieceSet in BuilderSetManager.instance.GetAllPieceSets())
		{
			foreach (BuilderPieceSet.BuilderPieceSubset subset in allPieceSet.subsets)
			{
				foreach (BuilderPieceSet.PieceInfo pieceInfo in subset.pieceInfos)
				{
					int staticHash2 = pieceInfo.piecePrefab.name.GetStaticHash();
					if (!pieceTypeToIndex.ContainsKey(staticHash2))
					{
						pieceList.Add(pieceInfo.piecePrefab);
						pieceTypes.Add(staticHash2);
						pieceTypeToIndex.Add(staticHash2, num);
						num++;
					}
				}
			}
		}
	}

	public void Show()
	{
		RefreshUI();
	}

	public BuilderPiece GetPiecePrefab(int pieceType)
	{
		if (pieceTypeToIndex.TryGetValue(pieceType, out var value))
		{
			return pieceList[value];
		}
		Debug.LogErrorFormat("No Prefab found for type {0}", pieceType);
		return null;
	}

	public void OnBuildItem(BuilderOptionButton button, bool isLeftHand)
	{
		if (pieceTypes != null && pieceTypes.Count > currPieceTypeIndex)
		{
			int selectedMaterialType = GetSelectedMaterialType();
			table.RequestCreatePiece(pieceTypes[currPieceTypeIndex], spawnLocation.position, spawnLocation.rotation, selectedMaterialType);
			if (audioSource != null && buildPieceSound != null)
			{
				audioSource.GTPlayOneShot(buildPieceSound);
			}
		}
	}

	public void OnPrevItem(BuilderOptionButton button, bool isLeftHand)
	{
		if (pieceTypes == null || pieceTypes.Count <= 0)
		{
			return;
		}
		for (int i = 0; i < pieceTypes.Count; i++)
		{
			currPieceTypeIndex = (currPieceTypeIndex - 1 + pieceTypes.Count) % pieceTypes.Count;
			if (CanBuildPieceType(pieceTypes[currPieceTypeIndex]))
			{
				break;
			}
		}
		RefreshUI();
	}

	public void OnNextItem(BuilderOptionButton button, bool isLeftHand)
	{
		if (pieceTypes == null || pieceTypes.Count <= 0)
		{
			return;
		}
		for (int i = 0; i < pieceTypes.Count; i++)
		{
			currPieceTypeIndex = (currPieceTypeIndex + 1 + pieceTypes.Count) % pieceTypes.Count;
			if (CanBuildPieceType(pieceTypes[currPieceTypeIndex]))
			{
				break;
			}
		}
		RefreshUI();
	}

	public void OnPrevMaterial(BuilderOptionButton button, bool isLeftHand)
	{
		if (pieceTypes == null || pieceTypes.Count <= 0)
		{
			return;
		}
		BuilderPiece piecePrefab = GetPiecePrefab(pieceTypes[currPieceTypeIndex]);
		if (!(piecePrefab != null))
		{
			return;
		}
		BuilderMaterialOptions materialOptions = piecePrefab.materialOptions;
		if (materialOptions != null && materialOptions.options.Count > 0)
		{
			for (int i = 0; i < materialOptions.options.Count; i++)
			{
				currPieceMaterialIndex = (currPieceMaterialIndex - 1 + materialOptions.options.Count) % materialOptions.options.Count;
				if (CanUseMaterialType(materialOptions.options[currPieceMaterialIndex].materialId.GetHashCode()))
				{
					break;
				}
			}
		}
		RefreshUI();
	}

	public void OnNextMaterial(BuilderOptionButton button, bool isLeftHand)
	{
		if (pieceTypes == null || pieceTypes.Count <= 0)
		{
			return;
		}
		BuilderPiece piecePrefab = GetPiecePrefab(pieceTypes[currPieceTypeIndex]);
		if (!(piecePrefab != null))
		{
			return;
		}
		BuilderMaterialOptions materialOptions = piecePrefab.materialOptions;
		if (materialOptions != null && materialOptions.options.Count > 0)
		{
			for (int i = 0; i < materialOptions.options.Count; i++)
			{
				currPieceMaterialIndex = (currPieceMaterialIndex + 1 + materialOptions.options.Count) % materialOptions.options.Count;
				if (CanUseMaterialType(materialOptions.options[currPieceMaterialIndex].materialId.GetHashCode()))
				{
					break;
				}
			}
		}
		RefreshUI();
	}

	private int GetSelectedMaterialType()
	{
		int result = -1;
		BuilderPiece piecePrefab = GetPiecePrefab(pieceTypes[currPieceTypeIndex]);
		if (piecePrefab != null)
		{
			BuilderMaterialOptions materialOptions = piecePrefab.materialOptions;
			if (materialOptions != null && materialOptions.options != null && currPieceMaterialIndex >= 0 && currPieceMaterialIndex < materialOptions.options.Count)
			{
				result = materialOptions.options[currPieceMaterialIndex].materialId.GetHashCode();
			}
		}
		return result;
	}

	private string GetSelectedMaterialName()
	{
		string result = "DEFAULT";
		BuilderPiece piecePrefab = GetPiecePrefab(pieceTypes[currPieceTypeIndex]);
		if (piecePrefab != null)
		{
			BuilderMaterialOptions materialOptions = piecePrefab.materialOptions;
			if (materialOptions != null && materialOptions.options != null && currPieceMaterialIndex >= 0 && currPieceMaterialIndex < materialOptions.options.Count)
			{
				result = materialOptions.options[currPieceMaterialIndex].materialId;
			}
		}
		return result;
	}

	public bool CanBuildPieceType(int pieceType)
	{
		BuilderPiece piecePrefab = GetPiecePrefab(pieceType);
		if (piecePrefab == null || piecePrefab.isBuiltIntoTable)
		{
			return false;
		}
		return true;
	}

	public bool CanUseMaterialType(int materalType)
	{
		return true;
	}

	public void RefreshUI()
	{
		if (pieceList != null && pieceList.Count > currPieceTypeIndex)
		{
			itemLabel.SetText(pieceList[currPieceTypeIndex].displayName);
		}
		else
		{
			itemLabel.SetText("No Items");
		}
		if (previewPiece != null)
		{
			table.builderPool.DestroyPiece(previewPiece);
			previewPiece = null;
		}
		if (currPieceTypeIndex >= 0 && currPieceTypeIndex < pieceTypes.Count)
		{
			int pieceType = pieceTypes[currPieceTypeIndex];
			previewPiece = table.builderPool.CreatePiece(pieceType, assertNotEmpty: false);
			previewPiece.SetTable(table);
			previewPiece.pieceType = pieceType;
			string selectedMaterialName = GetSelectedMaterialName();
			materialLabel.SetText(selectedMaterialName);
			previewPiece.SetScale(table.pieceScale * 0.75f);
			previewPiece.SetupPiece(table.gridSize);
			int selectedMaterialType = GetSelectedMaterialType();
			previewPiece.SetMaterial(selectedMaterialType, force: true);
			previewPiece.transform.SetPositionAndRotation(previewMarker.position, previewMarker.rotation);
			previewPiece.SetState(BuilderPiece.State.Displayed);
			previewPiece.enabled = false;
			RefreshCostUI();
		}
	}

	private void RefreshCostUI()
	{
		List<BuilderResourceQuantity> list = null;
		if (previewPiece != null)
		{
			list = previewPiece.cost.quantities;
		}
		for (int i = 0; i < resourceCostUIs.Count; i++)
		{
			if (!(resourceCostUIs[i] == null))
			{
				bool flag = list != null && i < list.Count;
				resourceCostUIs[i].gameObject.SetActive(flag);
				if (flag)
				{
					resourceCostUIs[i].SetResourceCost(list[i], table);
				}
			}
		}
	}

	public void OnAvailableResourcesChange()
	{
		RefreshCostUI();
	}

	public void CreateRandomPiece()
	{
		Debug.LogError("Create Random Piece No longer implemented");
	}
}
