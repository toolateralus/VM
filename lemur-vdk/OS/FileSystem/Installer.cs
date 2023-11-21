using System.IO;
using System.Reflection;
using Path = System.IO.Path;

namespace Lemur.FS
{
    public partial class FileSystem
    {
        private static class Installer
        {
            const string PATH = "computer.utils";

            public static void Install(string root)
            {
                string assemblyLocation = Assembly.GetExecutingAssembly().Location;
                string currentDirectory = Path.GetDirectoryName(assemblyLocation) ?? throw new FileNotFoundException("Couldn't locate computer.utils, the file structure was likely modified. the executable must stay in the bin folder.");

                string fullPath = Path.Combine(currentDirectory, PATH);

                if (Directory.Exists(fullPath))
                    CopyDirectory(fullPath, root);
            }

            private static void CopyDirectory(string sourceDir, string destDir)
            {
                if (!Directory.Exists(destDir))
                    Directory.CreateDirectory(destDir);

                foreach (string file in Directory.GetFiles(sourceDir))
                {
                    string destFile = Path.Combine(destDir, Path.GetFileName(file));
                    File.Copy(file, destFile, true);
                }

                foreach (string subDir in Directory.GetDirectories(sourceDir))
                {
                    string destSubDir = Path.Combine(destDir, Path.GetFileName(subDir));
                    CopyDirectory(subDir, destSubDir);
                }
            }
        }
    }
}
