using UnityEngine;

namespace Abismo
{
    /// <summary>
    /// Fragmento de mente: aparece donde moriste (como la Culpa de Blasphemous).
    /// Mientras no lo recuperes, tu Revelación máxima se reduce y ganas la mitad.
    /// </summary>
    public class MindFragment : MonoBehaviour
    {
        public Transform visual;
        [SerializeField] float collectRadius = 1.1f;

        void Update()
        {
            if (visual != null)
            {
                visual.localPosition = new Vector3(0f, Mathf.Sin(Time.time * 2f) * 0.15f, 0f);
                visual.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 1.3f) * 12f);
            }

            var player = GameManager.Instance != null ? GameManager.Instance.Player : null;
            if (player == null || player.IsDead) return;
            if (Vector2.Distance(player.Center, transform.position) < collectRadius)
            {
                Effects.Sparkle(transform.position, new Color(0.7f, 0.5f, 1f), 24);
                if (GameManager.Instance != null) GameManager.Instance.OnMindFragmentRecovered(this);
                Destroy(gameObject);
            }
        }
    }
}
