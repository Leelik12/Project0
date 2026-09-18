/// <summary>Дверь в магазине апгрейдов: срабатывает после покупки апгрейда.</summary>
public class CloseUpdateDoors : SlidingDoor
{
    protected override bool CanOpen() => GameState.UpdateLevelEnd;
}
