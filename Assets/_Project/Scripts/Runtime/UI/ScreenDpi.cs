using UnityEngine;

namespace CubeTower.Runtime.UI
{
    /// <summary>
    /// Physical units for touch input: distances in inches feel the same on any screen density.
    /// </summary>
    public static class ScreenDpi
    {
        private const float FallbackDpi = 160f;

        public static float Dpi => Screen.dpi > 0f ? Screen.dpi : FallbackDpi;

        public static float InchesToPixels(float inches) => inches * Dpi;
    }
}
