using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(AudioSource))]
public class VolumeCalculator : MonoBehaviour
{
    [Header("=== OUTPUT (визуализация) ===")]
    public float rawVolume;
    public float processedVolume;
    public float currentGain = 1f;
    public float gainReductionDB = 0f;
    public float[] rawBand = new float[5];

    [Header("=== ПАРАМЕТРЫ КОМПРЕССОРА ===")]
    [Range(0.001f, 0.5f)]
    public float currentThreshold = 0.05f;
    
    [Range(1f, 10f)]
    public float currentRatio = 3f;
    
    [Range(0.1f, 200f)]
    public float attackMs = 10f;
    
    [Range(10f, 1000f)]
    public float releaseMs = 200f;

    public bool enableProcessing = true;
    public enum ProcessingMode { None, Compressor, Expander }
    public ProcessingMode currentMode = ProcessingMode.Compressor;

    [Header("=== УСИЛЕНИЕ ===")]
    [Range(0.1f, 10f)]
    public float outputGain = 2f;
    
    [Range(1f, 30f)]
    public float inputBoost = 10f;

    [Header("=== ЭФФЕКТЫ ===")]
    public bool enableDistortion = false;
    [Range(0f, 1f)]
    public float distortionDrive = 0.5f;
    
    public bool enablePumping = false;
    [Range(0f, 0.5f)]
    public float pumpingIntensity = 0.3f;
    
    public bool enableBassEnhance = false;
    [Range(0f, 1.5f)]
    public float bassBoost = 0.5f;

    [Header("=== ДОПОЛНИТЕЛЬНЫЕ ЭФФЕКТЫ ===")]
    public bool enableStereoWiden = false;
    [Range(0f, 0.8f)]
    public float stereoWidth = 0.3f;
    
    public bool enableSaturation = false;
    [Range(0f, 0.5f)]
    public float saturationAmount = 0.2f;

    [Header("=== ВИЗУАЛИЗАЦИЯ (ОБРАБОТАННЫЙ ЗВУК) ===")]
    public bool provideProcessedDataForVisualization = true;
    private float[] processedAudioBuffer;
    private int processedBufferSamples = 1024;
    private float[] spectrumData = new float[1024];
    private float[] outputData = new float[1024];

    [Header("=== СОБЫТИЯ ===")]
    public UnityEvent onSettingsReset;
    public UnityEvent<float> onCompressionActive;
    public UnityEvent<float> onGainReductionChanged;
    public UnityEvent<float> onVolumeChanged;
    
    // События для визуализации
    public UnityEvent<float[]> onProcessedSpectrum;
    public UnityEvent<float[]> onProcessedOutputData;

    // Приватные
    private float attackCoef;
    private float releaseCoef;
    private float envelope = 0f;
    private float[] spectrum = new float[512];
    private int sampleRate;
    private float prevSample = 0f;

    void Start()
    {
        sampleRate = AudioSettings.outputSampleRate;
        UpdateCoefficients();
        processedAudioBuffer = new float[processedBufferSamples];
        Debug.Log("🎛 КОМПРЕССОР АКТИВИРОВАН (нормальный режим)");
    }

    void Update()
    {
        UpdateCoefficients();

        AudioSource src = GetComponent<AudioSource>();
        if (src != null)
        {
            src.GetSpectrumData(spectrum, 0, FFTWindow.Blackman);

            float sum = 0f;
            for (int i = 0; i < spectrum.Length; i++)
                sum += spectrum[i];
            rawVolume = sum / spectrum.Length;

            for (int b = 0; b < 5; b++)
            {
                float bandSum = 0;
                int start = b * (spectrum.Length / 5);
                int end = (b == 4) ? spectrum.Length : start + spectrum.Length / 5;
                for (int i = start; i < end; i++)
                    bandSum += spectrum[i];
                rawBand[b] = bandSum / (end - start);
            }
        }

        // События
        if (gainReductionDB < -1f)
            onCompressionActive?.Invoke(Mathf.Abs(gainReductionDB));
        
        onGainReductionChanged?.Invoke(gainReductionDB);
        onVolumeChanged?.Invoke(rawVolume * inputBoost);
    }

