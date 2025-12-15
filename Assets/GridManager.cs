using UnityEngine;

public class GridManager : MonoBehaviour
{
    [Header("Ayarlar")]
    public int width = 6;
    public int height = 4;
    public GameObject cellPrefab;
    public Transform cam;

    [Header("Görsel Ayar")]
    [Range(0.1f, 2f)] public float targetSize = 0.9f; // Blok boyutu
    public Color emptyColor = Color.white;

    // YENÝ: Renk Listesi
    [Header("Þekil Renkleri (Sýrasýyla 0-5)")]
    public Color[] shapeColors;

    private GameObject[,] cellObjects;

    void Start()
    {
        GenerateGrid();
    }

    void GenerateGrid()
    {
        if (cellObjects != null)
        {
            foreach (GameObject cell in cellObjects) if (cell != null) Destroy(cell);
        }
        else
        {
            foreach (Transform child in transform) Destroy(child.gameObject);
        }

        cellObjects = new GameObject[height, width];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                GameObject newCell = Instantiate(cellPrefab, transform);

                // Pozisyon ve Boyut Ayarý
                newCell.transform.localPosition = new Vector3(x * targetSize, -y * targetSize, 0);

                SpriteRenderer sr = newCell.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    newCell.transform.localScale = Vector3.one;
                    float spriteWidth = sr.bounds.size.x;
                    float spriteHeight = sr.bounds.size.y;
                    float newScaleX = targetSize / spriteWidth;
                    float newScaleY = targetSize / spriteHeight;
                    // %95 doluluk oranýyla sýðdýr
                    newCell.transform.localScale = new Vector3(newScaleX * 0.95f, newScaleY * 0.95f, 1f);

                    sr.color = emptyColor;
                }
                cellObjects[y, x] = newCell;
            }
        }

    //    if (cam != null)
    //        cam.transform.position = new Vector3(
    //            -(width * targetSize) / 2f + targetSize / 2f,
    //            (height * targetSize) / 2f - targetSize / 2f,
    //            -10);
    }

    public void UpdateVisuals(int[,] gridData)
    {
        if (cellObjects == null) return;

        for (int r = 0; r < height; r++)
        {
            for (int c = 0; c < width; c++)
            {
                // Görsel hizalama (Unity Y ekseni ters)
                int visualY = r;

                GameObject cellObj = cellObjects[visualY, c];
                if (cellObj == null) continue;

                SpriteRenderer sr = cellObj.GetComponent<SpriteRenderer>();
                if (sr == null) continue;

                int cellValue = gridData[r, c];

                if (cellValue == 0)
                {
                    sr.color = emptyColor;
                }
                else
                {
                    // Deðer 1 ise index 0 (Shape 0) rengini almalý
                    int colorIndex = cellValue - 1;
                    if (shapeColors != null && colorIndex >= 0 && colorIndex < shapeColors.Length)
                        sr.color = shapeColors[colorIndex];
                    else
                        sr.color = Color.gray;
                }
            }
        }
    }
}