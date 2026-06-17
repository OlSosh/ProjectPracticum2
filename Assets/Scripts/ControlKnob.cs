using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioDynamicsController : MonoBehaviour
{
    [Header("Режим Работы")]
    public DynamicsMode mode = DynamicsMode.Compressor;
    
    public enum DynamicsMode
    {
        Compressor,
        Expander
    }
    
    [Header("Ручка 1: Threshold (Порог)")]
    [Range(-60f, 0f)]
    public float threshold = -20f;
    
    [Header("Ручка 2: Ratio (Соотношение)")]
    [Range(1.1f, 20f)]
    public float ratio = 4f;
    
    [Header("Ручка 3: Attack (Атака)")]
    [Range(0.1f, 100f)]
    public float attackTimeMs = 10f;
    
    [Header("Ручка 4: Release (Восстановление)")]
    [Range(10f, 2000f)]
    public float releaseTimeMs = 100f;
    
    private AudioSource audioSource;
    private float envelope = 0f;
    private float sampleRate;
    
    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        sampleRate = AudioSettings.outputSampleRate;
    }
    
    void OnAudioFilterRead(float[] data, int channels)
    {
        if (audioSource == null || data == null) return;
        
        float attackCoef = Mathf.Exp(-1f / (Mathf.Max(0.0001f, attackTimeMs / 1000f) * sampleRate));
        float releaseCoef = Mathf.Exp(-1f / (Mathf.Max(0.0001f, releaseTimeMs / 1000f) * sampleRate));
        float thresholdLinear = DecibelToLinear(threshold);
        
        for (int i = 0; i < data.Length; i += channels)
        {
            float maxAmplitude = 0f;
            for (int c = 0; c < channels; c++)
            {
                float absSample = Mathf.Abs(data[i + c]);
                if (absSample > maxAmplitude) maxAmplitude = absSample;
            }
            
            if (maxAmplitude > envelope)
                envelope = attackCoef * envelope + (1f - attackCoef) * maxAmplitude;
            else
                envelope = releaseCoef * envelope + (1f - releaseCoef) * maxAmplitude;
            
            float gain = 1f;
            
            if (mode == DynamicsMode.Compressor && envelope > thresholdLinear && thresholdLinear > 0.0001f)
            {
                float envelopeDB = LinearToDecibel(envelope);
                float thresholdDB = LinearToDecibel(thresholdLinear);
                float overDB = envelopeDB - thresholdDB;
                float compressedDB = overDB / ratio;
                float outputDB = thresholdDB + compressedDB;
                float outputLinear = DecibelToLinear(outputDB);
                gain = Mathf.Clamp(outputLinear / envelope, 0f, 1f);
            }
            
            for (int c = 0; c < channels; c++)
            {
                float sample = data[i + c] * gain;
                if (sample > 1f) sample = 0.99f;
                if (sample < -1f) sample = -0.99f;
                data[i + c] = sample;
            }
        }
    }
    
    float DecibelToLinear(float db)
    {
        if (db <= -60f) return 0f;
        return Mathf.Pow(10f, db / 20f);
    }
    
    float LinearToDecibel(float linear)
    {
        if (linear <= 0.0001f) return -60f;
        return 20f * Mathf.Log10(linear);
    }
    
    public void SetThreshold(float value) { threshold = value; }
    public void SetRatio(float value) { ratio = value; }
    public void SetAttackTime(float value) { attackTimeMs = value; }
    public void SetReleaseTime(float value) { releaseTimeMs = value; }
}