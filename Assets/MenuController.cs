using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic; // List için gerekli
using TMPro; // TextMeshPro kullanýyorsan bunu aç

public class MenuController : MonoBehaviour
{
    [Header("Referanslar")]
    public PuzzleSolver puzzleSolver; // GameManager üzerindeki scripti baðlayacaðýz

    [Header("Paneller")]
    public GameObject MainMenuPanel;
    public GameObject SetupPanel;
    public GameObject GamePanel;

    [Header("Setup Ekraný Referanslarý")]
    // 6 tane slotun içindeki sayý yazýlarý (Sýrasýyla 0-5)
    public Text[] countTexts;
    // Veya TextMeshPro kullanýyorsan: public TMP_Text[] countTexts;

    private int[] tempInventory = new int[6]; // O an ekranda seçilen sayýlar

    void Start()
    {
        ShowMainMenu();
    }

    // --- PANEL YÖNETÝMÝ ---
    public void ShowMainMenu()
    {
        MainMenuPanel.SetActive(true);
        SetupPanel.SetActive(false);
        GamePanel.SetActive(false);
    }

    public void ShowSetupPanel()
    {
        MainMenuPanel.SetActive(false);
        SetupPanel.SetActive(true);
        GamePanel.SetActive(false);

        // Setup açýlýnca envanteri sýfýrla
        tempInventory = new int[6];
        UpdateSetupUI();
    }

    public void ShowGamePanel()
    {
        MainMenuPanel.SetActive(false);
        SetupPanel.SetActive(false);
        GamePanel.SetActive(true);
    }

    // --- ANA MENÜ BUTONLARI ---
    // Bu fonksiyonlarý Inspector'dan butonlara baðlayacaðýz
    public void OnBtn_PlayerManual()
    {
        GameSettings.CurrentMode = GameMode.Player_Manual;
        ShowSetupPanel(); // Manuel olduðu için seçim ekranýna git
    }

    public void OnBtn_PlayerAuto()
    {
        GameSettings.CurrentMode = GameMode.Player_Auto;
        // Otomatik olduðu için direkt oyuna baþla (Setup'ý atla)
        StartGameDirectly();
    }

    public void OnBtn_AIManual()
    {
        GameSettings.CurrentMode = GameMode.AI_Manual;
        ShowSetupPanel(); // Manuel olduðu için seçim ekranýna git
    }

    public void OnBtn_AIAuto()
    {
        GameSettings.CurrentMode = GameMode.AI_Auto;
        StartGameDirectly();
    }

    // --- SETUP EKRANI BUTONLARI (+ / -) ---
    public void IncreaseItem(int shapeIndex)
    {
        if (shapeIndex >= 0 && shapeIndex < 6)
        {
            tempInventory[shapeIndex]++;
            UpdateSetupUI();
        }
    }

    public void DecreaseItem(int shapeIndex)
    {
        if (shapeIndex >= 0 && shapeIndex < 6 && tempInventory[shapeIndex] > 0)
        {
            tempInventory[shapeIndex]--;
            UpdateSetupUI();
        }
    }

    void UpdateSetupUI()
    {
        for (int i = 0; i < 6; i++)
        {
            if (countTexts[i] != null)
                countTexts[i].text = tempInventory[i].ToString();
        }
    }

    // --- OYUNU BAÞLAT ---
    // Setup panelindeki "BAÞLAT" butonu buna baðlanacak
    public void OnBtn_StartGameFromSetup()
    {
        // Seçilen envanteri kaydet
        System.Array.Copy(tempInventory, GameSettings.SelectedInventory, 6);

        // Oyunu baþlat
        StartGameDirectly();
    }

    async void StartGameDirectly() // async yaptýk çünkü solver biraz bekletebilir
    {
        // Yükleme ekraný gibi bir þey olmadýðý için anlýk donma olabilir, normaldir.

        if (GameSettings.CurrentMode == GameMode.AI_Auto || GameSettings.CurrentMode == GameMode.Player_Auto)
        {
            Debug.Log("Çözülebilir senaryo aranýyor...");

            // PuzzleSolver'dan garanti çözülebilir envanter iste
            int[] solvableInventory = puzzleSolver.TryGenerateSolvableInventory();

            if (solvableInventory != null)
            {
                Debug.Log("Çözülebilir senaryo bulundu!");
                // Bulunan envanteri ayarlara kaydet
                System.Array.Copy(solvableInventory, GameSettings.SelectedInventory, 6);
            }
            else
            {
                Debug.LogError("Uygun senaryo bulunamadý, rastgele veriliyor.");
                // Bulamazsa rastgele salla (Fallback)
                for (int i = 0; i < 6; i++) GameSettings.SelectedInventory[i] = Random.Range(2, 4);
            }
        }
        else
        {
            // Manuel modlarda zaten tempInventory'den SelectedInventory'e kopyalamýþtýk
            // Ekstra bir þey yapmaya gerek yok.
        }

        ShowGamePanel();

        // PuzzleController'ý bul ve baþlat
        GameObject.FindObjectOfType<PuzzleController>().StartGameLogic();
    }

    // --- OYUN ÝÇÝ HEADER BUTONLARI ---

    public void OnBtn_ReturnToMenu()
    {
        PuzzleController pc = GameObject.FindObjectOfType<PuzzleController>();
        if (pc != null)
        {
            pc.StopGame();
            // YENÝ: Grid görsellerini tamamen yok et
            if (pc.gridManager != null) pc.gridManager.ClearGridVisuals();
        }

        ShowMainMenu();
    }

    public void OnBtn_RestartGame()
    {
        PuzzleController pc = GameObject.FindObjectOfType<PuzzleController>();
        if (pc != null)
        {
            pc.StopGame();
            if (pc.gridManager != null) pc.gridManager.ClearGridVisuals(); // Temizle
        }

        // Mevcut restart mantýðýn...
        if (GameSettings.CurrentMode == GameMode.Player_Manual || GameSettings.CurrentMode == GameMode.AI_Manual)
            ShowSetupPanel();
        else
            StartGameDirectly();
    }
    // "BAÞLAT" Butonuna baðlanacak
    public void OnBtn_StartAI()
    {
        PuzzleController pc = GameObject.FindObjectOfType<PuzzleController>();
        if (pc != null) pc.StartAIAutoPlay();
    }

    // "DURDUR" Butonuna baðlanacak (Opsiyonel ama sunumda iyi durur)
    public void OnBtn_StopAI()
    {
        PuzzleController pc = GameObject.FindObjectOfType<PuzzleController>();
        if (pc != null) pc.StopAIAutoPlay();
    }

    public void OnBtn_AIMove()
    {
        PuzzleController pc = GameObject.FindObjectOfType<PuzzleController>();
        if (pc != null)
        {
            pc.MakeSingleAIMove();
        }
    }
}