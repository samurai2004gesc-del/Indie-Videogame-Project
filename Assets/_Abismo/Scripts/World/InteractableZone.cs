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
