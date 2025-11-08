using UnityEngine;

[CreateAssetMenu(fileName = "New Hand Gesture", menuName = "Gorilla/Hand Gesture")]
public class GorillaHandGesture : ScriptableObject
{
	public bool track = true;

	public GestureNode[] nodes = InitNodes();

	public GestureHandNode hand
	{
		get
		{
			return (GestureHandNode)nodes[0];
		}
		set
		{
			nodes[0] = value;
		}
	}

	public GestureNode palm
	{
		get
		{
			return nodes[1];
		}
		set
		{
			nodes[1] = value;
		}
	}

	public GestureNode wrist
	{
		get
		{
			return nodes[2];
		}
		set
		{
			nodes[2] = value;
		}
	}

	public GestureNode digits
	{
		get
		{
			return nodes[3];
		}
		set
		{
			nodes[3] = value;
		}
	}

	public GestureDigitNode thumb
	{
		get
		{
			return (GestureDigitNode)nodes[4];
		}
		set
		{
			nodes[4] = value;
		}
	}

	public GestureDigitNode index
	{
		get
		{
			return (GestureDigitNode)nodes[5];
		}
		set
		{
			nodes[5] = value;
		}
	}

	public GestureDigitNode middle
	{
		get
		{
			return (GestureDigitNode)nodes[6];
		}
		set
		{
			nodes[6] = value;
		}
	}

	private static GestureNode[] InitNodes()
	{
		return new GestureNode[7]
		{
			new GestureHandNode(),
			new GestureNode(),
			new GestureNode(),
			new GestureNode(),
			new GestureDigitNode(),
			new GestureDigitNode(),
			new GestureDigitNode()
		};
	}
}
