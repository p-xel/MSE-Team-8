using UnityEngine.UIElements;

[UxmlElement]
public partial class TableElement : VisualElement
{
    private readonly VisualElement header;
    private readonly ScrollView body;
    private float[] weights;
    private int rowCount;

    public TableElement()
    {
        AddToClassList("table");

        header = new VisualElement();
        header.AddToClassList("table_header");
        Add(header);

        body = new ScrollView(ScrollViewMode.Vertical);
        body.AddToClassList("table_body");
        body.verticalScrollerVisibility = ScrollerVisibility.Hidden;
        body.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
        Add(body);
    }

    public void SetColumns(string[] headers, float[] columnWeights = null)
    {
        weights = columnWeights;
        header.Clear();
        ClearRows();
        for (int i = 0; i < headers.Length; i++)
            header.Add(MakeCell(headers[i], i, "table_header-cell"));
    }

    public void AddRow(params string[] cells)
    {
        var row = new VisualElement();
        row.AddToClassList("table_row");
        if (rowCount % 2 == 1) row.AddToClassList("table_row--alt");
        rowCount++;
        for (int i = 0; i < cells.Length; i++)
            row.Add(MakeCell(cells[i], i, "table_cell"));
        body.Add(row);
    }

    public void ClearRows()
    {
        body.Clear();
        rowCount = 0;
    }

    private Label MakeCell(string text, int index, string className)
    {
        var cell = new Label(text);
        cell.AddToClassList(className);
        cell.style.flexGrow = weights != null && index < weights.Length ? weights[index] : 1f;
        cell.style.flexBasis = new StyleLength(0f);
        return cell;
    }
}
