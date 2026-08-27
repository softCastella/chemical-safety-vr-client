using UnityEngine;

namespace Prototype.Tyche.UI.Keyboard
{
    /// <summary>
    /// Shares the short submit-input suppression window with UI outside the Hangul keyboard assembly.
    /// </summary>
    public static class HangulKeyboardSubmitGuard
    {
        static int s_SuppressUntilFrame = -1;

        public static bool isSuppressed => Time.frameCount <= s_SuppressUntilFrame;

        public static void SuppressForFrames(int frameCount)
        {
            s_SuppressUntilFrame = Mathf.Max(
                s_SuppressUntilFrame,
                Time.frameCount + Mathf.Max(1, frameCount));
        }
    }
}
