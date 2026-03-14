using UnityEngine;
using SDG.Unturned;
using Rocket.Unturned.Player;
using System.Reflection;

namespace AntiDropshot
{
    public class AntiDropshotComponent : MonoBehaviour
    {
        private Player player;
        private float _crouchDuration = 0f;
        private bool _isCorrecting = false;
        private EPlayerStance _lastFrameStance;
        private EPlayerStance _lastValidStance;

        // Рефлексия для доступа к закрытому полю стамины
        private static readonly FieldInfo StaminaField = typeof(PlayerLife).GetField("_stamina", BindingFlags.Instance | BindingFlags.NonPublic);

        private const float TRANSITION_TIME = 3.0f; 
        private const byte STAMINA_COST = 10;
        private const float TICK_RATE = 0.02f;

        void Awake()
        {
            player = GetComponent<Player>();
            _lastFrameStance = player.stance.stance;
            _lastValidStance = _lastFrameStance;
        }

        void FixedUpdate()
        {
            if (player == null || player.life.isDead) return;

            EPlayerStance currentStance = player.stance.stance;

            // 1. СПИСАНИЕ СТАМИНЫ ЧЕРЕЗ РЕФЛЕКСИЮ (Гарантия обхода защиты)
            if (currentStance != _lastFrameStance)
            {
                byte currentStamina = player.life.stamina;
                byte newStamina = (byte)Mathf.Max(0, currentStamina - STAMINA_COST);
                
                // Устанавливаем значение напрямую в закрытое поле _stamina
                if (StaminaField != null)
                {
                    StaminaField.SetValue(player.life, newStamina);
                    // Синхронизируем изменения с клиентом
                    player.life.sendRevive();
                }
                
                _lastFrameStance = currentStance;
            }

            // 2. БЛОКИРОВКА В ВОЗДУХЕ
            if (!player.movement.isGrounded)
            {
                if (currentStance != EPlayerStance.STAND) ForceStanceImmediate(EPlayerStance.STAND);
                _crouchDuration = 0f;
                return;
            }

            // 3. ТАЙМЕР ПРИСЕДА
            if (currentStance == EPlayerStance.CROUCH) _crouchDuration += TICK_RATE;
            else if (currentStance != EPlayerStance.PRONE) _crouchDuration = 0f;

            // 4. ДВУСТОРОННИЙ ШЛЮЗ
            // Вход в PRONE
            if (currentStance == EPlayerStance.PRONE)
            {
                if (_lastValidStance != EPlayerStance.CROUCH || _crouchDuration < TRANSITION_TIME)
                {
                    ForceStanceImmediate(EPlayerStance.CROUCH);
                    return;
                }
            }

            // Выход в STAND
            if (currentStance == EPlayerStance.STAND)
            {
                if (_lastValidStance == EPlayerStance.PRONE || (_lastValidStance == EPlayerStance.CROUCH && _crouchDuration < TRANSITION_TIME))
                {
                    ForceStanceImmediate(EPlayerStance.CROUCH);
                    return;
                }
            }

            _lastValidStance = currentStance;
        }

        private void ForceStanceImmediate(EPlayerStance target)
        {
            if (_isCorrecting) return;
            _isCorrecting = true;
            player.stance.checkStance(target, true);
            _isCorrecting = false;
        }

        void OnDestroy()
        {
            player = null;
        }
    }
}
