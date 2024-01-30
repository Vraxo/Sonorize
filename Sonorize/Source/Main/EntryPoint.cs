using System.Reflection;

namespace Sonorize;

class EntryPoint
{
    public static Program Program = new();

    [STAThread]
    public static void Main(string[] args)
    {
        string assemblyLocation = Assembly.GetEntryAssembly().Location;
        Environment.CurrentDirectory = Path.GetDirectoryName(assemblyLocation);
        Program.Start(args);
    }
}