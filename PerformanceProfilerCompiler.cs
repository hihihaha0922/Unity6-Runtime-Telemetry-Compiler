using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

public class PerformanceProfilerCompiler
{
    private List<ProfilerTelemetryData> _telemetryCache = new List<ProfilerTelemetryData>();
    private bool _isProcessingData = false;

    /// <summary>
    /// Gathers live frame diagnostics from the Unity Editor and current UI Document trees.
    /// </summary>
    public void RecordTelemetryFrame(UIDocument activeUiDocument)
    {
        int elementCount = 0;
        int maxDepth = 0;

        if (activeUiDocument != null && activeUiDocument.rootVisualElement != null)
        {
            CalculateUiTreeMetrics(activeUiDocument.rootVisualElement, 1, ref elementCount, ref maxDepth);
        }

        ProfilerTelemetryData frameData = new ProfilerTelemetryData
        {
            FrameTimestamp = DateTime.Now.ToString("HH:mm:ss.fff"),
            FrameTimeMs = Time.unscaledDeltaTime * 1000f,
            TotalAllocatedMemoryBytes = GC.GetTotalMemory(false),
            ActiveVisualElementsCount = elementCount,
            DeepestUIHierarchyDepth = maxDepth
        };

        _telemetryCache.Add(frameData);
    }

    /// <summary>
    /// Recursive algorithm calculating UI complexity layout metrics.
    /// </summary>
    private void CalculateUiTreeMetrics(VisualElement element, int currentDepth, ref int totalCount, ref int maxDepth)
    {
        totalCount++;
        if (currentDepth > maxDepth) maxDepth = currentDepth;

        // FIX: Use element.hierarchy.Children() instead of element.children
        foreach (var child in element.hierarchy.Children())
        {
            CalculateUiTreeMetrics(child, currentDepth + 1, ref totalCount, ref maxDepth);
        }
    }

    /// <summary>
    /// ASYNCHRONOUS COMPILER: Offloads telemetry list processing and binary disk serialization 
    /// to a background thread pool, maintaining absolute fluid 60FPS on Unity's main thread.
    /// </summary>
    public async Task<string> CompileReportToBinaryAsync(string targetDirectory)
    {
        if (_isProcessingData) return "Profiler is currently busy compilation processing.";
        _isProcessingData = true;

        // Clone current cache buffer snapshot so the main thread can instantly keep recording safely
        List<ProfilerTelemetryData> snapshotToProcess = new List<ProfilerTelemetryData>(_telemetryCache);
        _telemetryCache.Clear();

        string filename = $"Telemetry_Report_{DateTime.Now:yyyyMMdd_HHmmss}.bin";
        string fullPath = Path.Combine(targetDirectory, filename);

        // Task.Run explicitly tells .NET to jump out of Unity's main thread and utilize a background thread
        await Task.Run(() =>
        {
            using (FileStream stream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                using (BinaryWriter writer = new BinaryWriter(stream, Encoding.UTF8))
                {
                    // Write Header Signature Magic Bytes (Verifies file data schema validity)
                    writer.Write(new char[] { 'L', 'A', 'R', 'I' });
                    writer.Write(snapshotToProcess.Count); // Total records identifier

                    foreach (var data in snapshotToProcess)
                    {
                        writer.Write(data.FrameTimestamp);
                        writer.Write(data.FrameTimeMs);
                        writer.Write(data.TotalAllocatedMemoryBytes);
                        writer.Write(data.ActiveVisualElementsCount);
                        writer.Write(data.DeepestUIHierarchyDepth);
                    }
                }
            }
        });

        _isProcessingData = false;
        return fullPath;
    }

    public List<ProfilerTelemetryData> LoadAndParseBinaryReport(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Debug.LogError($"[Larian Tool Suite] Target file path not found: {filePath}");
            return null;
        }

        List<ProfilerTelemetryData> parsedData = new List<ProfilerTelemetryData>();

        using (FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            using (BinaryReader reader = new BinaryReader(stream, Encoding.UTF8))
            {
                // Verify Magic Bytes Header Signature
                char[] header = reader.ReadChars(4);
                string sig = new string(header);
                if (sig != "LARI")
                {
                    Debug.LogError("[Larian Tool Suite] Invalid data signature. This file is not an authorized telemetry file.");
                    return null;
                }

                int totalRecords = reader.ReadInt32();

                for (int i = 0; i < totalRecords; i++)
                {
                    ProfilerTelemetryData record = new ProfilerTelemetryData
                    {
                        FrameTimestamp = reader.ReadString(),
                        FrameTimeMs = reader.ReadSingle(),
                        TotalAllocatedMemoryBytes = reader.ReadInt64(),
                        ActiveVisualElementsCount = reader.ReadInt32(),
                        DeepestUIHierarchyDepth = reader.ReadInt32()
                    };
                    parsedData.Add(record);
                }
            }
        }

        return parsedData;
    }
}