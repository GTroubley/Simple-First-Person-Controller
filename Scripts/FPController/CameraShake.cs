using System.Collections;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance;

    [SerializeField, Range(0, 10)] private float _duration = 1f;
    [SerializeField, Range(0, 2)] private float _strengthMultiplier = 0.2f;
    [SerializeField] private AnimationCurve _curve;

    public bool IsShaking;

    private Vector3 _originalPos;
    private Coroutine _shakeCoroutine;

    // Setup the default curve only when there is none, so a curve edited in the inspector is kept
    private void OnValidate()
    {
        if (_curve == null || _curve.length == 0)
            CurveSetup();
    }

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(this);      // Remove only the duplicate component, not the camera it's attached to

        // Store the original local position once, so overlapping shakes cant store an already shaken position
        _originalPos = transform.localPosition;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public static void Shake(float duration, float strengthMultiplier)
    {
        if (Instance == null)
        {
            Debug.LogWarning("CameraShake: there is no CameraShake in the scene");
            return;
        }

        // If a shake is already running, stop it and start the new one from the original position
        if (Instance._shakeCoroutine != null)
            Instance.StopCoroutine(Instance._shakeCoroutine);
        Instance._shakeCoroutine = Instance.StartCoroutine(Instance.ShakeCoroutine(duration, strengthMultiplier));
    }

    private IEnumerator ShakeCoroutine(float duration, float strengthMultiplier)
    {
        float elapsed = 0f;
        IsShaking = true;

        // Loop until the total shake duration has passed
        while (elapsed < duration)
        {
            // Calculate shake offset based on curve intensity over time and apply it to the position and then update elapsed time
            float strengthOverTime = _curve.Evaluate(elapsed / duration);
            transform.localPosition = _originalPos + Random.insideUnitSphere * strengthOverTime * strengthMultiplier;
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = _originalPos;
        IsShaking = false;
        _shakeCoroutine = null;
    }

    [ContextMenu("Curve Setup")]
    private void CurveSetup()
    {
        Keyframe[] keyframes = new Keyframe[3];
        // First keyframe, values 0,0
        keyframes[0].time = 0f;
        keyframes[0].value = 0f;
        // Second keyframe, values 0.2,0.25
        keyframes[1].time = 0.2f;
        keyframes[1].value = 0.25f;
        // Third keyframe, values 1,0
        keyframes[2].time = 1f;
        keyframes[2].value = 0f;
        // Adjusting the of the curve for the second keyframe
        keyframes[1].inTangent = -0.01f;
        keyframes[1].inWeight = -0.3f;
        _curve ??= new AnimationCurve();
        _curve.keys = keyframes;
    }

#if UNITY_EDITOR
    [ContextMenu("Shake")]
    public void TestShake() => Shake(_duration, _strengthMultiplier);
#endif
}
