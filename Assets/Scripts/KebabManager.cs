using UnityEngine;
using System.Collections;

public class KebabManager : MonoBehaviour
{
    [Header("Объекты шашлыков")]
    public GameObject[] allKebabs;
    
    [Header("Настройки переворота")]
    public float flipDuration = 0.5f;
    public float flipAngle = 180f;
    public AnimationCurve flipCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    
    [Header("Настройки вращения")]
    public bool enableRotation = true;
    public RotationAxis rotationAxis = RotationAxis.Y;
    public Space rotationSpace = Space.Self;
    public float rotationSpeed = 100f;
    public float rotationDuration = 0f;
    public float rotationStep = 45f;
    
    public enum RotationAxis { X, Y, Z, Custom }
    
    [Header("Пользовательская ось (только для Custom)")]
    public Vector3 customAxis = Vector3.up;
    
    [Header("Эффекты")]
    public ParticleSystem globalFlipParticles;
    public AudioClip flipSound;
    public GameObject flipEffect;
    
    [Header("Doneness Materials (прожарка)")]
    public Material[] donenessMaterials; // 5 материалов: сырое, недожар, средне, хорошо, готово
    
    private bool isFlipping = false;
    private bool isSideA = true;
    private bool isRotating = false;
    private bool isStepRotating = false;
    private float rotationTimer = 0f;
    
    public float sideADoneness = 0f;
    public float sideBDoneness = 0f;
    public float currentHeat = 0f;
    
    private float flipTimer = 0f;
    private Quaternion[] startRotations;
    private Quaternion[] targetRotations;
    private Vector3[] originalPositions;
    private Rigidbody[] rigidbodies;
    
    public int totalFlips = 0;
    private AudioSource audioSource;
    
    void Start()
    {
        InitializeKebabs();
        SetDonenessLevel(0); // начальная текстура (сырое)
    }
    
