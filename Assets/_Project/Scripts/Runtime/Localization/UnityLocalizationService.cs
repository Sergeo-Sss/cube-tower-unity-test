using System;
using System.Collections.Generic;
using System.Threading;
using CubeTower.Core.Localization;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Localization.Settings;

namespace CubeTower.Runtime.Localization
{
    /// <summary>
    /// <see cref="ILocalizationService"/> on top of the Unity Localization package.
    /// Locale is chosen by the package's startup selectors (command line → system language → English);
    /// strings live in the "Messages" string table collection.
    /// </summary>
    public sealed class UnityLocalizationService : ILocalizationService
    {
        public const string MessagesTable = "Messages";
        private const string FallbackLanguage = "en";

        private readonly HashSet<string> _reportedMissing = new HashSet<string>(StringComparer.Ordinal);

        public string Language
        {
            get
            {
                var locale = LocalizationSettings.SelectedLocale;
                return locale != null ? locale.Identifier.Code : FallbackLanguage;
            }
        }

        /// <summary>
        /// Initializes the package and loads the messages table, so <see cref="Get"/> never blocks.
        /// </summary>
        public static async UniTask PreloadAsync(CancellationToken cancellationToken)
        {
            await LocalizationSettings.InitializationOperation.Task.AsUniTask().AttachExternalCancellation(cancellationToken);
            await LocalizationSettings.StringDatabase.GetTableAsync(MessagesTable).Task.AsUniTask()
                .AttachExternalCancellation(cancellationToken);
        }

        public string Get(string key)
        {
            if (string.IsNullOrEmpty(key))
                return string.Empty;

            // Synchronous access: the table is preloaded by the bootstrap; if the gameplay scene is started
            // directly the package completes the load synchronously.
            // Unknown keys are reported once; known keys go through the database so locale fallbacks apply.
            var table = LocalizationSettings.StringDatabase.GetTable(MessagesTable);
            if (table != null && table.SharedData.Contains(key))
                return LocalizationSettings.StringDatabase.GetLocalizedString(MessagesTable, key);

            if (_reportedMissing.Add(key))
                Debug.LogWarning($"[{nameof(UnityLocalizationService)}] Missing localization for key '{key}' ({Language}).");

            return key;
        }
    }
}
