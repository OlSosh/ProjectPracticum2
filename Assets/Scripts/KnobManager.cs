using UnityEngine;

public class KnobManager : MonoBehaviour
{
    [Header("Audio Knobs")]
    public RotatableKnob thresholdKnob;
    public RotatableKnob ratioKnob;
    public RotatableKnob attackKnob;
    public RotatableKnob releaseKnob;
    
    [Header("Visual Knobs")]
    public RotatableKnob spacingKnob;
    public RotatableKnob heightKnob;
    public RotatableKnob smoothingKnob;
    
    [Header("Audio Source")]
    public AudioSource audioSource;
    
    [Header("Audio Dynamics Controller")]
    [SerializeField] private VolumeCalculator volumeCalculator;  // ← ИЗМЕНЕНО
    
    private void Start()
    {
        InitializeKnobs();
        SetupAudioListeners();
    }
    
    private void InitializeKnobs()
    {
        if (thresholdKnob != null)
        {
            // Конвертируем dB в линейное значение для VolumeCalculator
            thresholdKnob.onValueChanged.AddListener(OnThresholdChanged);
        }
        
        if (ratioKnob != null)
        {
            ratioKnob.onValueChanged.AddListener(OnRatioChanged);
        }
        
        if (attackKnob != null)
        {
            attackKnob.onValueChanged.AddListener(OnAttackChanged);
        }
        
        if (releaseKnob != null)
        {
            releaseKnob.onValueChanged.AddListener(OnReleaseChanged);
        }
        
        // Остальные инициализации...
        if (spacingKnob != null)
        {
            spacingKnob.minValue = 0.1f;
            spacingKnob.maxValue = 3f;
        }
        
        if (heightKnob != null)
        {
            heightKnob.minValue = 50f;
            heightKnob.maxValue = 400f;
        }
        
        if (smoothingKnob != null)
        {
            smoothingKnob.minValue = 1f;
            smoothingKnob.maxValue = 30f;
        }
    }
    
    private void SetupAudioListeners()
    {
        // Находим VolumeCalculator если не назначен
        if (volumeCalculator == null)
            volumeCalculator = FindObjectOfType<VolumeCalculator>();
        
        if (volumeCalculator == null)
        {
            return;
        }
        
        // Инициализируем начальными значениями
        if (thresholdKnob != null) 
            OnThresholdChanged(thresholdKnob.CurrentValue);
        if (ratioKnob != null) 
            OnRatioChanged(ratioKnob.CurrentValue);
        if (attackKnob != null) 
            OnAttackChanged(attackKnob.CurrentValue);
        if (releaseKnob != null) 
            OnReleaseChanged(releaseKnob.CurrentValue);
        
    }
    
    private void OnThresholdChanged(float value)
    {
        if (volumeCalculator != null)
        {
            // Конвертируем dB в линейное значение (0.0001 = -80dB, 1 = 0dB)
            float linearThreshold = Mathf.Pow(10f, value / 20f);
            volumeCalculator.currentThreshold = Mathf.Clamp(linearThreshold, 0.0001f, 1f);
        }
    }
    
    private void OnRatioChanged(float value)
    {
        if (volumeCalculator != null)
        {
            volumeCalculator.currentRatio = value;
        }
    }
    
    private void OnAttackChanged(float value)
    {
        if (volumeCalculator != null)
        {
            volumeCalculator.attackMs = value;
        }
    }
    
    private void OnReleaseChanged(float value)
    {
        if (volumeCalculator != null)
        {
            volumeCalculator.releaseMs = value;
        }
    }
    
    // Остальные методы без изменений...
    public float GetSpacingNormalized() => spacingKnob != null ? spacingKnob.GetNormalizedValue() : 0.5f;
    public float GetHeightNormalized() => heightKnob != null ? heightKnob.GetNormalizedValue() : 0.5f;
    public float GetSmoothingNormalized() => smoothingKnob != null ? smoothingKnob.GetNormalizedValue() : 0.5f;
    public float GetThresholdNormalized() => thresholdKnob != null ? thresholdKnob.GetNormalizedValue() : 0.5f;
    public float GetRatioNormalized() => ratioKnob != null ? ratioKnob.GetNormalizedValue() : 0.3f;
    public float GetAttackNormalized() => attackKnob != null ? attackKnob.GetNormalizedValue() : 0.5f;
    public float GetReleaseNormalized() => releaseKnob != null ? releaseKnob.GetNormalizedValue() : 0.5f;
    
    public void ResetAllKnobs()
    {
        if (thresholdKnob != null) thresholdKnob.SetNormalizedValue(0.5f);
        if (ratioKnob != null) ratioKnob.SetNormalizedValue(0.3f);
        if (attackKnob != null) attackKnob.SetNormalizedValue(0.5f);
        if (releaseKnob != null) releaseKnob.SetNormalizedValue(0.5f);
    }
    
    private void OnDestroy()
    {
        // Отписываемся от событий
        if (thresholdKnob != null)
            thresholdKnob.onValueChanged.RemoveListener(OnThresholdChanged);
        if (ratioKnob != null)
            ratioKnob.onValueChanged.RemoveListener(OnRatioChanged);
        if (attackKnob != null)
            attackKnob.onValueChanged.RemoveListener(OnAttackChanged);
        if (releaseKnob != null)
            releaseKnob.onValueChanged.RemoveListener(OnReleaseChanged);
    }
}