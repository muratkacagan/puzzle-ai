using UnityEngine;
using UnityEngine.UI; // Text için gerekli
using System.Collections.Generic;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Referanslar")]
    public PuzzleController puzzleController;
    public GridManager gridManager;
    public Camera mainCamera;

    [Header("UI Referanslarý")] // --- EKLENEN KISIM 1 ---
    public Text[] handCountTexts; // O butonlarýn altýndaki sayý textlerini buraya atacaðýz

    [Header("Ayarlar")]
    public float ghostAlpha = 0.5f;

    private GameObject currentGhost;
    private int selectedShapeId = -1;
    private bool isDragging = false;

    // Þekil listesi (Ayný kalacak)
    private readonly List<Vector2Int[]> shapes = new List<Vector2Int[]>
    {
        new Vector2Int[] { new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(2,0) },
        new Vector2Int[] { new Vector2Int(0,0), new Vector2Int(0,1), new Vector2Int(1,0), new Vector2Int(1,1) },
        new Vector2Int[] { new Vector2Int(0,0), new Vector2Int(0,1), new Vector2Int(1,1), new Vector2Int(1,2) },
        new Vector2Int[] { new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(1,1) },
        new Vector2Int[] { new Vector2Int(0,0), new Vector2Int(0,1), new Vector2Int(1,1) },
        new Vector2Int[] { new Vector2Int(0,0) }
    };

    void Update()
    {
        // --- EKLENEN KISIM 2: SÜREKLÝ GÜNCELLEME ---
        // Oyun baþladýðýnda envanter deðiþtiyse UI da anýnda deðiþsin.
        UpdateHandUI();

        if (GameSettings.CurrentMode == GameMode.AI_Manual || GameSettings.CurrentMode == GameMode.AI_Auto)
            return;

        if (isDragging && currentGhost != null)
        {
            HandleDragging();

            if (Input.GetMouseButtonDown(0))
            {
                TryPlaceShape();
            }
            else if (Input.GetMouseButtonDown(1))
            {
                CancelDrag();
            }
        }
    }

    public void SelectShapeToDrag(int shapeId)
    {
        // Envanter kontrolü
        if (puzzleController.currentInventory[shapeId] <= 0)
        {
            Debug.Log("Bu parçadan kalmadý!");
            return;
        }

        selectedShapeId = shapeId;
        isDragging = true;

        if (currentGhost != null) Destroy(currentGhost);
        CreateGhost(shapeId);
    }

    void CreateGhost(int shapeId)
    {
        currentGhost = new GameObject("GhostShape");
        foreach (Vector2Int pos in shapes[shapeId])
        {
            GameObject cell = Instantiate(gridManager.cellPrefab, currentGhost.transform);

            // Pozisyon
            cell.transform.localPosition = new Vector3(pos.y * gridManager.targetSize, -pos.x * gridManager.targetSize, 0);

            // SCALE AYARI (Senin düzelttiðin kýsým)
            SpriteRenderer sr = cell.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                cell.transform.localScale = Vector3.one;
                float spriteSize = sr.sprite.bounds.size.x;
                float newScale = gridManager.targetSize / spriteSize;
                cell.transform.localScale = new Vector3(newScale * 0.95f, newScale * 0.95f, 1f);

                Color c = gridManager.shapeColors[shapeId];
                c.a = ghostAlpha;
                sr.color = c;
                sr.sortingOrder = 10;
            }
        }
    }

    void HandleDragging()
    {
        Vector3 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0;

        float cellSize = gridManager.targetSize;
        Vector3 gridOrigin = gridManager.transform.position;

        int col = Mathf.RoundToInt((mousePos.x - gridOrigin.x) / cellSize);
        int row = Mathf.RoundToInt(-(mousePos.y - gridOrigin.y) / cellSize);

        Vector3 snapPos = new Vector3(
            gridOrigin.x + (col * cellSize),
            gridOrigin.y - (row * cellSize),
            0
        );

        currentGhost.transform.position = snapPos;

        bool isValid = IsValidPlacement(selectedShapeId, row, col);
        SetGhostColor(isValid);
    }

    void TryPlaceShape()
    {
        float cellSize = gridManager.targetSize;
        Vector3 gridOrigin = gridManager.transform.position;
        Vector3 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);

        int col = Mathf.RoundToInt((mousePos.x - gridOrigin.x) / cellSize);
        int row = Mathf.RoundToInt(-(mousePos.y - gridOrigin.y) / cellSize);

        if (IsValidPlacement(selectedShapeId, row, col))
        {
            PlaceShapeLogic(selectedShapeId, row, col);

            // Envanterden düþ
            puzzleController.currentInventory[selectedShapeId]--;

            // --- EKLENEN KISIM 3: UI GÜNCELLE ---
            UpdateHandUI();

            CancelDrag();
        }
        else
        {
            Debug.Log("Geçersiz Hamle!");
        }
    }

    // --- YENÝ FONKSÝYON: UI TEXTLERÝNÝ GÜNCELLE ---
    public void UpdateHandUI()
    {
        if (handCountTexts == null || puzzleController == null) return;

        for (int i = 0; i < 6; i++)
        {
            if (i < handCountTexts.Length && handCountTexts[i] != null)
            {
                // PuzzleController'daki gerçek sayýyý alýp text'e yazýyoruz
                handCountTexts[i].text = puzzleController.currentInventory[i].ToString();
            }
        }
    }

    void CancelDrag()
    {
        isDragging = false;
        selectedShapeId = -1;
        if (currentGhost != null) Destroy(currentGhost);
    }

    void SetGhostColor(bool isValid)
    {
        Color c = isValid ? gridManager.shapeColors[selectedShapeId] : Color.red;
        c.a = ghostAlpha;
        foreach (Transform child in currentGhost.transform)
        {
            child.GetComponent<SpriteRenderer>().color = c;
        }
    }

    bool IsValidPlacement(int shapeId, int startRow, int startCol)
    {
        int[,] grid = GetCurrentGrid();
        foreach (Vector2Int p in shapes[shapeId])
        {
            int r = startRow + p.x;
            int c = startCol + p.y;
            if (r < 0 || r >= 4 || c < 0 || c >= 6) return false;
            if (grid[r, c] > 0) return false;
        }
        return true;
    }

    int[,] GetCurrentGrid()
    {
        System.Reflection.FieldInfo field = typeof(PuzzleController).GetField("currentGrid", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return (int[,])field.GetValue(puzzleController);
    }

    void PlaceShapeLogic(int shapeId, int startRow, int startCol)
    {
        // 1. Görseli ve GridManager'ý güncelle (Eski kodun burasýydý)
        int[,] grid = GetCurrentGrid(); // Reflection ile çekmiþtik
        foreach (Vector2Int p in shapes[shapeId])
        {
            grid[startRow + p.x, startCol + p.y] = shapeId + 1;
        }
        gridManager.UpdateVisuals(grid);

        // 2. YENÝ KISIM: PuzzleController'a "Ben burayý doldurdum" de.
        puzzleController.RegisterPlayerMove(shapeId, startRow, startCol);
    }
}