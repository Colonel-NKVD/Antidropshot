using UnityEngine;
using SDG.Unturned;
using Rocket.Unturned.Player;
using Steamworks;

namespace AntiDropshot
{
    public class AntiDropshotComponent : MonoBehaviour
    {
        private Player player;
        private float _crouchDuration = 0f;
        private bool _isCorrecting = false;
        private EPlayerStance _lastFrameStance;
        private EPlayerStance _lastValidStance;

        // Константы
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

            // 1. ГАРАНТИРОВАННОЕ СПИСАНИЕ СТАМИНЫ (Ядерный вариант)
            if (currentStance != _lastFrameStance)
            {
                // Используем askDamage с типом STAMINA. 
                // Это единственный 100% публичный путь во всех версиях API.
                EPlayerKill kill;
                player.life.askDamage(
                    STAMINA_COST, 
                    Vector3.up, 
                    EDeathCause.STAMINA, 
                    ELimb.SPINE, 
                    CSteamID.Nil, 
                    out kill
                );
                
                _lastFrameStance = currentStance;
            }

            // 2. GROUND LOCK
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
            
            // Нативный метод из вашего SuppressionSystem
            player.stance.checkStance(target, true);
            
            _isCorrecting = false;
        }

        void OnDestroy()
        {
            player = null;
        }
    }
}
