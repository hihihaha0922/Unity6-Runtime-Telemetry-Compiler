using UnityEngine;
using UnityEngine.UIElements;

public class UiStressTestSimulator : MonoBehaviour
{
    private UIDocument _uiDocument;

    void Start()
    {
        _uiDocument = GetComponent<UIDocument>();
        if (_uiDocument == null) _uiDocument = FindFirstObjectByType<UIDocument>();

        if (_uiDocument != null && _uiDocument.rootVisualElement != null)
        {
            SimulateHeavyRpgUiPipeline();
        }
    }

    /// <summary>
    /// Intentionally creates a highly unoptimized, deeply nested UI hierarchy 
    /// to trigger architectural budget alerts in our analyzer suite.
    /// </summary>
    private void SimulateHeavyRpgUiPipeline()
    {
        VisualElement root = _uiDocument.rootVisualElement;

        // Let's build a deep hierarchical stack (15 layers deep!)
        VisualElement currentParent = root;
        for (int i = 0; i < 15; i++)
        {
            VisualElement nestedContainer = new VisualElement();
            nestedContainer.name = $"Nested_Layer_{i}";

            // Apply arbitrary complex styling to force expensive engine layout engine math
            nestedContainer.style.paddingLeft = 5;
            nestedContainer.style.borderLeftWidth = 2;
            nestedContainer.style.borderLeftColor = Color.red;

            currentParent.Add(nestedContainer);
            currentParent = nestedContainer; // Shift the focus deeper
        }

        // At the absolute bottom leaf node, spawn 200 heavy item slots
        for (int j = 0; j < 200; j++)
        {
            Button itemSlot = new Button();
            itemSlot.text = $"[Item {j}]";
            itemSlot.style.width = 60;
            itemSlot.style.height = 60;
            itemSlot.style.marginTop = 2;

            currentParent.Add(itemSlot);
        }

        Debug.LogWarning("[Studio Sandbox] Heavy layout simulation generated! 15 Layers deep, 200 elements injected.");
    }
}