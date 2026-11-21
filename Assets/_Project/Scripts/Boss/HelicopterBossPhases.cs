using System;
using UnityEngine;

/// <summary>
/// Фазы боя с вертолётом. Фаза 2 включается, когда HP упало до порога: вертолёт кружит быстрее,
/// пулемёты стреляют чаще и начинаются залпы самонаводящихся ракет. Полоса HP подписывается на PhaseChanged.
/// </summary>
public class HelicopterBossPhases : MonoBehaviour
{
    [SerializeField] private HelicopterController controller;
    [SerializeField] private EnemyHealth health;
    [SerializeField] private HelicopterGunSystem gunSystem;
    [SerializeField] private HelicopterMissileLauncher launcher;

    [Header("Фаза 2")]
    [SerializeField, Range(0.05f, 0.95f)] private float phase2Threshold = 0.5f;  // доля HP, с которой начинается фаза 2
    [SerializeField] private float circleSpeedMultiplier = 1.6f;                // во сколько раз быстрее кружит
    [SerializeField] private float burstIntervalMultiplier = 0.75f;             // очереди пулемётов чаще

    /// <summary>Текущая фаза боя: 1 или 2.</summary>
    public int Phase { get; private set; } = 1;
    public float Phase2Threshold => phase2Threshold;

    /// <summary>Фаза сменилась (передаётся номер новой фазы).</summary>
    public event Action<int> PhaseChanged;

    private void Update()
    {
        if (controller == null || health == null || !controller.IsActivated) return;

        if (controller.IsDefeated || health.IsDead)
        {
            if (launcher != null) launcher.Stop();
            enabled = false;
            return;
        }

        if (Phase == 1 && health.GetCurrentHp() <= health.maxHealth * phase2Threshold) EnterPhase2();
    }

    private void EnterPhase2()
    {
        Phase = 2;
        controller.circleSpeed *= circleSpeedMultiplier;
        if (gunSystem != null) gunSystem.burstInterval *= burstIntervalMultiplier;
        if (launcher != null) launcher.StartSalvos();
        PhaseChanged?.Invoke(Phase);
    }
}
