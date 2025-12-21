public enum GameMode
{
    Player_Manual, // Oyuncu Oynar - Envanteri Biz Seçeriz
    Player_Auto,   // Oyuncu Oynar - AI Envanter Üretir
    AI_Manual,     // AI Oynar - Envanteri Biz Seçeriz
    AI_Auto        // AI Oynar - AI Envanter Üretir
}

public static class GameSettings
{
    // Varsayýlan mod
    public static GameMode CurrentMode = GameMode.Player_Manual;

    // Setup ekranýnda seçilen envanter buraya kaydedilecek
    public static int[] SelectedInventory = new int[6];
}