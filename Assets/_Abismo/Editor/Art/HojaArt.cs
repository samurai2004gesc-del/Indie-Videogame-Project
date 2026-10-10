using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Abismo.EditorTools
{
    /// <summary>
    /// Arte sacado de la hoja conceptual (ArteHoja/hoja_original.webp): fondos pintados y decorado. Los PNG los deja
    /// Tools/ExtraerHoja/extraer_hoja.py en la carpeta ArteHoja de la raíz del proyecto (fuera de Assets, para que Unity
    /// no los importe dos veces); aquí se leen y se convierten en sprites del juego con su normal map y su brillo.
    /// Si la carpeta no existe, el juego sigue funcionando con el arte pintado por código.
    /// </summary>
    public static class HojaArt
    {
        /// <summary>Carpeta ArteHoja. Relativa a la carpeta del proyecto (el directorio de trabajo de Unity).</summary>
        public static string Raiz = "ArteHoja";

        enum Brillo { Ninguno, Fuego, Rojo }

        sealed class Pieza
        {
            public string Nombre;
            public bool Colgante;   // cuelga del techo (pivote arriba)
            public Brillo Brillo;   // partes que brillan solas
            public bool Luz;        // además pone una luz 2D cálida donde brilla
            public bool SinLuces;   // no recibe luces (niebla)
        }

        static Pieza P(string nombre, bool colgante = false, Brillo brillo = Brillo.Ninguno, bool luz = false, bool sinLuces = false) =>
            new Pieza { Nombre = nombre, Colgante = colgante, Brillo = brillo, Luz = luz, SinLuces = sinLuces };

        static readonly Pieza[] Piezas =
        {
            P("h_columna"), P("h_columna_rota"), P("h_columnilla"), P("h_aguja"), P("h_portico"), P("h_hornacina"),
            P("h_ventana_a"), P("h_ventana_b"), P("h_verja"), P("h_cadenas", colgante: true),
            P("h_farola", brillo: Brillo.Fuego, luz: true), P("h_farolillo", brillo: Brillo.Fuego, luz: true),
            P("h_farol_colgante", colgante: true, brillo: Brillo.Fuego, luz: true), P("h_relicario"), P("h_estatua_verde"),
            P("h_estatua_velada"), P("h_lampara_ojo", colgante: true, brillo: Brillo.Rojo), P("h_tentaculo"), P("h_escombros"),
            P("h_puente"), P("h_balcon"), P("h_balaustrada"), P("h_altar_piedra"),
            P("h_estatua_monje"), P("h_idolo_dorado"), P("h_linterna", brillo: Brillo.Fuego, luz: true), P("h_cofre"), P("h_capilla"),
            P("h_vela", brillo: Brillo.Fuego, luz: true), P("h_relicario_alto"), P("h_estatua_peregrino"), P("h_cruz"),
            P("h_hornacina_farol", brillo: Brillo.Fuego, luz: true), P("h_cruz_pequena"), P("h_puerta"), P("h_estatua_capucha"),
            P("h_estatua_pilar"), P("h_ojo_emblema"), P("h_niebla_a", sinLuces: true), P("h_niebla_b", sinLuces: true),
        };

        /// <summary>Los objetos de la hoja ya traen su llama pintada: en sus FlameAnchors solo se pone la luz.</summary>
        public static bool LlamaPintada(string nombre) => nombre.StartsWith("h_");

        /// <summary>¿Está la carpeta del arte de la hoja?</summary>
        public static bool Disponible => Directory.Exists(Raiz);

        /// <summary>Fondo pintado de una zona (768 px de ancho, se repite en horizontal) o null si no hay.</summary>
        public static PixelCanvas Fondo(Zone zone)
        {
            string archivo = zone == Zone.Ruins ? "ruinas" : zone == Zone.Sanctuary ? "santuario" : zone == Zone.Reef ? "arrecife" : "costa";
            string path = Path.Combine(Raiz, "fondos", archivo + ".png");
            return File.Exists(path) ? PixelCanvas.LoadPng(path) : null;
        }

        /// <summary>Todo el decorado de la hoja que esté en disco, listo para el constructor.</summary>
        public static List<PropSprite> Decorado()
        {
            var lista = new List<PropSprite>();
            foreach (var pieza in Piezas)
            {
                string path = Path.Combine(Raiz, "decorado", pieza.Nombre + ".png");
                if (!File.Exists(path)) continue;
                var color = PixelCanvas.LoadPng(path);
                var prop = new PropSprite
                {
                    Name = pieza.Nombre,
                    Color = color,
                    Pivot01 = pieza.Colgante ? new Vector2(0.5f, 1f) : new Vector2(0.5f, 0f),
                    Unlit = pieza.SinLuces,
                    Normal = pieza.SinLuces ? null : NormalDesdeLuminancia(color),
                };
                if (pieza.Brillo != Brillo.Ninguno)
                {
                    var brillo = MascaraDeBrillo(color, pieza.Brillo, out Vector2 centro, out int cuantos);
                    if (cuantos > 0)
                    {
                        prop.Emission = brillo;
                        // Las anclas de llama son píxeles desde el pivote; aquí solo ponen la luz (la llama ya está pintada).
                        if (pieza.Luz)
                        {
                            Vector2 pivote = new Vector2(color.Width * prop.Pivot01.x, color.Height * prop.Pivot01.y);
                            prop.FlameAnchors.Add(centro - pivote);
                        }
                    }
                }
                lista.Add(prop);
            }
            return lista;
        }

        /// <summary>Píxeles que brillan solos: llamas y cristales encendidos (cálidos) o el ojo rojo.</summary>
        static PixelCanvas MascaraDeBrillo(PixelCanvas color, Brillo tipo, out Vector2 centro, out int cuantos)
        {
            var mask = new PixelCanvas(color.Width, color.Height);
            float sx = 0f, sy = 0f;
            cuantos = 0;
            for (int y = 0; y < color.Height; y++)
            {
                for (int x = 0; x < color.Width; x++)
                {
                    var c = color.Pixels[y * color.Width + x];
                    if (c.a < 128) continue;
                    float r = c.r / 255f, g = c.g / 255f, b = c.b / 255f;
                    float mx = Mathf.Max(r, Mathf.Max(g, b)), mn = Mathf.Min(r, Mathf.Min(g, b));
                    float sat = mx > 0f ? (mx - mn) / mx : 0f;
                    bool brilla = tipo == Brillo.Rojo
                        ? r > 0.55f && r > g * 1.7f && r > b * 1.3f
                        : r >= g && g >= b * 0.9f && ((mx > 0.72f && sat > 0.35f) || (mx > 0.9f && sat > 0.1f));
                    if (!brilla) continue;
                    mask.Pixels[y * color.Width + x] = c;
                    sx += x + 0.5f;
                    sy += y + 0.5f;
                    cuantos++;
                }
            }
            centro = cuantos > 0 ? new Vector2(sx / cuantos, sy / cuantos) : Vector2.zero;
            return mask;
        }

        /// <summary>Normal map aproximado: la luminancia (suavizada) hace de altura, así las luces 2D marcan el relieve.</summary>
        static PixelCanvas NormalDesdeLuminancia(PixelCanvas color)
        {
            int w = color.Width, h = color.Height;
            var alt = new float[w * h];
            for (int i = 0; i < alt.Length; i++)
            {
                var c = color.Pixels[i];
                alt[i] = c.a < 128 ? 0f : (0.299f * c.r + 0.587f * c.g + 0.114f * c.b) / 255f;
            }
            float A(int x, int y) => alt[Mathf.Clamp(y, 0, h - 1) * w + Mathf.Clamp(x, 0, w - 1)];
            var normal = new PixelCanvas(w, h);
            const float fuerza = 2.2f;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int i = y * w + x;
                    if (color.Pixels[i].a < 128) continue;
                    float dx = (A(x + 1, y) - A(x - 1, y)) * fuerza, dy = (A(x, y + 1) - A(x, y - 1)) * fuerza;
                    float nx = -dx, ny = -dy, nz = 1f;
                    float len = Mathf.Sqrt(nx * nx + ny * ny + nz * nz);
                    normal.Pixels[i] = new Color32((byte)Mathf.RoundToInt((nx / len * 0.5f + 0.5f) * 255f),
                                                   (byte)Mathf.RoundToInt((ny / len * 0.5f + 0.5f) * 255f),
                                                   (byte)Mathf.RoundToInt((nz / len * 0.5f + 0.5f) * 255f), 255);
                }
            }
            return normal;
        }
    }
}
