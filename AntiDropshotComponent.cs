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

        // Константы тайминга (3 секунды "отсидки")
        private const float REQUIRED_CROUCH_TIME = 3.0f; 
        private const float TICK_RATE = 0.02f;

        void Awake()
        {
            player = GetComponent<Player>();
        }

        void FixedUpdate()
        {
            if (player == null || player.life.isDead) return;

            EPlayerStance currentStance = player.stance.stance;

            // 1. Блокировка любых смен стоек в воздухе (Ground Lock)
            if (!player.movement.isGrounded)
            {
                if (currentStance != EPlayerStance.STAND)
                {
                    ForceStanceImmediate(EPlayerStance.STAND);
                }
                _crouchDuration = 0f;
                return;
            }

            // 2. Накопительный счетчик времени в приседе
            if (currentStance == EPlayerStance.CROUCH)
            {
                _crouchDuration += TICK_RATE;
            }
            else if (currentStance != EPlayerStance.PRONE)
            {
                // Если игрок встал (STAND/SPRINT) - обнуляем прогресс
                _crouchDuration = 0f;
            }

            // 3. Перехват и блокировка PRONE (Dropshot)
            if (currentStance == EPlayerStance.PRONE)
            {
                if (_crouchDuration < REQUIRED_CROUCH_TIME)
                {
                    // Игрок не выждал 3 секунды. Мгновенный возврат.
                    ForceStanceImmediate(EPlayerStance.CROUCH);
                }
            }
        }

        private void ForceStanceImmediate(EPlayerStance target)
        {
            if (_isCorrecting) return;
            _isCorrecting = true;

            // Используем нативный метод смены стойки. 
            // Второй аргумент 'true' заставляет сервер принудительно обновить пакет для клиента.
            player.stance.checkStance(target, true);

            // Чтобы исключить "проскальзывание" и спам, мы принудительно обновляем 
            // время последней смены стойки в самом движке (если доступно через поле)
            // и фиксируем камеру игрока.
            player.animator.halt(); // Мгновенно останавливает текущие анимации перехода
            
            _isCorrecting = false;
        }

        void OnDestroy()
        {
            player = null;
        }
    }
}