    void UpdateCoefficients()
    {
        float attackSec = Mathf.Max(attackMs * 0.001f, 0.0001f);
        float releaseSec = Mathf.Max(releaseMs * 0.001f, 0.0001f);
        attackCoef = Mathf.Exp(-1f / (attackSec * sampleRate));
        releaseCoef = Mathf.Exp(-1f / (releaseSec * sampleRate));
    }

    void OnAudioFilterRead(float[] data, int channels)
    {
        if (!enableProcessing || currentMode == ProcessingMode.None)
        {
            for (int i = 0; i < data.Length; i++)
                data[i] *= outputGain;
            
            if (provideProcessedDataForVisualization)
                SaveProcessedData(data);
            
            return;
        }

        for (int i = 0; i < data.Length; i += channels)
        {
            float inputSample = data[i];
            
            // Дисторшн (умеренный)
            if (enableDistortion)
                inputSample = ApplyDistortion(inputSample);

            // Сатурация (лёгкая)
            if (enableSaturation)
                inputSample = ApplySaturation(inputSample);

            // Усиление входа
            float boostedSample = inputSample * inputBoost;

            // Бас-усиление (лёгкое)
            if (enableBassEnhance)
            {
                float bass = boostedSample - prevSample;
                float enhanced = boostedSample + bass * bassBoost;
                boostedSample = Mathf.Lerp(boostedSample, enhanced, 0.3f);
            }
            prevSample = boostedSample;

            // Envelope follower
            float inputLevel = Mathf.Abs(boostedSample);

            if (inputLevel > envelope)
            {
                envelope = attackCoef * envelope + (1f - attackCoef) * inputLevel;
            }
            else
            {
                if (enablePumping)
                {
                    float pumpCoef = releaseCoef * (1f - pumpingIntensity * 0.5f);
                    envelope = pumpCoef * envelope + (1f - pumpCoef) * inputLevel;
                }
                else
                {
                    envelope = releaseCoef * envelope + (1f - releaseCoef) * inputLevel;
                }
            }

            // Вычисление компрессии
            float envelopeDB = 20f * Mathf.Log10(Mathf.Max(envelope, 0.0001f));
            float thresholdDB = 20f * Mathf.Log10(Mathf.Max(currentThreshold * inputBoost, 0.0001f));

            float gainReduction = 1f;
            float currentGainReductionDB = 0f;

            if (currentMode == ProcessingMode.Compressor)
            {
                if (envelopeDB > thresholdDB)
                {
                    float overDB = envelopeDB - thresholdDB;
                    float compressedDB = thresholdDB + overDB / currentRatio;
                    currentGainReductionDB = compressedDB - envelopeDB;
                    gainReduction = Mathf.Pow(10f, currentGainReductionDB / 20f);
                    gainReduction = Mathf.Clamp(gainReduction, 0.1f, 1f);
                }
            }
            else if (currentMode == ProcessingMode.Expander)
            {
                if (envelopeDB < thresholdDB)
                {
                    float underDB = thresholdDB - envelopeDB;
                    float expandedDB = thresholdDB - underDB * currentRatio;
                    currentGainReductionDB = expandedDB - envelopeDB;
                    gainReduction = Mathf.Pow(10f, currentGainReductionDB / 20f);
                    gainReduction = Mathf.Clamp(gainReduction, 0.1f, 2f);
                }
            }

            gainReductionDB = currentGainReductionDB;

            float finalGain = gainReduction * outputGain;
            currentGain = finalGain;
            finalGain = Mathf.Min(finalGain, 5f);

            // Стерео расширение (умеренное)
            if (enableStereoWiden && channels == 2)
            {
                int leftIdx = i;
                int rightIdx = i + 1;
                
                if (rightIdx < data.Length)
                {
                    float mid = (data[leftIdx] + data[rightIdx]) * 0.5f;
                    float side = (data[leftIdx] - data[rightIdx]) * 0.5f;
                    
                    side *= (1f + stereoWidth);
                    
                    data[leftIdx] = (mid + side) * finalGain;
                    data[rightIdx] = (mid - side) * finalGain;
                }
            }
            else
            {
                for (int c = 0; c < channels && (i + c) < data.Length; c++)
                {
                    data[i + c] *= finalGain;
                    
                    if (Mathf.Abs(data[i + c]) > 0.99f)
                        data[i + c] = Mathf.Sign(data[i + c]) * 0.99f;
                }
            }
        }

        processedVolume = envelope;
        
        // Сохраняем обработанные данные для визуализации
        if (provideProcessedDataForVisualization)
            SaveProcessedData(data);
    }
    
