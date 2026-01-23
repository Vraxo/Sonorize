using Sonorize.Core.Models;
using Sonorize.Core.Settings;

namespace Sonorize.Core.Services.Library;

public class FolderTreeBuilder
{
    private readonly SonorizeSettings _settings;

    public FolderTreeBuilder(SonorizeSettings settings)
    {
        _settings = settings;
    }

    public List<FolderNode> Build(IEnumerable<Song> songs)
    {
        List<FolderNode> rootNodes = CreateRootNodes();

        if (rootNodes.Count == 0)
        {
            return rootNodes;
        }

        foreach (Song? song in songs.Where(s => !IsDemoPath(s.FilePath)))
        {
            ProcessSong(song, rootNodes);
        }

        foreach (FolderNode root in rootNodes)
        {
            SortNode(root);
        }

        return rootNodes.OrderBy(n => n.Name, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private List<FolderNode> CreateRootNodes()
    {
        return [.. _settings.Library.MusicFolderPaths
            .Where(Directory.Exists)
            .Select(path => new FolderNode { Name = Path.GetFileName(path), Path = path })];
    }

    private void ProcessSong(Song song, List<FolderNode> rootNodes)
    {
        string? dirPath = Path.GetDirectoryName(song.FilePath);

        if (string.IsNullOrEmpty(dirPath))
        {
            return;
        }

        FolderNode? rootNode = rootNodes.FirstOrDefault(r => dirPath.StartsWith(r.Path, StringComparison.OrdinalIgnoreCase));

        if (rootNode == null)
        {
            return;
        }

        string relativePath = dirPath[rootNode.Path.Length..];
        string[] segments = relativePath.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);

        FolderNode currentNode = rootNode;

        foreach (string segment in segments)
        {
            currentNode = GetOrCreateChildNode(currentNode, segment);
        }

        currentNode.Songs.Add(song);
    }

    private FolderNode GetOrCreateChildNode(FolderNode parent, string segment)
    {
        FolderNode? childNode = parent.Children.FirstOrDefault(c => c.Name.Equals(segment, StringComparison.OrdinalIgnoreCase));

        if (childNode != null)
        {
            return childNode;
        }

        string newPath = Path.Combine(parent.Path, segment);
        childNode = new FolderNode { Name = segment, Path = newPath };
        parent.Children.Add(childNode);
        return childNode;
    }

    private static void SortNode(FolderNode node)
    {
        node.Children = [.. node.Children.OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)];
        node.Songs = [.. node.Songs.OrderBy(s => s.Title, StringComparer.OrdinalIgnoreCase)];

        foreach (FolderNode child in node.Children)
        {
            SortNode(child);
        }
    }

    private static bool IsDemoPath(string path)
    {
        return path.StartsWith("demo://", StringComparison.OrdinalIgnoreCase);
    }
}