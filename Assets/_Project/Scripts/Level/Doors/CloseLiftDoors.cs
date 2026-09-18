/// <summary>Двери лифта на входе на этаж: закрываются за игроком.</summary>
public class CloseLiftDoors : SlidingDoor
{
    protected override bool CanOpen() => true;
}
