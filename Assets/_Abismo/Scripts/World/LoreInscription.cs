using UnityEngine;

namespace Abismo
{
    /// <summary>Lápida con texto: tutoriales o fragmentos de historia del mundo.</summary>
    public class LoreInscription : InteractableZone
    {
        [SerializeField, TextArea(3, 10)] string text = "Las palabras están demasiado erosionadas para leerlas.";
        [SerializeField] string prompt = "Leer la inscripción";

        public override string Prompt => prompt;

        public void Configure(string inscriptionText) => text = inscriptionText;

        public override void Interact(PlayerController player)
        {
            if (HUD.Instance != null) HUD.Instance.ShowReading(text);
        }
    }
}
