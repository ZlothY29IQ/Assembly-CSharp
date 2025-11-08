using UnityEngine;

namespace BoingKit;

public class BoingBase : MonoBehaviour
{
	[SerializeField]
	private Version m_currentVersion;

	[SerializeField]
	private Version m_previousVersion;

	[SerializeField]
	private Version m_initialVersion = BoingKit.Version;

	public Version CurrentVersion => m_currentVersion;

	public Version PreviousVersion => m_previousVersion;

	public Version InitialVersion => m_initialVersion;

	protected virtual void OnUpgrade(Version oldVersion, Version newVersion)
	{
		m_previousVersion = m_currentVersion;
		if (m_currentVersion.Revision < 33)
		{
			m_initialVersion = Version.Invalid;
			m_previousVersion = Version.Invalid;
		}
		m_currentVersion = newVersion;
	}
}
