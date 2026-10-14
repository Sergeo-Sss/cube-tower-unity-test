namespace CubeTower.Core.Progress
{
    /// <summary>
    /// Key-value persistent storage of serializable progress objects.
    /// </summary>
    public interface IProgressStorage
    {
        bool TryLoad<T>(string key, out T data) where T : class;

        void Save<T>(string key, T data) where T : class;

        void Delete(string key);
    }
}
