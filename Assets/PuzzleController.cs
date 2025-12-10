using System.Collections;
using System.Collections.Generic;
using Unity.Sentis;
using UnityEngine;
using UnityEngine.UI;

public class PuzzleController : MonoBehaviour
{
    [Header("Yapay Zeka Beyni (ONNX)")]
    public ModelAsset brainModel;
    private Worker worker;

    // Eski Tip Tensor (Senin istediğin)
    private Tensor<float> gridTensor;
    private Tensor<float> inventoryTensor;

    // Model giriş/çıkış isimleri
    private string inputName1 = "grid";
    private string inputName2 = "inventory";
    private List<string> outputNames = new List<string>();

    [Header("Bağlantılar")]
    public GridManager gridManager;
    public Text[] countTexts;

    [Header("Ayarlar")]
    public float moveDelay = 0.3f;

    private int[,] currentGrid = new int[4, 6];
    public int[] currentInventory = new int[6];

    private readonly List<Vector2Int[]> shapes = new List<Vector2Int[]>
    {
        new Vector2Int[] { new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(2,0) },
        new Vector2Int[] { new Vector2Int(0,0), new Vector2Int(0,1), new Vector2Int(1,0), new Vector2Int(1,1) },
        new Vector2Int[] { new Vector2Int(0,0), new Vector2Int(0,1), new Vector2Int(1,1), new Vector2Int(1,2) },
        new Vector2Int[] { new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(1,1) },
        new Vector2Int[] { new Vector2Int(0,0), new Vector2Int(0,1), new Vector2Int(1,1) },
        new Vector2Int[] { new Vector2Int(0,0) }
    };

    private readonly int[,] shapeDimensions = new int[,] { { 3, 1 }, { 2, 2 }, { 2, 3 }, { 2, 2 }, { 2, 2 }, { 1, 1 } };

    void Start()
    {
        if (brainModel == null) { Debug.LogError("🚨 Brain Model BOŞ!"); return; }

        Model model = ModelLoader.Load(brainModel);

        // Giriş isimlerini al
        if (model.inputs.Count >= 2)
        {
            inputName1 = model.inputs[0].name;
            inputName2 = model.inputs[1].name;
        }

        // Çıkış isimlerini listeye at
        outputNames.Clear();
        foreach (var outDef in model.outputs)
        {
            outputNames.Add(outDef.name);
        }

        // CPU Moduna aldım (En garantisi budur, GPU veri okurken hata verebiliyor)
        worker = new Worker(model, BackendType.CPU);

        UpdateUI();
        Debug.Log($"Sistem Hazır. Çıkışlar taranacak: {string.Join(", ", outputNames)}");
    }

    public void IncreaseItem(int shapeId) { if (shapeId < 6) { currentInventory[shapeId]++; UpdateUI(); } }
    public void DecreaseItem(int shapeId) { if (shapeId < 6 && currentInventory[shapeId] > 0) { currentInventory[shapeId]--; UpdateUI(); } }

    void UpdateUI()
    {
        if (countTexts == null) return;
        for (int i = 0; i < countTexts.Length; i++)
            if (countTexts[i] != null) countTexts[i].text = currentInventory[i].ToString();
    }

    public void StartGame()
    {
        bool isEmpty = true;
        foreach (int c in currentInventory) if (c > 0) isEmpty = false;
        if (isEmpty) { Debug.LogError("⚠️ Envanter boş!"); return; }

        currentGrid = new int[4, 6];
        if (gridManager != null) gridManager.UpdateVisuals(currentGrid);

        StopAllCoroutines();
        StartCoroutine(GameLoop());
    }

