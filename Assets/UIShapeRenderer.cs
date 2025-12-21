using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class UIShapeRenderer : MonoBehaviour
{
    public GameObject cellImagePrefab; // Senin o tek karelik 'Cell' prefab'ýn (UI Image olaný)
    public Transform container; // Karelerin dizileceði boþ obje (Genelde bu scriptin olduðu obje)
    public float cellSize = 30f; // Karelerin boyutu
    public Color shapeColor = Color.white; // Þeklin rengi

    // Þekil verilerini buraya da kopyalýyoruz (Merkezi bir yerden de çekilebilir)
    private readonly List<Vector2Int[]> shapes = new List<Vector2Int[]>
    {
        new Vector2Int[] { new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(2,0) }, // 0: Dikey
        new Vector2Int[] { new Vector2Int(0,0), new Vector2Int(0,1), new Vector2Int(1,0), new Vector2Int(1,1) }, // 1: Kare
        new Vector2Int[] { new Vector2Int(0,0), new Vector2Int(0,1), new Vector2Int(1,1), new Vector2Int(1,2) }, // 2: Z
        new Vector2Int[] { new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(1,1) }, // 3: L
        new Vector2Int[] { new Vector2Int(0,0), new Vector2Int(0,1), new Vector2Int(1,1) }, // 4: Ters L
        new Vector2Int[] { new Vector2Int(0,0) } // 5: Tekli
    };

    public int shapeIDToDraw = 0; // Inspector'dan elle gireceðiz: 0, 1, 2...

    void Start()
    {
        // Oyun baþlayýnca otomatik çizsin
        DrawShape(shapeIDToDraw, shapeColor);
    }
    // Bu fonksiyonu dýþarýdan çaðýracaðýz: "Bana 2 numaralý þekli çiz"
    public void DrawShape(int shapeId, Color color)
    {
        // Önce eskileri temizle
        foreach (Transform child in container)
        {
            Destroy(child.gameObject);
        }

        if (shapeId < 0 || shapeId >= shapes.Count) return;

        Vector2Int[] coords = shapes[shapeId];

        // Ortalamak için hesaplama (Opsiyonel ama þýk durur)
        float maxX = 0, maxY = 0;
        foreach (var p in coords)
        {
            if (p.y > maxX) maxX = p.y;
            if (p.x > maxY) maxY = p.x;
        }
        float width = (maxX + 1) * cellSize;
        float height = (maxY + 1) * cellSize;
        Vector2 offset = new Vector2(-width / 2f + cellSize / 2f, height / 2f - cellSize / 2f);

        // Kareleri oluþtur
        foreach (Vector2Int p in coords)
        {
            GameObject cell = Instantiate(cellImagePrefab, container);
            RectTransform rt = cell.GetComponent<RectTransform>();

            // UI Image olduðundan emin olalým
            if (rt == null)
            {
                // Eðer Image deðilse (SpriteRenderer ise) UI'a uyarlamak gerekir.
                // Þimdilik prefab'ýn bir UI Image olduðunu varsayýyoruz.
                rt = cell.AddComponent<RectTransform>();
            }

            rt.sizeDelta = new Vector2(cellSize, cellSize);

            // Koordinat sistemi Unity UI'da Y yukarýdýr, ama matriste Y aþaðýdýr.
            // O yüzden -p.x yapýyoruz.
            rt.anchoredPosition = new Vector2(p.y * cellSize, -p.x * cellSize) + offset;

            Image img = cell.GetComponent<Image>();
            if (img != null) img.color = color;
        }
    }
}