namespace Sonorize;

class Settings : Component
{
    // Fields

    private readonly string filePath = "Resources/Config/Settings.txt";

    // Constructor

    public Settings()
    {
        Load();
    }

    // Public

    public void Save()
    {
        string defaultSettings = Properties.Resources.DefaultSettings.ToString();
        File.WriteAllText(filePath, defaultSettings);
    }

    // Private

    private void Load()
    {
        if (!File.Exists(filePath))
        {
            Save();
            return;
        }

        string[] settingsFile = File.ReadAllLines(filePath);

        foreach (string line in settingsFile)
        {
            string[] tokens = line.Split(": ");

            if (tokens.Length == 2)
            {
                string setting = tokens[0];
                string value = tokens[1];

                ReadValue(setting, value);
            }
        }
    }

    private void ReadValue(string setting, string value)
    {
        switch (setting)
        {
            case "Volume":
                if (int.TryParse(value, out _))
                {
                    program.VolumeController.Volume = int.Parse(value);
                }
                break;

            case "Replay":
                if (Enum.TryParse(value, out ReplayMode _))
                {
                    program.Replayer.Mode = (ReplayMode)Enum.Parse(typeof(ReplayMode), value);
                }
                break;

            case "ShowInstructions":
                if (bool.TryParse(value, out _))
                {
                    program.InstructionsPrinter.IsEnabled = bool.Parse(value);
                }
                break;
        }
    }
}