@echo off
title Puzzle AI - GitHub Yukleme Araci
color 0A

echo ==========================================
echo      DAYI GITHUB GUNCELLEME SISTEMI
echo ==========================================
echo.

:: 1. ADIM: Dogru klasore ve diske git (cd /d komutu diski de degistirir)
echo [1/4] Proje klasorune geciliyor...
cd /d D:\AI_Puzzle_Solver

:: Klasor yoksa hata verip durdur
if %errorlevel% neq 0 (
    echo HATA: D:\AI_Puzzle_Solver klasoru bulunamadi!
    echo Lutfen diski kontrol et.
    pause
    exit
)

:: 2. ADIM: Dosyalari ekle
echo [2/4] Degisiklikler ekleniyor (git add)...
git add .

:: 3. ADIM: Commit at (Mesaji senin icin hazir yazdim)
echo [3/4] Commit atiliyor...
git commit -m "Canvas scaling fixed, Game Over panel added, and Android build settings configured"

:: 4. ADIM: Pushla
echo [4/4] GitHub'a gonderiliyor (git push)...
git push origin main

echo.
echo ==========================================
if %errorlevel% equ 0 (
    echo    ISLEM TAMAMDIR DAYI, GONDERILDI!
) else (
    echo    BIR SIKINTI VAR, YUKARIDAKI HATAYI OKU!
)
echo ==========================================
echo Kapatmak icin bir tusa bas...
pause