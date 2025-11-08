using System;
using System.Collections.Generic;

[Serializable]
public class ScenePerformanceData
{
	public string _mapName;

	public int _gorillaCount;

	public int _droppedFrames;

	public int _msHigh;

	public int _medianMS;

	public int _medianFPS;

	public int _medianDrawCallCount;

	public List<int> _msCaptures;

	public ScenePerformanceData(string mapName, int gorillaCount, int droppedFrames, int msHigh, int medianMS, int medianFPS, int medianDrawCalls, List<int> msCaptures)
	{
		_mapName = mapName;
		_gorillaCount = gorillaCount;
		_droppedFrames = droppedFrames;
		_msHigh = msHigh;
		_medianMS = medianMS;
		_medianFPS = medianFPS;
		_medianDrawCallCount = medianDrawCalls;
		_msCaptures = new List<int>(msCaptures);
	}
}
