using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.IO;

public class AAAProfilerWindow : EditorWindow
{
    private PerformanceProfilerCompiler _compiler;
    private bool _isProfiling = false;

    private Label _statusLabel;
    private Label _liveMetricsLabel;
    private Label _analysisResultsLabel;
    private ScrollView _visualTreeScrollView; // New ScrollBox for the Directory View
    private Button _toggleButton;
    private Button _compileButton;

    private UIDocument _trackedUiDocument;
    private string _customDirectory = @"D:\Tools\Tools unity\Report";

    [MenuItem("Larian Workflow Suite/Runtime Profiler & Compiler")]
    public static void ShowWindow()
    {
        AAAProfilerWindow window = GetWindow<AAAProfilerWindow>();
        window.titleContent = new GUIContent("Studio Profiler");
        window.minSize = new Vector2(450, 650);
    }

    public void CreateGUI()
    {
        _compiler = new PerformanceProfilerCompiler();
        VisualElement root = rootVisualElement;
        root.style.paddingTop = new StyleLength(new Length(12, LengthUnit.Pixel));
        root.style.paddingBottom = new StyleLength(new Length(12, LengthUnit.Pixel));
        root.style.paddingLeft = new StyleLength(new Length(12, LengthUnit.Pixel));
        root.style.paddingRight = new StyleLength(new Length(12, LengthUnit.Pixel));

        // SECTION 1: Telemetry Controls
        Label header = new Label("AAA RUNTIME PROFILER TELEMETRY");
        header.style.unityFontStyleAndWeight = FontStyle.Bold;
        header.style.fontSize = 14;
        header.style.marginBottom = 10;
        root.Add(header);

        _statusLabel = new Label("Status: Idle");
        _statusLabel.style.color = Color.gray;
        root.Add(_statusLabel);

        _liveMetricsLabel = new Label("\n--- Live Visual Analytics --- \nNo Active Captures.");
        _liveMetricsLabel.style.marginBottom = 15;
        root.Add(_liveMetricsLabel);

        _toggleButton = new Button(ToggleProfiling) { text = "Start Telemetry Session" };
        _toggleButton.style.height = 30;
        _toggleButton.style.backgroundColor = new Color(0.2f, 0.4f, 0.2f);
        root.Add(_toggleButton);

        _compileButton = new Button(CompileDataAsync) { text = "Compile to Binary Data Log" };
        _compileButton.style.height = 30;
        _compileButton.style.marginTop = 5;
        _compileButton.style.marginBottom = 15;
        _compileButton.SetEnabled(false);
        root.Add(_compileButton);

        // Visual Separator Line Break
        VisualElement line = new VisualElement();
        line.style.height = 2;
        line.style.backgroundColor = Color.gray;
        line.style.marginBottom = 15;
        root.Add(line);

        // SECTION 2: Automated Pipeline Analysis
        Label analysisHeader = new Label("STUDIO DATA PIPELINE ANALYZER");
        analysisHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
        analysisHeader.style.fontSize = 12;
        root.Add(analysisHeader);

        Button loadButton = new Button(SelectAndAnalyzeFile) { text = "Select & Analyze Report File (.bin)" };
        loadButton.style.height = 30;
        loadButton.style.marginTop = 8;
        loadButton.style.backgroundColor = new Color(0.2f, 0.3f, 0.5f);
        root.Add(loadButton);

        _analysisResultsLabel = new Label("\nNo report data loaded. Awaiting binary packet stream...");
        _analysisResultsLabel.style.marginTop = 10;
        root.Add(_analysisResultsLabel);

        // NEW SECTION 3: The Live Directory Architecture Mapping Box
        Label directoryHeader = new Label("LIVE LAYOUT TREE ARCHITECTURE DIRECTORY");
        directoryHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
        directoryHeader.style.fontSize = 11;
        directoryHeader.style.marginTop = 15;
        directoryHeader.style.marginBottom = 5;
        root.Add(directoryHeader);

        _visualTreeScrollView = new ScrollView();
        _visualTreeScrollView.style.flexGrow = 1;
        _visualTreeScrollView.style.backgroundColor = new Color(0.12f, 0.12f, 0.12f);
        // FIX 1: Use StyleFloat to assign explicit uniform borders on all 4 sides
        _visualTreeScrollView.style.borderTopWidth = new StyleFloat(1f);
        _visualTreeScrollView.style.borderBottomWidth = new StyleFloat(1f);
        _visualTreeScrollView.style.borderLeftWidth = new StyleFloat(1f);
        _visualTreeScrollView.style.borderRightWidth = new StyleFloat(1f);

        // FIX 2: Use StyleColor to apply uniform color across all border edges
        _visualTreeScrollView.style.borderTopColor = new StyleColor(Color.gray);
        _visualTreeScrollView.style.borderBottomColor = new StyleColor(Color.gray);
        _visualTreeScrollView.style.borderLeftColor = new StyleColor(Color.gray);
        _visualTreeScrollView.style.borderRightColor = new StyleColor(Color.gray);

        // FIX 3: Use StyleLength with Length.Pixel to pad all 4 internal edges
        _visualTreeScrollView.style.paddingTop = new StyleLength(new Length(8, LengthUnit.Pixel));
        _visualTreeScrollView.style.paddingBottom = new StyleLength(new Length(8, LengthUnit.Pixel));
        _visualTreeScrollView.style.paddingLeft = new StyleLength(new Length(8, LengthUnit.Pixel));
        _visualTreeScrollView.style.paddingRight = new StyleLength(new Length(8, LengthUnit.Pixel));
        root.Add(_visualTreeScrollView);
    }

