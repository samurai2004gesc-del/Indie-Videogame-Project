using UnityEngine;

namespace Abismo
{
    /// <summary>
    /// Cofre del decorado: se abre una sola vez (E) y suelta oro de Innsmouth. No se vuelve a cerrar al rezar.
    /// </summary>
    public class TreasureChest : InteractableZone
    {
        public GoldPickup goldPrefab;
        public SpriteRenderer sprite;
        [SerializeField] int gold = 60;

        bool opened;

        public override string Prompt => "Abrir el cofre";
        protected override float PromptHeight => 1.9f;

        public void Configure(GoldPickup prefab, SpriteRenderer chestSprite, int amount)
        {
            goldPrefab = prefab;
            sprite = chestSprite;
            gold = amount;
        }

        public override void Interact(PlayerController player)
        {
            if (opened) return;
            opened = true;
            if (goldPrefab != null) GoldPickup.SpawnBurst(goldPrefab, (Vector2)transform.position + Vector2.up * 0.8f, gold);
            Effects.Sparkle((Vector2)transform.position + Vector2.up * 0.8f, new Color(1f, 0.85f, 0.4f), 16);
            Sfx.Play(SfxId.Gate, 0.5f);
            GameFeel.Shake(0.15f);
            // Abierto: el cofre se queda apagado y deja de ofrecer el aviso.
            if (sprite != null) sprite.color = new Color(0.6f, 0.55f, 0.5f);
            var trigger = GetComponent<Collider2D>();
            if (trigger != null) trigger.enabled = false;
            player.ClearInteractable(this);
        }
    }
}
