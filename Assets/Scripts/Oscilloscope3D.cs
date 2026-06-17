using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class WaveformOscilloscope3D : MonoBehaviour
{
    [Header("Прототип пика")]
    [SerializeField] private GameObject barPrefab;
    [SerializeField] private int barCount = 128;
    [SerializeField] private bool circularLayout = false;

    [Header("Высота волны")]
    [SerializeField] private float maxHeight = 5f;
    [SerializeField] private float smoothingSpeed = 15f;

    [Header("Расстояние между пиками")]
    [SerializeField] private float baseRadius = 8f;
    [SerializeField] private float baseSpacing = 0.15f;
    [Range(0.1f, 3f)]
    [SerializeField] private float spacingMultiplier = 1f;
    [SerializeField] private bool autoCloseGaps = false;

    [Header("Аудио")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private bool useMicrophone = false;
    
    [Header("Volume Calculator (обработанный звук)")]
    [SerializeField] private VolumeCalculator volumeCalculator;

    [Header("Визуал")]
    [SerializeField] private float minBarHeight = 0.02f;

    private GameObject[] bars;
    private float[] waveformData;
    private float[] currentHeights;
    private Vector3[] fixedBasePositions;
    private float barWidth;
    private bool useProcessedAudio = false;
    private float[] processedOutputBuffer = new float[512];

    void Start()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (useMicrophone && Microphone.devices.Length > 0)
        {
            audioSource.clip = Microphone.Start(null, true, 1, 44100);
            audioSource.loop = true;
            while (Microphone.GetPosition(null) <= 0) { }
            audioSource.Play();
        }
        
        // Находим VolumeCalculator
        if (volumeCalculator == null)
            volumeCalculator = FindObjectOfType<VolumeCalculator>();
        
        if (volumeCalculator != null)
        {
            volumeCalculator.onProcessedOutputData.AddListener(OnProcessedOutputReceived);
            useProcessedAudio = true;
            Debug.Log("WaveformOscilloscope3D: Использую обработанный звук из VolumeCalculator");
        }
        else
        {
            Debug.LogWarning("WaveformOscilloscope3D: VolumeCalculator не найден, использую исходный AudioSource");
        }

        if (barPrefab != null)
        {
            MeshFilter mf = barPrefab.GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null)
                barWidth = mf.sharedMesh.bounds.size.x * barPrefab.transform.localScale.x;
            else
                barWidth = barPrefab.transform.localScale.x;
        }

        waveformData = new float[barCount];
        currentHeights = new float[barCount];
        fixedBasePositions = new Vector3[barCount];
        bars = new GameObject[barCount];
        processedOutputBuffer = new float[barCount * 2];

        CreateBars();
    }
    
    private void OnProcessedOutputReceived(float[] outputData)
    {
        // Сохраняем обработанные выходные данные
        if (outputData != null && outputData.Length > 0)
        {
            if (processedOutputBuffer.Length != outputData.Length)
                processedOutputBuffer = new float[outputData.Length];
            System.Array.Copy(outputData, processedOutputBuffer, Mathf.Min(outputData.Length, processedOutputBuffer.Length));
        }
    }
    
    private void OnDestroy()
    {
        if (volumeCalculator != null)
        {
            volumeCalculator.onProcessedOutputData.RemoveListener(OnProcessedOutputReceived);
        }
    }

    float GetSpacing()
    {
        if (autoCloseGaps)
        {
            return circularLayout
                ? (barWidth / (2f * Mathf.PI)) * barCount
                : barWidth;
        }
        else
        {
            return circularLayout
                ? baseRadius * spacingMultiplier
                : baseSpacing * spacingMultiplier;
        }
    }

    void CreateBars()
    {
        float spacing = GetSpacing();

        for (int i = 0; i < barCount; i++)
        {
            GameObject bar = Instantiate(barPrefab, transform);
            bar.SetActive(true);
            bar.name = $"Wave_{i}";

            if (circularLayout)
            {
                float angle = i * Mathf.PI * 2f / barCount;
                float x = Mathf.Cos(angle) * spacing;
                float z = Mathf.Sin(angle) * spacing;
                fixedBasePositions[i] = new Vector3(x, 0, z);
                bar.transform.localPosition = fixedBasePositions[i];
            }
            else
            {
                float x = (i - barCount / 2f) * spacing;
                fixedBasePositions[i] = new Vector3(x, 0, 0);
                bar.transform.localPosition = fixedBasePositions[i];
            }

            bars[i] = bar;
        }
    }

    void RepositionAllBars()
    {
        float spacing = GetSpacing();

        for (int i = 0; i < bars.Length; i++)
        {
            if (bars[i] == null) continue;

            if (circularLayout)
            {
                float angle = i * Mathf.PI * 2f / barCount;
                float x = Mathf.Cos(angle) * spacing;
                float z = Mathf.Sin(angle) * spacing;
                fixedBasePositions[i] = new Vector3(x, 0, z);
            }
            else
            {
                float x = (i - barCount / 2f) * spacing;
                fixedBasePositions[i] = new Vector3(x, 0, 0);
            }

            bars[i].transform.localPosition = fixedBasePositions[i];
        }
    }

    void Update()
    {
        if (useProcessedAudio && volumeCalculator != null)
        {
            // Используем обработанные данные из VolumeCalculator
            UpdateFromProcessedData();
        }
        else if (audioSource != null && audioSource.isPlaying)
        {
            // Fallback: используем исходный AudioSource
            audioSource.GetOutputData(waveformData, 0);
            UpdateVisualsFromData(waveformData);
        }
    }
    
    private void UpdateFromProcessedData()
    {
        // Получаем обработанные выходные данные
        volumeCalculator.GetProcessedOutputData(processedOutputBuffer);
        
        // Интерполируем данные для нашего количества баров
        for (int i = 0; i < barCount; i++)
        {
            int sourceIndex = (int)((float)i / barCount * processedOutputBuffer.Length);
            sourceIndex = Mathf.Clamp(sourceIndex, 0, processedOutputBuffer.Length - 1);
            waveformData[i] = processedOutputBuffer[sourceIndex];
        }
        
        UpdateVisualsFromData(waveformData);
    }
    
    private void UpdateVisualsFromData(float[] data)
    {
        for (int i = 0; i < barCount; i++)
        {
            float rawAmplitude = data.Length > i ? data[i] : 0f;
            
            // Используем абсолютное значение для высоты (пики вверх)
            float targetHeight = Mathf.Abs(rawAmplitude) * maxHeight * 2f; // Усиливаем для лучшей видимости
            
            // Плавное сглаживание
            currentHeights[i] = Mathf.Lerp(currentHeights[i], targetHeight, Time.deltaTime * smoothingSpeed);

            if (bars[i] == null) continue;
            
            // Позиция фиксирована
            bars[i].transform.localPosition = fixedBasePositions[i];
            
            // Меняется ТОЛЬКО масштаб по Y
            float scaleY = Mathf.Max(minBarHeight, currentHeights[i]);
            bars[i].transform.localScale = new Vector3(
                bars[i].transform.localScale.x,
                scaleY,
                bars[i].transform.localScale.z
            );
            
            // Цвет в зависимости от высоты
            Renderer rend = bars[i].GetComponent<Renderer>();
            if (rend != null)
            {
                float t = Mathf.Clamp01(currentHeights[i] / maxHeight);
                Color flameColor = Color.Lerp(
                    new Color(1f, 0.9f, 0.2f),
                    new Color(0.9f, 0.2f, 0f),
                    1f - t
                );
                rend.material.SetColor("_BaseColor", flameColor);
                rend.material.SetColor("_EmissionColor", flameColor * 3f);
            }
        }
    }

    public void SetSpacingMultiplier(float value)
    {
        spacingMultiplier = Mathf.Clamp(value, 0.1f, 3f);
        autoCloseGaps = false;
        RepositionAllBars();
    }

    public void ToggleAutoClose()
    {
        autoCloseGaps = !autoCloseGaps;
        RepositionAllBars();
    }

    public void SetMaxHeight(float value)
    {
        maxHeight = Mathf.Clamp(value, 0.5f, 20f);
    }

    public void SetSmoothingSpeed(float value)
    {
        smoothingSpeed = Mathf.Clamp(value, 1f, 50f);
    }
}