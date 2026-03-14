using UnityEngine;
using SDG.Unturned;
using Rocket.Unturned.Player;

namespace AntiDropshot
{
    public class AntiDropshotComponent : MonoBehaviour
    {
        private Player player;
        private UnturnedPlayer uPlayer;
        
        private float _crouchDuration = 0f;
        private bool _isCorrecting = false;
        private EPlayerStance _lastActualStance;
        private EPlayerStance _lastValidStance;

        // Настройки баланса
        private const float REQUIRED_TIME = 3.0f; // Задержка для смены позы (сек)
        private const ushort STAMINA_COST = 10;   // Трата стамины
        private const float TICK_RATE = 0.02f;

        void Awake()
        {
            player = GetComponent<Player>();
            uPlayer = UnturnedPlayer.FromPlayer(player);
            _lastActualStance = player.stance.stance;
            _lastValidStance = _lastActualStance;
        }

        void FixedUpdate()
        {
            if (player == null || player.life.isDead) return;

            EPlayerStance currentStance = player.stance.stance;

            // 1. ТРАТА СТАМИНЫ ПРИ ЛЮБОЙ СМЕНЕ ПОЗЫ
            if (currentStance != _lastActualStance)
            {
                // Уменьшаем стамину, не уходя в минус
                if (uPlayer.Stamina > STAMINA_COST)
                    uPlayer.Stamina -= STAMINA_COST;
                else
                    uPlayer.Stamina = 0;

                _lastActualStance = currentStance;
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

            // 3. ЛОГИКА ТАЙМЕРА ПРИСЕДА
            if (currentStance == EPlayerStance.CROUCH)
            {
                _crouchDuration += TICK_RATE;
            }
            else if (currentStance != EPlayerStance.PRONE)
            {
                _crouchDuration = 0f;
            }

            // 4. ЖЕСТКИЙ КОНТРОЛЬ ПЕРЕХОДОВ (ШЛЮЗ)
            
            // А) Блокировка падения (Stand -> Prone мимо Crouch)
            if (currentStance == EPlayerStance.PRONE)
            {
                if (_lastValidStance != EPlayerStance.CROUCH || _crouchDuration < REQUIRED_TIME)
                {
                    ForceStanceImmediate(EPlayerStance.CROUCH);
                    return;
                }
            }

            // Б) Блокировка вставания (Prone -> Stand мимо Crouch)
            if (currentStance == EPlayerStance.STAND && _lastValidStance == EPlayerStance.PRONE)
            {
                // Если игрок лежал и попытался сразу встать — принудительно сажаем
                ForceStanceImmediate(EPlayerStance.CROUCH);
                return;
            }

            // В) Задержка при вставании (Crouch -> Stand после Prone)
            if (currentStance == EPlayerStance.STAND && _lastValidStance == EPlayerStance.CROUCH)
            {
                // Если мы в приседе после положения лежа, нужно выждать те же 3 секунды
                if (_crouchDuration < REQUIRED_TIME)
                {
                    ForceStanceImmediate(EPlayerStance.CROUCH);
                    return;
                }
            }

            // Если все проверки пройдены, обновляем валидную стойку
            _lastValidStance = currentStance;
        }

        private void ForceStanceImmediate(EPlayerStance target)
        {
            if (_isCorrecting) return;
            _isCorrecting = true;

            // Надежный метод смены стойки сервером с принудительной синхронизацией
            player.stance.checkStance(target, true);
            
            _isCorrecting = false;
        }

        void OnDestroy()
        {
            player = null;
            uPlayer = null;
        }
    }
}
