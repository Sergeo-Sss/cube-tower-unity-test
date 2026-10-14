using System.Threading;
using Cysharp.Threading.Tasks;

namespace CubeTower.Core.Config
{
    /// <summary>
    /// Abstraction over the config data source. Asynchronous on purpose so that
    /// remote / file based sources can be plugged in without touching the game code.
    /// </summary>
    public interface IGameConfigProvider
    {
        UniTask<GameConfig> LoadAsync(CancellationToken cancellationToken);
    }
}
