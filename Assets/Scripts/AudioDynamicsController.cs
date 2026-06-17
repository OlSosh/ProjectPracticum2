using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class ShashlikAudioSpectrogram : MonoBehaviour
{
    [Header("Audio Source")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private FFTWindow fftWindow = FFTWindow.BlackmanHarris;
    [SerializeField] private int spectrumSamples = 1024;
    
    [Header("Bar Settings")]
    [SerializeField] private RectTransform barContainer;
    [SerializeField] private GameObject barPrefab;
    [SerializeField] private int barCount = 64;
    [SerializeField] private float maxHeight = 200f;
    [SerializeField] private float smoothingSpeed = 15f;
    [SerializeField] private float barWidth = 6f;
    [SerializeField] private float barSpacing = 2f;
    [SerializeField] private float minBarHeight = 2f;
    
    [Header("Temperature Colors")]
    [SerializeField] private Color coldColor = new Color(0f, 0.5f, 1f);
    [SerializeField] private Color warmingColor = new Color(0.3f, 0.7f, 1f);
    [SerializeField] private Color optimalColor = new Color(0f, 1f, 0.3f);
    [SerializeField] private Color hotColor = new Color(1f, 0.6f, 0f);
    [SerializeField] private Color overheatColor = new Color(1f, 0f, 0f);
    
    [Header("Knob Manager")]
    [SerializeField] private KnobManager knobManager;
    
    [Header("Volume Calculator (обработанный звук)")]
    [SerializeField] private VolumeCalculator volumeCalculator;
    
    [Header("References")]
    [SerializeField] private TextMeshProUGUI temperatureText;
    
    public event Action<bool> onRedZoneChanged;
    
    private GameObject[] bars;
    private RectTransform[] barRects;
    private Image[] barImages;
    private float[] spectrumData;
    private float[] currentHeights;
    private float[] targetHeights;
    private float currentTemperature = 0f;
    private bool wasInRedZone = false;
    private bool useProcessedAudio = false;
    
    public bool IsInRedZone => currentTemperature > 0.78f;
    public float CurrentTemperature => currentTemperature;
    public float AverageHeight { get; private set; }
    public KnobManager KnobManager => knobManager;
    
    private void Start()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
        
        if (knobManager == null)
            knobManager = FindObjectOfType<KnobManager>();
        
        // Находим VolumeCalculator
        if (volumeCalculator == null)
            volumeCalculator = FindObjectOfType<VolumeCalculator>();
        
        if (volumeCalculator != null)
        {
            // Подписываемся на события обработанного спектра
            volumeCalculator.onProcessedSpectrum.AddListener(OnProcessedSpectrumReceived);
            useProcessedAudio = true;
            Debug.Log("ShashlikAudioSpectrogram: Использую обработанный звук из VolumeCalculator");
        }
        else
        {
            Debug.LogWarning("ShashlikAudioSpectrogram: VolumeCalculator не найден, использую исходный AudioSource");
        }
        
        spectrumData = new float[spectrumSamples];
        currentHeights = new float[barCount];
        targetHeights = new float[barCount];
        bars = new GameObject[barCount];
        barRects = new RectTransform[barCount];
        barImages = new Image[barCount];
        
        if (barContainer == null)
            barContainer = GetComponent<RectTransform>();
        
        CreateBars();
    }
    
    private void OnProcessedSpectrumReceived(float[] processedSpectrum)
    {
        // Копируем обработанные спектральные данные
        if (processedSpectrum != null && processedSpectrum.Length >= spectrumSamples)
        {
            System.Array.Copy(processedSpectrum, spectrumData, spectrumSamples);
        }
    }
    
    private void OnDestroy()
    {
        if (volumeCalculator != null)
        {
            volumeCalculator.onProcessedSpectrum.RemoveListener(OnProcessedSpectrumReceived);
        }
        
        if (bars == null) return;
        
        for (int i = 0; i < bars.Length; i++)
        {
            if (bars[i] != null) 
                Destroy(bars[i]);
        }
    }
    
    private void CreateBars()
    {
        if (barPrefab == null || barContainer == null)
        {
            Debug.LogError("Bar Prefab или Bar Container не назначены!");
            return;
        }
        
        float totalBarWidth = barWidth + barSpacing;
        float totalWidth = barCount * totalBarWidth - barSpacing;
        float startX = -totalWidth / 2f + barWidth / 2f;
        
        for (int i = 0; i < barCount; i++)
        {
            GameObject bar = Instantiate(barPrefab, barContainer);
            bar.name = $"Bar_{i}";
            
            RectTransform rect = bar.GetComponent<RectTransform>();
            if (rect == null) rect = bar.AddComponent<RectTransform>();
            
            Image img = bar.GetComponent<Image>();
            if (img == null) img = bar.AddComponent<Image>();
            
            float xPos = startX + i * totalBarWidth;
            rect.anchoredPosition = new Vector2(xPos, 0);
            rect.sizeDelta = new Vector2(barWidth, minBarHeight);
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            
            img.color = coldColor;
            
            bars[i] = bar;
            barRects[i] = rect;
            barImages[i] = img;
        }
    }
    
    private void Update()
    {
        if (useProcessedAudio && volumeCalculator != null)
        {
            // Используем обработанные данные напрямую из VolumeCalculator
            UpdateSpectrumFromProcessedData();
        }
        else
        {
            // Fallback: используем исходный AudioSource
            GetSpectrumData();
        }
        
        ApplyKnobsToSpectrum();
        UpdateBars();
        CalculateTemperature();
        UpdateTemperatureDisplay();
        
        bool isInRedZone = IsInRedZone;
        if (isInRedZone != wasInRedZone)
        {
            wasInRedZone = isInRedZone;
            onRedZoneChanged?.Invoke(isInRedZone);
        }
    }
    
    private void UpdateSpectrumFromProcessedData()
    {
        // Получаем обработанную амплитуду из VolumeCalculator
        float processedAmplitude = volumeCalculator.GetProcessedAmplitude();
        
        // Также получаем спектральные данные
        float[] tempSpectrum = new float[spectrumSamples];
        volumeCalculator.GetProcessedSpectrumData(tempSpectrum);
        
        for (int i = 0; i < tempSpectrum.Length && i < spectrumData.Length; i++)
        {
            spectrumData[i] = Mathf.Lerp(spectrumData[i], tempSpectrum[i], Time.deltaTime * 10f);
        }
        
        // Обновляем targetHeights на основе спектра
        for (int i = 0; i < barCount; i++)
        {
            float freqStart = 20f * Mathf.Pow(20000f / 20f, (float)i / barCount);
            float freqEnd = 20f * Mathf.Pow(20000f / 20f, (float)(i + 1) / barCount);
            
            int startIndex = FreqToIndex(freqStart);
            int endIndex = FreqToIndex(freqEnd);
            
            float sum = 0;
            int count = 0;
            for (int j = startIndex; j <= endIndex && j < spectrumSamples; j++)
            {
                sum += spectrumData[j];
                count++;
            }
            
            targetHeights[i] = count > 0 ? (sum / count) * maxHeight * 8f : 0;
            targetHeights[i] = Mathf.Clamp(targetHeights[i], 0, maxHeight);
        }
    }
    
    private void GetSpectrumData()
    {
        if (audioSource != null && audioSource.isPlaying && audioSource.clip != null)
        {
            audioSource.GetSpectrumData(spectrumData, 0, fftWindow);
        }
        else
        {
            FillTestSpectrum();
        }
        
        for (int i = 0; i < barCount; i++)
        {
            float freqStart = 20f * Mathf.Pow(20000f / 20f, (float)i / barCount);
            float freqEnd = 20f * Mathf.Pow(20000f / 20f, (float)(i + 1) / barCount);
            
            int startIndex = FreqToIndex(freqStart);
            int endIndex = FreqToIndex(freqEnd);
            
            float sum = 0;
            int count = 0;
            for (int j = startIndex; j <= endIndex && j < spectrumSamples; j++)
            {
                sum += spectrumData[j];
                count++;
            }
            
            targetHeights[i] = count > 0 ? (sum / count) * maxHeight * 5f : 0;
            targetHeights[i] = Mathf.Clamp(targetHeights[i], 0, maxHeight);
        }
    }
    
    private void FillTestSpectrum()
    {
        float time = Time.time;
        for (int i = 0; i < spectrumSamples; i++)
        {
            float t = (float)i / spectrumSamples;
            
            float low = Mathf.Abs(Mathf.Sin(t * 8f + time * 1.5f)) * 0.45f;
            float mid = Mathf.Abs(Mathf.Sin(t * 25f + time * 2f)) * 0.35f;
            float high = Mathf.Abs(Mathf.Sin(t * 60f + time * 2.5f)) * 0.2f;
            
            float envelope = 0.5f + Mathf.Sin(time * 0.5f) * 0.15f;
            float fade = 1f - t * 0.5f;
            
            spectrumData[i] = (low + mid + high) * envelope * fade;
        }
    }
    
    private int FreqToIndex(float freq)
    {
        float nyquist = AudioSettings.outputSampleRate / 2f;
        return Mathf.Clamp(Mathf.RoundToInt(freq / nyquist * spectrumSamples), 0, spectrumSamples - 1);
    }
    
    private void ApplyKnobsToSpectrum()
    {
        if (knobManager == null) return;
        
        float thresholdNorm = knobManager.thresholdKnob != null ? 
            Mathf.InverseLerp(-60f, 0f, knobManager.thresholdKnob.CurrentValue) : 0.5f;
        
        float ratioNorm = knobManager.ratioKnob != null ? 
            Mathf.InverseLerp(1f, 20f, knobManager.ratioKnob.CurrentValue) : 0.3f;
        
        float attackNorm = knobManager.attackKnob != null ? 
            Mathf.InverseLerp(0.01f, 1f, knobManager.attackKnob.CurrentValue) : 0.5f;
        
        float releaseNorm = knobManager.releaseKnob != null ? 
            Mathf.InverseLerp(0.01f, 2f, knobManager.releaseKnob.CurrentValue) : 0.5f;
        
        float coalHeight = thresholdNorm * 0.6f + 0.2f;
        coalHeight = Mathf.Pow(coalHeight, 1.2f);
        coalHeight = Mathf.Clamp(coalHeight, 0.2f, 0.85f);
        
        float flamePower = ratioNorm * 0.5f + 0.25f;
        flamePower = Mathf.Clamp(flamePower, 0.25f, 0.8f);
        
        float burnSpeed = attackNorm * 0.4f + 0.3f;
        burnSpeed = Mathf.Clamp(burnSpeed, 0.3f, 0.75f);
        
        float smoothness = releaseNorm * 0.45f + 0.3f;
        smoothness = Mathf.Clamp(smoothness, 0.3f, 0.8f);
        
        for (int i = 0; i < barCount; i++)
        {
            float t = (float)i / barCount;
            
            float effect;
            
            if (t < 0.33f)
            {
                effect = Mathf.Lerp(coalHeight, flamePower * 0.9f, t / 0.33f);
            }
            else if (t < 0.66f)
            {
                effect = Mathf.Lerp(flamePower * 0.9f, burnSpeed, (t - 0.33f) / 0.33f);
            }
            else
            {
                effect = Mathf.Lerp(burnSpeed, smoothness, (t - 0.66f) / 0.34f);
            }
            
            float chaos = 1f - smoothness * 0.3f;
            float scale = 0.4f + effect * 1.3f * chaos;
            scale = Mathf.Clamp(scale, 0.3f, 1.5f);
            
            targetHeights[i] *= scale;
            targetHeights[i] = Mathf.Clamp(targetHeights[i], 0, maxHeight * 0.88f);
        }
    }
    
    private void UpdateBars()
    {
        float totalHeight = 0f;
        
        for (int i = 0; i < barCount; i++)
        {
            if (bars[i] == null) continue;
            
            currentHeights[i] = Mathf.Lerp(currentHeights[i], targetHeights[i], Time.deltaTime * smoothingSpeed);
            totalHeight += currentHeights[i];
            
            float height = Mathf.Max(minBarHeight, currentHeights[i]);
            barRects[i].sizeDelta = new Vector2(barWidth, height);
            
            Color barColor = GetTemperatureColor(currentHeights[i]);
            barImages[i].color = barColor;
        }
        
        AverageHeight = totalHeight / barCount;
        currentTemperature = Mathf.Clamp01(AverageHeight / maxHeight);
    }
    
    private Color GetTemperatureColor(float height)
    {
        float t = height / maxHeight;
        
        if (t < 0.15f)
            return Color.Lerp(coldColor, warmingColor, t / 0.15f);
        else if (t < 0.35f)
            return Color.Lerp(warmingColor, optimalColor, (t - 0.15f) / 0.2f);
        else if (t < 0.6f)
            return optimalColor;
        else if (t < 0.78f)
            return Color.Lerp(optimalColor, hotColor, (t - 0.6f) / 0.18f);
        else
            return Color.Lerp(hotColor, overheatColor, (t - 0.78f) / 0.22f);
    }
    
    private void CalculateTemperature()
    {
        currentTemperature = Mathf.Clamp01(AverageHeight / maxHeight);
    }
    
    private void UpdateTemperatureDisplay()
    {
        if (temperatureText == null) return;
        
        float t = currentTemperature;
        
        if (t < 0.15f)
        {
            temperatureText.text = "Огонь: ОЧЕНЬ СЛАБЫЙ 🔵";
            temperatureText.color = coldColor;
        }
        else if (t < 0.3f)
        {
            temperatureText.text = "Огонь: СЛАБЫЙ 💙";
            temperatureText.color = warmingColor;
        }
        else if (t < 0.5f)
        {
            temperatureText.text = "Огонь: ХОРОШИЙ 🟢";
            temperatureText.color = optimalColor;
        }
        else if (t < 0.65f)
        {
            temperatureText.text = "Огонь: ОПТИМАЛЬНЫЙ 🟢";
            temperatureText.color = optimalColor;
        }
        else if (t < 0.8f)
        {
            temperatureText.text = "Огонь: ЖАРКО 🟠";
            temperatureText.color = hotColor;
        }
        else
        {
            temperatureText.text = "ПЕРЕГРЕВ! ПЕРЕВОРАЧИВАЙ! 🔴";
            temperatureText.color = overheatColor;
        }
    }
    
    public float GetAccuracy()
    {
        float optimalTarget = 0.5f;
        float diff = Mathf.Abs(currentTemperature - optimalTarget);
        return 1f - Mathf.Clamp01(diff / 0.4f);
    }
    
    public float GetProgress()
    {
        return currentTemperature;
    }
}