using UnityEngine;

namespace Abismo
{
    /// <summary>Muestra el nombre de la zona al cruzarla (como al entrar en una región de Blasphemous).</summary>
    [RequireComponent(typeof(Collider2D))]
    public class AreaTitleTrigger : MonoBehaviour
    {
        [SerializeField] string title = "Zona sin nombre";

        public void Configure(string areaTitle) => title = areaTitle;

        void Reset()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponentInParent<PlayerController>() == null) return;
            if (GameManager.Instance != null) GameManager.Instance.EnterArea(title);
        }
    }
}
