using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GorillaTagScripts;

public class BuilderPool : MonoBehaviour, IGorillaSimpleBackgroundWorker
{
	public List<List<BuilderPiece>> piecePools;

	public Dictionary<int, int> piecePoolLookup;

	[HideInInspector]
	public List<BuilderBumpGlow> bumpGlowPool;

	public BuilderBumpGlow bumpGlowPrefab;

	[HideInInspector]
	public List<SnapOverlap> snapOverlapPool;

	public static BuilderPool instance;

	private const int POOl_CAPACITY = 128;

	private const int INITIAL_INSTANCE_COUNT_STARTER = 32;

	private const int INITIAL_INSTANCE_COUNT_PREMIUM = 8;

	private bool isSetup;

	private bool hasBuiltPieceSets;

	private Queue<int> piecesToAdd = new Queue<int>();

	private void Awake()
	{
		if (instance == null)
		{
			instance = this;
		}
		else
		{
			Object.Destroy(this);
		}
	}

	public void Setup()
	{
		if (!isSetup)
		{
			piecePools = new List<List<BuilderPiece>>(512);
			piecePoolLookup = new Dictionary<int, int>(512);
			bumpGlowPool = new List<BuilderBumpGlow>(256);
			AddToGlowBumpPool(256);
			snapOverlapPool = new List<SnapOverlap>(4096);
			AddToSnapOverlapPool(4096);
			isSetup = true;
		}
	}

	public void BuildFromShelves(List<BuilderShelf> shelves)
	{
		for (int i = 0; i < shelves.Count; i++)
		{
			BuilderShelf builderShelf = shelves[i];
			for (int j = 0; j < builderShelf.buildPieceSpawns.Count; j++)
			{
				BuilderShelf.BuildPieceSpawn buildPieceSpawn = builderShelf.buildPieceSpawns[j];
				AddToPool(buildPieceSpawn.buildPiecePrefab.name.GetStaticHash(), buildPieceSpawn.count);
			}
		}
	}

	public IEnumerator BuildFromPieceSets()
	{
		if (hasBuiltPieceSets)
		{
			yield break;
		}
		hasBuiltPieceSets = true;
		List<BuilderPieceSet> allPieceSets = BuilderSetManager.instance.GetAllPieceSets();
		foreach (BuilderPieceSet item in allPieceSets)
		{
			bool isStarterSet = BuilderSetManager.instance.GetStarterSetsConcat().Contains(item.playfabID);
			bool isFallbackSet = item.SetName.Equals("HIDDEN");
			foreach (BuilderPieceSet.BuilderPieceSubset subset in item.subsets)
			{
				foreach (BuilderPieceSet.PieceInfo pieceInfo in subset.pieceInfos)
				{
					int staticHash = pieceInfo.piecePrefab.name.GetStaticHash();
					if (!piecePoolLookup.TryGetValue(staticHash, out var value))
					{
						value = piecePools.Count;
						piecePools.Add(new List<BuilderPiece>(128));
						piecePoolLookup.Add(staticHash, value);
						if (!isFallbackSet)
						{
							int num = (isStarterSet ? 32 : 8);
							int num2 = 0;
							while (num2 < num)
							{
								if (piecesToAdd.Count == 0)
								{
									GorillaSimpleBackgroundWorkerManager.WorkerSignup(this);
								}
								num2 += 2;
								piecesToAdd.Enqueue(staticHash);
							}
						}
					}
					yield return null;
				}
			}
		}
	}

	public void SimpleWork()
	{
		int count = 2;
		if (piecesToAdd.Count > 0)
		{
			AddToPool(piecesToAdd.Dequeue(), count);
		}
		if (piecesToAdd.Count > 0)
		{
			GorillaSimpleBackgroundWorkerManager.WorkerSignup(this);
		}
	}

	private void AddToPool(int pieceType, int count)
	{
		if (!piecePoolLookup.TryGetValue(pieceType, out var value))
		{
			value = piecePools.Count;
			piecePools.Add(new List<BuilderPiece>(count * 8));
			piecePoolLookup.Add(pieceType, value);
			Debug.LogWarningFormat("Creating Pool for piece {0} of size {1}. Is this piece not in a piece set?", pieceType, count * 8);
		}
		BuilderPiece piecePrefab = BuilderSetManager.instance.GetPiecePrefab(pieceType);
		if (!(piecePrefab == null))
		{
			List<BuilderPiece> list = piecePools[value];
			for (int i = 0; i < count; i++)
			{
				BuilderPiece builderPiece = Object.Instantiate(piecePrefab);
				builderPiece.OnCreatedByPool();
				builderPiece.gameObject.SetActive(value: false);
				list.Add(builderPiece);
			}
		}
	}

