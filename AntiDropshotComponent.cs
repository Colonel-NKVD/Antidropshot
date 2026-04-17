using UnityEngine;
using SDG.Unturned;
using HarmonyLib; 
using System.Diagnostics;
using Rocket.Core.Plugins; // Нужно добавить Rocket.API и Rocket.Core в ссылки

namespace AntiDropshot
{
    // 1. ОСНОВНОЙ КЛАСС (Сердце плагина)
    // Именно его RocketMod видит при старте сервера
    public class AntiDropshotPlugin : RocketPlugin
    {
        public static AntiDropshotPlugin Instance;

        protected override void Load()
        {
            Instance = this;

            // ЗАПУСК HARMONY
            // Без этой строки твой патч [HarmonyPatch] будет просто текстом, он не включится!
            var harmony = new Harmony("com.project.antidropshot");
            harmony.PatchAll(); 

            Rocket.Core.Logging.Logger.Log("AntiDropshot загружен и Harmony-патчи применены!");
        }

        protected override void Unload()
        {
            Rocket.Core.Logging.Logger.Log("AntiDropshot выгружен.");
            Instance = null;
        }
    }

    // 2. ХАРМОНИ ПАТЧ (Тот самый "шпион", который смотрит, кто меняет позу)
    [HarmonyPatch(typeof(PlayerStance), nameof(PlayerStance.checkStance))]
    public static class PlayerStance_CheckStance_Patch
    {
        [HarmonyPrefix]
        public static void Prefix(PlayerStance __instance, EPlayerStance newStance)
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
                if (comp != null) comp.SilentlyAcceptStance(newStance);
            }
        }
    }

    // 3. КОМПОНЕНТ (Твоя логика, которую ты вешаешь на игрока)
    public class AntiDropshotComponent : MonoBehaviour
    {
        private Player player;
        private float _crouchDuration = 0f;
        private bool _isCorrecting = false;
        private EPlayerStance _lastValidStance;

        private const float TRANSITION_TIME = 2.0f; 
        private const float STAMINA_COST = 10f;     

        void Awake()
        {
            player = GetComponent<Player>();
            _lastValidStance = player.stance.stance;
        }

        public void SilentlyAcceptStance(EPlayerStance targetStance)
        {
            _lastValidStance = targetStance;
            if (targetStance == EPlayerStance.CROUCH || targetStance == EPlayerStance.PRONE)
                _crouchDuration = TRANSITION_TIME; 
            else
                _crouchDuration = 0f;
        }

        void FixedUpdate()
        {
            if (player == null || player.life.isDead) return;
            EPlayerStance currentStance = player.stance.stance;

            if (!player.movement.isGrounded)
            {
                if (currentStance != EPlayerStance.STAND) ForceStanceImmediate(EPlayerStance.STAND);
                _lastValidStance = EPlayerStance.STAND;
                _crouchDuration = 0f;
                return;
            }

            if (currentStance == _lastValidStance || 
                (currentStance == EPlayerStance.SPRINT && _lastValidStance == EPlayerStance.STAND) ||
                (currentStance == EPlayerStance.STAND && _lastValidStance == EPlayerStance.SPRINT))
            {
                if (currentStance == EPlayerStance.CROUCH) _crouchDuration += Time.fixedDeltaTime; 
                else _crouchDuration = 0f;
                _lastValidStance = currentStance;
                return; 
            }

            bool isTransitionAllowed = true;
            if (currentStance == EPlayerStance.PRONE)
            {
                if (_lastValidStance != EPlayerStance.CROUCH || _crouchDuration < TRANSITION_TIME) isTransitionAllowed = false; 
            }
            else if (currentStance == EPlayerStance.STAND || currentStance == EPlayerStance.SPRINT)
            {
                if (_lastValidStance == EPlayerStance.PRONE || (_lastValidStance == EPlayerStance.CROUCH && _crouchDuration < TRANSITION_TIME)) isTransitionAllowed = false; 
            }

            if (!isTransitionAllowed) ForceStanceImmediate(EPlayerStance.CROUCH);
            else
            {
                if (currentStance != EPlayerStance.SPRINT && _lastValidStance != EPlayerStance.SPRINT)
                {
                    if (STAMINA_COST > 0 && player.life.stamina >= STAMINA_COST) player.life.serverModifyStamina(-STAMINA_COST); 
                }
                _lastValidStance = currentStance;
                _crouchDuration = 0f; 
            }
        }

        private void ForceStanceImmediate(EPlayerStance target)
        {
            if (_isCorrecting) return;
            _isCorrecting = true;
            player.stance.checkStance(target, true);
            _isCorrecting = false;
        }

        void OnDestroy() => player = null;
    }
}
