using UnityEngine;

namespace Abismo
{
    /// <summary>
    /// Altar del Signo Antiguo (el "reclinatorio" de Blasphemous): rezar aquí te cura,
    /// rellena el láudano y lo convierte en tu punto de reaparición... pero las criaturas reviven.
    /// </summary>
    public class ElderSignAltar : InteractableZone
    {
        public SpriteRenderer glow;

        [SerializeField] Color litColor = new Color(0.45f, 1f, 0.8f, 0.6f);
        [SerializeField] Color unlitColor = new Color(0.45f, 1f, 0.8f, 0.08f);

        bool lit;

        public override string Prompt => "Rezar ante el Signo Antiguo";
        protected override float PromptHeight => 2.9f;
        public Vector2 RespawnPoint => transform.position;

        public override void Interact(PlayerController player)
        {
            player.BeginRest();
            if (GameManager.Instance != null) GameManager.Instance.RestAtAltar(this);
        }

        public void SetLit(bool value) => lit = value;

        void Update()
        {
            if (glow == null) return;
            Color target = lit ? litColor : unlitColor;
            float pulse = lit ? 0.85f + 0.15f * Mathf.Sin(Time.time * 2.2f) : 1f;
            glow.color = new Color(target.r, target.g, target.b, target.a * pulse);
        }
    }
}
