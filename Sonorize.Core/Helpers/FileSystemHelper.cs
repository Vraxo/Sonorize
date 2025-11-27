namespace Sonorize.Core.Helpers;

public static class FileSystemHelper
{
    /// <summary>
    /// Recursively enumerates files in a directory, skipping inaccessible folders (System Volume Info, etc.)
    /// to prevent UnauthorizedAccessException from aborting the entire scan.
    /// </summary>
    public static IEnumerable<string> GetFilesSafe(string root, HashSet<string> validExtensions)
    {
        if (!Directory.Exists(root))
        {
            yield break;
        }

        var stack = new Stack<string>();
        stack.Push(root);

        while (stack.Count > 0)
        {
            string dir = stack.Pop();
            string[]? files = null;

            try
            {
                files = Directory.GetFiles(dir);
            }
            catch (UnauthorizedAccessException) { /* Skip locked folders */ }
            catch (Exception) { /* Skip other errors */ }

            if (files != null)
            {
                foreach (string file in files)
                {
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
            catch (UnauthorizedAccessException) { /* Skip locked folders */ }
            catch (Exception) { /* Skip other errors */ }
        }
    }
}