using TMPro;
using UnityEngine;

/// <summary>
/// Дорожка тира в лобби (C-01): счёт и точность на табло, цифры урона над мишенью.
/// Пока она есть на сцене, выстрелы VRGun считаются здесь, а не в GameState (RegisterShot) — тренировка не портит
/// статистику забега и достижения. Эдди и убийства мишени не даёт: см. RangeTarget.
/// </summary>
public class RangeLane : MonoBehaviour
{
    private const string UiFont = "Fonts/TechnoDeath UI SDF";
    private const int NumberPoolSize = 10;
    private const float NumberLifetime = 0.9f;
    private const float NumberRiseSpeed = 0.5f;
    private static readonly Color BodyColor = new(1f, 1f, 1f, 1f);
    private static readonly Color HeadColor = new(1f, 0.85f, 0.2f, 1f);

    /// <summary>Тир на текущей сцене (null вне лобби).</summary>
    public static RangeLane Current { get; private set; }

    [SerializeField] private TMP_Text boardText;

    private TextMeshPro[] numbers;
    private float[] numberStart;
    private int nextNumber;
    private int activeNumbers;
    private Camera viewCamera;
    private bool dirty = true;

    public int Shots { get; private set; }
    public int Hits { get; private set; }
    public int Headshots { get; private set; }
    public int Kills { get; private set; }
    public float Damage { get; private set; }

    private void Awake()
    {
        Current = this;
        BuildNumberPool();
    }

    private void OnDestroy()
    {
        if (Current == this) Current = null;
    }

    private void BuildNumberPool()
    {
        TMP_FontAsset font = Resources.Load<TMP_FontAsset>(UiFont);
        numbers = new TextMeshPro[NumberPoolSize];
        numberStart = new float[NumberPoolSize];
        for (int i = 0; i < NumberPoolSize; i++)
        {
            var go = new GameObject("DamageNumber");
            go.transform.SetParent(transform, false);
            var text = go.AddComponent<TextMeshPro>();
            if (font != null) text.font = font;
            text.fontSize = 0.6f;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.rectTransform.sizeDelta = new Vector2(1f, 0.3f);
            text.raycastTarget = false;
            go.SetActive(false);
            numbers[i] = text;
        }
    }

    // ---------- Счёт ----------

    /// <summary>Выстрел игрока (из VRGun.Shoot).</summary>
    public void RegisterShot()
    {
        Shots++;
        dirty = true;
    }

    public void RegisterHit(float damage, bool headshot, bool killed, bool countsAsHit)
    {
        if (countsAsHit) Hits++;
        if (headshot) Headshots++;
        if (killed) Kills++;
        Damage += damage;
        dirty = true;
    }

    public void ResetScore()
    {
        Shots = Hits = Headshots = Kills = 0;
        Damage = 0f;
        dirty = true;
    }

    // ---------- Цифры урона ----------

    public void ShowDamage(Vector3 point, float damage, bool headshot)
    {
        TextMeshPro text = numbers[nextNumber];
        bool wasActive = text.gameObject.activeSelf; // пул круговой: самая старая цифра может быть ещё на экране
        numberStart[nextNumber] = Time.time;
        nextNumber = (nextNumber + 1) % NumberPoolSize;

        text.transform.position = point + Vector3.up * 0.1f;
        text.text = Mathf.RoundToInt(damage).ToString();
        text.color = headshot ? HeadColor : BodyColor;
        text.fontSize = headshot ? 0.8f : 0.55f;
        text.gameObject.SetActive(true);
        if (!wasActive) activeNumbers++;
    }

    private void LateUpdate()
    {
        if (dirty) RefreshBoard();
        if (activeNumbers > 0) AnimateNumbers();
    }

    private void RefreshBoard()
    {
        dirty = false;
        if (boardText == null) return;

        int accuracy = Shots > 0 ? Mathf.RoundToInt(100f * Hits / Shots) : 0;
        boardText.text = UiText.Widen(
            $"Выстрелов: {Shots}\nПопаданий: {Hits}  ({accuracy}%)\nВ голову: {Headshots}\nМишеней: {Kills}\nУрон: {Mathf.RoundToInt(Damage)}");
    }

    private void AnimateNumbers()
    {
        if (viewCamera == null) viewCamera = Camera.main;

        for (int i = 0; i < numbers.Length; i++)
        {
            TextMeshPro text = numbers[i];
            if (!text.gameObject.activeSelf) continue;

            float age = Time.time - numberStart[i];
            if (age >= NumberLifetime)
            {
                text.gameObject.SetActive(false);
                activeNumbers--;
                continue;
            }

            Transform t = text.transform;
            t.position += Vector3.up * (NumberRiseSpeed * Time.deltaTime);
            if (viewCamera != null) t.rotation = Quaternion.LookRotation(t.position - viewCamera.transform.position); // лицом к игроку
            Color c = text.color;
            c.a = 1f - Mathf.InverseLerp(NumberLifetime * 0.5f, NumberLifetime, age);
            text.color = c;
        }
    }
}
