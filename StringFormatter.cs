using System;
using System.Collections.Generic;
using System.Text;

public class StringFormatter
{
	private static StringBuilder builder = new StringBuilder();

	private string[] spans;

	private int[] indices;

	public StringFormatter(string[] spans, int[] indices)
	{
		this.spans = spans;
		this.indices = indices;
	}

	public string Format(string term1)
	{
		builder.Clear();
		builder.Append(spans[0]);
		for (int i = 1; i < spans.Length; i++)
		{
			builder.Append(term1);
			builder.Append(spans[i]);
		}
		return builder.ToString();
	}

	public string Format(Func<string> term1)
	{
		builder.Clear();
		builder.Append(spans[0]);
		for (int i = 1; i < spans.Length; i++)
		{
			builder.Append(term1());
			builder.Append(spans[i]);
		}
		return builder.ToString();
	}

	public string Format(string term1, string term2)
	{
		builder.Clear();
		builder.Append(spans[0]);
		for (int i = 1; i < spans.Length; i++)
		{
			builder.Append((indices[i - 1] == 0) ? term1 : term2);
			builder.Append(spans[i]);
		}
		return builder.ToString();
	}

	public string Format(string term1, string term2, string term3)
	{
		builder.Clear();
		builder.Append(spans[0]);
		for (int i = 1; i < spans.Length; i++)
		{
			switch (indices[i - 1])
			{
			case 0:
				builder.Append(term1);
				break;
			case 1:
				builder.Append(term2);
				break;
			default:
				builder.Append(term3);
				break;
			}
			builder.Append(spans[i]);
		}
		return builder.ToString();
	}

	public string Format(Func<string> term1, Func<string> term2)
	{
		builder.Clear();
		builder.Append(spans[0]);
		for (int i = 1; i < spans.Length; i++)
		{
			if (indices[i - 1] == 0)
			{
				builder.Append(term1());
			}
			else
			{
				builder.Append(term2());
			}
			builder.Append(spans[i]);
		}
		return builder.ToString();
	}

	public string Format(Func<string> term1, Func<string> term2, Func<string> term3)
	{
		builder.Clear();
		builder.Append(spans[0]);
		for (int i = 1; i < spans.Length; i++)
		{
			switch (indices[i - 1])
			{
			case 0:
				builder.Append(term1());
				break;
			case 1:
				builder.Append(term2());
				break;
			default:
				builder.Append(term3());
				break;
			}
			builder.Append(spans[i]);
		}
		return builder.ToString();
	}

	public string Format(Func<string> term1, Func<string> term2, Func<string> term3, Func<string> term4)
	{
		builder.Clear();
		builder.Append(spans[0]);
		for (int i = 1; i < spans.Length; i++)
		{
			switch (indices[i - 1])
			{
			case 0:
				builder.Append(term1());
				break;
			case 1:
				builder.Append(term2());
				break;
			case 2:
				builder.Append(term3());
				break;
			default:
				builder.Append(term4());
				break;
			}
			builder.Append(spans[i]);
		}
		return builder.ToString();
	}

	public string Format(params string[] terms)
	{
		builder.Clear();
		builder.Append(spans[0]);
		for (int i = 1; i < spans.Length; i++)
		{
			builder.Append(terms[indices[i - 1]]);
			builder.Append(spans[i]);
		}
		return builder.ToString();
	}

	public string Format(params Func<string>[] terms)
	{
		builder.Clear();
		builder.Append(spans[0]);
		for (int i = 1; i < spans.Length; i++)
		{
			builder.Append(terms[indices[i - 1]]());
			builder.Append(spans[i]);
		}
		return builder.ToString();
	}

	public static StringFormatter Parse(string input)
	{
		int num = 0;
		List<string> list = new List<string>();
		List<int> list2 = new List<int>();
		while (true)
		{
			int num2 = input.IndexOf('%', num);
			if (num2 == -1)
			{
				break;
			}
			list.Add(input.Substring(num, num2 - num));
			list2.Add(input[num2 + 1] - 48);
			num = num2 + 2;
		}
		list.Add(input.Substring(num));
		return new StringFormatter(list.ToArray(), list2.ToArray());
	}
}
