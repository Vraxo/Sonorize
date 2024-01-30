namespace Sonorize;

class Theme
{
    // Fields

    private readonly string filePath = "Resources/Config/Theme.txt";

    // Properties

    public ConsoleColor Default     { get; private set; } = ConsoleColor.White;
    public ConsoleColor Selected    { get; private set; } = ConsoleColor.Green;
    public ConsoleColor SongPlaying { get; private set; } = ConsoleColor.Yellow;
    public ConsoleColor SongPaused  { get; private set; } = ConsoleColor.DarkYellow;
    public ConsoleColor SongStopped { get; private set; } = ConsoleColor.Red;
    public ConsoleColor Volume      { get; private set; } = ConsoleColor.Blue;
    public ConsoleColor Replay      { get; private set; } = ConsoleColor.Magenta;
    public ConsoleColor Playlists   { get; private set; } = ConsoleColor.Cyan;

    // Constructor

    public Theme()
    {
        if (!File.Exists(filePath))
        {
            SaveDefaultTheme();
            return;
        }
        
        string[] themeFile = File.ReadAllLines(filePath);
        
        foreach (string line in themeFile)
        {
            string[] tokens = line.Split(": ");
        
            if (tokens.Length == 2)
            {
                string color = tokens[0];
                string value = tokens[1];
        
                ReadValue(color, value);
            }
        }
    }

    // Private

    private void SaveDefaultTheme()
    {
        string defaultTheme = Properties.Resources.DefaultTheme;
        File.WriteAllText(filePath, defaultTheme);
    }

    private void ReadValue(string color, string value)
    {
        ConsoleColor parsedColor = GetColor(value);

        switch (color)
        {
            case "Default":
                Default = parsedColor;
                break;

            case "Selected":
                Selected = parsedColor;
                break;

            case "SongPlaying":
                SongPlaying = parsedColor;
                break;

            case "SongPaused":
                SongPaused = parsedColor;
                break;

            case "SongStopped":
                SongStopped = parsedColor;
                break;

            case "Volume":
                Volume = parsedColor;
                break;

            case "Replay":
                Replay = parsedColor;
                break;

            case "Playlists":
                Playlists = parsedColor;
                break;
        }
    }

    private ConsoleColor GetColor(string value)
    {
        bool isColorValid = Enum.TryParse(value, out ConsoleColor _);

        if (isColorValid)
        {
            return (ConsoleColor)Enum.Parse(typeof(ConsoleColor), value);
        }

        return ConsoleColor.White;
    }
}