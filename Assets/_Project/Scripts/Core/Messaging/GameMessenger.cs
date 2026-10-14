using System;
using UniRx;

namespace CubeTower.Core.Messaging
{
    public readonly struct GameMessage
    {
        public GameMessage(string key)
        {
            Key = key;
        }

        /// <summary>
        /// Localization key of the message.
        /// </summary>
        public string Key { get; }
    }

    /// <summary>
    /// Channel of user facing gameplay messages.
    /// </summary>
    public interface IGameMessenger
    {
        IObservable<GameMessage> Messages { get; }

        void Publish(string key);
    }

    public sealed class GameMessenger : IGameMessenger, IDisposable
    {
        private readonly Subject<GameMessage> _messages = new Subject<GameMessage>();

        public IObservable<GameMessage> Messages => _messages;

        public void Publish(string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Message key must not be empty.", nameof(key));

            _messages.OnNext(new GameMessage(key));
        }

        public void Dispose()
        {
            _messages.OnCompleted();
            _messages.Dispose();
        }
    }
}
