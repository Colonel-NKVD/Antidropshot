using HarmonyLib;
using SDG.Unturned;
using System;
using System.Diagnostics;

namespace AntiDropshot
{
    // Явно указываем типы аргументов: (EPlayerStance, bool), чтобы избежать AmbiguousMatchException
    [HarmonyPatch(typeof(PlayerStance), nameof(PlayerStance.checkStance), new Type[] { typeof(EPlayerStance), typeof(bool) })]
    public static class PlayerStance_CheckStance_Patch
    {
        [HarmonyPrefix]
        public static void Prefix(PlayerStance __instance, EPlayerStance newStance, bool force)
        {
            // Если поза фактически не меняется, игнорируем
            if (__instance.stance == newStance) return;

            // Захватываем стек вызовов
            StackTrace stackTrace = new StackTrace();
            StackFrame[] frames = stackTrace.GetFrames();

            bool isExternalPlugin = false;

            // Проверяем, кто вызвал метод
            for (int i = 1; i < frames.Length; i++)
            {
                var method = frames[i].GetMethod();
                if (method == null || method.DeclaringType == null) continue;

                string assemblyName = method.DeclaringType.Assembly.GetName().Name;

                // Если вызов НЕ от игры, НЕ от движка и НЕ от нашего плагина
                if (assemblyName != "Assembly-CSharp" && 
                    assemblyName != "UnityEngine.CoreModule" &&
                    assemblyName != "Rocket.Unturned" &&
                    assemblyName != "AntiDropshot") 
                {
                    isExternalPlugin = true;
                    break;
                }
            }

            if (isExternalPlugin)
            {
                var comp = __instance.player.GetComponent<AntiDropshotComponent>();
                if (comp != null)
                {
                    // Сообщаем компоненту принять позу "молча" (без штрафов и задержек)
                    comp.SilentlyAcceptStance(newStance);
                }
            }
        }
    }
}
