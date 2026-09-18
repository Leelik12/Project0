/// <summary>Двери лифта на выход с этажа: открываются, когда все враги уничтожены.</summary>
public class LevelDoors : SlidingDoor
{
    protected override bool CanOpen() => GameState.levelCheksComplete;
}
