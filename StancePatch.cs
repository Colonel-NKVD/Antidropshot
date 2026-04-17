using HarmonyLib;
using SDG.Unturned;
using System.Diagnostics;

namespace AntiDropshot
{
    // Перехватываем коренной метод смены позы в Unturned
    [HarmonyPatch(typeof(PlayerStance), nameof(PlayerStance.checkStance))]
    public static class PlayerStance_CheckStance_Patch
    {
        [HarmonyPrefix]
        public static void Prefix(PlayerStance __instance, EPlayerStance newStance)
        {
            // Если поза фактически не меняется, игнорируем
            if (__instance.stance == newStance) return;

            // Захватываем стек вызовов, чтобы понять, КТО пытается изменить позу
            StackTrace stackTrace = new StackTrace();
            StackFrame[] frames = stackTrace.GetFrames();

            bool isExternalPlugin = false;

            // Начинаем с 1, чтобы пропустить сам метод Prefix
            for (int i = 1; i < frames.Length; i++)
            {
                var method = frames[i].GetMethod();
                if (method == null || method.DeclaringType == null) continue;

                string assemblyName = method.DeclaringType.Assembly.GetName().Name;

                // Фильтруем "легальные" вызовы. 
                // Если запрос идет от ядра игры, движка Unity, ядра Rocket или НАШЕГО плагина - это норма.
                if (assemblyName != "Assembly-CSharp" && 
                    assemblyName != "UnityEngine.CoreModule" &&
                    assemblyName != "Rocket.Unturned" &&
                    assemblyName != "AntiDropshot") // Имя сборки вашего плагина
                {
                    // Вызов пришел от неизвестной библиотеки (чужого плагина)!
                    isExternalPlugin = true;
                    break;
                }
            }

            // Если позу меняет чужой плагин, предупреждаем наш компонент
            if (isExternalPlugin)
            {
                AntiDropshotComponent comp = __instance.player.GetComponent<AntiDropshotComponent>();
                if (comp != null)
                {
                    // Говорим компоненту: "Смирись с этой позой, это приказ от другого мода"
                    comp.SilentlyAcceptStance(newStance);
                }
            }
        }
    }
}
