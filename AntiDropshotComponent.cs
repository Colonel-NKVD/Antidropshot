using UnityEngine;
using SDG.Unturned;
using Rocket.Unturned.Player;

namespace AntiDropshot
{
    public class AntiDropshotComponent : MonoBehaviour
    {
        private Player player;
        private float _crouchDuration = 0f;
        private bool _isCorrecting = false;
        private EPlayerStance _lastFrameStance;
        private EPlayerStance _lastValidStance;

        // Настройки
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

            // 1. СПИСАНИЕ СТАМИНЫ (Через askRestamina)
            if (currentStance != _lastFrameStance)
            {
                // Рассчитываем новую стамину (не ниже 0)
                byte currentStamina = player.life.stamina;
                byte newStamina = (byte)Mathf.Max(0, currentStamina - STAMINA_COST);
                
                // askRestamina — самый надежный способ обновления стамины в API SDG
                player.life.askRestamina(newStamina);
                
                _lastFrameStance = currentStance;
            }

            // 2. БЛОКИРОВКА В ВОЗДУХЕ
            if (!player.movement.isGrounded)
            {
                if (currentStance != EPlayerStance.STAND)
                {
                    ForceStanceImmediate(EPlayerStance.STAND);
                }
                _crouchDuration = 0f;
                return;
            }

            // 3. ТАЙМЕР ПРИСЕДА
            if (currentStance == EPlayerStance.CROUCH)
            {
                _crouchDuration += TICK_RATE;
            }
            else if (currentStance != EPlayerStance.PRONE)
            {
                _crouchDuration = 0f;
            }

            // 4. ЛОГИКА "ШЛЮЗА" (ПАДЕНИЕ И ПОДЪЕМ)
            
            // Проверка при падении (PRONE)
            if (currentStance == EPlayerStance.PRONE)
            {
                if (_lastValidStance != EPlayerStance.CROUCH || _crouchDuration < TRANSITION_TIME)
                {
                    ForceStanceImmediate(EPlayerStance.CROUCH);
                    return;
                }
            }

            // Проверка при вставании (STAND)
            if (currentStance == EPlayerStance.STAND)
            {
                // Если лежал или не выждал 3 сек в приседе
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
