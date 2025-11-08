using System;
using UnityEngine;

namespace com.AnotherAxiom.SpaceFight;

public class SpaceFight : ArcadeGame
{
	[Serializable]
	private struct SpaceFlightNetState : IEquatable<SpaceFlightNetState>
	{
		public float P1LocX;

		public float P1LocY;

		public float P1Rot;

		public float P2LocX;

		public float P2LocY;

		public float P2Rot;

		public float P1PrLocX;

		public float P1PrLocY;

		public float P2PrLocX;

		public float P2PrLocY;

		public bool Equals(SpaceFlightNetState other)
		{
			if (!P1LocX.Approx(other.P1LocX))
			{
				return false;
			}
			if (!P1LocY.Approx(other.P1LocY))
			{
				return false;
			}
			if (!P1Rot.Approx(other.P1Rot))
			{
				return false;
			}
			if (!P2LocX.Approx(other.P2LocX))
			{
				return false;
			}
			if (!P2LocY.Approx(other.P2LocY))
			{
				return false;
			}
			if (!P1Rot.Approx(other.P1Rot))
			{
				return false;
			}
			if (!P1PrLocX.Approx(other.P1PrLocX))
			{
				return false;
			}
			if (!P1PrLocY.Approx(other.P1PrLocY))
			{
				return false;
			}
			if (!P2PrLocX.Approx(other.P2PrLocX))
			{
				return false;
			}
			if (!P2PrLocY.Approx(other.P2PrLocY))
			{
				return false;
			}
			return true;
		}
	}

	[SerializeField]
	private Transform[] player;

	[SerializeField]
	private Transform[] projectile;

	[SerializeField]
	private Vector2 tableSize;

	private bool[] projectilesFired = new bool[2];

	private SpaceFlightNetState netStateLast;

	private SpaceFlightNetState netStateCur;

	private void Update()
	{
		for (int i = 0; i < 2; i++)
		{
			if (getButtonState(i, ArcadeButtons.UP))
			{
				move(player[i], 0.15f);
				clamp(player[i]);
			}
			if (getButtonState(i, ArcadeButtons.RIGHT))
			{
				turn(player[i], cw: true);
			}
			if (getButtonState(i, ArcadeButtons.LEFT))
			{
				turn(player[i], cw: false);
			}
			if (projectilesFired[i])
			{
				move(projectile[i], 0.5f);
				if (Vector2.Distance(player[1 - i].localPosition, projectile[i].localPosition) < 0.25f)
				{
					PlaySound(1, 2);
					player[1 - i].Rotate(0f, 0f, 180f);
					projectilesFired[i] = false;
				}
				if (Mathf.Abs(projectile[i].localPosition.x) > tableSize.x || Mathf.Abs(projectile[i].localPosition.y) > tableSize.y)
				{
					projectilesFired[i] = false;
				}
			}
			if (!projectilesFired[i])
			{
				projectile[i].position = player[i].position;
				projectile[i].rotation = player[i].rotation;
			}
		}
	}

	private void clamp(Transform tr)
	{
		tr.localPosition = new Vector2(Mathf.Clamp(tr.localPosition.x, 0f - tableSize.x, tableSize.x), Mathf.Clamp(tr.localPosition.y, 0f - tableSize.y, tableSize.y));
	}

	protected override void ButtonDown(int player, ArcadeButtons button)
	{
		if (button == ArcadeButtons.TRIGGER)
		{
			if (!projectilesFired[player])
			{
				PlaySound(0);
			}
			projectilesFired[player] = true;
		}
	}

	private void move(Transform p, float speed)
	{
		p.Translate(p.up * Time.deltaTime * speed, Space.World);
	}

	private void turn(Transform p, bool cw)
	{
		p.Rotate(0f, 0f, (float)(cw ? 180 : (-180)) * Time.deltaTime);
	}

	public override byte[] GetNetworkState()
	{
		netStateCur.P1LocX = player[0].localPosition.x;
		netStateCur.P1LocY = player[0].localPosition.y;
		netStateCur.P1Rot = player[0].localRotation.eulerAngles.z;
		netStateCur.P2LocX = player[1].localPosition.x;
		netStateCur.P2LocY = player[1].localPosition.y;
		netStateCur.P2Rot = player[1].localRotation.eulerAngles.z;
		netStateCur.P1PrLocX = projectile[0].localPosition.x;
		netStateCur.P1PrLocY = projectile[0].localPosition.y;
		netStateCur.P2PrLocX = projectile[1].localPosition.x;
		netStateCur.P2PrLocY = projectile[1].localPosition.y;
		if (!netStateCur.Equals(netStateLast))
		{
			netStateLast = netStateCur;
			SwapNetStateBuffersAndStreams();
			ArcadeGame.WrapNetState(netStateLast, netStateMemStream);
		}
		return netStateBuffer;
	}

	public override void SetNetworkState(byte[] b)
	{
		SpaceFlightNetState spaceFlightNetState = (SpaceFlightNetState)ArcadeGame.UnwrapNetState(b);
		player[0].localPosition = new Vector2(spaceFlightNetState.P1LocX, spaceFlightNetState.P1LocY);
		player[0].localRotation = Quaternion.Euler(0f, 0f, spaceFlightNetState.P1Rot);
		player[1].localPosition = new Vector2(spaceFlightNetState.P2LocX, spaceFlightNetState.P2LocY);
		player[1].localRotation = Quaternion.Euler(0f, 0f, spaceFlightNetState.P2Rot);
		projectile[0].localPosition = new Vector2(spaceFlightNetState.P1PrLocX, spaceFlightNetState.P1PrLocY);
		projectile[1].localPosition = new Vector2(spaceFlightNetState.P2PrLocX, spaceFlightNetState.P2PrLocY);
	}

	protected override void ButtonUp(int player, ArcadeButtons button)
	{
	}

	public override void OnTimeout()
	{
	}
}