    IEnumerator GameLoop()
    {
        bool gameOver = false;
        while (!gameOver)
        {
            // 1. Action Masking
            List<int> validMoves = new List<int>();
            for (int i = 0; i < 144; i++)
            {
                int sId = i / 24; int rem = i % 24; int r = rem / 6; int c = rem % 6;
                if (IsValidPlacement(sId, r, c)) validMoves.Add(i);
            }

            if (validMoves.Count == 0) { Debug.Log("🏁 Oyun Bitti (Hamle yok)"); gameOver = true; yield break; }

            // 2. AI Tahmini
            int bestAction = -1;
            float[] aiScores = RunAIModel();

            // 3. Karar Verme
            if (aiScores != null)
            {
                // Boyut kontrolü: En az 144 eleman olmalı
                if (aiScores.Length < 144)
                {
                    Debug.LogError($"🚨 HATA: Alınan çıktı boyutu {aiScores.Length}. 144 bekleniyordu!");
                }
                else
                {
                    float maxScore = float.NegativeInfinity;
                    foreach (int moveIndex in validMoves)
                    {
                        float score = aiScores[moveIndex];
                        if (!float.IsNaN(score) && score > maxScore)
                        {
                            maxScore = score;
                            bestAction = moveIndex;
                        }
                    }
                }
            }

            // 4. Fallback
            if (bestAction == -1)
            {
                Debug.LogWarning("⚠️ AI kararsız, rastgele geçerli hamle.");
                bestAction = validMoves[UnityEngine.Random.Range(0, validMoves.Count)];
            }

            // 5. Oyna
            int shapeId = bestAction / 24;
            int remaining = bestAction % 24;
            int row = remaining / 6;
            int col = remaining % 6;

            Debug.Log($"Oynanan Hamle: Şekil {shapeId} -> ({row}, {col})");
            PlaceShape(shapeId, row, col);

            if (currentInventory[shapeId] > 0) { currentInventory[shapeId]--; UpdateUI(); }

            yield return new WaitForSeconds(moveDelay);
        }
    }

    float[] RunAIModel()
    {
        try
        {
            float[] gridData = new float[24];
            for (int r = 0; r < 4; r++)
                for (int c = 0; c < 6; c++)
                    gridData[r * 6 + c] = (float)currentGrid[r, c];

            float[] invData = new float[6];
            for (int i = 0; i < 6; i++)
                invData[i] = (float)currentInventory[i];

            gridTensor = new Tensor<float>(new TensorShape(1, 1, 4, 6), gridData);
            inventoryTensor = new Tensor<float>(new TensorShape(1, 6), invData);

            worker.SetInput(inputName1, gridTensor);
            worker.SetInput(inputName2, inventoryTensor);
            worker.Schedule();

            // --- KESİN ÇÖZÜM BURASI: EN BÜYÜK ÇIKTIYI BUL ---
            Tensor<float> output = null;
            int maxElements = 0;

            // Tüm çıkışları gez, eleman sayısı en fazla olanı (Action) al.
            foreach (string outName in outputNames)
            {
                var tempOutput = worker.PeekOutput(outName) as Tensor<float>;
                if (tempOutput != null)
                {
                    // TensorShape'in içindeki 'length' (toplam eleman sayısı) özelliğini okuyoruz.
                    // Not: Eski sürümlerde tensor.length, yenilerde tensor.shape.length kullanılır.
                    // En garantisi tensor.shape.length'dir.
                    int currentLen = tempOutput.shape.length;

                    Debug.Log($"Çıktı Bulundu: {outName}, Boyut: {currentLen}");

                    if (currentLen > maxElements)
                    {
                        maxElements = currentLen;
                        output = tempOutput;
                    }
                }
            }

            if (output == null)
            {
                Debug.LogError("🚨 HATA: Hiçbir çıktı yakalanamadı!");
                gridTensor.Dispose(); inventoryTensor.Dispose();
                return null;
            }

            float[] results = output.DownloadToArray();

            gridTensor.Dispose(); inventoryTensor.Dispose();
            return results;
        }
        catch (System.Exception e)
        {
            Debug.LogError("Model Hatası: " + e.Message);
            return null;
        }
    }

    bool IsValidPlacement(int shapeId, int startRow, int startCol)
    {
        if (currentInventory[shapeId] <= 0) return false;
        int h = shapeDimensions[shapeId, 0];
        int w = shapeDimensions[shapeId, 1];
        if (startRow + h > 4) return false;
        if (startCol + w > 6) return false;
        Vector2Int[] coords = shapes[shapeId];
        foreach (Vector2Int p in coords)
        {
            int r = startRow + p.x; int c = startCol + p.y;
            if (currentGrid[r, c] == 1) return false;
        }
        return true;
    }

    void PlaceShape(int shapeId, int startRow, int startCol)
    {
        Vector2Int[] coords = shapes[shapeId];
        foreach (Vector2Int p in coords)
        {
            int r = startRow + p.x; int c = startCol + p.y;
            currentGrid[r, c] = 1;
        }
        if (gridManager != null) gridManager.UpdateVisuals(currentGrid);
    }

    void OnDisable() { if (worker != null) worker.Dispose(); }
}