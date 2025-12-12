using UnityEngine;

namespace Cosmetics;

public interface ICreatorCodeProvider
{
	GameObject GameObject { get; }

	string TerminalId { get; }

	void GetCreatorCode(out string code, out NexusGroupId[] groups);
}
