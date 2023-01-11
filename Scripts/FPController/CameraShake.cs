using System.Collections;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance;

    [SerializeField, Range(0, 10)] private float _duration = 1f;
    [SerializeField, Range(0, 2)] private float _strengthMultiplier = 0.2f;
    [SerializeField] private AnimationCurve _curve;

    public bool IsShaking;

    // Setup the curve, remove to change to another curve
    private void OnValidate() => CurveSetup();

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public static void Shake(float duration, float strengthMultiplier) => Instance.StartCoroutine(Instance.ShakeCoroutine(duration, strengthMultiplier));

    private IEnumerator ShakeCoroutine(float duration, float strengthMultiplier)
    {
        // Cache the position
        Vector3 originalPos = transform.localPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            IsShaking = true;
            // Get the strength value from curve
            float strengthOverTime = _curve.Evaluate(elapsed / duration);
            // Calculate a random position based on the start position, a random point in a unit sphere and the strength
            transform.localPosition = originalPos + Random.insideUnitSphere * strengthOverTime * strengthMultiplier;
            elapsed += Time.deltaTime;
            yield return null;
            IsShaking = false;
        }

        transform.localPosition = originalPos;
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
        _curve.keys = keyframes;
    }

#if UNITY_EDITOR
    [ContextMenu("Shake")]
    public void TestShake() => Shake(_duration, _strengthMultiplier);
#endif
}
