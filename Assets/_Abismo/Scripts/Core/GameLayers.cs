using UnityEngine;

namespace Abismo
{
    /// <summary>
    /// Nombres de las capas (Layers) que usa el juego. El menú "Abismo > Construir demo jugable"
    /// las crea automáticamente en Project Settings > Tags and Layers.
    /// Las máscaras se calculan una sola vez (preguntar por nombre cada frame es lento).
    /// </summary>
    public static class GameLayers
    {
        public const string Ground = "Ground";
        public const string Player = "Player";
        public const string Enemy = "Enemy";

        /// <summary>Píxeles por unidad del arte (1 casilla = 32 px). La cámara se ajusta a esta rejilla.</summary>
        public const float PixelsPerUnit = 32f;

        static int groundMask = -1, playerMask = -1, enemyMask = -1;
        static PhysicsMaterial2D noFriction;

        public static int GroundLayer => LayerMask.NameToLayer(Ground);
        public static int PlayerLayer => LayerMask.NameToLayer(Player);
        public static int EnemyLayer => LayerMask.NameToLayer(Enemy);

        public static LayerMask GroundMask => groundMask >= 0 ? groundMask : (groundMask = LayerMask.GetMask(Ground));
        public static LayerMask PlayerMask => playerMask >= 0 ? playerMask : (playerMask = LayerMask.GetMask(Player));
        public static LayerMask EnemyMask => enemyMask >= 0 ? enemyMask : (enemyMask = LayerMask.GetMask(Enemy));

        /// <summary>Material físico sin fricción: evita que los personajes se queden pegados a las paredes.</summary>
        public static PhysicsMaterial2D NoFriction
        {
            get
            {
                if (noFriction == null) noFriction = new PhysicsMaterial2D("SinFriccion") { friction = 0f, bounciness = 0f };
                return noFriction;
            }
        }

        /// <summary>Limpia la caché al entrar en Play (necesario si desactivas "Domain Reload").</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            groundMask = playerMask = enemyMask = -1;
            noFriction = null;
        }

        /// <summary>Jugador y enemigos se atraviesan (el daño se hace con golpes, no con empujones).</summary>
        public static void SetupCollisionMatrix()
        {
            int player = PlayerLayer, enemy = EnemyLayer;
            if (player < 0 || enemy < 0)
            {
                Debug.LogWarning("[Abismo] Faltan las capas Player/Enemy. Usa el menú Abismo > Construir demo jugable.");
                return;
            }
            Physics2D.IgnoreLayerCollision(player, enemy, true);
            Physics2D.IgnoreLayerCollision(enemy, enemy, true);
        }
    }
}
