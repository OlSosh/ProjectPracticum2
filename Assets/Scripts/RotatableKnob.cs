using UnityEngine;
using UnityEngine.Events;

public class RotatableKnob : MonoBehaviour
{
    [Header("Ось Вращения")]
    [Tooltip("Выберите ось, вокруг которой будет вращаться ручка.")]
    public RotationAxis rotationAxis = RotationAxis.Z;
    
    public enum RotationAxis
    {
        X, Y, Z,
        NegativeX, NegativeY, NegativeZ,
        CustomAxis // Пользовательская ось
    }
    
    [Header("Пользовательская Ось")]
    [Tooltip("Если выбрана CustomAxis, укажите локальный вектор оси вращения.")]
    public Vector3 customAxis = Vector3.forward;
    
    [Header("Диапазон Значений")]
    [Tooltip("Минимальное значение параметра.")]
    public float minValue = -60f;
    [Tooltip("Максимальное значение параметра.")]
    public float maxValue = 0f;
    [Tooltip("Текущее значение (только для чтения).")]
    [SerializeField]
    private float currentValue;
    
    [Header("Ограничения Вращения")]
    [Tooltip("Минимальный угол поворота ручки.")]
    public float minAngle = -150f;
    [Tooltip("Максимальный угол поворота ручки.")]
    public float maxAngle = 150f;
    
    [Header("Чувствительность")]
    [Tooltip("Чувствительность вращения мышью.")]
    public float sensitivity = 1f;
    
    [Header("Обратная Связь")]
    [Tooltip("Визуальная часть ручки (должна вращаться). Если не указана, вращается сам объект.")]
    public Transform knobVisual;
    
    [Header("События")]
    [Tooltip("Вызывается при изменении значения.")]
    public UnityEvent<float> onValueChanged;
    
    // Приватные переменные
    private float currentAngle = 0f;
    private bool isDragging = false;
    private Vector3 lastMousePosition;
    private Transform rotationTarget;
    private Quaternion initialRotation; // Сохраняем начальный поворот
    
    // Свойство для доступа к текущему значению
    public float CurrentValue
    {
        get { return currentValue; }
        private set
        {
            currentValue = Mathf.Clamp(value, minValue, maxValue);
        }
    }
    
    void Start()
    {
        // Определяем, что будет вращаться
        rotationTarget = (knobVisual != null) ? knobVisual : transform;
        
        // Сохраняем начальный поворот
        initialRotation = rotationTarget.localRotation;
        
        // Устанавливаем начальное значение
        SetValue((minValue + maxValue) * 0.5f);
    }
    
    void Update()
    {
        // Обработка ввода мышью
        HandleMouseInput();
        
        // Отладка: вращение колесиком мыши
        HandleScrollWheel();
    }
    
