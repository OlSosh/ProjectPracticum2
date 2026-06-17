using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class ShashlikGameManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ShashlikAudioSpectrogram spectrogram;
    [SerializeField] private KnobManager knobManager;
    [SerializeField] private KebabManager kebabManager;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI donenessLevelText;
    [SerializeField] private Slider scoreProgressBar;
    [SerializeField] private GameObject winPanel;
    [SerializeField] private GameObject losePanel;
    
    [Header("Flip Button")]
    [SerializeField] private Button flipButton;
    [SerializeField] private float flipCooldown = 0.8f;
    
    [Header("Scoring")]
    [SerializeField] private int successFlipReward = 10;
    [SerializeField] private int penaltyFlipPenalty = -5;
    [SerializeField] private int[] stageThresholds = new int[] { 0, 30, 60, 90, 120 };
    [SerializeField] private int maxScoreForBurn = 150;
    
    private int currentScore = 0;
    private int currentDonenessStage = 0;
    private bool canFlip = true;
    private bool gameOver = false;
    private bool isWin = false;
    
    private void Start()
    {
        if (spectrogram == null) spectrogram = FindObjectOfType<ShashlikAudioSpectrogram>();
        if (knobManager == null) knobManager = FindObjectOfType<KnobManager>();
        if (kebabManager == null) kebabManager = FindObjectOfType<KebabManager>();
        
        if (flipButton != null)
            flipButton.onClick.AddListener(OnFlipButtonPressed);
        
        // ===== ИСПРАВЛЕНО: проверка на null перед подпиской =====
        if (spectrogram != null)
            spectrogram.onRedZoneChanged += OnRedZoneChanged;
        
        //ResetGame();
    }
    
    private void OnDestroy()
    {
        if (spectrogram != null)
            spectrogram.onRedZoneChanged -= OnRedZoneChanged;
        if (flipButton != null)
            flipButton.onClick.RemoveListener(OnFlipButtonPressed);
    }
    
    private void ResetGame()
    {
        currentScore = 0;
        currentDonenessStage = 0;
        gameOver = false;
        isWin = false;
        canFlip = true;
        
        UpdateUI();
        ApplyDonenessStage();
        
        if (winPanel != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);
        
        // ===== ДОБАВЛЕНО: сброс состояния кнопки =====
        if (flipButton != null) flipButton.interactable = true;
    }
    
    private void OnRedZoneChanged(bool isRed)
    {
        Debug.Log($"Красная зона: {(isRed ? "АКТИВНА" : "не активна")}");
        // Можно добавить визуальный фидбек здесь
    }
    
    private void OnFlipButtonPressed()
    {
        if (!canFlip || gameOver) return;
        
        // ===== ИСПРАВЛЕНО: проверка на null =====
        bool isRed = spectrogram != null && spectrogram.IsInRedZone;
        
        int deltaScore = isRed ? successFlipReward : penaltyFlipPenalty;
        
        if (deltaScore > 0)
        {
            currentScore += deltaScore;
            Debug.Log($"Успешный поворот! +{deltaScore} очков. Всего: {currentScore}");
            FlipKebab(true);
        }
        else if (deltaScore < 0)
        {
            currentScore += deltaScore;
            Debug.Log($"Штрафной поворот! {deltaScore} очков. Всего: {currentScore}");
            FlipKebab(false);
        }
        
        CheckScoreBoundaries();
        UpdateDonenessStageByScore();
        UpdateUI();
        
        StartCoroutine(FlipCooldownRoutine());
    }
    
    private void FlipKebab(bool success)
    {
        if (kebabManager != null)
            kebabManager.FlipAllKebabs();
        
        // Можно добавить звук успеха/неудачи
    }
    
    private void CheckScoreBoundaries()
    {
        if (currentScore >= maxScoreForBurn && !isWin && !gameOver)
        {
            GameOver(false);
        }
        else if (currentDonenessStage == 4 && currentScore >= stageThresholds[4] && !gameOver)
        {
            Win();
        }
    }
    
    private void UpdateDonenessStageByScore()
    {
        int newStage = 0;
        for (int i = stageThresholds.Length - 1; i >= 0; i--)
        {
            if (currentScore >= stageThresholds[i])
            {
                newStage = i;
                break;
            }
        }
        
        if (newStage != currentDonenessStage)
        {
            currentDonenessStage = newStage;
            ApplyDonenessStage();
            Debug.Log($"Уровень прожарки повышен до {currentDonenessStage}");
        }
    }
    
    private void ApplyDonenessStage()
    {
        if (kebabManager != null)
            kebabManager.SetDonenessLevel(currentDonenessStage);
    }
    
    private void Win()
    {
        gameOver = true;
        isWin = true;
        if (winPanel != null) winPanel.SetActive(true);
        if (losePanel != null) losePanel.SetActive(false);
        Debug.Log("ПОБЕДА! Шашлык идеально приготовлен!");
        if (flipButton != null) flipButton.interactable = false;
    }
    
    private void GameOver(bool burned)
    {
        gameOver = true;
        if (losePanel != null) losePanel.SetActive(true);
        if (winPanel != null) winPanel.SetActive(false);
        if (flipButton != null) flipButton.interactable = false;
        Debug.Log(burned ? "ПРОИГРЫШ: Шашлык сгорел!" : "ПРОИГРЫШ: Неудача");
    }
    
    private void UpdateUI()
    {
        if (scoreText != null)
            scoreText.text = $"Очки: {currentScore}";
        
        if (donenessLevelText != null)
        {
            string stageName = GetStageName(currentDonenessStage);
            donenessLevelText.text = $"Прожарка: {stageName}";
        }
        
        if (scoreProgressBar != null)
        {
            float progress = (float)currentScore / maxScoreForBurn;
            scoreProgressBar.value = Mathf.Clamp01(progress);
        }
    }
    
    private string GetStageName(int stage)
    {
        switch (stage)
        {
            case 0: return "Сырое 🥩";
            case 1: return "Недожар 🔥";
            case 2: return "Среднее 🍖";
            case 3: return "Хорошо 🍗";
            case 4: return "Готово! 🎉";
            default: return "?";
        }
    }
    
    private IEnumerator FlipCooldownRoutine()
    {
        canFlip = false;
        if (flipButton != null) flipButton.interactable = false;
        yield return new WaitForSeconds(flipCooldown);
        canFlip = true;
        if (flipButton != null && !gameOver) flipButton.interactable = true;
    }
    
    public void RestartGame()
    {
        ResetGame();
    }
}