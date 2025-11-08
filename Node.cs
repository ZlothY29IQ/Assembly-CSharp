using System.Collections.Generic;

public class Node<T>
{
	public T Value { get; set; }

	public Node<T> Parent { get; private set; }

	public List<Node<T>> Children { get; } = new List<Node<T>>();

	public Node(T value)
	{
		Value = value;
	}

	public Node<T> AddChild(T value)
	{
		Node<T> node = new Node<T>(value)
		{
			Parent = this
		};
		Children.Add(node);
		return node;
	}

	public Node<T> AddChild(Node<T> child)
	{
		child.Parent?.RemoveChild(child);
		Children.Add(child);
		child.Parent = this;
		return child;
	}

	public void RemoveChild(Node<T> child)
	{
		if (Children.Remove(child))
		{
			child.Parent = null;
		}
	}

	public IEnumerable<Node<T>> TraversePreOrder()
	{
		yield return this;
		foreach (Node<T> child in Children)
		{
			foreach (Node<T> item in child.TraversePreOrder())
			{
				yield return item;
			}
		}
	}

	public IEnumerable<Node<T>> TraverseBreadthFirst()
	{
		Queue<Node<T>> queue = new Queue<Node<T>>();
		queue.Enqueue(this);
		while (queue.Count > 0)
		{
			Node<T> current = queue.Dequeue();
			yield return current;
			foreach (Node<T> child in current.Children)
			{
				queue.Enqueue(child);
			}
		}
	}

	public List<Node<T>> GetPath()
	{
		List<Node<T>> list = new List<Node<T>> { this };
		for (Node<T> parent = Parent; parent != null; parent = parent.Parent)
		{
			list.Insert(0, parent);
		}
		return list;
	}
}
