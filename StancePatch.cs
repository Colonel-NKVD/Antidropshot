using HarmonyLib;
using SDG.Unturned;
using System;
using System.Diagnostics;

namespace AntiDropshot
{
    [HarmonyPatch(typeof(PlayerStance), nameof(PlayerStance.checkStance), new Type[] { typeof(EPlayerStance), typeof(bool) })]
    public static class PlayerStance_CheckStance_Patch
    {
        [HarmonyPrefix]
        public static void Prefix(PlayerStance __instance, EPlayerStance newStance, bool all)
        {
            if (__instance.stance == newStance) return;

            StackTrace stackTrace = new StackTrace();
            StackFrame[] frames = stackTrace.GetFrames();

            bool isExternalPlugin = false;

            for (int i = 1; i < frames.Length; i++)
            {
                var method = frames[i].GetMethod();
                if (method == null || method.DeclaringType == null) continue;

                string assemblyName = method.DeclaringType.Assembly.GetName().Name;

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
                    comp.SilentlyAcceptStance(newStance);
                }
            }
        }
    }
}
