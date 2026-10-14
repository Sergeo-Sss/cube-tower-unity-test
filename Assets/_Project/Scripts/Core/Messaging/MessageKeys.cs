namespace CubeTower.Core.Messaging
{
    /// <summary>
    /// Localization keys of gameplay feedback messages.
    /// </summary>
    public static class MessageKeys
    {
        public const string CubePlaced = "message.cube_placed";
        public const string CubeDiscarded = "message.cube_discarded";
        public const string CubeMissed = "message.cube_missed";
        public const string HeightLimit = "message.height_limit";
        public const string ColorMismatch = "message.color_mismatch";

        public static readonly string[] All = { CubePlaced, CubeDiscarded, CubeMissed, HeightLimit, ColorMismatch };
    }
}
