using Sonorize;

class Program
{
    // Fields

    #region [ - - - FIELDS - - - ]

    public bool IsStopping = false;

    public Theme               Theme;
    public GlobalState         GlobalState;

    public Player              Player;
    public Replayer            Replayer;
    public VolumeController    VolumeController;
    public Settings            Settings;

    public SelectionCursor     SelectionCursor;
    public ConsoleUI           ConsoleUI;
    public InputHandler        InputHandler;

    public SongUndeleter       SongUndeleter;
    public PlaylistUndeleter   PlaylistUndeleter;

    public SongsList           SongsList;
    public PlaylistCollection  PlaylistCollection;

    public NewPlaylistPrompter NewPlaylistPrompter;
    public SongsListPrinter    SongsListPrinter;
    public SongNamePrinter     SongNamePrinter;
    public PlaylistsPrinter    PlaylistsPrinter;
    public InstructionsPrinter InstructionsPrinter;
    public PlaylistNamePrinter PlaylistNamePrinter;
    public SettingsPrinter     SettingsPrinter;

    private readonly string configFolderPath    = "Resources/Config";
    private readonly string playlistsFolderpath = "Resources/Playlists";

    #endregion

    // Public

    public void Start(string[] args)
    {
        Initialize();

        if (args.Length > 0)
        {
            GlobalState.ToggleToSingleSong(args[0]);
        }

        while (!IsStopping)
        {
            ConsoleUI.Update();
            InputHandler.Update();
            ConsoleUI.Update();
        }
    }

    // Private

    private void Initialize()
    {
        CreateConfigDirectory();

        Player              = new();
        Replayer            = new();
        VolumeController    = new();

        SelectionCursor     = new();
        ConsoleUI           = new();
        InputHandler        = new();

        SongUndeleter       = new();
        PlaylistUndeleter   = new();

        SongsList           = new();
        PlaylistCollection  = new();

        NewPlaylistPrompter = new();
        SongsListPrinter    = new();
        SongNamePrinter     = new();
        PlaylistsPrinter    = new();
        InstructionsPrinter = new();
        PlaylistNamePrinter = new();
        SettingsPrinter     = new();

        Theme               = new();
        GlobalState         = new();
        Settings            = new();
    }

    private void CreateConfigDirectory()
    {
        if (!Directory.Exists(configFolderPath))
        {
            Directory.CreateDirectory(configFolderPath);
        }

        if (!Directory.Exists(playlistsFolderpath))
        {
            Directory.CreateDirectory(playlistsFolderpath);
        }
    }
}