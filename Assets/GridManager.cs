using UnityEngine;

public class GridManager : MonoBehaviour
{
    public GameObject cellPrefab; // Senin beyaz kare prefabýn
    public float targetSize = 1f; // Hücre boyutu
    public Color[] shapeColors; // Þekil renkleri dizisi (Inspector'dan atanmýþ olmalý)

    // Eskiden Start idi, þimdi InitializeGrid yaptýk
    public void InitializeGrid()
    {
        // Önce sahnede kalan eski gridleri temizle
        ClearGridVisuals();

        // 4 satýr, 6 sütunluk gridi oluþtur (Senin asýl kodun burasý)
        for (int r = 0; r < 4; r++)
        {
            for (int c = 0; c < 6; c++)
            {
                // Hücreyi oluþtur ve GridManager'ýn çocuðu yap
                GameObject cell = Instantiate(cellPrefab, transform);

                // Pozisyonu ayarla (Hücreleri yan yana ve alt alta dizer)
                cell.transform.localPosition = new Vector3(c * targetSize, -r * targetSize, 0);

                // Ýsmini düzenle (Hierarchy'de rahat bulmak için)
                cell.name = $"Cell_{r}_{c}";

                // Hücrenin baþlangýç rengini beyaz yap (Boþ hücre)
                SpriteRenderer sr = cell.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.color = Color.white;
                    // Scale ayarýný yap (Prefabýn boyutuna göre targetSize'a uydur)
                    float spriteSize = sr.sprite.bounds.size.x;
                    float newScale = targetSize / spriteSize;
                    cell.transform.localScale = new Vector3(newScale * 0.95f, newScale * 0.95f, 1f);
                }
            }
        }
    }

    public void ClearGridVisuals()
    {
        // GridManager'ýn altýndaki tüm hücre objelerini kökten siler
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
    }

    // PuzzleController bu fonksiyonu çaðýrarak renkleri günceller
    public void UpdateVisuals(int[,] grid)
    {
        // Sahnede oluþturduðumuz hücreleri tek tek kontrol et
        for (int r = 0; r < 4; r++)
        {
            for (int c = 0; c < 6; c++)
            {
                Transform cellTransform = transform.Find($"Cell_{r}_{c}");
                if (cellTransform != null)
                {
                    SpriteRenderer sr = cellTransform.GetComponent<SpriteRenderer>();
                    int shapeIdPlusOne = grid[r, c];

                    if (shapeIdPlusOne == 0)
                        sr.color = Color.white; // Boþsa beyaz
                    else
                        sr.color = shapeColors[shapeIdPlusOne - 1]; // Doluysa þekil rengi
                }
            }
        }
    }
}