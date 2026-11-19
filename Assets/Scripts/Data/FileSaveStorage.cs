using System.IO;

namespace TheLostDungeon.Data
{
    /// <summary>
    /// Stores the save as a JSON file (Windows, Android, iOS, Editor).
    /// Writes to a temporary file first, then swaps it in, so a crash or power cut during a
    /// save can't leave a half-written, corrupted save file.
    /// </summary>
    public class FileSaveStorage : ISaveStorage
    {
        private readonly string path;

        public FileSaveStorage(string path)
        {
            this.path = path;
        }

        public string FilePath => path;

        public bool Exists() => File.Exists(path);

        public string Read() => Exists() ? File.ReadAllText(path) : null;

        public void Write(string json)
        {
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

            string tempPath = path + ".tmp";
            File.WriteAllText(tempPath, json);

            if (File.Exists(path)) File.Delete(path);
            File.Move(tempPath, path);
        }

        public void Delete()
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