    private void ToggleProfiling()
    {
        _isProfiling = !_isProfiling;
        if (_isProfiling)
        {
            _toggleButton.text = "Stop Telemetry Session";
            _toggleButton.style.backgroundColor = new Color(0.5f, 0.2f, 0.2f);
            _statusLabel.text = "Status: RECORDING LIVE DATA...";
            _statusLabel.style.color = Color.red;
            _compileButton.SetEnabled(false);
            _trackedUiDocument = FindFirstObjectByType<UIDocument>();
        }
        else
        {
            _toggleButton.text = "Resume Telemetry Session";
            _toggleButton.style.backgroundColor = new Color(0.2f, 0.4f, 0.2f);
            _statusLabel.text = "Status: Session Paused. Buffer Loaded.";
            _statusLabel.style.color = Color.yellow;
            _compileButton.SetEnabled(true);
        }
    }

    private void Update()
    {
        if (!_isProfiling) return;

        _compiler.RecordTelemetryFrame(_trackedUiDocument);
        _liveMetricsLabel.text = $"--- Live Visual Analytics --- \n" +
                                 $"Engine Runtime: {Time.unscaledTime:F2}s\n" +
                                 $"Active UI Components Processed: {(_trackedUiDocument != null ? "Active Node Tree Logged" : "No Target Active")}\n" +
                                 $"Sys Alloc RAM Buffer: {(System.GC.GetTotalMemory(false) / 1024f / 1024f):F2} MB";

        // Every frame update, rebuild the folder view live!
        if (_trackedUiDocument != null && _trackedUiDocument.rootVisualElement != null)
        {
            GenerateLiveDirectoryMap(_trackedUiDocument.rootVisualElement);
        }
    }

    /// <summary>
    /// GENERATOR: Clears the display scrollbox and traces the hierarchy live, 
    /// instantiating indented, color-coded custom row folders.
    /// </summary>
    private void GenerateLiveDirectoryMap(VisualElement rootElement)
    {
        _visualTreeScrollView.Clear();
        RenderDirectoryNode(rootElement, 0);
    }

