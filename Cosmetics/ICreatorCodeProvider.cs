namespace Cosmetics;

public interface ICreatorCodeProvider
{
	string TerminalId { get; }

	void GetCreatorCode(out string code, out NexusGroupId[] groups);
}
