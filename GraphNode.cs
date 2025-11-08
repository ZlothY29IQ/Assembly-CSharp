using System;
using System.Collections.Generic;

public class GraphNode<T>
{
	public T Value { get; set; }

	public List<GraphNode<T>> Parents { get; } = new List<GraphNode<T>>();

	public List<GraphNode<T>> Children { get; } = new List<GraphNode<T>>();

	public int ChildCount => Children.Count;

	public GraphNode(T value)
	{
		Value = value;
	}

	public GraphNode(T value, GraphNode<T> parent)
	{
		Value = value;
		Parents.Add(parent);
	}

	public int GetSubtreeWidth(int depthLimit = int.MaxValue)
	{
		if (ChildCount == 0 || depthLimit == 0)
		{
			return 1;
		}
		int num = 0;
		foreach (GraphNode<T> child in Children)
		{
			num += child.GetSubtreeWidth(depthLimit - 1);
		}
		return num;
	}

	public GraphNode<T> AddChild(T value)
	{
		return AddChild(new GraphNode<T>(value));
	}

	public GraphNode<T> AddChild(GraphNode<T> child)
	{
		if (child.Parents.Contains(this))
		{
			throw new InvalidOperationException("Cannot add child more than once");
		}
		Children.Add(child);
		child.Parents.Add(this);
		return child;
	}

	public void RemoveChild(GraphNode<T> child)
	{
		if (Children.Remove(child))
		{
			child.Parents.Remove(this);
		}
	}

	public IEnumerable<GraphNode<T>> TraversePreOrder()
	{
		yield return this;
		foreach (GraphNode<T> child in Children)
		{
			foreach (GraphNode<T> item in child.TraversePreOrder())
			{
				yield return item;
			}
		}
	}

	public IEnumerable<GraphNode<T>> TraversePreOrderDistinct(HashSet<GraphNode<T>> visited = null)
	{
		if (visited == null)
		{
			visited = new HashSet<GraphNode<T>>();
		}
		if (visited.Contains(this))
		{
			yield break;
		}
		yield return this;
		visited.Add(this);
		foreach (GraphNode<T> child in Children)
		{
			foreach (GraphNode<T> item in child.TraversePreOrderDistinct(visited))
			{
				yield return item;
			}
		}
	}

	public IEnumerable<GraphNode<T>> TraverseBreadthFirst()
	{
		Queue<GraphNode<T>> queue = new Queue<GraphNode<T>>();
		queue.Enqueue(this);
		while (queue.Count > 0)
		{
			GraphNode<T> current = queue.Dequeue();
			yield return current;
			foreach (GraphNode<T> child in current.Children)
			{
				queue.Enqueue(child);
			}
		}
	}

	public IEnumerable<GraphNode<T>> TraverseBreadthFirstDistinct()
	{
		Queue<GraphNode<T>> queue = new Queue<GraphNode<T>>();
		HashSet<GraphNode<T>> visited = new HashSet<GraphNode<T>>();
		queue.Enqueue(this);
		while (queue.Count > 0)
		{
			GraphNode<T> current = queue.Dequeue();
			if (visited.Contains(current))
			{
				continue;
			}
			visited.Add(current);
			yield return current;
			foreach (GraphNode<T> child in current.Children)
			{
				queue.Enqueue(child);
			}
		}
	}

	public int GetGraphDepth()
	{
		if (Children.Count == 0)
		{
			return 1;
		}
		int num = 0;
		foreach (GraphNode<T> child in Children)
		{
			num = Math.Max(num, child.GetGraphDepth());
		}
		return num + 1;
	}

	public int GetNodeDepth()
	{
		if (Parents.Count == 0)
		{
			return 1;
		}
		int num = 0;
		foreach (GraphNode<T> parent in Parents)
		{
			num = Math.Max(num, parent.GetNodeDepth());
		}
		return num + 1;
	}
}
