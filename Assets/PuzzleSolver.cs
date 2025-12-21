using UnityEngine;
using Unity.Sentis;
using System.Collections.Generic;

public class PuzzleSolver : MonoBehaviour
{
    [Header("Model")]
    public ModelAsset brainModel;
    private Worker worker;

    private string inputName1 = "grid";
    private string inputName2 = "inventory";
    private List<string> outputNames = new List<string>();

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
        if (brainModel == null) return;
        Model model = ModelLoader.Load(brainModel);
        if (model.inputs.Count >= 2) { inputName1 = model.inputs[0].name; inputName2 = model.inputs[1].name; }
        outputNames.Clear();
        foreach (var outDef in model.outputs) outputNames.Add(outDef.name);
        worker = new Worker(model, BackendType.CPU);
    }

    // --- DÜZELTİLEN ANA FONKSİYON ---
    public int[] TryGenerateSolvableInventory()
    {
        // 100 kereye kadar dene. Mutlaka birinde tutturur.
        for (int attempt = 0; attempt < 100; attempt++)
        {
            int[,] simGrid = new int[4, 6];

            // AI'a sahte ve BOL KEPÇE envanter veriyoruz.
            // Neden? Çünkü kısıtlı verirsek (0-3 arası), AI "Parçam bitti" der ve tahtayı fulleyemez.
            // Bizim amacımız tahtayı fulletmek. Artanları zaten çöpe atacağız.
            int[] simulationInventory = GenerateRichInventory();

            int[] usedPieces = new int[6]; // Sadece kullanılanları sayacağız

            bool gameActive = true;
            int moves = 0;

            while (gameActive && moves < 30)
            {
                float[] scores = RunInference(simGrid, simulationInventory);
                if (scores == null) break;

                // Biraz çeşitlilik katmak için %20 şansla en iyi 2. hamleyi yapabilir (Hep aynı oyunu oynamasın)
                int bestMove = GetBestValidMove(scores, simGrid, simulationInventory, attempt);

                if (bestMove == -1)
                {
                    gameActive = false;
                }
                else
                {
                    int sId = bestMove / 24;
                    int rem = bestMove % 24;
                    int r = rem / 6;
                    int c = rem % 6;

                    PlaceShape(simGrid, sId, r, c);

                    simulationInventory[sId]--;   // Sahte stoktan düş
                    usedPieces[sId]++;            // GERÇEK LİSTEYE EKLE
                    moves++;
                }
            }

            // KONTROL: Tahta tamamen doldu mu?
            int filledCount = 0;
            foreach (int cell in simGrid) if (cell > 0) filledCount++;

            if (filledCount == 24)
            {
                Debug.Log($"✅ Çözüm {attempt + 1}. denemede bulundu!");
                return usedPieces; // BINGO! Sadece kullanılanları dön.
            }
        }

        Debug.LogError("❌ 100 denemede bile tam çözüm bulunamadı!");
        return null; // Hiçbiri tutmadıysa null dön (O zaman 2-3'lü fallback çalışır)
    }

    // AI rahat oynasın diye bol veriyoruz. Oyuncu bunu görmeyecek.
    private int[] GenerateRichInventory()
    {
        int[] inv = new int[6];
        for (int i = 0; i < 6; i++) inv[i] = UnityEngine.Random.Range(0, 3); // Bol bol ver
        return inv;
    }

    // --- (Aşağısı Standart Fonksiyonlar) ---
    private float[] RunInference(int[,] grid, int[] inventory)
    {
        Tensor<float> gridTensor = null;
        Tensor<float> inventoryTensor = null;
        try
        {
            float[] gridData = new float[24];
            for (int r = 0; r < 4; r++) for (int c = 0; c < 6; c++) gridData[r * 6 + c] = grid[r, c] > 0 ? 1.0f : 0.0f;
            float[] invData = new float[6];
            for (int i = 0; i < 6; i++) invData[i] = (float)inventory[i];

            gridTensor = new Tensor<float>(new TensorShape(1, 1, 4, 6), gridData);
            inventoryTensor = new Tensor<float>(new TensorShape(1, 6), invData);

            worker.SetInput(inputName1, gridTensor);
            worker.SetInput(inputName2, inventoryTensor);
            worker.Schedule();

            Tensor<float> output = null;
            int maxElements = 0;
            foreach (string outName in outputNames)
            {
                var tempOutput = worker.PeekOutput(outName) as Tensor<float>;
                if (tempOutput != null && tempOutput.shape.length > maxElements) { maxElements = tempOutput.shape.length; output = tempOutput; }
            }
            if (output == null) return null;
            return output.DownloadToArray();
        }
        catch { return null; }
        finally { if (gridTensor != null) gridTensor.Dispose(); if (inventoryTensor != null) inventoryTensor.Dispose(); }
    }

    private int GetBestValidMove(float[] scores, int[,] grid, int[] inventory, int attemptSeed)
    {
        // Çeşitlilik için bazen en iyi değil, 2. en iyi hamleyi seçebiliriz (Randomness)
        // Ama şimdilik en iyiyi seçsin, önce çözümü bulsun.
        float maxScore = float.NegativeInfinity;
        int bestAction = -1;

        for (int i = 0; i < 144; i++)
        {
            int sId = i / 24; int rem = i % 24; int r = rem / 6; int c = rem % 6;
            if (IsValidPlacement(grid, inventory, sId, r, c))
            {
                // Rastgelelik (Gürültü) ekle ki her denemede farklı oynasın
                // Her denemede skorlara ufak rastgele bir sayı ekliyoruz
                float noise = UnityEngine.Random.Range(0f, 0.05f);

                if ((scores[i] + noise) > maxScore)
                {
                    maxScore = scores[i] + noise;
                    bestAction = i;
                }
            }
        }
        return bestAction;
    }

    private void PlaceShape(int[,] grid, int shapeId, int startRow, int startCol)
    {
        foreach (Vector2Int p in shapes[shapeId]) grid[startRow + p.x, startCol + p.y] = shapeId + 1;
    }

    private bool IsValidPlacement(int[,] grid, int[] inventory, int shapeId, int startRow, int startCol)
    {
        if (inventory[shapeId] <= 0) return false;
        if (startRow + shapeDimensions[shapeId, 0] > 4) return false;
        if (startCol + shapeDimensions[shapeId, 1] > 6) return false;
        foreach (Vector2Int p in shapes[shapeId]) if (grid[startRow + p.x, startCol + p.y] > 0) return false;
        return true;
    }

    void OnDisable() { if (worker != null) worker.Dispose(); }
}