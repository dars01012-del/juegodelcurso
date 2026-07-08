using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// PostFXSetup — Arma un Volumen global de post-procesado para URP por código,
/// sin necesitar crear ningún VolumeProfile a mano en el Editor.
///
/// Attach: a cualquier GameObject de la escena (uno vacío llamado "PostFX" está bien).
///
/// Requisitos de una sola vez en el proyecto (esto sí es manual, en el Editor):
/// 1) El asset "Renderer" de tu URP (Universal Renderer Data) tiene que tener
///    tildado "Post-processing" — normalmente ya viene tildado por defecto.
/// 2) La Camera principal necesita, en su componente Camera (sección Rendering),
///    "Post Processing" tildado. Este script también lo fuerza por código, pero
///    si no aparece el efecto, revisalo ahí como respaldo.
/// </summary>
public class PostFXSetup : MonoBehaviour
{
    [Header("Cámara")]
    [Tooltip("Si lo dejás vacío, usa Camera.main.")]
    [SerializeField] private Camera targetCamera;

    [Header("Bloom (brillo en las zonas más claras)")]
    [SerializeField] private bool  enableBloom     = true;
    [SerializeField] [Range(0f, 10f)] private float bloomIntensity  = 1.2f;
    [SerializeField] [Range(0f, 1f)]  private float bloomThreshold  = 0.9f;

    [Header("Viñeta (oscurece los bordes de la pantalla)")]
    [SerializeField] private bool  enableVignette      = true;
    [SerializeField] [Range(0f, 1f)] private float vignetteIntensity = 0.25f;
    [SerializeField] private Color vignetteColor        = Color.black;

    [Header("Color / saturación")]
    [SerializeField] private bool  enableColorAdjustments = true;
    [SerializeField] [Range(-100f, 100f)] private float saturation = 15f;
    [SerializeField] [Range(-100f, 100f)] private float contrast   = 5f;

    [Header("Aberración cromática (efecto glitch/impacto)")]
    [Tooltip("Suele quedar mejor activarla solo en momentos puntuales (golpes, dash) en vez de siempre.")]
    [SerializeField] private bool  enableChromaticAberration = false;
    [SerializeField] [Range(0f, 1f)] private float chromaticIntensity = 0.15f;

    private VolumeProfile _profile;
    private Bloom               _bloom;
    private Vignette             _vignette;
    private ColorAdjustments     _colorAdjustments;
    private ChromaticAberration  _chromaticAberration;

    private void Awake()
    {
        if (targetCamera == null) targetCamera = Camera.main;

        SetupCameraPostProcessing();
        SetupGlobalVolume();
    }

    private void SetupCameraPostProcessing()
    {
        if (targetCamera == null)
        {
            Debug.LogWarning("[PostFXSetup] No encontré ninguna cámara (Camera.main es null). " +
                "Asignala manualmente en el campo 'Target Camera'.");
            return;
        }

        var camData = targetCamera.GetUniversalAdditionalCameraData();
        if (camData != null)
            camData.renderPostProcessing = true;
    }

    private void SetupGlobalVolume()
    {
        GameObject volumeGO = new GameObject("Global Volume (PostFX)");
        volumeGO.transform.SetParent(transform, false);

        Volume volume  = volumeGO.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.weight    = 1f;
        volume.priority  = 0f;

        _profile          = ScriptableObject.CreateInstance<VolumeProfile>();
        volume.sharedProfile = _profile;

        if (enableBloom)
        {
            _bloom = _profile.Add<Bloom>(true);
            _bloom.intensity.overrideState = true;
            _bloom.intensity.value         = bloomIntensity;
            _bloom.threshold.overrideState = true;
            _bloom.threshold.value         = bloomThreshold;
        }

        if (enableVignette)
        {
            _vignette = _profile.Add<Vignette>(true);
            _vignette.intensity.overrideState = true;
            _vignette.intensity.value         = vignetteIntensity;
            _vignette.color.overrideState     = true;
            _vignette.color.value             = vignetteColor;
        }

        if (enableColorAdjustments)
        {
            _colorAdjustments = _profile.Add<ColorAdjustments>(true);
            _colorAdjustments.saturation.overrideState = true;
            _colorAdjustments.saturation.value         = saturation;
            _colorAdjustments.contrast.overrideState   = true;
            _colorAdjustments.contrast.value           = contrast;
        }

        if (enableChromaticAberration)
        {
            _chromaticAberration = _profile.Add<ChromaticAberration>(true);
            _chromaticAberration.intensity.overrideState = true;
            _chromaticAberration.intensity.value         = chromaticIntensity;
        }
    }

    // ── API pública para efectos puntuales (golpes, dash, etc.) ───────

    /// <summary>Sube la aberración cromática momentáneamente (ej: al recibir daño).</summary>
    public void PulseChromaticAberration(float peakIntensity, float duration)
    {
        if (_chromaticAberration == null) return;
        StopAllCoroutines();
        StartCoroutine(PulseRoutine(peakIntensity, duration));
    }

    private System.Collections.IEnumerator PulseRoutine(float peak, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            _chromaticAberration.intensity.value = Mathf.Lerp(peak, chromaticIntensity, t / duration);
            yield return null;
        }
        _chromaticAberration.intensity.value = chromaticIntensity;
    }
}
