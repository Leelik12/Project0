/// <summary>Громкость окружения.</summary>
public class SettingsMenuEnv : VolumeSlider
{
    protected override string MixerParameter => "EnvVolume";
    protected override float SavedVolume { get => GameState.EnvVolume; set => GameState.EnvVolume = value; }
}
