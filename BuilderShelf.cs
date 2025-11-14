using System;
using System.Collections.Generic;
using GorillaTagScripts;
using Photon.Pun;
using UnityEngine;

public class BuilderShelf : MonoBehaviour
{
	[Serializable]
	public class BuildPieceSpawn
	{
		public GameObject buildPiecePrefab;

		public string materialID;

		public int count = 1;

		public Vector3 localAxis = Vector3.right;

		[Tooltip("Optional Editor Visual")]
		public Mesh previewMesh;
	}

	private int count;

	public float separation;

	public Transform center;

	public List<BuildPieceSpawn> buildPieceSpawns;

	private List<BuilderResources> spawnCosts;

	private int shelfSlot;

	private int buildPieceSpawnIndex;

	private int spawnCount;

	public void Init()
	{
		shelfSlot = 0;
		buildPieceSpawnIndex = 0;
		spawnCount = 0;
		count = 0;
		spawnCosts = new List<BuilderResources>(buildPieceSpawns.Count);
		for (int i = 0; i < buildPieceSpawns.Count; i++)
		{
			count += buildPieceSpawns[i].count;
			BuilderPiece component = buildPieceSpawns[i].buildPiecePrefab.GetComponent<BuilderPiece>();
			spawnCosts.Add(component.cost);
		}
	}

	public bool HasOpenSlot()
	{
		return shelfSlot < count;
	}

	public void BuildNextPiece(BuilderTable table)
	{
		if (!HasOpenSlot())
		{
			return;
		}
		BuildPieceSpawn buildPieceSpawn = buildPieceSpawns[buildPieceSpawnIndex];
		BuilderResources resources = spawnCosts[buildPieceSpawnIndex];
		while (!table.HasEnoughUnreservedResources(resources) && buildPieceSpawnIndex < buildPieceSpawns.Count - 1)
		{
			int num = buildPieceSpawn.count - spawnCount;
			shelfSlot += num;
			spawnCount = 0;
			buildPieceSpawnIndex++;
			buildPieceSpawn = buildPieceSpawns[buildPieceSpawnIndex];
			resources = spawnCosts[buildPieceSpawnIndex];
		}
		if (!table.HasEnoughUnreservedResources(resources))
		{
			int num2 = buildPieceSpawn.count - spawnCount;
			shelfSlot += num2;
			spawnCount = 0;
			return;
		}
		int staticHash = buildPieceSpawn.buildPiecePrefab.name.GetStaticHash();
		int materialType = (string.IsNullOrEmpty(buildPieceSpawn.materialID) ? (-1) : buildPieceSpawn.materialID.GetHashCode());
		GetSpawnLocation(shelfSlot, buildPieceSpawn, out var spawnPosition, out var spawnRotation);
		int pieceId = table.CreatePieceId();
		table.CreatePiece(staticHash, pieceId, spawnPosition, spawnRotation, materialType, BuilderPiece.State.OnShelf, PhotonNetwork.LocalPlayer);
		spawnCount++;
		shelfSlot++;
		if (spawnCount >= buildPieceSpawn.count)
		{
			buildPieceSpawnIndex++;
			spawnCount = 0;
		}
	}

	public void InitCount()
	{
		count = 0;
		for (int i = 0; i < buildPieceSpawns.Count; i++)
		{
			count += buildPieceSpawns[i].count;
		}
	}

	public void BuildItems(BuilderTable table)
	{
		int num = 0;
		InitCount();
		for (int i = 0; i < buildPieceSpawns.Count; i++)
		{
			BuildPieceSpawn buildPieceSpawn = buildPieceSpawns[i];
			if (buildPieceSpawn == null || buildPieceSpawn.count == 0)
			{
				continue;
			}
			int staticHash = buildPieceSpawn.buildPiecePrefab.name.GetStaticHash();
			int materialType = (string.IsNullOrEmpty(buildPieceSpawn.materialID) ? (-1) : buildPieceSpawn.materialID.GetHashCode());
			for (int j = 0; j < buildPieceSpawn.count; j++)
			{
				if (num >= count)
				{
					break;
				}
				GetSpawnLocation(num, buildPieceSpawn, out var spawnPosition, out var spawnRotation);
				int pieceId = table.CreatePieceId();
				table.CreatePiece(staticHash, pieceId, spawnPosition, spawnRotation, materialType, BuilderPiece.State.OnShelf, PhotonNetwork.LocalPlayer);
				num++;
			}
		}
	}

	public void GetSpawnLocation(int slot, BuildPieceSpawn spawn, out Vector3 spawnPosition, out Quaternion spawnRotation)
	{
		if (center == null)
		{
			center = base.transform;
		}
		Vector3 vector = Vector3.zero;
		Vector3 euler = Vector3.zero;
		BuilderPiece component = spawn.buildPiecePrefab.GetComponent<BuilderPiece>();
		if (component != null)
		{
			vector = component.desiredShelfOffset;
			euler = component.desiredShelfRotationOffset;
		}
		spawnRotation = center.rotation * Quaternion.Euler(euler);
		float num = (float)slot * separation - (float)(count - 1) * separation / 2f;
		spawnPosition = center.position + center.rotation * (spawn.localAxis * num + vector);
	}
}