    /// <summary>
    /// Recursive function that acts as a compiler layout display drawer.
    /// </summary>
    private void RenderDirectoryNode(VisualElement element, int currentDepth)
    {
        string elementName = string.IsNullOrEmpty(element.name) ? $"[{element.GetType().Name}]" : element.name;

        // Truncate clean names if they are too long for our view box row
        if (elementName.Length > 30) elementName = elementName.Substring(0, 27) + "...";

        // Generate folder list label row layout
        Label rowLabel = new Label($"{new string(' ', currentDepth * 4)}📁 {elementName}");

        // DYNAMIC STUDIO THERMAL BUDGET COLORING RULES:
        if (currentDepth < 5)
            rowLabel.style.color = new Color(0.4f, 0.9f, 0.4f); // Healthy Green (Shallow layers)
        else if (currentDepth >= 5 && currentDepth < 12)
            rowLabel.style.color = new Color(1f, 0.66f, 0f);    // Warning Orange (Medium depth burden)
        else
            rowLabel.style.color = new Color(1f, 0.25f, 0.25f); // Critical Deep Red (Severe calculation load risk)

        rowLabel.style.fontSize = 11;
        _visualTreeScrollView.Add(rowLabel);

        // Crawl deeper down through children elements
        foreach (var child in element.hierarchy.Children())
        {
            RenderDirectoryNode(child, currentDepth + 1);
        }
    }

    private async void CompileDataAsync()
    {
        _statusLabel.text = "Status: FORKING PROCESS TO BACKGROUND THREAD...";
        _statusLabel.style.color = Color.cyan;
        _compileButton.SetEnabled(false);

        if (!Directory.Exists(_customDirectory)) Directory.CreateDirectory(_customDirectory);
        string finalFileLocation = await _compiler.CompileReportToBinaryAsync(_customDirectory);

        _statusLabel.text = "Status: SUCCESS! Binary Stream Compressed.";
        _statusLabel.style.color = Color.green;
    }

    private void SelectAndAnalyzeFile()
    {
        string selectedPath = EditorUtility.OpenFilePanel("Select Studio Telemetry File", _customDirectory, "bin");
        if (string.IsNullOrEmpty(selectedPath)) return;

        List<ProfilerTelemetryData> reports = _compiler.LoadAndParseBinaryReport(selectedPath);
        if (reports == null || reports.Count == 0) return;

        float maxFrameTime = 0f;
        int totalVisualElementsChecked = 0;
        int absoluteWorstLayoutDepth = 0;

        foreach (var frame in reports)
        {
            if (frame.FrameTimeMs > maxFrameTime) maxFrameTime = frame.FrameTimeMs;
            if (frame.DeepestUIHierarchyDepth > absoluteWorstLayoutDepth) absoluteWorstLayoutDepth = frame.DeepestUIHierarchyDepth;
            totalVisualElementsChecked += frame.ActiveVisualElementsCount;
        }

        float averageElementsPerFrame = (float)totalVisualElementsChecked / reports.Count;

        _analysisResultsLabel.text = $"<b>Report File:</b> {Path.GetFileName(selectedPath)}\n\n" +
                                     $"<b>DIAGNOSTIC PIPELINE REPORT RESULTS:</b>\n" +
                                     $"• Total Recorded Frames Analyzed: {reports.Count}\n" +
                                     $"• Worst Frame Render Latency Spike: <color=#ff4444>{maxFrameTime:F2} ms</color>\n" +
                                     $"• Avg Rendered Elements/Frame: {averageElementsPerFrame:F1}\n" +
                                     $"• Maximum Nesting Tree Depth Registered: <color=#ffaa00>{absoluteWorstLayoutDepth} Layers</color>\n\n" +
                                     $"<b>Verdict:</b> {(maxFrameTime > 33.3f ? "<color=#ff4444>CRITICAL STUTTER: Optimizations Required</color>" : "<color=#44ff44>PASSED ENGINE BUDGET STANDARDS</color>")}";
    }
}