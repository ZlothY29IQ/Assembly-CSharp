using System.Collections.Generic;
using GorillaTagScripts;
using Photon.Pun;
using UnityEngine;

public class BuilderDispenserShelf : MonoBehaviour
{
	[Header("Set Selection")]
	[SerializeField]
	private BuilderSetSelector setSelector;

	public List<BuilderPieceSet.BuilderPieceCategory> _includedCategories;

	[Header("Dispenser Shelf Properties")]
	public Transform shelfCenter;

	public float shelfWidth = 1.4f;

	public Animation resetAnimation;

	[SerializeField]
	private SoundBankPlayer resetSoundBank;

	[SerializeField]
	private AudioClip spawnNewSetSound;

	[SerializeField]
	private AudioSource audioSource;

	private bool playSpawnSetSound;

	[HideInInspector]
	public BuilderTable table;

	public int shelfID = -1;

	private BuilderPieceSet.BuilderDisplayGroup currentGroup;

	private bool initialized;

	public BuilderDispenser dispenserPrefab;

	private List<BuilderDispenser> dispenserPool;

	private List<BuilderDispenser> activeDispensers;

	private List<BuilderPieceSet.PieceInfo> piecesInSet = new List<BuilderPieceSet.PieceInfo>(10);

	private bool animatingShelf;

	private double timeToClearShelf = double.MaxValue;

	private int dispenserToClear;

	private int dispenserToUpdate;

	private bool shouldVerifySetSelection;

	private void BuildDispenserPool()
	{
		dispenserPool = new List<BuilderDispenser>(12);
		activeDispensers = new List<BuilderDispenser>(6);
		AddToDispenserPool(6);
	}

	private void AddToDispenserPool(int count)
	{
		if (!(dispenserPrefab == null))
		{
			for (int i = 0; i < count; i++)
			{
				BuilderDispenser builderDispenser = Object.Instantiate(dispenserPrefab, shelfCenter);
				builderDispenser.gameObject.SetActive(value: false);
				builderDispenser.table = table;
				builderDispenser.shelfID = shelfID;
				dispenserPool.Add(builderDispenser);
			}
		}
	}

	private void ActivateDispensers()
	{
		piecesInSet.Clear();
		foreach (BuilderPieceSet.BuilderPieceSubset pieceSubset in currentGroup.pieceSubsets)
		{
			if (_includedCategories.Contains(pieceSubset.pieceCategory))
			{
				piecesInSet.AddRange(pieceSubset.pieceInfos);
			}
		}
		if (piecesInSet.Count <= 0)
		{
			return;
		}
		int count = piecesInSet.Count;
		if (dispenserPool.Count < count)
		{
			AddToDispenserPool(count - dispenserPool.Count);
		}
		activeDispensers.Clear();
		for (int i = 0; i < dispenserPool.Count; i++)
		{
			if (i < count)
			{
				BuilderDispenser builderDispenser = dispenserPool[i];
				builderDispenser.gameObject.SetActive(value: true);
				float x = shelfWidth / -2f + shelfWidth / (float)(count * 2) + shelfWidth / (float)count * (float)i;
				builderDispenser.transform.localPosition = new Vector3(x, 0f, 0f);
				builderDispenser.AssignPieceType(piecesInSet[i], currentGroup.defaultMaterial.GetHashCode());
				activeDispensers.Add(builderDispenser);
			}
			else
			{
				dispenserPool[i].ClearDispenser();
				dispenserPool[i].gameObject.SetActive(value: false);
			}
		}
		dispenserToUpdate = 0;
	}

	public void Setup()
	{
		InitIfNeeded();
		foreach (BuilderDispenser item in dispenserPool)
		{
			item.table = table;
			item.shelfID = shelfID;
		}
	}

	private void InitIfNeeded()
	{
		if (!initialized)
		{
			setSelector.Setup(_includedCategories);
			currentGroup = setSelector.GetSelectedGroup();
			setSelector.OnSelectedGroup.AddListener(OnSelectedSetChange);
			BuildDispenserPool();
			ActivateDispensers();
			initialized = true;
		}
	}

	private void OnDestroy()
	{
		if (setSelector != null)
		{
			setSelector.OnSelectedGroup.RemoveListener(OnSelectedSetChange);
		}
	}

