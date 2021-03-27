using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SafeAreaUtil
{
    public static Rect SafeArea => Screen.safeArea;

    public static float TopAreaHeight => Screen.height - SafeArea.yMax;

    public static float BottomAreaHeight => SafeArea.y;
    public static float BottomAreaHeightIncBanner => SafeArea.y + BannerHeight;

    private static float _editorDpi
    {
        get
        {
            var width = Screen.width;
            var height = Screen.height;

            if (width == 1125 && height == 2436)
                return 326f;
            return 160f;
        }
    }

    public static float BannerHeight => 50f * Dpi / 160f;

    public static float Dpi
    {
        get
        {
            var sdpi = Screen.dpi;
            if (sdpi < 25 || sdpi > 1000)
                sdpi = 160;
#if UNITY_EDITOR
            sdpi = _editorDpi;
#endif
            return sdpi;
        }
    }
}