	public BuilderPiece CreatePiece(int pieceType, bool assertNotEmpty)
	{
		if (!piecePoolLookup.TryGetValue(pieceType, out var value))
		{
			if (assertNotEmpty)
			{
				Debug.LogErrorFormat("No Pool Found for {0} Adding 4", pieceType);
			}
			value = piecePools.Count;
			AddToPool(pieceType, 4);
		}
		List<BuilderPiece> list = piecePools[value];
		if (list.Count == 0)
		{
			if (assertNotEmpty)
			{
				Debug.LogErrorFormat("Pool for {0} is Empty Adding 4", pieceType);
			}
			AddToPool(pieceType, 4);
		}
		BuilderPiece result = list[list.Count - 1];
		list.RemoveAt(list.Count - 1);
		return result;
	}

	public void DestroyPiece(BuilderPiece piece)
	{
		if (piece == null)
		{
			Debug.LogError("Why is a null piece being destroyed");
			return;
		}
		if (!piecePoolLookup.TryGetValue(piece.pieceType, out var value))
		{
			Debug.LogErrorFormat("No Pool Found for {0} Cannot return to pool", piece.pieceType);
			return;
		}
		List<BuilderPiece> list = piecePools[value];
		if (list.Count == 128)
		{
			piece.OnReturnToPool();
			Object.Destroy(piece.gameObject);
			return;
		}
		piece.gameObject.SetActive(value: false);
		piece.transform.SetParent(null);
		piece.transform.SetPositionAndRotation(Vector3.up * 10000f, Quaternion.identity);
		piece.OnReturnToPool();
		list.Add(piece);
	}

	private void AddToGlowBumpPool(int count)
	{
		if (!(bumpGlowPrefab == null))
		{
			for (int i = 0; i < count; i++)
			{
				BuilderBumpGlow builderBumpGlow = Object.Instantiate(bumpGlowPrefab);
				builderBumpGlow.gameObject.SetActive(value: false);
				bumpGlowPool.Add(builderBumpGlow);
			}
		}
	}

	public BuilderBumpGlow CreateGlowBump()
	{
		if (bumpGlowPool.Count == 0)
		{
			AddToGlowBumpPool(4);
		}
		BuilderBumpGlow result = bumpGlowPool[bumpGlowPool.Count - 1];
		bumpGlowPool.RemoveAt(bumpGlowPool.Count - 1);
		return result;
	}

	public void DestroyBumpGlow(BuilderBumpGlow bump)
	{
		if (!(bump == null))
		{
			bump.gameObject.SetActive(value: false);
			bump.transform.SetPositionAndRotation(Vector3.up * 10000f, Quaternion.identity);
			bumpGlowPool.Add(bump);
		}
	}

	private void AddToSnapOverlapPool(int count)
	{
		snapOverlapPool.Capacity += count;
		for (int i = 0; i < count; i++)
		{
			snapOverlapPool.Add(new SnapOverlap
			{
				inPool = true
			});
		}
	}

	public SnapOverlap CreateSnapOverlap(BuilderAttachGridPlane otherPlane, SnapBounds bounds)
	{
		if (snapOverlapPool.Count == 0)
		{
			AddToSnapOverlapPool(1024);
		}
		SnapOverlap snapOverlap = snapOverlapPool[snapOverlapPool.Count - 1];
		snapOverlapPool.RemoveAt(snapOverlapPool.Count - 1);
		snapOverlap.otherPlane = otherPlane;
		snapOverlap.bounds = bounds;
		snapOverlap.nextOverlap = null;
		snapOverlap.inPool = false;
		return snapOverlap;
	}

	public void DestroySnapOverlap(SnapOverlap snapOverlap)
	{
		if (!snapOverlap.inPool)
		{
			snapOverlap.otherPlane = null;
			snapOverlap.nextOverlap = null;
			snapOverlap.inPool = true;
			snapOverlapPool.Add(snapOverlap);
		}
	}

	private void OnDestroy()
	{
		for (int i = 0; i < piecePools.Count; i++)
		{
			if (piecePools[i] == null)
			{
				continue;
			}
			foreach (BuilderPiece item in piecePools[i])
			{
				if (item != null)
				{
					Object.Destroy(item);
				}
			}
			piecePools[i].Clear();
		}
		piecePoolLookup.Clear();
		foreach (BuilderBumpGlow item2 in bumpGlowPool)
		{
			Object.Destroy(item2);
		}
		bumpGlowPool.Clear();
	}
}
