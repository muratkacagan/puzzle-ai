using UnityEngine;

public class GridManager : MonoBehaviour
{
    [Header("Ayarlar")]
    public int width = 6;
    public int height = 4;
    public GameObject cellPrefab;
    public Transform cam;

    [Header("Görsel Ayar")]
    [Range(0.1f, 1f)]
    public float targetSize = 0.9f;

    public Color emptyColor = Color.white;
    public Color filledColor = new Color(1f, 0.3f, 0.3f); // Tatlý bir kýrmýzý

    private SpriteRenderer[,] cellRenderers;

    void Start()
    {
        GenerateGrid();
    }

    void GenerateGrid()
    {
        foreach (Transform child in transform) Destroy(child.gameObject);

        cellRenderers = new SpriteRenderer[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                GameObject newCell = Instantiate(cellPrefab);

                // --- KOORDÝNAT SÝSTEMÝ DÜZELTMESÝ ---
                // Unity'de Y yukarý artar, Matriste aþaðý artar.
                // Görsel olarak (x, y) diziyoruz, ama mantýksal eþleþtirmeyi UpdateVisuals'da yapacaðýz.
                newCell.transform.position = new Vector2(x, y);
                newCell.transform.parent = this.transform;
                newCell.name = $"Cell {x},{y}";

                SpriteRenderer sr = newCell.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    newCell.transform.localScale = Vector3.one;
                    float spriteWidth = sr.bounds.size.x;
                    float spriteHeight = sr.bounds.size.y;
                    newCell.transform.localScale = new Vector3(targetSize / spriteWidth, targetSize / spriteHeight, 1f);

                    cellRenderers[x, y] = sr;
                    sr.color = emptyColor;
                }
            }
        }

        if (cam != null)
            cam.transform.position = new Vector3((float)width / 2 - 0.5f, (float)height / 2 - 0.5f, -10);
    }

    // --- KRÝTÝK GÜNCELLEME BURADA ---
    public void UpdateVisuals(int[,] gridData)
    {
        if (cellRenderers == null) return;

        for (int r = 0; r < height; r++) // r: Satýr (Logic Row 0..3)
        {
            for (int c = 0; c < width; c++) // c: Sütun (Logic Col 0..5)
            {
                // MANTIK: Python'daki 0. satýr (En Üst), Unity'deki 3. satýrdýr (En Üst).
                // Formül: UnityY = (ToplamYükseklik - 1) - MantýksalSatýr
                int visualY = (height - 1) - r;

                int value = gridData[r, c]; // Mantýksal veriyi oku

                if (cellRenderers[c, visualY] != null)
                {
                    cellRenderers[c, visualY].color = (value == 1) ? filledColor : emptyColor;
                }
            }
        }
    }
}