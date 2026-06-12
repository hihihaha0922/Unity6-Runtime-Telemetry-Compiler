using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class UiPerformanceHudOverlay : MonoBehaviour
{
    private UIDocument _uiDocument;
    private VisualElement _hudContainer;
    private ScrollView _hudDirectoryView;
    private Label _hudMetricsLabel;

    void OnEnable()
    {
        _uiDocument = GetComponent<UIDocument>();

        // Wait a frame to ensure the main game UI has stabilized and initialized
        Invoke(nameof(InitializeHudOverlay), 0.1f);
    }

    private void InitializeHudOverlay()
    {
        if (_uiDocument == null || _uiDocument.rootVisualElement == null) return;

        VisualElement root = _uiDocument.rootVisualElement;

        // 1. Create a floating HUD Panel container box
        _hudContainer = new VisualElement();
        _hudContainer.name = "Studio_Performance_HUD";

        // Style it to float elegantly in the top-right corner of the game screen
        _hudContainer.style.position = Position.Absolute;
        _hudContainer.style.right = new StyleLength(new Length(20, LengthUnit.Pixel));
        _hudContainer.style.top = new StyleLength(new Length(20, LengthUnit.Pixel));
        _hudContainer.style.width = new StyleLength(new Length(320, LengthUnit.Pixel));
        _hudContainer.style.height = new StyleLength(new Length(450, LengthUnit.Pixel));
        _hudContainer.style.backgroundColor = new StyleColor(new Color(0.08f, 0.08f, 0.08f, 0.85f)); // Translucent dark slate
        _hudContainer.style.borderTopWidth = _hudContainer.style.borderBottomWidth = _hudContainer.style.borderLeftWidth = _hudContainer.style.borderRightWidth = new StyleFloat(2f);
        _hudContainer.style.borderTopColor = _hudContainer.style.borderBottomColor = _hudContainer.style.borderLeftColor = _hudContainer.style.borderRightColor = new StyleColor(new Color(0.3f, 0.3f, 0.3f));
        _hudContainer.style.paddingTop = _hudContainer.style.paddingBottom = _hudContainer.style.paddingLeft = _hudContainer.style.paddingRight = new StyleLength(new Length(10, LengthUnit.Pixel));

        // 2. Add Header Label
        Label title = new Label("SYSTEMS HUD DIAGNOSTICS");
        title.style.unityFontStyleAndWeight = FontStyle.Bold;
        title.style.fontSize = 11;
        title.style.color = new StyleColor(Color.white);
        _hudContainer.Add(title);

        // 3. Add Live Numerical Stats Label
        _hudMetricsLabel = new Label("Calculating...");
        _hudMetricsLabel.style.fontSize = 10;
        _hudMetricsLabel.style.color = new StyleColor(Color.gray);
        _hudMetricsLabel.style.marginTop = 5;
        _hudMetricsLabel.style.marginBottom = 10;
        _hudContainer.Add(_hudMetricsLabel);

        // 4. Add the scrolling Directory window box
        _hudDirectoryView = new ScrollView();
        _hudDirectoryView.style.flexGrow = 1;
        _hudDirectoryView.style.backgroundColor = new StyleColor(new Color(0.04f, 0.04f, 0.04f, 0.9f));
        _hudDirectoryView.style.paddingTop = _hudDirectoryView.style.paddingBottom = _hudDirectoryView.style.paddingLeft = _hudDirectoryView.style.paddingRight = new StyleLength(new Length(6, LengthUnit.Pixel));
        _hudContainer.Add(_hudDirectoryView);

        // Inject our complete HUD element straight into the running Game View canvas layer
        root.Add(_hudContainer);
    }

    void Update()
    {
        if (_uiDocument == null || _uiDocument.rootVisualElement == null || _hudDirectoryView == null) return;

        // Track standard live framework metrics
        float currentFrameMs = Time.unscaledDeltaTime * 1000f;
        float allocatedRamMb = System.GC.GetTotalMemory(false) / 1024f / 1024f;

        _hudMetricsLabel.text = $"• Frame Latency: {currentFrameMs:F1} ms\n" +
                                $"• System Heap RAM: {allocatedRamMb:F2} MB";

        // Rebuild the directory overview tree list inside the screen window layout
        _hudDirectoryView.Clear();

        // Start recursive mapping pass, skipping our own HUD box so we don't profile ourselves!
        foreach (var child in _uiDocument.rootVisualElement.hierarchy.Children())
        {
            if (child.name == "Studio_Performance_HUD") continue;
            CrawlAndRenderHudTree(child, 0);
        }
    }

    private void CrawlAndRenderHudTree(VisualElement element, int currentDepth)
    {
        string nodeName = string.IsNullOrEmpty(element.name) ? $"[{element.GetType().Name}]" : element.name;
        if (nodeName.Length > 24) nodeName = nodeName.Substring(0, 21) + "...";

        // Create the indented text row layout
        Label row = new Label($"{new string(' ', currentDepth * 3)}📁 {nodeName}");

        // Thermal Budget Map colors matched explicitly to depth allocation limits
        if (currentDepth < 5)
            row.style.color = new StyleColor(new Color(0.4f, 0.9f, 0.4f)); // Green
        else if (currentDepth >= 5 && currentDepth < 12)
            row.style.color = new StyleColor(new Color(1f, 0.66f, 0f));    // Orange
        else
            row.style.color = new StyleColor(new Color(1f, 0.25f, 0.25f)); // Red

        row.style.fontSize = 10;
        _hudDirectoryView.Add(row);

        foreach (var child in element.hierarchy.Children())
        {
            CrawlAndRenderHudTree(child, currentDepth + 1);
        }
    }
}