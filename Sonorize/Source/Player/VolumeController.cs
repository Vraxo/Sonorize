namespace Sonorize;

class VolumeController : Component
{
    // Fields
    
    private readonly int unit = 5;

    // Properties

    private int _volume = 100;

    public int Volume
    {
        get => _volume;

        set
        {
            _volume = value;
            _volume = Math.Clamp(_volume, 0, 100);
            program.Player.WaveOutDevice.Volume = _volume / 100F;
            program.Settings?.Save();
        }
    }

    // public

    public void Increase()
    {
        Volume += unit;
    }

    public void Decrease()
    {
        Volume -= unit;
    }
}