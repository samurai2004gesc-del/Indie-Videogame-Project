using UnityEngine;

namespace Abismo
{
    /// <summary>
    /// Oro de Innsmouth: la moneda del juego. Las criaturas lo sueltan al morir
    /// y vuela hacia ti cuando te acercas.
    /// </summary>
    public class GoldPickup : MonoBehaviour
    {
        public Transform visual;

        [SerializeField] int value = 25;
        [SerializeField] float magnetRadius = 3f;
        [SerializeField] float collectRadius = 0.7f;
        [SerializeField] float gravity = 30f;

        Vector2 velocity;
        bool loose, landed;
        float age;

        public void Configure(int amount) => value = amount;

        /// <summary>Esparce varias monedas que suman <paramref name="total"/>.</summary>
        public static void SpawnBurst(GoldPickup prefab, Vector2 position, int total)
        {
            int coins = Mathf.Clamp(total / 5, 1, 10);
            int each = Mathf.Max(1, total / coins);
            for (int i = 0; i < coins; i++)
            {
                var coin = Instantiate(prefab, position, Quaternion.identity);
                coin.value = i == coins - 1 ? total - each * (coins - 1) : each;
                coin.loose = true;
                coin.velocity = new Vector2(Random.Range(-3f, 3f), Random.Range(5f, 9f));
            }
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            age += dt;
            Vector2 pos = transform.position;

            var player = GameManager.Instance != null ? GameManager.Instance.Player : null;
            if (player != null && !player.IsDead && age > 0.4f)
            {
                Vector2 toPlayer = player.Center - pos;
                float distance = toPlayer.magnitude;
                if (distance < collectRadius)
                {
                    player.Stats.AddGold(value);
                    Sfx.Play(SfxId.Pickup, 0.6f, 0.15f);
                    Effects.Sparkle(pos, new Color(1f, 0.85f, 0.4f), 4);
                    Destroy(gameObject);
                    return;
                }
                if (distance < magnetRadius)
                {
                    velocity = Vector2.Lerp(velocity, toPlayer.normalized * 14f, 8f * dt);
                    transform.position = pos + velocity * dt;
                    return;
                }
            }

            if (loose && !landed)
            {
                velocity.y -= gravity * dt;
                Vector2 step = velocity * dt;
                if (velocity.y < 0f)
                {
                    var hit = Physics2D.Raycast(pos, Vector2.down, -step.y + 0.12f, GameLayers.GroundMask);
                    if (hit)
                    {
                        transform.position = new Vector2(pos.x, hit.point.y + 0.12f);
                        landed = true;
                        velocity = Vector2.zero;
                        return;
                    }
                }
                if (Physics2D.Raycast(pos, new Vector2(Mathf.Sign(step.x), 0f), Mathf.Abs(step.x) + 0.1f, GameLayers.GroundMask))
                {
                    velocity.x = -velocity.x * 0.3f;
                    step.x = 0f;
                }
                transform.position = pos + step;
            }
            else if (visual != null)
            {
                visual.localPosition = new Vector3(0f, Mathf.Sin((Time.time + pos.x) * 3f) * 0.05f, 0f);
            }
        }
    }
}
