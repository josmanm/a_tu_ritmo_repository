using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RhythmGameManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text statusText;

    [Header("Buttons (3 lanes)")]
    [SerializeField] private Button[] laneButtons; // BtnLane0..2

    [Header("Lane Points")]
    [SerializeField] private RectTransform[] spawnPoints; // Spawn_0..2
    [SerializeField] private RectTransform[] hitPoints;   // Hit_0..2

    [Header("Note Prefab")]
    [SerializeField] private NoteUI notePrefab;
    [SerializeField] private RectTransform notesParent; // LanesRoot o un NotesRoot

    [Header("Gameplay (Accessible)")]
    [SerializeField] private float noteSpeed = 320f;     // px/seg
    [SerializeField] private float spawnInterval = 1.1f; // más alto = más fácil
    [SerializeField] private float hitWindowPx = 90f;    // tolerancia (grande)
    [SerializeField] private float missExtraPx = 140f;   // cuánto deja pasar antes de contar miss

    [Header("Lane Note Sprites (0=Blue,1=Green,2=Yellow)")]
    [SerializeField] private Sprite[] laneNoteSprites; // size 3

    [Header("Feedback")]
    [SerializeField] private float hitPopScale = 1.15f;
    [SerializeField] private float hitPopTime = 0.08f;
    [SerializeField] private Color hitTint = new Color(0.7f, 1f, 0.7f);

    private readonly List<NoteUI> activeNotes = new List<NoteUI>();
    private int score = 0;

    private void Start()
    {
        // conectar botones
        for (int i = 0; i < laneButtons.Length; i++)
        {
            int idx = i;
            laneButtons[i].onClick.AddListener(() => OnLanePress(idx));
        }

        UpdateScore();
        SetStatus("Toca cuando la nota esté en la zona", Color.white);

        StartCoroutine(SpawnLoop());
    }

    private void Update()
    {
        for (int i = activeNotes.Count - 1; i >= 0; i--)
        {
            var note = activeNotes[i];

            // ✅ si está destruida, quítala de la lista
            if (note == null)
            {
                activeNotes.RemoveAt(i);
                continue;
            }

            // ✅ si está en animación de hit, no la muevas ni la evalúes
            if (note.isResolving)
                continue;

            RectTransform rt = (RectTransform)note.transform;
            rt.anchoredPosition += Vector2.down * noteSpeed * Time.deltaTime;

            float hitY = hitPoints[note.laneIndex].anchoredPosition.y;

            // ✅ si se pasó sin acertar => miss
            if (!note.wasHit && rt.anchoredPosition.y < (hitY - hitWindowPx - missExtraPx))
            {
                note.wasHit = true;
                Destroy(note.gameObject);
                activeNotes.RemoveAt(i);
                SetStatus("Intenta de nuevo", new Color(1f, 0.85f, 0.2f), animate: false);
            }
        }
    }

    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            SpawnNote(Random.Range(0, 3)); // 3 lanes

            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void SpawnNote(int lane)
    {
        NoteUI note = Instantiate(notePrefab, notesParent);
        note.laneIndex = lane;
        note.wasHit = false;
        note.isResolving = false;

        RectTransform rt = (RectTransform)note.transform;
        rt.anchoredPosition = spawnPoints[lane].anchoredPosition;

        //asignar sprite según carril
        if (laneNoteSprites != null && laneNoteSprites.Length > lane && note.img != null)
            note.img.sprite = laneNoteSprites[lane];

        activeNotes.Add(note);
    }

    private void OnLanePress(int lane)
    {
        // encontrar la nota más cercana a la HitZone de ese carril
        NoteUI best = null;
        float bestDist = float.MaxValue;
        float hitY = hitPoints[lane].anchoredPosition.y;

        foreach (var n in activeNotes)
        {
            if (n == null || n.wasHit) continue;
            if (n.laneIndex != lane) continue;

            float dist = Mathf.Abs(((RectTransform)n.transform).anchoredPosition.y - hitY);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = n;
            }
        }

        if (best != null && bestDist <= hitWindowPx)
        {
            best.wasHit = true;
            best.isResolving = true;

            score += 10;
            UpdateScore();
            SetStatus("¡Bien!", new Color(0.2f, 0.9f, 0.4f));

            activeNotes.Remove(best);
            StartCoroutine(HitAndDestroy(best));
        }
        else
        {
            // fallo suave (no castigar fuerte)
            SetStatus("Casi...", new Color(1f, 0.85f, 0.2f), animate: false);
        }
    }

    private void UpdateScore()
    {
        if (scoreText != null)
            scoreText.text = $"Puntos: {score}";
    }

    private void SetStatus(string msg, Color color, bool animate = true)
    {
        if (statusText == null) return;
        statusText.text = msg;
        statusText.color = color;
    }
    private IEnumerator HitFeedback(NoteUI note)
    {
        if (note == null) yield break;

        var rt = note.transform as RectTransform;
        var img = note.img;

        if (rt == null || img == null) yield break;

        Vector3 originalScale = rt.localScale;
        Color originalColor = img.color;

        img.color = hitTint;
        rt.localScale = originalScale * hitPopScale;

        yield return new WaitForSeconds(hitPopTime);

        // ✅ IMPORTANTE: puede haber sido destruida durante el wait
        if (note == null || rt == null || img == null) yield break;

        rt.localScale = originalScale;
        img.color = originalColor;
    }

    private IEnumerator HitAndDestroy(NoteUI note)
    {
        if (note == null) yield break;
        yield return StartCoroutine(HitFeedback(note));
        if (note != null) Destroy(note.gameObject);
    }
}
