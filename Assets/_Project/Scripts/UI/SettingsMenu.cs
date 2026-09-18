/// <summary>Громкость оружия.</summary>
public class SettingsMenu : VolumeSlider
{
    protected override string MixerParameter => "GunVolume";
    protected override float SavedVolume { get => GameState.GunVolume; set => GameState.GunVolume = value; }
}
