using UnityEngine;
using SDG.Unturned;

namespace AntiDropshot
{
    public class AntiDropshotComponent : MonoBehaviour
    {
        private Player player;
        private float _crouchDuration = 0f;
        private bool _isCorrecting = false;
        
        private EPlayerStance _lastValidStance;

        // Настройки баланса
        private const float TRANSITION_TIME = 2.0f; // Уменьшено до 2 секунд, как просили
        private const float STAMINA_COST = 10f;     // Используем float, так как этого требует современный метод

        void Awake()
        {
            player = GetComponent<Player>();
            _lastValidStance = player.stance.stance;
        }

        void FixedUpdate()
        {
            if (player == null || player.life.isDead) return;

            EPlayerStance currentStance = player.stance.stance;

            // 1. БЛОКИРОВКА В ВОЗДУХЕ
            if (!player.movement.isGrounded)
            {
                if (currentStance != EPlayerStance.STAND) ForceStanceImmediate(EPlayerStance.STAND);
                _lastValidStance = EPlayerStance.STAND;
                _crouchDuration = 0f;
                return;
            }

            // 2. ОПТИМИЗАЦИЯ ХОЛОСТОГО ХОДА
            if (currentStance == _lastValidStance)
            {
                if (currentStance == EPlayerStance.CROUCH)
                {
                    _crouchDuration += Time.fixedDeltaTime; 
                }
                return; 
            }

            // === 3. ЛОГИКА ИЗМЕНЕНИЯ ПОЗЫ ===
            bool isTransitionAllowed = true;

            // А) Игрок пытается лечь
            if (currentStance == EPlayerStance.PRONE)
            {
                if (_lastValidStance != EPlayerStance.CROUCH || _crouchDuration < TRANSITION_TIME)
                {
                    isTransitionAllowed = false; 
                }
            }
            // Б) Игрок пытается встать
            else if (currentStance == EPlayerStance.STAND)
            {
                if (_lastValidStance == EPlayerStance.PRONE || (_lastValidStance == EPlayerStance.CROUCH && _crouchDuration < TRANSITION_TIME))
                {
                    isTransitionAllowed = false; 
                }
            }

            // === 4. ПРИМЕНЕНИЕ РЕЗУЛЬТАТА ===
            if (!isTransitionAllowed)
            {
                ForceStanceImmediate(EPlayerStance.CROUCH);
            }
            else
            {
                // Переход разрешен!

                // 100% рабочее списание стамины для современных серверов Unturned
                if (STAMINA_COST > 0 && player.life.stamina >= STAMINA_COST)
                {
                    // Метод serverModifyStamina принимает float. 
                    // Отрицательное значение принудительно отнимает стамину и обновляет полоску у клиента.
                    player.life.serverModifyStamina(-STAMINA_COST); 
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

        void OnDestroy()
        {
            player = null;
        }
    }
}
