/// <summary>Обычная дверь: открывается при подходе игрока.</summary>
public class DoorOpen : SlidingDoor
{
    protected override bool CanOpen() => true;
}
