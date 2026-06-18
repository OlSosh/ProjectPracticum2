using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ScoreDisplay : MonoBehaviour
{

    public ScoreManager scoreManager;
    public Slider grillProgressBar;
    public Slider heatProgressBar;
    public Slider timeProgressBar;

    public Image grillFill;
    public Image heatFill;
    public Image timeFill;

    public Gradient heatGradient;
    public Gradient timeGradient;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        grillProgressBar.value = (scoreManager.getNormalizedTotalGrillingScore() + 1.0f) / 2.0f;
        heatProgressBar.value = (scoreManager.totalIterationScore + 1.0f) / 2.0f;
        timeProgressBar.value = scoreManager.GetIterationTimeLeft() / scoreManager.GetIterationTime();

        grillFill.color = heatGradient.Evaluate(grillProgressBar.value);
        heatFill.color = heatGradient.Evaluate(heatProgressBar.value);
        timeFill.color = timeGradient.Evaluate(timeProgressBar.value);

        // currentIterationScoreText.text = "Влияние на счёт итерации: " + scoreManager.currentIterationScore.ToString();
        // totalIterationScoreText.text = "Текущий счёт итерации: " + scoreManager.totalIterationScore.ToString();
        // totalGrillingScoreText.text = "Средний счёт итераций: " + scoreManager.totalGrillingScore.ToString();
        // donenessText.text = "Степень прожарки: " + scoreManager.GetDoneness();
        // timeLeftText.text = "Время до переворота: " + scoreManager.GetIterationTimeLeft();
    }
}
