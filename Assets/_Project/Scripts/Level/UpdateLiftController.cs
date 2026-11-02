/// <summary>Лифт в магазине апгрейдов: когда магазин открыт (UpgradeShop), везёт на следующий этаж (CurrentLevel + 1).</summary>
public class UpdateLiftController : SceneLoaderBase
{
    private int nextLevelIndex;

    private void Start()
    {
        nextLevelIndex = GameState.CurrentLevel + 1;
    }

    protected override int TargetSceneIndex => nextLevelIndex;

    protected override bool CanEnter() => GameState.UpdateLevelEnd;

    protected override bool CanDepart() => true;

    protected override void OnBeforeLoad()
    {
        GameState.CurrentLevel = nextLevelIndex;
        GameState.UpdateLevelEnd = false;
        GameState.ItemPickedUp = false;
        GameState.ResetPerLevelFlags();
    }
}
