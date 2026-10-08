using System.Collections.Generic;
using UnityEngine;

namespace Abismo
{
    /// <summary>Qué pasó al intentar dañar a algo.</summary>
    public enum DamageResult
    {
        Ignored,  // no hubo daño (invulnerable, esquivando, ya muerto...)
        Hit,      // recibió el golpe
        Parried,  // el jugador paró el golpe
        Killed    // el golpe lo mató
    }

    /// <summary>Toda la información de un golpe.</summary>
    public struct DamageInfo
    {
        public int Amount;
        public Vector2 SourcePosition;
        public float Knockback;
        public bool Parryable;
        public GameObject Source;

        public DamageInfo(int amount, Vector2 sourcePosition, float knockback, bool parryable, GameObject source)
        {
            Amount = amount;
            SourcePosition = sourcePosition;
            Knockback = knockback;
            Parryable = parryable;
            Source = source;
        }
    }

    /// <summary>Cualquier cosa que puede recibir daño (jugador, enemigos).</summary>
    public interface IDamageable
    {
        DamageResult TakeDamage(DamageInfo info);
    }

    /// <summary>Atacantes que reaccionan cuando el jugador para su golpe.</summary>
    public interface IParryable
    {
        void OnParried();
    }

    /// <summary>Objetos que vuelven a su estado inicial al descansar en un altar o al morir.</summary>
    public interface IResettable
    {
        void ResetState();
    }

    /// <summary>Objetos con los que el jugador puede interactuar (altares, inscripciones).</summary>
    public interface IInteractable
    {
        string Prompt { get; }
        void Interact(PlayerController player);
    }

    /// <summary>Utilidades para buscar objetivos dentro de una zona de golpe.</summary>
    public static class Combat
    {
        static readonly List<Collider2D> buffer = new List<Collider2D>(32);

        static ContactFilter2D Filter(LayerMask mask)
        {
            var filter = ContactFilter2D.noFilter; // incluye triggers
            filter.SetLayerMask(mask);
            return filter;
        }

        /// <summary>Rellena <paramref name="results"/> con todo lo dañable dentro de la caja.</summary>
        public static void OverlapBox(Vector2 center, Vector2 size, LayerMask mask, List<IDamageable> results)
        {
            results.Clear();
            int count = Physics2D.OverlapBox(center, size, 0f, Filter(mask), buffer);
            for (int i = 0; i < count; i++)
            {
                var target = buffer[i].GetComponentInParent<IDamageable>();
                if (target != null && !results.Contains(target)) results.Add(target);
            }
        }

        /// <summary>Rellena <paramref name="results"/> con todo lo dañable dentro del círculo.</summary>
        public static void OverlapCircle(Vector2 center, float radius, LayerMask mask, List<IDamageable> results)
        {
            results.Clear();
            int count = Physics2D.OverlapCircle(center, radius, Filter(mask), buffer);
            for (int i = 0; i < count; i++)
            {
                var target = buffer[i].GetComponentInParent<IDamageable>();
                if (target != null && !results.Contains(target)) results.Add(target);
            }
        }

        /// <summary>Devuelve el jugador si está dentro de la caja (o null).</summary>
        public static PlayerController FindPlayerInBox(Vector2 center, Vector2 size)
        {
            int count = Physics2D.OverlapBox(center, size, 0f, Filter(GameLayers.PlayerMask), buffer);
            for (int i = 0; i < count; i++)
            {
                var player = buffer[i].GetComponentInParent<PlayerController>();
                if (player != null) return player;
            }
            return null;
        }

        /// <summary>Devuelve el jugador si está dentro del círculo (o null).</summary>
        public static PlayerController FindPlayerInCircle(Vector2 center, float radius)
        {
            int count = Physics2D.OverlapCircle(center, radius, Filter(GameLayers.PlayerMask), buffer);
            for (int i = 0; i < count; i++)
            {
                var player = buffer[i].GetComponentInParent<PlayerController>();
                if (player != null) return player;
            }
            return null;
        }
    }
}
