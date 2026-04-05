namespace Sonorize.Core.Helpers;

public static class FileSystemHelper
{
    public static IEnumerable<string> GetFilesSafe(string root, HashSet<string> validExtensions)
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
            string[]? files = null;

            try
            {
                files = Directory.GetFiles(dir);
            }
            catch (UnauthorizedAccessException) { /* Skip locked folders */ }
            catch (Exception) { /* Skip other errors */ }

            if (files is not null)
            {
                foreach (string file in files)
                {
                    if (!validExtensions.Contains(Path.GetExtension(file)))
                    {
                        continue;
                    }

                    yield return file;
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