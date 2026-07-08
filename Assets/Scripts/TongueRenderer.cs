using UnityEngine;

/// <summary>
/// TongueRenderer — Configura el LineRenderer de la lengua automáticamente.
/// 
/// Attach: mismo GameObject que TongueGrab.
/// No requiere ningún material asignado — crea uno en runtime, detectando
/// si el proyecto usa Built-in Render Pipeline o URP/HDRP (Scriptable RP)
/// para elegir un shader que sí se vea en ambos casos.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
[RequireComponent(typeof(TongueGrab))]
public class TongueRenderer : MonoBehaviour
{
    [Header("Forma")]
    [Tooltip("Grosor en la base (boca).")]
    [SerializeField] private float widthBase = 0.10f;
    [Tooltip("Grosor en la punta.")]
    [SerializeField] private float widthTip  = 0.04f;

    [Header("Color")]
    [SerializeField] private Color colorBase = new Color(0.85f, 0.18f, 0.25f, 1f); // rojo oscuro en base
    [SerializeField] private Color colorMid  = new Color(0.95f, 0.35f, 0.45f, 1f); // rosa en el medio
    [SerializeField] private Color colorTip  = new Color(1.00f, 0.55f, 0.60f, 1f); // rosa claro en punta

    [Header("Orden de dibujo")]
    [Tooltip("Sorting Layer donde se dibuja la lengua. Debe existir en Project Settings > Tags and Layers > Sorting Layers.")]
    [SerializeField] private string sortingLayerName = "Default";
    [Tooltip("Orden dentro del layer. Si el sprite de tu personaje usa un order alto (ej. 10, 50), poné acá un número MAYOR para que la lengua no quede tapada detrás del cuerpo.")]
    [SerializeField] private int    sortingOrder     = 100;

    [Header("Tip sprite (opcional)")]
    [Tooltip("Si asignas un sprite aquí se dibuja un círculo en la punta de la lengua.")]
    [SerializeField] private GameObject tipDecorPrefab;

    // ── Privado ──────────────────────────────────────────────────────
    private LineRenderer _line;
    private TongueGrab   _tongue;
    private GameObject   _tipDecor;

    // ── Lifecycle ────────────────────────────────────────────────────

    private void Awake()
    {
        _line   = GetComponent<LineRenderer>();
        _tongue = GetComponent<TongueGrab>();

        SetupLineRenderer();

        if (tipDecorPrefab != null)
            _tipDecor = Instantiate(tipDecorPrefab, transform);
    }

    private void LateUpdate()
    {
        // Punta decorativa sigue al último punto de la lengua
        if (_tipDecor != null && _line.enabled && _line.positionCount > 0)
        {
            Vector3 tip = _line.GetPosition(_line.positionCount - 1);
            _tipDecor.transform.position = tip;
            _tipDecor.SetActive(_line.enabled);
        }
        else if (_tipDecor != null)
        {
            _tipDecor.SetActive(false);
        }
    }

    // ── Setup ─────────────────────────────────────────────────────────

    private void SetupLineRenderer()
    {
        // ── Material ─────────────────────────────────────────────────
        Shader shader = FindBestTongueShader();

        if (shader != null)
        {
            Material mat   = new Material(shader);
            if (mat.HasProperty("_Color")) mat.color = colorMid;
            _line.material = mat;
        }
        else
        {
            Debug.LogWarning("[TongueRenderer] DIAGNÓSTICO: no se encontró NINGÚN " +
                "shader compatible (ni Built-in ni URP). Por eso no se ve la lengua. " +
                "Asigna manualmente un material al LineRenderer en el Inspector.");
        }

        // ── Ancho con curva (gruesa en base, delgada en punta) ────────
        AnimationCurve widthCurve = new AnimationCurve(
            new Keyframe(0f, widthBase),   // base
            new Keyframe(0.6f, widthBase * 0.75f),
            new Keyframe(1f, widthTip)     // punta
        );
        _line.widthCurve        = widthCurve;
        _line.widthMultiplier   = 1f;

        // ── Gradiente de color ────────────────────────────────────────
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(colorBase, 0.00f),
                new GradientColorKey(colorMid,  0.45f),
                new GradientColorKey(colorTip,  1.00f),
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 0.85f),
                new GradientAlphaKey(1f, 1f),   // antes 0.6 (semi-transparente) — subido a 1
            }
        );
        _line.colorGradient = gradient;

        // ── Otras propiedades ─────────────────────────────────────────
        _line.useWorldSpace     = true;
        _line.textureMode       = LineTextureMode.Stretch;
        _line.numCornerVertices = 5;   // esquinas suaves
        _line.numCapVertices    = 5;   // extremos redondeados (punta redondeada)
        _line.alignment         = LineAlignment.TransformZ; // más predecible en 2D que View
        _line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _line.receiveShadows    = false;

        // Sorting: se aplica SIEMPRE lo que configures acá arriba (ya no es
        // condicional), justamente porque un sorting incorrecto — la lengua
        // dibujándose detrás del sprite del cuerpo — es la causa más común
        // de "no se ve" cuando el material y el gradiente sí están bien.
        _line.sortingLayerName = sortingLayerName;
        _line.sortingOrder     = sortingOrder;

        Debug.Log($"[TongueRenderer] DIAGNÓSTICO: material={(_line.sharedMaterial != null ? _line.sharedMaterial.shader.name : "NINGUNO")}, " +
                  $"sortingLayer='{_line.sortingLayerName}', order={_line.sortingOrder}, " +
                  $"enabled={_line.enabled}, positionCount={_line.positionCount}. " +
                  "Si el sprite de tu personaje usa un 'Order in Layer' igual o mayor a este número, " +
                  "la lengua queda tapada detrás del cuerpo — subí 'sortingOrder' en el Inspector de TongueRenderer.");
    }

    /// <summary>
    /// Elige un shader que renderice tanto en Built-in Render Pipeline como en
    /// URP/HDRP (Scriptable Render Pipeline). Usar solo "Sprites/Default" puede
    /// dejar la lengua invisible en algunos setups de URP sin dar ningún error.
    /// </summary>
    private Shader FindBestTongueShader()
    {
        bool usingSRP = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null;

        if (usingSRP)
        {
            Shader urpShader =
                   Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default")
                ?? Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");

            if (urpShader != null) return urpShader;
        }

        return Shader.Find("Sprites/Default")
            ?? Shader.Find("Unlit/Color")
            ?? Shader.Find("Legacy Shaders/Particles/Alpha Blended");
    }

    // ── Cuando cambias valores en el Inspector durante Play Mode ──────
    private void OnValidate()
    {
        if (_line == null) return;
        SetupLineRenderer();
    }
}