    private void SaveProcessedData(float[] data)
    {
        if (processedAudioBuffer == null || processedAudioBuffer.Length != data.Length)
            processedAudioBuffer = new float[data.Length];
        
        System.Array.Copy(data, processedAudioBuffer, data.Length);
        
        // Заполняем спектр-данные для визуализации
        for (int i = 0; i < spectrumData.Length && i < data.Length; i++)
        {
            spectrumData[i] = Mathf.Abs(data[i % data.Length]) * 3f;
        }
        
        // Заполняем выходные данные
        for (int i = 0; i < outputData.Length && i < data.Length; i++)
        {
            outputData[i] = data[i % data.Length];
        }
        
        onProcessedSpectrum?.Invoke(spectrumData);
        onProcessedOutputData?.Invoke(outputData);
    }
    
    public void GetProcessedSpectrumData(float[] targetArray)
    {
        if (targetArray == null) return;
        
        for (int i = 0; i < targetArray.Length && i < spectrumData.Length; i++)
        {
            targetArray[i] = spectrumData[i];
        }
    }
    
    public void GetProcessedOutputData(float[] targetArray)
    {
        if (targetArray == null) return;
        
        for (int i = 0; i < targetArray.Length && i < outputData.Length; i++)
        {
            targetArray[i] = outputData[i];
        }
    }
    
    public float GetProcessedAmplitude()
    {
        if (processedAudioBuffer == null || processedAudioBuffer.Length == 0)
            return 0f;
        
        float sum = 0f;
        for (int i = 0; i < Mathf.Min(256, processedAudioBuffer.Length); i++)
        {
            sum += Mathf.Abs(processedAudioBuffer[i]);
        }
        return sum / 256f;
    }

    float ApplyDistortion(float sample)
    {
        float drive = distortionDrive * 3f;
        float distorted = sample * (1f + drive);
        distorted = Mathf.Clamp(distorted, -0.9f, 0.9f);
        return Mathf.Lerp(sample, distorted, 0.5f);
    }

    float ApplySaturation(float sample)
    {
        float amount = saturationAmount;
        float saturated = Mathf.Sign(sample) * (1f - Mathf.Exp(-Mathf.Abs(sample * (1f + amount * 2f))));
        return Mathf.Lerp(sample, saturated, amount);
    }

    public void SetCompressorPreset()
    {
        currentThreshold = 0.05f;
        currentRatio = 3f;
        attackMs = 10f;
        releaseMs = 200f;
        outputGain = 2f;
        inputBoost = 10f;
        distortionDrive = 0.5f;
        pumpingIntensity = 0.3f;
        bassBoost = 0.5f;
        enableDistortion = false;
        enablePumping = false;
        enableBassEnhance = false;
        enableSaturation = false;
        UpdateCoefficients();
        Debug.Log("✅ КОМПРЕССОР");
    }

    public void SetSoftPreset()
    {
        currentThreshold = 0.03f;
        currentRatio = 2f;
        attackMs = 20f;
        releaseMs = 300f;
        outputGain = 1.5f;
        inputBoost = 8f;
        distortionDrive = 0f;
        pumpingIntensity = 0f;
        bassBoost = 0f;
        enableDistortion = false;
        enablePumping = false;
        enableBassEnhance = false;
        enableSaturation = false;
        UpdateCoefficients();
        Debug.Log("🎵 МЯГКИЙ КОМПРЕССОР");
    }

    public void SetCleanPreset()
    {
        currentThreshold = 0.1f;
        currentRatio = 1.5f;
        attackMs = 5f;
        releaseMs = 100f;
        outputGain = 1f;
        inputBoost = 5f;
        distortionDrive = 0f;
        pumpingIntensity = 0f;
        bassBoost = 0f;
        enableDistortion = false;
        enablePumping = false;
        enableBassEnhance = false;
        enableSaturation = false;
        UpdateCoefficients();
        Debug.Log("🧹 ЧИСТЫЙ ЗВУК");
    }

    public void ResetToDefaults()
    {
        SetCompressorPreset();
        onSettingsReset?.Invoke();
    }

    public float GetCompressionAmount() => Mathf.Abs(gainReductionDB) / 20f;
    public float GetVolumeLevel() => Mathf.Clamp01(rawVolume * inputBoost);
}