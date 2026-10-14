using System;
using System.IO;
using System.Text;
using CubeTower.Core.Progress;
using UnityEngine;

namespace CubeTower.Runtime.Progress
{
    /// <summary>
    /// Stores every key as a JSON file in <see cref="Application.persistentDataPath"/>.
    /// Writes are atomic (temp file + move) so a crash during saving never corrupts the progress;
    /// unreadable files are moved aside instead of breaking the game.
    /// </summary>
    public sealed class JsonFileStorage : IProgressStorage
    {
        private const string Extension = ".json";

        private readonly string _directory;

        public JsonFileStorage() : this(DefaultDirectory)
        {
        }

        public JsonFileStorage(string directory)
        {
            if (string.IsNullOrEmpty(directory))
                throw new ArgumentException("Storage directory must not be empty.", nameof(directory));

            _directory = directory;
        }

        public static string DefaultDirectory => Path.Combine(Application.persistentDataPath, "Saves");

        public bool TryLoad<T>(string key, out T data) where T : class
        {
            data = null;
            var path = GetPath(key);
            if (!File.Exists(path))
                return false;

            try
            {
                var json = File.ReadAllText(path, Encoding.UTF8);
                data = JsonUtility.FromJson<T>(json);
                return data != null;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[{nameof(JsonFileStorage)}] Failed to read '{path}', the file is moved aside. {exception}");
                MoveAside(path);
                return false;
            }
        }

        public void Save<T>(string key, T data) where T : class
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            var path = GetPath(key);
            var tempPath = path + ".tmp";
            try
            {
                Directory.CreateDirectory(_directory);
                File.WriteAllText(tempPath, JsonUtility.ToJson(data), Encoding.UTF8);
                if (File.Exists(path))
                    File.Delete(path);
                File.Move(tempPath, path);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[{nameof(JsonFileStorage)}] Failed to save '{path}'. {exception}");
            }
        }

        public void Delete(string key)
        {
            var path = GetPath(key);
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[{nameof(JsonFileStorage)}] Failed to delete '{path}'. {exception}");
            }
        }

        private string GetPath(string key)
        {
            if (string.IsNullOrWhiteSpace(key) || key.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                throw new ArgumentException($"Invalid storage key '{key}'.", nameof(key));

            return Path.Combine(_directory, key + Extension);
        }

        private static void MoveAside(string path)
        {
            try
            {
                var corruptedPath = path + ".corrupted";
                if (File.Exists(corruptedPath))
                    File.Delete(corruptedPath);
                File.Move(path, corruptedPath);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[{nameof(JsonFileStorage)}] Failed to move aside '{path}'. {exception}");
            }
        }
    }
}
