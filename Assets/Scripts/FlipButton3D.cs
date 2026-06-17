using UnityEngine;
using System.Collections;

public class RotationButton3D : MonoBehaviour
{
    [Header("Ссылки")]
    public KebabManager kebabManager;
    public ScoreManager scoreManager;
    
    [Header("Визуал кнопки")]
    public Color normalColor = new Color(0.2f, 0.4f, 0.8f);
    public Color pressedColor = new Color(0.2f, 0.8f, 0.2f);
    public Color hoverColor = new Color(0.4f, 0.6f, 1f);
    
    [Header("Настройки шагового вращения")]
    public float stepAngle = 45f;
    public bool rotateClockwise = true;
    
    [Header("Анимация кнопки")]
    public float pressDepth = 0.15f;
    public float pressDuration = 0.3f;
    public AnimationCurve pressCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    
    [Header("Звуки")]
    public AudioClip pressSound;
    public AudioClip releaseSound;
    
    private MeshRenderer buttonRenderer;
    private Vector3 originalPosition;
    private AudioSource audioSource;
    private bool isPressed = false;
    private bool isAnimating = false;
    private bool isHovered = false;
    private Camera mainCamera;
    private Collider buttonCollider;
    private Material buttonMaterial;
    
    void Start()
    {
        buttonRenderer = GetComponent<MeshRenderer>();
        originalPosition = transform.localPosition;
        mainCamera = Camera.main;
        
        if (mainCamera == null)
        {
            Debug.LogError("Camera.main не найдена! Назначь тег MainCamera на камеру.");
        }
        
        // Настройка коллайдера
        buttonCollider = GetComponent<Collider>();
        if (buttonCollider == null)
        {
            BoxCollider boxCol = gameObject.AddComponent<BoxCollider>();
            boxCol.size = new Vector3(0.3f, 0.1f, 0.3f);
            boxCol.center = Vector3.zero;
            buttonCollider = boxCol;
        }
        
        buttonCollider.enabled = true;
        buttonCollider.isTrigger = false;
        
        // Настройка материала
        if (buttonRenderer != null)
        {
            buttonMaterial = buttonRenderer.material;
            if (buttonMaterial == null)
            {
                buttonMaterial = new Material(Shader.Find("Standard"));
                buttonRenderer.material = buttonMaterial;
            }
            buttonMaterial.color = normalColor;
        }
        
        // Настройка AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;
        
        if (kebabManager == null)
        {
            kebabManager = FindFirstObjectByType<KebabManager>();
        }
    }
    
    void Update()
    {
        if (mainCamera == null || buttonCollider == null) return;
        
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        
        bool isHittingThisButton = false;
        
        if (Physics.Raycast(ray, out hit, 100f))
        {
            if (hit.collider == buttonCollider)
            {
                isHittingThisButton = true;
            }
        }
        
        // Наведение
        if (isHittingThisButton && !isHovered)
        {
            isHovered = true;
            if (!isPressed && !isAnimating && !(kebabManager != null && kebabManager.IsRotating()))
            {
                if (buttonMaterial != null) buttonMaterial.color = hoverColor;
            }
        }
        else if (!isHittingThisButton && isHovered)
        {
            isHovered = false;
            if (!isPressed && !isAnimating)
            {
                if (buttonMaterial != null)
                {
                    if (kebabManager != null && kebabManager.IsRotating())
                        buttonMaterial.color = pressedColor;
                    else
                        buttonMaterial.color = normalColor;
                }
            }
        }
        
        // Клик
        if (isHittingThisButton && Input.GetMouseButtonDown(0))
        {
            PressButton();
        }
        
        // Цвет при вращении
        if (!isAnimating && !isPressed && !isHovered)
        {
            if (buttonMaterial != null)
            {
                if (kebabManager != null && kebabManager.IsRotating())
                    buttonMaterial.color = pressedColor;
                else
                    buttonMaterial.color = normalColor;
            }
        }
    }
    
    public void PressButton()
	{
		if (!isPressed && !isAnimating)
        {
            StartCoroutine(PressAnimation());
            scoreManager.EndGrillingIteration();
        }
	}

    IEnumerator PressAnimation()
    {
        isPressed = true;
        isAnimating = true;
        
        // Меняем цвет
        if (buttonMaterial != null)
            buttonMaterial.color = pressedColor;
        
        // Звук нажатия (с проверкой)
        if (pressSound != null && audioSource != null)
        {
            audioSource.clip = pressSound;
            audioSource.Play();
        }
        
        // Анимация вниз
        float elapsed = 0f;
        float pressPhaseDuration = pressDuration * 0.3f;
        Vector3 pressedPosition = originalPosition - new Vector3(0, pressDepth, 0);
        
        while (elapsed < pressPhaseDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / pressPhaseDuration);
            float curvedT = pressCurve.Evaluate(t);
            transform.localPosition = Vector3.Lerp(originalPosition, pressedPosition, curvedT);
            yield return null;
        }
        
        transform.localPosition = pressedPosition;
        
        // Шаговый поворот
        if (kebabManager != null)
        {
            float angle = rotateClockwise ? stepAngle : -stepAngle;
            kebabManager.PerformStepRotation(angle);
        }
        
        // Пауза в нажатом состоянии
        yield return new WaitForSeconds(0.15f);
        
        // Звук отпускания
        if (releaseSound != null && audioSource != null)
        {
            audioSource.clip = releaseSound;
            audioSource.Play();
        }
        
        // Анимация возврата
        elapsed = 0f;
        float returnPhaseDuration = pressDuration * 0.7f;
        
        while (elapsed < returnPhaseDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / returnPhaseDuration);
            float curvedT = pressCurve.Evaluate(t);
            transform.localPosition = Vector3.Lerp(pressedPosition, originalPosition, curvedT);
            yield return null;
        }
        
        transform.localPosition = originalPosition;
        
        // Восстанавливаем цвет
        if (buttonMaterial != null)
        {
            if (kebabManager != null && kebabManager.IsRotating())
                buttonMaterial.color = pressedColor;
            else
                buttonMaterial.color = isHovered ? hoverColor : normalColor;
        }
        
        isPressed = false;
        isAnimating = false;
    }
    
    void OnDrawGizmos()
    {
        if (buttonCollider == null)
            buttonCollider = GetComponent<Collider>();
            
        if (buttonCollider != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.matrix = transform.localToWorldMatrix;
            
            if (buttonCollider is BoxCollider boxCol)
            {
                Gizmos.DrawWireCube(boxCol.center, boxCol.size);
            }
        }
    }
}