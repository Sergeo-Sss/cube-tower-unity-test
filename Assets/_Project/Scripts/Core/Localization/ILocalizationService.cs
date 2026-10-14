namespace CubeTower.Core.Localization
{
    /// <summary>
    /// Text localization. Gameplay code only operates on keys.
    /// </summary>
    public interface ILocalizationService
    {
        /// <summary>
        /// ISO 639-1 code of the current language.
        /// </summary>
        string Language { get; }

        /// <summary>
        /// Localized text by key. Never returns null: falls back to the default language and then to the key itself.
        /// </summary>
        string Get(string key);
    }
}
