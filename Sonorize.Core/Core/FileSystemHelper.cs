namespace Sonorize.Core.Core;

public static class FileSystemHelper
{
    public static IEnumerable<string> GetFilesSafe(string root, HashSet<string> validExtensions)
    {
        return GetFilesSafe(root, validExtensions, null, null, null);
    }

    public static IEnumerable<string> GetFilesSafe(
        string root,
        HashSet<string> validExtensions,
        Action<string>? onDirectoryVisited,
        Action<string>? onFileFound,
        Action<string, Exception>? onError)
    {
        if (!Directory.Exists(root))
        {
            yield break;
        }

        Stack<string> stack = new();
        stack.Push(root);

        while (stack.Count > 0)
        {
            string dir = stack.Pop();
            onDirectoryVisited?.Invoke(dir);

            string[]? files = null;

            try
            {
                files = Directory.GetFiles(dir);
            }
            catch (Exception ex)
            {
                onError?.Invoke(dir, ex);
            }

            if (files is not null)
            {
                foreach (string file in files)
                {
                    onFileFound?.Invoke(file);

                    if (validExtensions.Contains(Path.GetExtension(file)))
                    {
                        yield return file;
                    }
                }
            }

            try
            {
                foreach (string subDir in Directory.GetDirectories(dir))
                {
                    stack.Push(subDir);
                }
            }
            catch (Exception ex)
            {
                onError?.Invoke(dir, ex);
            }
        }
    }
}
