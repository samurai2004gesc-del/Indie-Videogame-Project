using UnityEngine;

namespace Abismo
{
    /// <summary>
    /// Base para objetos con los que se interactúa: necesita un Collider2D marcado como "Is Trigger".
    /// Cuando el jugador entra en la zona aparece el aviso "[E] ..." en pantalla.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public abstract class InteractableZone : MonoBehaviour, IInteractable
    {
        public abstract string Prompt { get; }
        public abstract void Interact(PlayerController player);

        /// <summary>Altura (unidades) sobre el pivote a la que flota el aviso "[E] ...".</summary>
        protected virtual float PromptHeight => 2.4f;
        public Vector2 PromptAnchor => (Vector2)transform.position + Vector2.up * PromptHeight;

        protected virtual void Reset()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            var player = other.GetComponentInParent<PlayerController>();
            if (player != null) player.SetInteractable(this);
        }

        void OnTriggerExit2D(Collider2D other)
        {
            var player = other.GetComponentInParent<PlayerController>();
            if (player != null) player.ClearInteractable(this);
        }
    }
}