    void InitializeKebabs()
    {
        if (allKebabs == null || allKebabs.Length == 0)
        {
            allKebabs = new GameObject[4];
            for (int i = 0; i < 4; i++)
            {
                allKebabs[i] = GameObject.Find("kebab" + (i + 1));
                if (allKebabs[i] == null)
                    Debug.LogWarning($"kebab{i + 1} не найден!");
            }
        }
        
        startRotations = new Quaternion[allKebabs.Length];
        targetRotations = new Quaternion[allKebabs.Length];
        originalPositions = new Vector3[allKebabs.Length];
        rigidbodies = new Rigidbody[allKebabs.Length];
        
        for (int i = 0; i < allKebabs.Length; i++)
        {
            if (allKebabs[i] != null)
            {
                startRotations[i] = allKebabs[i].transform.rotation;
                originalPositions[i] = allKebabs[i].transform.position;
                targetRotations[i] = startRotations[i] * Quaternion.Euler(flipAngle, 0, 0);
                
                Rigidbody rb = allKebabs[i].GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = true;
                    rb.useGravity = false;
                    rigidbodies[i] = rb;
                }
            }
        }
        
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;
        Debug.Log("🍖 KebabManager готов");
    }
    
    void Update()
    {
        if (isFlipping) AnimateFlip();
        if (isRotating && enableRotation && !isStepRotating) ContinuousRotate();
    }
    
    void FixedUpdate()
    {
        for (int i = 0; i < allKebabs.Length; i++)
        {
            if (allKebabs[i] != null && !isFlipping && !isRotating && !isStepRotating)
            {
                allKebabs[i].transform.position = originalPositions[i];
                if (rigidbodies[i] != null)
                {
                    rigidbodies[i].linearVelocity = Vector3.zero;
                    rigidbodies[i].angularVelocity = Vector3.zero;
                }
            }
        }
    }
    
    Vector3 GetRotationAxis()
    {
        switch (rotationAxis)
        {
            case RotationAxis.X: return Vector3.right;
            case RotationAxis.Y: return Vector3.up;
            case RotationAxis.Z: return Vector3.forward;
            case RotationAxis.Custom: return customAxis.normalized;
            default: return Vector3.up;
        }
    }
    
    void ContinuousRotate()
    {
        if (rotationDuration > 0)
        {
            rotationTimer += Time.deltaTime;
            if (rotationTimer >= rotationDuration) { StopRotation(); return; }
        }
        
        Vector3 axis = GetRotationAxis();
        for (int i = 0; i < allKebabs.Length; i++)
        {
            if (allKebabs[i] != null)
            {
                Vector3 savedPos = allKebabs[i].transform.position;
                allKebabs[i].transform.Rotate(axis * rotationSpeed * Time.deltaTime, rotationSpace);
                allKebabs[i].transform.position = savedPos;
                originalPositions[i] = allKebabs[i].transform.position;
                startRotations[i] = allKebabs[i].transform.rotation;
                if (rigidbodies[i] != null)
                {
                    rigidbodies[i].linearVelocity = Vector3.zero;
                    rigidbodies[i].angularVelocity = Vector3.zero;
                }
            }
        }
    }
    
    public void PerformStepRotation(float deltaAngle)
    {
        if (!enableRotation || isFlipping) return;
        if (isRotating) StopRotation();
        if (isStepRotating) return;
        StartCoroutine(StepRotationCoroutine(deltaAngle));
    }
    
    private IEnumerator StepRotationCoroutine(float deltaAngle)
    {
        isStepRotating = true;
        float[] startAngles = new float[allKebabs.Length];
        float[] targetAngles = new float[allKebabs.Length];
        Vector3[] savedPositions = new Vector3[allKebabs.Length];
        
        for (int i = 0; i < allKebabs.Length; i++)
        {
            if (allKebabs[i] != null)
            {
                startAngles[i] = GetCurrentAngle(allKebabs[i].transform);
                targetAngles[i] = NormalizeAngle(startAngles[i] + deltaAngle);
                savedPositions[i] = allKebabs[i].transform.position;
                originalPositions[i] = savedPositions[i];
            }
        }
        
        float elapsed = 0f;
        float animDuration = 0.3f;
        while (elapsed < animDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / animDuration);
            for (int i = 0; i < allKebabs.Length; i++)
            {
                if (allKebabs[i] != null)
                {
                    float curr = Mathf.LerpAngle(startAngles[i], targetAngles[i], t);
                    SetTargetAngle(allKebabs[i].transform, curr);
                    allKebabs[i].transform.position = savedPositions[i];
                }
            }
            yield return null;
        }
        
        for (int i = 0; i < allKebabs.Length; i++)
        {
            if (allKebabs[i] != null)
            {
                SetTargetAngle(allKebabs[i].transform, targetAngles[i]);
                allKebabs[i].transform.position = savedPositions[i];
                originalPositions[i] = allKebabs[i].transform.position;
                startRotations[i] = allKebabs[i].transform.rotation;
            }
        }
        isStepRotating = false;
    }
    
    float GetCurrentAngle(Transform obj)
    {
        Vector3 axis = GetRotationAxis();
        bool isSelf = rotationSpace == Space.Self;
        if (rotationAxis == RotationAxis.X || (rotationAxis == RotationAxis.Custom && axis == Vector3.right))
            return isSelf ? obj.localEulerAngles.x : obj.eulerAngles.x;
        if (rotationAxis == RotationAxis.Y || (rotationAxis == RotationAxis.Custom && axis == Vector3.up))
            return isSelf ? obj.localEulerAngles.y : obj.eulerAngles.y;
        if (rotationAxis == RotationAxis.Z || (rotationAxis == RotationAxis.Custom && axis == Vector3.forward))
            return isSelf ? obj.localEulerAngles.z : obj.eulerAngles.z;
        return isSelf ? obj.localEulerAngles.y : obj.eulerAngles.y;
    }
    
    void SetTargetAngle(Transform obj, float angle)
    {
        Vector3 axis = GetRotationAxis();
        if (rotationSpace == Space.Self)
        {
            Vector3 euler = obj.localEulerAngles;
            if (rotationAxis == RotationAxis.X || (rotationAxis == RotationAxis.Custom && axis == Vector3.right)) euler.x = angle;
            else if (rotationAxis == RotationAxis.Y || (rotationAxis == RotationAxis.Custom && axis == Vector3.up)) euler.y = angle;
            else if (rotationAxis == RotationAxis.Z || (rotationAxis == RotationAxis.Custom && axis == Vector3.forward)) euler.z = angle;
            else euler.y = angle;
            obj.localEulerAngles = euler;
        }
        else
        {
            Vector3 euler = obj.eulerAngles;
            if (rotationAxis == RotationAxis.X || (rotationAxis == RotationAxis.Custom && axis == Vector3.right)) euler.x = angle;
            else if (rotationAxis == RotationAxis.Y || (rotationAxis == RotationAxis.Custom && axis == Vector3.up)) euler.y = angle;
            else if (rotationAxis == RotationAxis.Z || (rotationAxis == RotationAxis.Custom && axis == Vector3.forward)) euler.z = angle;
            else euler.y = angle;
            obj.eulerAngles = euler;
        }
    }
    
    void AnimateFlip()
    {
        flipTimer += Time.deltaTime;
        float progress = flipTimer / flipDuration;
        float curved = flipCurve.Evaluate(Mathf.Clamp01(progress));
        for (int i = 0; i < allKebabs.Length; i++)
        {
            if (allKebabs[i] != null)
            {
                allKebabs[i].transform.rotation = Quaternion.Slerp(startRotations[i], targetRotations[i], curved);
                allKebabs[i].transform.position = originalPositions[i];
                allKebabs[i].transform.position += Vector3.up * Mathf.Sin(curved * Mathf.PI) * 0.1f;
            }
        }
        if (progress >= 1f) CompleteFlip();
    }
    
    void CompleteFlip()
    {
        isFlipping = false;
        for (int i = 0; i < allKebabs.Length; i++)
        {
            if (allKebabs[i] != null)
            {
                allKebabs[i].transform.rotation = targetRotations[i];
                allKebabs[i].transform.position = originalPositions[i];
                startRotations[i] = targetRotations[i];
                targetRotations[i] = startRotations[i] * Quaternion.Euler(flipAngle, 0, 0);
                if (rigidbodies[i] != null)
                {
                    rigidbodies[i].linearVelocity = Vector3.zero;
                    rigidbodies[i].angularVelocity = Vector3.zero;
                }
            }
        }
        Debug.Log($"✅ Переворот #{totalFlips} завершен! Сторона: {(isSideA ? "A" : "B")}");
    }
    

    public void SetDonenessLevel(int level)
	{
		
        foreach (GameObject kebab in allKebabs)
        {
            Renderer[] renderers = kebab.GetComponentsInChildren<Renderer>();
            Renderer parentRenderer = kebab.GetComponent<Renderer>();


            foreach (Renderer renderer in renderers)
            {
                if (parentRenderer == renderer) {continue;}
                renderer.material = donenessMaterials[level];
            }
        }

	}
    
    // ===== ПУБЛИЧНЫЕ МЕТОДЫ =====
    public void FlipAllKebabs()
    {
        if (isFlipping) return;
        isFlipping = true;
        flipTimer = 0f;
        totalFlips++;
        isSideA = !isSideA;
        if (flipSound && audioSource) audioSource.PlayOneShot(flipSound);
        if (globalFlipParticles) globalFlipParticles.Play();
        if (flipEffect) StartCoroutine(ShowFlipEffect());
        Debug.Log($"🔄 Переворот шашлыков! Всего: {totalFlips}");
    }
    
    public void StartRotation()
    {
        if (!enableRotation || isFlipping) return;
        if (isStepRotating) StopStepRotation();
        isRotating = true;
        rotationTimer = 0f;
        for (int i = 0; i < allKebabs.Length; i++)
            if (allKebabs[i] != null)
            {
                originalPositions[i] = allKebabs[i].transform.position;
                startRotations[i] = allKebabs[i].transform.rotation;
            }
        Debug.Log($"🔄 Вращение запущено вокруг {rotationAxis}");
    }
    
    public void StopRotation()
    {
        isRotating = false;
        for (int i = 0; i < allKebabs.Length; i++)
            if (allKebabs[i] != null)
            {
                originalPositions[i] = allKebabs[i].transform.position;
                startRotations[i] = allKebabs[i].transform.rotation;
            }
        Debug.Log($"⏹ Вращение остановлено");
    }
    
    public void StopStepRotation() { StopAllCoroutines(); isStepRotating = false; }
    public void ToggleRotation()
    {
        if (isRotating || isStepRotating) { StopRotation(); StopStepRotation(); }
        else PerformStepRotation(rotationStep);
    }
    public void StartStepRotation() { PerformStepRotation(rotationStep); }
    public void SetRotationAxis(RotationAxis newAxis) { rotationAxis = newAxis; }
    public void SetCustomAxis(Vector3 axis) { customAxis = axis.normalized; rotationAxis = RotationAxis.Custom; }
    public void SetRotationSpace(Space space) { rotationSpace = space; }
    public void SetRotationSpeed(float speed) { rotationSpeed = speed; }
    public void SetRotationStep(float step) { rotationStep = step; }
    public bool IsRotating() { return isRotating || isStepRotating; }
    public bool IsStepRotating() { return isStepRotating; }
    public bool IsContinuousRotating() { return isRotating; }
    
    private float NormalizeAngle(float angle) { angle %= 360f; if (angle < 0) angle += 360f; return angle; }
    
    IEnumerator ShowFlipEffect()
    {
        if (flipEffect != null)
        {
            flipEffect.SetActive(true);
            yield return new WaitForSeconds(0.3f);
            flipEffect.SetActive(false);
        }
    }
    
    public float CalculateJuiciness()
    {
        float diff = Mathf.Abs(sideADoneness - sideBDoneness);
        float avg = (sideADoneness + sideBDoneness) / 2f;
        float j = 1f - diff * 2f;
        if (avg > 0.8f) j -= (avg - 0.8f) * 3f;
        return Mathf.Clamp01(j);
    }
    
    public int GetDonenessStage()
    {
        float avg = (sideADoneness + sideBDoneness) / 2f;
        if (avg < 0.2f) return 0;
        if (avg < 0.4f) return 1;
        if (avg < 0.6f) return 2;
        if (avg < 0.8f) return 3;
        return 4;
    }
    
    public void SetHeat(float heat) { currentHeat = heat; }
    
    void OnDrawGizmos()
    {
        if (allKebabs == null) return;
        foreach (var kebab in allKebabs)
        {
            if (kebab == null) continue;
            Gizmos.color = isSideA ? new Color(1f, 1f, 0f, 0.5f) : new Color(0f, 1f, 1f, 0.5f);
            Gizmos.DrawWireCube(kebab.transform.position, kebab.transform.localScale * 1.2f);
            Vector3 axis = GetRotationAxis();
            Vector3 worldAxis = rotationSpace == Space.Self ? kebab.transform.TransformDirection(axis) : axis;
            Gizmos.color = Color.green;
            Gizmos.DrawRay(kebab.transform.position, worldAxis * 0.5f);
            Gizmos.DrawRay(kebab.transform.position, -worldAxis * 0.2f);
            Gizmos.color = Color.yellow;
            Vector3 perp = Vector3.Cross(worldAxis, Vector3.up).normalized;
            if (perp.magnitude < 0.1f) perp = Vector3.Cross(worldAxis, Vector3.right).normalized;
            Gizmos.DrawRay(kebab.transform.position, perp * 0.3f);
        }
    }
}