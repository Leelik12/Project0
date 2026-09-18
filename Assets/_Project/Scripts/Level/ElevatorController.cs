/// <summary>Лифт на игровом этаже: когда все враги убиты, везёт в магазин апгрейдов (сцена 1).</summary>
public class ElevatorController : SceneLoaderBase
{
    private const int UpgradeShopSceneIndex = 1;

    public int CurrentBuildScene; // индекс этой сцены в Build Settings — магазин по нему поймёт, какой этаж следующий

    protected override int TargetSceneIndex => UpgradeShopSceneIndex;

    protected override bool CanDepart() => GameState.levelCheksComplete;

    protected override void OnBeforeLoad()
    {
        GameState.CurrentLevel = CurrentBuildScene;
        GameState.ResetPerLevelFlags();
    }
}