    /// <summary>
    /// Обработка перетаскивания мыши
    /// </summary>
    void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // Проверяем, кликнули ли по ручке
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit))
            {
                if (hit.transform == transform || hit.transform.IsChildOf(transform))
                {
                    isDragging = true;
                    lastMousePosition = Input.mousePosition;
                }
            }
        }
        
        if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }
        
        if (isDragging)
        {
            Vector3 mouseDelta = Input.mousePosition - lastMousePosition;
            
            // Определяем направление вращения в зависимости от оси
            float rotationDelta = GetRotationDelta(mouseDelta);
            
            if (Mathf.Abs(rotationDelta) > 0.001f)
            {
                RotateKnob(rotationDelta * sensitivity);
            }
            
            lastMousePosition = Input.mousePosition;
        }
    }
    
    /// <summary>
    /// Вычисляет величину вращения на основе движения мыши и выбранной оси
    /// </summary>
    float GetRotationDelta(Vector3 mouseDelta)
    {
        switch (rotationAxis)
        {
            case RotationAxis.X:
            case RotationAxis.NegativeX:
            case RotationAxis.CustomAxis:
                return mouseDelta.y; // Вертикальное движение для этих осей
                
            case RotationAxis.Y:
            case RotationAxis.NegativeY:
            case RotationAxis.Z:
            case RotationAxis.NegativeZ:
                return mouseDelta.x; // Горизонтальное движение
                
            default:
                return mouseDelta.x;
        }
    }
    
    /// <summary>
    /// Вращение колесиком мыши для точной настройки
    /// </summary>
    void HandleScrollWheel()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.001f)
        {
            // Проверяем, наведена ли мышь на ручку
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit))
            {
                if (hit.transform == transform || hit.transform.IsChildOf(transform))
                {
                    RotateKnob(scroll * 10f * sensitivity);
                }
            }
        }
    }
    
    /// <summary>
    /// Вращает ручку на заданный угол
    /// </summary>
    public void RotateKnob(float angleDelta)
    {
        // Определяем направление вращения
        float direction = 1f;
        if (rotationAxis == RotationAxis.NegativeX || 
            rotationAxis == RotationAxis.NegativeY || 
            rotationAxis == RotationAxis.NegativeZ)
        {
            direction = -1f;
        }
        
        // Применяем изменение угла
        float newAngle = currentAngle + (angleDelta * direction);
        newAngle = Mathf.Clamp(newAngle, minAngle, maxAngle);
        
        if (Mathf.Abs(newAngle - currentAngle) > 0.001f)
        {
            currentAngle = newAngle;
            
            // Применяем вращение
            ApplyRotation();
            
            // Обновляем значение
            UpdateValue();
        }
    }
    
    /// <summary>
    /// Применяет вращение к трансформу относительно начального положения
    /// </summary>
    void ApplyRotation()
    {
        // Создаем кватернион вращения вокруг нужной оси
        Quaternion rotationOffset;
        
        switch (rotationAxis)
        {
            case RotationAxis.X:
                rotationOffset = Quaternion.AngleAxis(currentAngle, Vector3.right);
                break;
                
            case RotationAxis.NegativeX:
                rotationOffset = Quaternion.AngleAxis(-currentAngle, Vector3.right);
                break;
                
            case RotationAxis.Y:
                rotationOffset = Quaternion.AngleAxis(currentAngle, Vector3.up);
                break;
                
            case RotationAxis.NegativeY:
                rotationOffset = Quaternion.AngleAxis(-currentAngle, Vector3.up);
                break;
                
            case RotationAxis.Z:
                rotationOffset = Quaternion.AngleAxis(currentAngle, Vector3.forward);
                break;
                
            case RotationAxis.NegativeZ:
                rotationOffset = Quaternion.AngleAxis(-currentAngle, Vector3.forward);
                break;
                
            case RotationAxis.CustomAxis:
                rotationOffset = Quaternion.AngleAxis(currentAngle, customAxis.normalized);
                break;
                
            default:
                rotationOffset = Quaternion.identity;
                break;
        }
        
        // Применяем вращение ОТНОСИТЕЛЬНО начального положения
        rotationTarget.localRotation = initialRotation * rotationOffset;
    }
    
    /// <summary>
    /// Обновляет значение параметра на основе угла поворота
    /// </summary>
    void UpdateValue()
    {
        // Маппим угол на диапазон значений
        float t = (currentAngle - minAngle) / (maxAngle - minAngle);
        CurrentValue = Mathf.Lerp(minValue, maxValue, t);
        
        // Вызываем событие
        onValueChanged.Invoke(CurrentValue);
    }
    
    /// <summary>
    /// Устанавливает значение напрямую (для инициализации)
    /// </summary>
    public void SetValue(float value)
    {
        CurrentValue = Mathf.Clamp(value, minValue, maxValue);
        
        // Маппим значение на угол
        float t = (CurrentValue - minValue) / (maxValue - minValue);
        currentAngle = Mathf.Lerp(minAngle, maxAngle, t);
        
        ApplyRotation();
    }
    
    /// <summary>
    /// Устанавливает значение по нормализованному параметру (0-1)
    /// </summary>
    public void SetNormalizedValue(float normalizedValue)
    {
        SetValue(Mathf.Lerp(minValue, maxValue, normalizedValue));
    }
    
    /// <summary>
    /// Получает нормализованное значение (0-1)
    /// </summary>
    public float GetNormalizedValue()
    {
        return (CurrentValue - minValue) / (maxValue - minValue);
    }
    
    /// <summary>
    /// Сбрасывает ручку в начальное положение
    /// </summary>
    public void ResetKnob()
    {
        currentAngle = 0f;
        rotationTarget.localRotation = initialRotation;
        UpdateValue();
    }
    
    /// <summary>
    /// Визуализация в редакторе
    /// </summary>
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.1f);
        
        // Показываем ось вращения
        Vector3 axisDirection = GetAxisVector();
        
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, axisDirection * 0.2f);
        Gizmos.DrawRay(transform.position, -axisDirection * 0.2f);
        
        // Показываем начальное положение
        if (Application.isPlaying && rotationTarget != null)
        {
            Gizmos.color = Color.green;
            Vector3 initialDirection = initialRotation * axisDirection;
            Gizmos.DrawRay(transform.position, initialDirection * 0.15f);
        }
    }
    
    /// <summary>
    /// Возвращает вектор оси вращения
    /// </summary>
    Vector3 GetAxisVector()
    {
        switch (rotationAxis)
        {
            case RotationAxis.X:
            case RotationAxis.NegativeX:
                return Vector3.right;
            case RotationAxis.Y:
            case RotationAxis.NegativeY:
                return Vector3.up;
            case RotationAxis.Z:
            case RotationAxis.NegativeZ:
                return Vector3.forward;
            case RotationAxis.CustomAxis:
                return customAxis.normalized;
            default:
                return Vector3.forward;
        }
    }
}