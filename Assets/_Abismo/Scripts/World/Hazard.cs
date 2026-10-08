using UnityEngine;

namespace Abismo
{
    /// <summary>
    /// Peligro del escenario (coral espinoso, agua abisal). Hace daño y te devuelve al último suelo firme.
    /// Activa "instantKill" si lo quieres tan cruel como los pinchos de Blasphemous.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Hazard : MonoBehaviour
    {
        [SerializeField] int damage = 20;
        [SerializeField] bool instantKill = false;

        public void Configure(int hazardDamage, bool kill)
        {
            damage = hazardDamage;
            instantKill = kill;
        }

        void Reset()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        void OnTriggerEnter2D(Collider2D other) => Touch(other);
        void OnTriggerStay2D(Collider2D other) => Touch(other);

        void Touch(Collider2D other)
        {
            var player = other.GetComponentInParent<PlayerController>();
            if (player != null) player.HitByHazard(damage, instantKill);
        }
    }
}
