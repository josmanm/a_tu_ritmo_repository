using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class CompassBeatRingGraphic : MaskableGraphic
{
    [SerializeField] [Range(0.04f, 0.35f)] private float thickness = 0.2f;
    [SerializeField] [Range(24, 120)] private int segments = 72;
    [SerializeField] [Range(1f, 2f)] private float glowMultiplier = 1.35f;

    private float glowAmount;

    public void SetColor(Color color)
    {
        this.color = color;
        SetVerticesDirty();
    }

    public void SetGlow(float normalizedGlow)
    {
        glowAmount = Mathf.Clamp01(normalizedGlow);
        SetVerticesDirty();
    }

    protected override void Awake()
    {
        base.Awake();
        raycastTarget = false;
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        Rect rect = rectTransform.rect;
        float outerRadius = Mathf.Min(rect.width, rect.height) * 0.5f;
        float innerRadius = outerRadius * (1f - thickness);
        Vector2 center = rect.center;
        int safeSegments = Mathf.Max(24, segments);
        Color drawColor = Color.Lerp(color, Color.white, glowAmount * 0.45f);
        drawColor.r = Mathf.Clamp01(drawColor.r * Mathf.Lerp(1f, glowMultiplier, glowAmount));
        drawColor.g = Mathf.Clamp01(drawColor.g * Mathf.Lerp(1f, glowMultiplier, glowAmount));
        drawColor.b = Mathf.Clamp01(drawColor.b * Mathf.Lerp(1f, glowMultiplier, glowAmount));
        Color32 c = drawColor;

        for (int i = 0; i < safeSegments; i++)
        {
            float t0 = (float)i / safeSegments * Mathf.PI * 2f;
            float t1 = (float)(i + 1) / safeSegments * Mathf.PI * 2f;

            Vector2 outer0 = center + new Vector2(Mathf.Cos(t0), Mathf.Sin(t0)) * outerRadius;
            Vector2 outer1 = center + new Vector2(Mathf.Cos(t1), Mathf.Sin(t1)) * outerRadius;
            Vector2 inner0 = center + new Vector2(Mathf.Cos(t0), Mathf.Sin(t0)) * innerRadius;
            Vector2 inner1 = center + new Vector2(Mathf.Cos(t1), Mathf.Sin(t1)) * innerRadius;

            int start = vh.currentVertCount;
            vh.AddVert(outer0, c, Vector2.zero);
            vh.AddVert(outer1, c, Vector2.zero);
            vh.AddVert(inner1, c, Vector2.zero);
            vh.AddVert(inner0, c, Vector2.zero);
            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start, start + 2, start + 3);
        }
    }
}
