using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class RadialBeatIndicatorGraphic : MaskableGraphic
{
    [SerializeField] [Range(0f, 1f)] private float progress;
    [SerializeField] [Range(0.05f, 0.45f)] private float ringThickness = 0.18f;
    [SerializeField] [Range(24, 180)] private int segments = 96;
    [SerializeField] private Color baseRingColor = new(1f, 1f, 1f, 0.18f);
    [SerializeField] private Color earlyZoneColor = new(1f, 0.82f, 0.15f, 0.55f);
    [SerializeField] private Color perfectZoneColor = new(0.2f, 1f, 0.38f, 0.75f);
    [SerializeField] private Color progressRingColor = new(0.2f, 0.95f, 1f, 1f);
    [SerializeField] private bool clockwise = true;
    [SerializeField] [Range(0.5f, 0.95f)] private float earlyZoneStart = 0.75f;
    [SerializeField] [Range(0.7f, 0.99f)] private float perfectZoneStart = 0.9f;
    [SerializeField] [Range(0.05f, 0.4f)] private float flashDuration = 0.16f;
    [SerializeField] [Range(1f, 2f)] private float flashIntensity = 1.35f;

    private float flashTimer;

    public void SetProgress(float value)
    {
        float clamped = Mathf.Clamp01(value);
        if (Mathf.Approximately(progress, clamped))
            return;

        progress = clamped;
        SetVerticesDirty();
    }

    public void TriggerFlash()
    {
        flashTimer = flashDuration;
        SetVerticesDirty();
    }

    protected override void Awake()
    {
        base.Awake();
        raycastTarget = false;
    }

    private void Update()
    {
        if (flashTimer <= 0f)
            return;

        flashTimer = Mathf.Max(0f, flashTimer - Time.unscaledDeltaTime);
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        Rect rect = rectTransform.rect;
        float outerRadius = Mathf.Min(rect.width, rect.height) * 0.5f;
        float innerRadius = outerRadius * (1f - ringThickness);
        Vector2 center = rect.center;
        int safeSegments = Mathf.Max(24, segments);
        float earlyStart = Mathf.Clamp01(earlyZoneStart);
        float perfectStart = Mathf.Clamp(perfectZoneStart, earlyStart + 0.01f, 0.99f);

        DrawArc(vh, center, innerRadius, outerRadius, 0f, earlyStart, baseRingColor, safeSegments);
        DrawArc(vh, center, innerRadius, outerRadius, earlyStart, perfectStart, earlyZoneColor, safeSegments);
        DrawArc(vh, center, innerRadius, outerRadius, perfectStart, 1f, perfectZoneColor, safeSegments);

        if (progress > 0f)
        {
            Color overlayColor = ApplyFlash(progressRingColor);
            DrawArc(vh, center, innerRadius, outerRadius, 0f, progress, overlayColor, safeSegments);
        }
    }

    private void DrawArc(VertexHelper vh, Vector2 center, float innerRadius, float outerRadius, float normalizedStart, float normalizedEnd, Color color, int safeSegments)
    {
        float clampedStart = Mathf.Clamp01(normalizedStart);
        float clampedEnd = Mathf.Clamp01(normalizedEnd);
        float sweep = Mathf.Max(0f, clampedEnd - clampedStart) * 360f;
        if (sweep <= 0f)
            return;

        float direction = clockwise ? -1f : 1f;
        float startAngle = 90f + direction * clampedStart * 360f;
        int steps = Mathf.Max(1, Mathf.CeilToInt(safeSegments * (clampedEnd - clampedStart)));

        for (int i = 0; i < steps; i++)
        {
            float t0 = (float)i / steps;
            float t1 = (float)(i + 1) / steps;
            float angle0 = startAngle + direction * sweep * t0;
            float angle1 = startAngle + direction * sweep * t1;

            Vector2 outer0 = center + AngleToDirection(angle0) * outerRadius;
            Vector2 outer1 = center + AngleToDirection(angle1) * outerRadius;
            Vector2 inner0 = center + AngleToDirection(angle0) * innerRadius;
            Vector2 inner1 = center + AngleToDirection(angle1) * innerRadius;

            int index = vh.currentVertCount;
            vh.AddVert(outer0, color, Vector2.zero);
            vh.AddVert(outer1, color, Vector2.zero);
            vh.AddVert(inner1, color, Vector2.zero);
            vh.AddVert(inner0, color, Vector2.zero);
            vh.AddTriangle(index, index + 1, index + 2);
            vh.AddTriangle(index, index + 2, index + 3);
        }
    }

    private static Vector2 AngleToDirection(float angleDegrees)
    {
        float radians = angleDegrees * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
    }

    private Color ApplyFlash(Color source)
    {
        if (flashTimer <= 0f || flashDuration <= 0f)
            return source;

        float normalized = flashTimer / flashDuration;
        float intensity = Mathf.Lerp(1f, flashIntensity, normalized);
        return new Color(
            Mathf.Clamp01(source.r * intensity),
            Mathf.Clamp01(source.g * intensity),
            Mathf.Clamp01(source.b * intensity),
            source.a);
    }
}
