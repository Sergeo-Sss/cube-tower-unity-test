using System;
using CubeTower.Core.Localization;
using CubeTower.Core.Messaging;
using UniRx;
using Zenject;

namespace CubeTower.Runtime.UI
{
    /// <summary>
    /// Shows localized gameplay messages.
    /// </summary>
    public sealed class GameMessagePresenter : IInitializable, IDisposable
    {
        private readonly IGameMessenger _messenger;
        private readonly ILocalizationService _localization;
        private readonly MessageView _view;
        private IDisposable _subscription;

        public GameMessagePresenter(IGameMessenger messenger, ILocalizationService localization, MessageView view)
        {
            _messenger = messenger;
            _localization = localization;
            _view = view;
        }

        public void Initialize()
        {
            var texts = new string[MessageKeys.All.Length];
            for (var i = 0; i < texts.Length; i++)
                texts[i] = _localization.Get(MessageKeys.All[i]);
            _view.Prewarm(texts);

            _subscription = _messenger.Messages.Subscribe(message => _view.Show(_localization.Get(message.Key)));
        }

        public void Dispose() => _subscription?.Dispose();
    }
}
