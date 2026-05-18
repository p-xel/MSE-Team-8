using UnityEngine;
using UnityEngine.UIElements;

[UxmlElement]
public partial class LifeBarElement : VisualElement
{
    private const int ChunkCount = 3;
    private readonly VisualElement[] chunks = new VisualElement[ChunkCount];

    public LifeBarElement()
    {
        AddToClassList("life-bar");
        pickingMode = PickingMode.Ignore;

        for (int i = 0; i < ChunkCount; i++)
        {
            chunks[i] = new VisualElement();
            chunks[i].AddToClassList("life-bar_chunk");
            chunks[i].pickingMode = PickingMode.Ignore;
            Add(chunks[i]);
        }
    }

    public void SetValue(float normalized)
    {
        int filled = Mathf.RoundToInt(Mathf.Clamp01(normalized) * ChunkCount);
        for (int i = 0; i < ChunkCount; i++)
        {
            bool active = i < filled;
            chunks[i].RemoveFromClassList("life-bar_chunk--filled");
            chunks[i].RemoveFromClassList("life-bar_chunk--empty");
            chunks[i].AddToClassList(active ? "life-bar_chunk--filled" : "life-bar_chunk--empty");
        }
    }
}
