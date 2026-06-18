using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public RotationButton3D flipButton;
    public RotatableKnob thresholdKnob;
    public RotatableKnob ratioKnob;
    public RotatableKnob attackKnob;
    public RotatableKnob releaseKnob;


    public GameManager gameManager;


    public float desiredThresholdValue = 0.5f;
    public float desiredRatioValue = 0.5f;
    public float desiredAttackValue = 0.5f;
    public float desiredReleaseValue = 0.5f;

    public float grillingScoreMult = 0.1f;

    public float knobDriftSpeed = 5.0f;

    [HideInInspector]
    public float totalGrillingScore = 0.0f;
    [HideInInspector]
    public int iterationsDone = 0;
    [HideInInspector]
    public int iterationAmount = 16;
    [HideInInspector]
    public float totalIterationScore = 0.0f;
    [HideInInspector]
    public float currentIterationScore = 0.0f;

    public KebabManager kebabManager;

    void Start()
	{
		
	}

    float GetKnobNormalizedValue(RotatableKnob knob)
	{
		float current = knob.CurrentValue;
        float max = knob.maxValue;
        float min = knob.minValue;

        float normalized = (current - min) / (max - min);

        return normalized;
	}

    float GetKnobGrillingScore(RotatableKnob knob, float desiredValue)
	{
		float knobValue = GetKnobNormalizedValue(knob);
        float distanceToDesired = desiredValue - knobValue;
        return distanceToDesired;
	}

    float GetCurrentIterationScore()
	{
		float thresholdKnobScore = -GetKnobGrillingScore(thresholdKnob, desiredThresholdValue);
        float ratioKnobScore = GetKnobGrillingScore(ratioKnob, desiredRatioValue);
        float attackKnobScore = -GetKnobGrillingScore(attackKnob, desiredAttackValue);
        float releaseKnobScore = GetKnobGrillingScore(releaseKnob, desiredReleaseValue);

        float currentIterationScore = thresholdKnobScore + ratioKnobScore + attackKnobScore + releaseKnobScore;

        return currentIterationScore;
    }
    public void EndGrillingIteration()
	{
		totalGrillingScore += totalIterationScore;
        iterationsDone += 1;
        totalIterationScore = 0.0f;
        kebabManager.SetDonenessLevel(GetDoneness());
	}

    public float GetIterationTime()
	{
		return gameManager.audioSource.clip.length / iterationAmount;
	}
    public float GetNextIterationTime()
	{
        float iterationTime = GetIterationTime();
        float nextTime = Mathf.Min((iterationsDone + 1) * iterationTime, gameManager.audioSource.clip.length);
		return nextTime;
	}
    public float GetIterationTimeLeft()
	{
        return GetNextIterationTime() - gameManager.audioSource.time;
	}

    public float getNormalizedTotalGrillingScore()
	{
        if (iterationsDone == 0) {return 0.0f;}
		return totalGrillingScore / iterationsDone;
	}
    public int GetDoneness()
	{
		float normalizedScore = getNormalizedTotalGrillingScore();
        //float transformedScore = normalizedScore + (normalizedScore + 1.0f) / iterationsDone;
        float score = -1 + (normalizedScore + 1.0f) * iterationsDone / iterationAmount;
        int i = Mathf.RoundToInt((score + 1) * 2);
        return Mathf.Clamp(i,0,4);
	}

    void Update()
    {
        if (iterationAmount == iterationsDone && gameManager.playing)
		{
			gameManager.StopPlaying();
		}

        if (iterationAmount == iterationsDone || !gameManager.playing) {return;}

        thresholdKnob.RotateKnob(knobDriftSpeed * Time.deltaTime);
        ratioKnob.RotateKnob(-knobDriftSpeed * Time.deltaTime);
        attackKnob.RotateKnob(knobDriftSpeed * Time.deltaTime);
        releaseKnob.RotateKnob(-knobDriftSpeed * Time.deltaTime);

        currentIterationScore = GetCurrentIterationScore();
        totalIterationScore += currentIterationScore * Time.deltaTime * grillingScoreMult;
        totalIterationScore = Mathf.Max(totalIterationScore, -1.0f);

        if (GetIterationTimeLeft() <= 0.0f  || gameManager.audioSource.time == 0.0f) {
			flipButton.PressButton();
		}

        // if (totalIterationScore > 0.2f)
		// {
        //      totalIterationScore = 1.0f;
		// 	    flipButton.PressButton();
		// }

    }
}
