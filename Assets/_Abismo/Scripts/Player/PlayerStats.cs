using UnityEngine;

namespace Abismo
{
    /// <summary>
    /// Números del jugador: Vida, Revelación (el "Fervor" de Blasphemous), frascos de láudano
    /// (los "Frascos Biliares") y Oro de Innsmouth (las "Lágrimas de Expiación").
    /// </summary>
    public class PlayerStats : MonoBehaviour
    {
        [Header("Vida")]
        [SerializeField] int maxHealth = 100;

        [Header("Revelación (se gana golpeando, se gasta en conjuros)")]
        [SerializeField] int maxRevelation = 100;

        [Header("Láudano")]
        [SerializeField] int maxFlasks = 2;
        [SerializeField, Range(0f, 1f)] float flaskHealPercent = 0.45f;

        [Header("Fragmento de mente perdido")]
        [Tooltip("Mientras no recuperes el fragmento, tu Revelación máxima se multiplica por esto.")]
        [SerializeField, Range(0.1f, 1f)] float mindPenaltyFactor = 0.66f;

        public int Health { get; private set; }
        public int MaxHealth => maxHealth;
        public float Revelation { get; private set; }
        public int MaxRevelationBase => maxRevelation;
        public int MaxRevelation => MindPenalty ? Mathf.RoundToInt(maxRevelation * mindPenaltyFactor) : maxRevelation;
        public int Flasks { get; private set; }
        public int MaxFlasks => maxFlasks;
        public int Gold { get; private set; }
        public bool MindPenalty { get; private set; }

        void Awake()
        {
            Health = maxHealth;
            Flasks = maxFlasks;
        }

        public void Damage(int amount) => Health = Mathf.Max(0, Health - Mathf.Max(0, amount));

        public void Heal(int amount) => Health = Mathf.Min(maxHealth, Health + Mathf.Max(0, amount));

        public bool TryUseFlask()
        {
            if (Flasks <= 0) return false;
            Flasks--;
            Heal(Mathf.RoundToInt(maxHealth * flaskHealPercent));
            return true;
        }

        public void AddRevelation(float amount)
        {
            if (MindPenalty) amount *= 0.5f;
            Revelation = Mathf.Min(MaxRevelation, Revelation + amount);
        }

        public bool TrySpendRevelation(float cost)
        {
            if (Revelation < cost) return false;
            Revelation -= cost;
            return true;
        }

        public void AddGold(int amount) => Gold += Mathf.Max(0, amount);

        /// <summary>Al rezar en un altar o reaparecer: vida y frascos al máximo.</summary>
        public void RefillAll()
        {
            Health = maxHealth;
            Flasks = maxFlasks;
        }

        public void SetMindPenalty(bool value)
        {
            MindPenalty = value;
            Revelation = Mathf.Min(Revelation, MaxRevelation);
        }
    }
}
