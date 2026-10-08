using UnityEngine;

namespace Abismo
{
    /// <summary>Una partícula: se mueve, cae (o sube) y se desvanece. La crea <see cref="Effects"/>.</summary>
    public class SimpleParticle : MonoBehaviour
    {
        Vector2 velocity;
        float lifetime, age, gravity;
        SpriteRenderer sr;
        Color baseColor;
        Vector3 baseScale;

        public void Init(Vector2 startVelocity, float life, float gravityAmount)
        {
            velocity = startVelocity;
            lifetime = Mathf.Max(0.05f, life);
            gravity = gravityAmount;
            sr = GetComponent<SpriteRenderer>();
            baseColor = sr != null ? sr.color : Color.white;
            baseScale = transform.localScale;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            age += dt;
            if (age >= lifetime)
            {
                Destroy(gameObject);
                return;
            }

            velocity.y -= gravity * dt;
            velocity *= 1f - Mathf.Min(1f, 2f * dt);
            transform.position += (Vector3)(velocity * dt);

            float k = 1f - age / lifetime;
            if (sr != null) sr.color = new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a * k);
            transform.localScale = baseScale * (0.5f + 0.5f * k);
        }
    }
}