	public void OnSelectedSetChange(int displayGroupID)
	{
		if (table.GetTableState() == BuilderTable.TableState.Ready)
		{
			table.RequestShelfSelection(shelfID, displayGroupID, isConveyor: false);
		}
	}

	public void SetSelection(int displayGroupID)
	{
		setSelector.SetSelection(displayGroupID);
		BuilderPieceSet.BuilderDisplayGroup selectedGroup = setSelector.GetSelectedGroup();
		if ((!initialized || currentGroup != null) && !(selectedGroup.displayName != currentGroup.displayName))
		{
			return;
		}
		currentGroup = selectedGroup;
		if (table.GetTableState() == BuilderTable.TableState.Ready)
		{
			if (!animatingShelf)
			{
				StartShelfSwap();
			}
		}
		else
		{
			animatingShelf = false;
			ImmediateShelfSwap();
		}
	}

	public int GetSelectedDisplayGroupID()
	{
		return setSelector.GetSelectedGroup().GetDisplayGroupIdentifier();
	}

	private void ImmediateShelfSwap()
	{
		foreach (BuilderDispenser activeDispenser in activeDispensers)
		{
			activeDispenser.ClearDispenser();
		}
		ActivateDispensers();
	}

	private void StartShelfSwap()
	{
		dispenserToClear = 0;
		timeToClearShelf = Time.time + 0.15f;
		resetAnimation.Rewind();
		foreach (BuilderDispenser activeDispenser in activeDispensers)
		{
			activeDispenser.ParentPieceToShelf(resetAnimation.transform);
		}
		resetAnimation.Play();
		animatingShelf = true;
	}

	public void UpdateShelf()
	{
		if (!animatingShelf || !((double)Time.time > timeToClearShelf))
		{
			return;
		}
		if (dispenserToClear < activeDispensers.Count)
		{
			if (dispenserToClear == 0)
			{
				resetSoundBank.Play();
			}
			activeDispensers[dispenserToClear].ClearDispenser();
			dispenserToClear++;
		}
		else if (!resetAnimation.isPlaying)
		{
			playSpawnSetSound = true;
			ActivateDispensers();
			animatingShelf = false;
		}
	}

	public void UpdateShelfSliced()
	{
		if (!PhotonNetwork.LocalPlayer.IsMasterClient || !initialized || animatingShelf)
		{
			return;
		}
		if (shouldVerifySetSelection)
		{
			BuilderPieceSet.BuilderDisplayGroup selectedGroup = setSelector.GetSelectedGroup();
			if (selectedGroup == null || !BuilderSetManager.instance.DoesAnyPlayerInRoomOwnPieceSet(selectedGroup.setID))
			{
				int defaultGroupID = setSelector.GetDefaultGroupID();
				if (defaultGroupID != -1)
				{
					OnSelectedSetChange(defaultGroupID);
				}
			}
			shouldVerifySetSelection = false;
		}
		if (activeDispensers.Count > 0)
		{
			activeDispensers[dispenserToUpdate].UpdateDispenser();
			dispenserToUpdate = (dispenserToUpdate + 1) % activeDispensers.Count;
		}
	}

	public void VerifySetSelection()
	{
		shouldVerifySetSelection = true;
	}

	public void OnShelfPieceCreated(BuilderPiece piece, bool playfx)
	{
		if (playSpawnSetSound && playfx)
		{
			audioSource.GTPlayOneShot(spawnNewSetSound);
			playSpawnSetSound = false;
		}
		foreach (BuilderDispenser activeDispenser in activeDispensers)
		{
			activeDispenser.ShelfPieceCreated(piece, playfx);
		}
	}

	public void OnShelfPieceRecycled(BuilderPiece piece)
	{
		foreach (BuilderDispenser activeDispenser in activeDispensers)
		{
			activeDispenser.ShelfPieceRecycled(piece);
		}
	}

	public void OnClearTable()
	{
		if (!initialized)
		{
			return;
		}
		foreach (BuilderDispenser activeDispenser in activeDispensers)
		{
			activeDispenser.OnClearTable();
		}
		StopAllCoroutines();
		if (animatingShelf)
		{
			resetAnimation.Rewind();
			animatingShelf = false;
		}
	}

	public void ClearShelf()
	{
		foreach (BuilderDispenser activeDispenser in activeDispensers)
		{
			activeDispenser.ClearDispenser();
		}
	}
}
