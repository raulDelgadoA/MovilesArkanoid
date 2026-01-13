using UnityEngine;
using System.Collections;

public static class Vibration
{
#if UNITY_ANDROID && !UNITY_EDITOR
    public static AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
    public static AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
    public static AndroidJavaObject vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator");
#endif

    public static void Vibrate(long milliseconds)
    {
        // 1. Primero miramos si el jugador desactivó la vibración en Opciones
        if (PlayerPrefs.GetInt("VibrationEnabled", 1) == 0) return;

#if UNITY_ANDROID && !UNITY_EDITOR
        // En Android podemos controlar los milisegundos exactos
        if (vibrator != null)
            vibrator.Call("vibrate", milliseconds);
#else
        // En iOS o Editor, usamos la vibración estándar (no deja elegir tiempo)
        // Solo vibramos si el golpe es fuertecillo (> 30ms) para no molestar en iOS
        if (milliseconds >= 30)
            Handheld.Vibrate();
#endif
    }
}