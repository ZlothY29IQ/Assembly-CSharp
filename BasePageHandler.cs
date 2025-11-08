using UnityEngine;

public abstract class BasePageHandler : MonoBehaviour
{
	protected int selectedIndex { get; private set; }

	protected int currentPage { get; private set; }

	protected int pages { get; private set; }

	protected int maxEntires { get; private set; }

	protected abstract int pageSize { get; }

	protected abstract int entriesCount { get; }

	protected virtual void Start()
	{
		Debug.Log("base page handler " + entriesCount + " " + pageSize);
		pages = entriesCount / pageSize + 1;
		maxEntires = pages * pageSize;
	}

	public void SelectEntryOnPage(int entryIndex)
	{
		int num = entryIndex + pageSize * currentPage;
		if (num <= entriesCount)
		{
			selectedIndex = num;
			PageEntrySelected(entryIndex, selectedIndex);
		}
	}

	public void SelectEntryFromIndex(int index)
	{
		selectedIndex = index;
		currentPage = selectedIndex / pageSize;
		int pageEntry = index - pageSize * currentPage;
		PageEntrySelected(pageEntry, index);
		SetPage(currentPage);
	}

	public void ChangePage(bool left)
	{
		int num = ((!left) ? 1 : (-1));
		SetPage(Mathf.Abs((currentPage + num) % pages));
	}

	public void SetPage(int page)
	{
		if (page <= pages)
		{
			currentPage = page;
			int num = pageSize * page;
			ShowPage(currentPage, num, Mathf.Min(num + pageSize, entriesCount));
		}
	}

	protected abstract void ShowPage(int selectedPage, int startIndex, int endIndex);

	protected abstract void PageEntrySelected(int pageEntry, int selectionIndex);
}
