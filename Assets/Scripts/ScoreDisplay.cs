using UnityEngine;
using TMPro;

public class ScoreDisplay : MonoBehaviour
{

    public ScoreManager scoreManager;
    public TextMeshProUGUI currentIterationScoreText;
    public TextMeshProUGUI totalIterationScoreText;
    public TextMeshProUGUI totalGrillingScoreText;
    public TextMeshProUGUI timeLeftText;
    public TextMeshProUGUI donenessText;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        currentIterationScoreText.text = "Влияние на счёт итерации: " + scoreManager.currentIterationScore.ToString();
        totalIterationScoreText.text = "Текущий счёт итерации: " + scoreManager.totalIterationScore.ToString();
        totalGrillingScoreText.text = "Средний счёт итераций: " + scoreManager.totalGrillingScore.ToString();
        donenessText.text = "Степень прожарки: " + scoreManager.GetDoneness();
        timeLeftText.text = "Время до переворота: " + scoreManager.GetIterationTimeLeft();
    }
}
