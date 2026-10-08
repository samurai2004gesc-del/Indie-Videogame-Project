using UnityEngine;

namespace Abismo
{
    /// <summary>Motas luminosas que flotan alrededor de la cámara, como esporas en agua profunda.</summary>
    public class AmbientMotes : MonoBehaviour
    {
        public Sprite sprite;
        public Material material;

        [SerializeField] int count = 45;
        [SerializeField] Color color = new Color(0.55f, 1f, 0.8f, 0.35f);
        [SerializeField] Vector2 area = new Vector2(32f, 18f);
        [SerializeField] int sortingOrder = 25;

        Transform cam;
        Transform[] motes;
        SpriteRenderer[] renderers;
        Vector2[] velocities;
        float[] phases;

        void Start()
        {
            var main = Camera.main;
            if (main == null || sprite == null) return;
            cam = main.transform;

            motes = new Transform[count];
            renderers = new SpriteRenderer[count];
            velocities = new Vector2[count];
            phases = new float[count];
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject("Mota");
                go.transform.SetParent(transform, false);
                go.transform.position = (Vector2)cam.position + new Vector2(Random.Range(-0.5f, 0.5f) * area.x, Random.Range(-0.5f, 0.5f) * area.y);
                float s = Random.Range(0.5f, 1.2f);
                go.transform.localScale = new Vector3(s, s, 1f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.color = color;
                sr.sortingOrder = sortingOrder;
                if (material != null) sr.sharedMaterial = material;

                motes[i] = go.transform;
                renderers[i] = sr;
                velocities[i] = new Vector2(Random.Range(-0.2f, 0.2f), Random.Range(0.1f, 0.45f));
                phases[i] = Random.value * 10f;
            }
        }

        void LateUpdate()
        {
            if (cam == null || motes == null) return;
            float dt = Time.deltaTime;
            Vector2 center = cam.position;
            Vector2 half = area * 0.5f;

            for (int i = 0; i < motes.Length; i++)
            {
                phases[i] += dt;
                Vector2 p = (Vector2)motes[i].position + (velocities[i] + new Vector2(Mathf.Sin(phases[i] * 0.7f) * 0.3f, 0f)) * dt;
                Vector2 local = p - center;
                if (local.x > half.x) p.x -= area.x;
                else if (local.x < -half.x) p.x += area.x;
                if (local.y > half.y) p.y -= area.y;
                else if (local.y < -half.y) p.y += area.y;
                motes[i].position = p;

                float twinkle = 0.5f + 0.5f * Mathf.Sin(phases[i] * 1.7f);
                renderers[i].color = new Color(color.r, color.g, color.b, color.a * twinkle);
            }
        }
    }
}
