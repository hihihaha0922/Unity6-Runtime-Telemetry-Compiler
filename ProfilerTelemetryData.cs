using System;

[Serializable]
public struct ProfilerTelemetryData
{
    public string FrameTimestamp;
    public float FrameTimeMs;
    public long TotalAllocatedMemoryBytes;
    public int ActiveVisualElementsCount;
    public int DeepestUIHierarchyDepth;
}
