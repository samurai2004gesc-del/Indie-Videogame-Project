using System.Collections.Generic;
using UnityEngine;

namespace Abismo.EditorTools
{
    /// <summary>Cómo generar e importar un sprite provisional.</summary>
    public class SpriteSpec
    {
        public string Name;
        public System.Func<PixelCanvas> Draw;
        public SpriteAlignment Alignment = SpriteAlignment.Center;
        public Vector2 Pivot = new Vector2(0.5f, 0.5f);
        public bool Smooth;          // filtrado suave (brillos, niebla) en vez de píxel nítido
        public int PixelsPerUnit = 16;
    }

    /// <summary>
    /// Arte PROVISIONAL generado por código: personajes en "ASCII art" (cada letra es un color)
    /// y el resto dibujado con formas y ruido. Sustituye los PNG de Assets/_Abismo/Art/Generated
    /// por tu propio arte cuando quieras: el constructor no los sobrescribe si ya existen.
    /// Escala: 16 píxeles = 1 unidad de Unity = 1 casilla del nivel.
    /// </summary>
    public static class AbismoArt
    {
        public const string Player = "ahogado";
        public const string Weapon = "arma";
        public const string Slash = "tajo";
        public const string DeepOne = "profundo";
        public const string Cultist = "sectario";
        public const string Eye = "ojo_vacio";
        public const string Boss = "arcipreste";
        public const string Tentacle = "tentaculo";
        public const string Orb = "orbe";
        public const string Spell = "signo_arcano";
        public const string Altar = "altar";
        public const string Inscription = "inscripcion";
        public const string Coin = "moneda";
        public const string Fragment = "fragmento";
        public const string FlaskFull = "frasco_lleno";
        public const string FlaskEmpty = "frasco_vacio";
        public const string Spikes = "coral";
        public const string Water = "agua";
        public const string WaterTop = "agua_superficie";
        public const string GroundTop = "suelo_superior";
        public const string GroundA = "suelo_a";
        public const string GroundB = "suelo_b";
        public const string BackWall = "pared_fondo";
        public const string Platform = "plataforma";
        public const string Gate = "reja";
        public const string Glow = "brillo";
        public const string Mote = "mota";
        public const string Vignette = "vineta";
        public const string Sky = "cielo";
        public const string Moon = "luna";
        public const string Colossus = "durmiente";
        public const string FarCity = "ciudad_lejana";
        public const string Ruins = "ruinas";
        public const string Fog = "niebla";

        static readonly Color32 Ink = PixelCanvas.Hex("0a090d");

        public static List<SpriteSpec> All() => new List<SpriteSpec>
        {
            Feet(Player, DrawPlayer),
            new SpriteSpec { Name = Weapon, Draw = DrawWeapon, Alignment = SpriteAlignment.Custom, Pivot = new Vector2(0.1f, 0.45f) },
            Middle(Slash, DrawSlash),
            Feet(DeepOne, DrawDeepOne),
            Feet(Cultist, DrawCultist),
            Middle(Eye, DrawEye),
            Feet(Boss, DrawBoss),
            Feet(Tentacle, DrawTentacle),
            Middle(Orb, DrawOrb),
            Middle(Spell, DrawSpell),
            Feet(Altar, DrawAltar),
            Feet(Inscription, DrawInscription),
            Middle(Coin, DrawCoin),
            Middle(Fragment, DrawFragment),
            Middle(FlaskFull, () => DrawFlask(true)),
            Middle(FlaskEmpty, () => DrawFlask(false)),
            Middle(Spikes, DrawSpikes),
            Middle(Water, () => DrawWater(false)),
            Middle(WaterTop, () => DrawWater(true)),
            Middle(GroundTop, DrawGroundTop),
            Middle(GroundA, () => DrawStone(0, false)),
            Middle(GroundB, () => DrawStone(1, false)),
            Middle(BackWall, () => DrawStone(2, true)),
            Middle(Platform, DrawPlatform),
            Middle(Gate, DrawGate),
            Middle(Glow, DrawGlow, true),
            Middle(Mote, DrawMote),
            Middle(Vignette, DrawVignette, true),
            Middle(Sky, DrawSky, true),
            Middle(Moon, DrawMoon, true),
            Feet(Colossus, DrawColossus),
            Feet(FarCity, DrawFarCity),
            Feet(Ruins, DrawRuins),
            Middle(Fog, DrawFog, true),
        };

        static SpriteSpec Feet(string name, System.Func<PixelCanvas> draw) =>
            new SpriteSpec { Name = name, Draw = draw, Alignment = SpriteAlignment.BottomCenter, Pivot = new Vector2(0.5f, 0f) };

        static SpriteSpec Middle(string name, System.Func<PixelCanvas> draw, bool smooth = false) =>
            new SpriteSpec { Name = name, Draw = draw, Smooth = smooth };

        static Vector2 V(float x, float y) => new Vector2(x, y);
        static Color32 C(string hex) => PixelCanvas.Hex(hex);

        static Dictionary<char, Color32> Palette(params string[] entries)
        {
            var palette = new Dictionary<char, Color32>();
            foreach (var e in entries) palette[e[0]] = C(e.Substring(2));
            return palette;
        }

        // ------------------------------------------------------------------
        // Personajes (ASCII art: cada letra es un color, '.' es transparente)
        // ------------------------------------------------------------------

        // El Ahogado: un pescador de Innsmouth que volvió del Arrecife del Diablo con una escafandra.
        const string PlayerArt = @"
.......bbbbb........
.....bBBBBBBBb......
....bBBBBBBBBBb.....
...bBBBBBBYYYYBb....
...bBBBBBYggggYBb...
..bBBBBBYgGGGggYBb..
..bBBBBBYgGHGGgYBb..
..bBBBBBYgGGGggYBb..
..bBBBBBBYggggYBBb..
...bBBBBBBYYYYBBb...
...bbBBBBBBBBBBbb...
....bbbbbbbbbbbb....
....YBBBBBBBBBBY....
...cCCCCCCCCCCCCc...
..cCCSSCCCCCCCCCCc..
..cCSSCCCCCCCCChhh..
..cCSCCCCCCCCCChhh..
..cCCCCCCCCCCCcChh..
..cCCCCCCCCCCCcc....
..crrrrrrRrrrrrc....
..cCCCCCCCCCCCCc....
..cCCCCCSCCCCCCc....
..ccCCCCSCCCCCcc....
...cCCCCCCCCCCc.....
...cCCcCCCCcCCc.....
...cCc.cCCc.cCc.....
....c.ll..ll.c......
......ll..ll........
......ll..ll........
.....lll..lll.......";

        static PixelCanvas DrawPlayer()
        {
            var palette = Palette("b=5a4120", "B=9c7638", "Y=d8b25c", "g=0f2b26", "G=56e0a8", "H=c8ffe6",
                                  "c=17242a", "C=26393f", "S=3e5a5f", "r=4a2e1c", "R=7a5232", "h=7f9c88", "l=121417");
            var canvas = PixelCanvas.FromAscii(PlayerArt, palette, "GH");
            canvas.Shade();
            canvas.Outline(Ink);
            return canvas;
        }

        const string WeaponArt = @"
.......................MW.
......................MWM.
rrrr.Y................MWM.
rRrrYYMMMMMMMMMMMMMMMMMWM.
rrrr.YmmmmmmmmmmmmmmmmmM..
.....Y....................";

        static PixelCanvas DrawWeapon()
        {
            var palette = Palette("r=4a2e1c", "R=7a5232", "Y=c9a14f", "M=8a959b", "m=4b5257", "W=e0ecef");
            var canvas = PixelCanvas.FromAscii(WeaponArt, palette);
            canvas.Shade(0.12f, 0.15f);
            canvas.Outline(Ink);
            return canvas;
        }

        // Profundo: hombre-pez encorvado con aletas dorsales y garras.
        const string DeepOneArt = @"
......E..E..............
.....EE.EE.FFFF.........
....EEEEEFFFFFFFF.......
....fFFFFFFFFFFFFF......
...fFFFFFFFFFFFeeFF.....
...fFFFFFFFFFFeKeFF.....
...fFFFFFFFFFFFeeFFF....
...fFFFFFFFFFFFFFpppp...
..ffFFFFFFFFFKKKKKKK....
..fFFFFFFFFFFFpppppp....
..fFFFFFFFFFFFFppp......
..fFFFFFFFFFFFFF........
.ffFFFFFppppFFFFFF......
.fFFFFFpppppFFFFFFF.....
.fFFFFFpppppFFF.FFFF....
.fFFFFFpppppFF...FFF....
.ffFFFFFpppFFF...FFF....
..fFFFFFFFFFF....FFF....
..ffFFFFFFFFF...hFhh....
...fFFFFFFFF....h.h.h...
...fFF...fFF............
..fFF.....fFF...........
..fF.......fF...........
.fFF.......fFF..........
.fF.........fF..........
EEEE.......EEEE.........";

        static PixelCanvas DrawDeepOne()
        {
            var palette = Palette("F=3b6a5a", "f=24443b", "E=5f9a7e", "p=a9bf8e", "e=e2e46a", "K=0d1410", "h=c9c3a8");
            var canvas = PixelCanvas.FromAscii(DeepOneArt, palette, "e");
            canvas.Shade();
            canvas.Outline(Ink);
            return canvas;
        }

        // Sectario de la Orden Esotérica de Dagón, con su báculo de brasas.
        const string CultistArt = @"
..............OO...
.............OooO..
.......VV....OooO..
......VVVV....OO...
.....VVVVVV...ss...
....VVVVVVVV..ss...
....VVVVVKKKV.ss...
....VVVVKKoKK.ss...
....VVVVKKKKKVss...
....VVVVVKKKVVss...
...VVVVVVVVVVVhh...
...VVVVVVVVVVhhh...
...VXVVVVVVVVVss...
..vVXVVVVVVVVVss...
..vVXVVVVVVVUVss...
..vVXVVVVVVVUVss...
..vVVVVVVVVVUVss...
..vVVVVVVVVVUVVs...
.vvVVVVVVVVVUVVs...
.vVVVVVVVVVVUVVs...
.vVVVVVVVVVVUVVs...
.vVVVVVVVVVVUVVs...
.vVVVVVVVVVVUVVVs..
vvVVVVVVVVVVUVVVs..
vVVVVVVVVVVVUVVVs..
vVVVVVVVVVVVUVVVs..
vVVVVVVVVVVVUVVVVs.
UUUUUUUUUUUUUUUUUs.
.................s.";

        static PixelCanvas DrawCultist()
        {
            var palette = Palette("V=4a1b34", "v=2c0f1f", "X=6e2c4c", "U=a8822e", "K=0b0508", "o=ff7a3a", "O=ffc070",
                                  "s=4a3020", "h=8a9a88");
            var canvas = PixelCanvas.FromAscii(CultistArt, palette, "oO");
            canvas.Shade();
            canvas.Outline(Ink);
            return canvas;
        }

        const string FlaskArt = @"
...rrr...
...rrr...
...www...
...w.w...
..wwwww..
.wLLLLLw.
wLLLLLLLw
wLLLLLLLw
wLLLLLLLw
wLLLLLLLw
.wLLLLLw.
..wwwww..";

        static PixelCanvas DrawFlask(bool full)
        {
            var palette = Palette("r=7a5232", "w=b8d4cc", full ? "L=c8601e" : "L=22302d");
            var canvas = PixelCanvas.FromAscii(FlaskArt, palette);
            canvas.Shade(0.25f, 0.2f);
            canvas.Outline(Ink);
            return canvas;
        }

        // ------------------------------------------------------------------
        // Personajes dibujados con formas
        // ------------------------------------------------------------------

        static PixelCanvas DrawEye()
        {
            var c = new PixelCanvas(22, 24);
            Color32 tentacle = C("6a3a5c"), vein = C("b0505a");
            for (int i = 0; i < 4; i++)
            {
                float baseX = 6.5f + i * 3f, px = baseX, py = 10f;
                for (int s = 1; s <= 8; s++)
                {
                    float nx = baseX + Mathf.Sin(s * 0.9f + i * 1.7f) * 1.6f, ny = 10f - s * 1.1f;
                    c.Line(px, py, nx, ny, s < 4 ? 2f : 1.2f, tentacle);
                    px = nx;
                    py = ny;
                }
            }
            c.FillCircle(11f, 15f, 7f, C("ddd6c4"));
            c.Line(5f, 13f, 8f, 14.5f, 1f, vein);
            c.Line(16.5f, 18f, 14f, 16.5f, 1f, vein);
            c.Line(7f, 20f, 9f, 18f, 1f, vein);
            c.Shade(0.2f, 0.35f);
            c.FillCircle(12f, 15f, 3.6f, C("b8202e"), true);
            c.FillCircle(12f, 15f, 2.3f, C("e0503a"), true);
            c.FillRect(12, 12, 12, 17, C("120306"), true);
            c.Set(13, 17, C("ffffff"), true);
            c.Outline(Ink);
            return c;
        }

        // El Arcipreste de las Mareas: sacerdote-pez con mitra, barba de tentáculos y báculo.
        static PixelCanvas DrawBoss()
        {
            var c = new PixelCanvas(48, 68);
            Color32 robe = C("4a1630"), robeDark = C("2c0c1c"), gold = C("b08a34"), skin = C("3d6b5d");
            Color32 skinLight = C("6a9c84"), tentacle = C("7a3e66"), wood = C("4a3020");

            c.Line(42f, 1f, 42f, 55f, 2f, wood);
            for (int i = 0; i < 40; i++)
            {
                float a = i * 0.25f, r = 4.5f - i * 0.09f;
                c.FillCircle(40.5f + Mathf.Cos(a) * r, 58f + Mathf.Sin(a) * r, 1f, gold);
            }

            c.FillPolygon(new[] { V(7, 1), V(41, 1), V(34, 36), V(14, 36) }, robe);
            c.Line(14f, 4f, 18f, 33f, 1f, robeDark);
            c.Line(34f, 4f, 30f, 33f, 1f, robeDark);
            c.FillPolygon(new[] { V(22, 1), V(27, 1), V(26.5f, 36), V(22.5f, 36) }, gold);
            c.FillRect(8, 1, 40, 3, gold);
            c.FillEllipse(24f, 36f, 14f, 5.5f, robeDark);
            c.Line(31f, 34f, 41f, 25f, 4f, robe);
            c.FillCircle(41.5f, 25f, 2.4f, skin);

            c.FillEllipse(24f, 44f, 8.5f, 7.5f, skin);
            c.FillEllipse(30f, 42f, 5.5f, 4.2f, skin);
            c.FillEllipse(27f, 39.5f, 6f, 2.2f, skinLight);
            for (int i = 0; i < 6; i++)
            {
                float x0 = 21f + i * 2.2f, px = x0, py = 39f;
                for (int s = 1; s <= 7; s++)
                {
                    float nx = x0 + Mathf.Sin(s * 0.8f + i) * 1.3f, ny = 39f - s * 1.6f;
                    c.Line(px, py, nx, ny, s < 3 ? 2f : 1.4f, tentacle);
                    px = nx;
                    py = ny;
                }
            }

            c.FillPolygon(new[] { V(16, 49), V(33, 49), V(30, 59), V(24.5f, 66), V(19, 59) }, gold);
            c.FillPolygon(new[] { V(22.5f, 49), V(26.5f, 49), V(25.5f, 62), V(23.5f, 62) }, C("7a5a20"));
            c.Shade(0.2f, 0.3f);

            c.FillCircle(29.5f, 46f, 1.9f, C("d8ff70"), true);
            c.Set(30, 46, C("101808"), true);
            DrawElderSign(c, 24.5f, 51f, 8f, C("6ff0c8"), true);
            c.Outline(Ink);
            return c;
        }

        /// <summary>El Signo Antiguo tal y como lo dibujó Lovecraft: una rama con cinco brazos.</summary>
        static void DrawElderSign(PixelCanvas c, float x, float y, float height, Color32 color, bool glow, float thickness = 1f)
        {
            c.Line(x, y, x, y + height, thickness, color, glow);
            float[] levels = { 0.75f, 0.5f, 0.25f };
            float[] spreads = { 0.3f, 0.25f, 0.2f };
            for (int i = 0; i < levels.Length; i++)
            {
                float by = y + height * levels[i];
                float dx = height * spreads[i];
                c.Line(x, by, x - dx, by + dx * 1.1f, thickness, color, glow);
                c.Line(x, by, x + dx, by + dx * 1.1f, thickness, color, glow);
            }
        }

        static PixelCanvas DrawTentacle()
        {
            var c = new PixelCanvas(16, 66);
            Color32 dark = C("6a3a5c"), light = C("8f5a7c"), sucker = C("d8a8b8");
            for (int y = 1; y < 65; y++)
            {
                float t = y / 64f;
                float half = Mathf.Lerp(5f, 0.7f, t);
                float cx = 8f + Mathf.Sin(y * 0.16f) * 2f * t;
                for (int x = 0; x < c.Width; x++)
                {
                    float d = x + 0.5f - cx;
                    if (Mathf.Abs(d) <= half) c.Set(x, y, d > half * 0.3f ? light : dark);
                }
                if (y % 6 == 3 && half > 1.5f) c.Set(Mathf.RoundToInt(cx - half + 1f), y, sucker);
            }
            c.Shade(0.15f, 0.25f);
            c.Outline(Ink);
            return c;
        }

        // ------------------------------------------------------------------
        // Efectos y objetos
        // ------------------------------------------------------------------

        static PixelCanvas DrawSlash()
        {
            var c = new PixelCanvas(40, 48);
            const float cx = 4f, cy = 24f, inner = 15f, outer = 23f, maxAngle = 80f;
            for (int y = 0; y < c.Height; y++)
            {
                for (int x = 0; x < c.Width; x++)
                {
                    float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    float angle = Mathf.Abs(Mathf.Atan2(dy, dx) * Mathf.Rad2Deg);
                    if (angle > maxAngle || r < inner || r > outer) continue;
                    float thick = 1f - angle / maxAngle;
                    float edge = (r - inner) / (outer - inner);
                    float limit = 0.25f + 0.75f * thick;
                    if (edge > limit) continue;
                    float alpha = Mathf.Lerp(1f, 0.25f, edge / limit) * Mathf.Sqrt(thick);
                    c.Set(x, y, PixelCanvas.WithAlpha(new Color32(255, 255, 255, 255), alpha));
                }
            }
            return c;
        }

        static PixelCanvas DrawOrb()
        {
            var c = new PixelCanvas(12, 12);
            for (int y = 0; y < 12; y++)
            {
                for (int x = 0; x < 12; x++)
                {
                    float d = Mathf.Sqrt((x + 0.5f - 6f) * (x + 0.5f - 6f) + (y + 0.5f - 6f) * (y + 0.5f - 6f)) / 6f;
                    if (d > 1f) continue;
                    float a = Mathf.Clamp01(1.25f - d * 1.2f);
                    byte v = (byte)Mathf.RoundToInt(Mathf.Lerp(255f, 170f, d));
                    c.Set(x, y, PixelCanvas.WithAlpha(new Color32(v, v, v, 255), a));
                }
            }
            return c;
        }

        static PixelCanvas DrawSpell()
        {
            var c = new PixelCanvas(20, 20);
            for (int y = 0; y < 20; y++)
            {
                for (int x = 0; x < 20; x++)
                {
                    float d = Mathf.Sqrt((x + 0.5f - 10f) * (x + 0.5f - 10f) + (y + 0.5f - 10f) * (y + 0.5f - 10f)) / 10f;
                    if (d < 1f) c.Set(x, y, PixelCanvas.WithAlpha(new Color32(255, 255, 255, 255), (1f - d) * 0.35f));
                }
            }
            DrawElderSign(c, 10f, 3f, 14f, new Color32(255, 255, 255, 255), true, 1.6f);
            return c;
        }

        static PixelCanvas DrawAltar()
        {
            var c = new PixelCanvas(26, 40);
            Color32 stone = C("4a5257"), stoneDark = C("2e3438"), tablet = C("5a6267"), moss = C("3f6e57");
            c.FillRect(2, 1, 23, 4, stoneDark);
            c.FillRect(4, 5, 21, 7, stone);
            c.FillRect(8, 8, 17, 21, stone);
            c.FillRect(10, 9, 10, 20, stoneDark);
            c.FillRect(4, 22, 21, 24, stone);
            c.FillRect(7, 25, 18, 32, tablet);
            c.FillCircle(12.5f, 32.5f, 5.5f, tablet);
            for (int x = 2; x <= 23; x++)
            {
                if (PixelCanvas.Hash(x, 0, 7) > 0.45f) c.Set(x, 4, moss);
                if (x >= 4 && x <= 21 && PixelCanvas.Hash(x, 1, 7) > 0.5f) c.Set(x, 24, moss);
            }
            c.Shade(0.2f, 0.3f);
            DrawElderSign(c, 12.5f, 27f, 9f, C("78f5cf"), true);
            c.Outline(Ink);
            return c;
        }

        static PixelCanvas DrawInscription()
        {
            var c = new PixelCanvas(18, 24);
            Color32 stone = C("4f575c"), dark = C("262b2e"), moss = C("3f6e57");
            c.FillRect(2, 1, 15, 15, stone);
            c.FillCircle(8.5f, 15f, 6.5f, stone);
            c.FillRect(1, 1, 16, 2, C("3a4044"));
            foreach (int y in new[] { 5, 8, 11, 14 })
            {
                for (int x = 4; x <= 13; x++)
                {
                    if (PixelCanvas.Hash(x, y, 3) > 0.25f) c.Set(x, y, dark);
                }
            }
            for (int x = 2; x <= 15; x++)
            {
                if (PixelCanvas.Hash(x, 2, 9) > 0.6f) c.Set(x, 3, moss);
            }
            c.Shade();
            c.Outline(Ink);
            return c;
        }

        static PixelCanvas DrawCoin()
        {
            var c = new PixelCanvas(8, 8);
            c.FillCircle(4f, 4f, 3.4f, C("b8862a"));
            c.FillCircle(4f, 4f, 2.2f, C("e8bc48"));
            c.Set(3, 5, C("fff2b0"));
            c.Set(4, 5, C("fff2b0"));
            c.Outline(C("4a300c"));
            return c;
        }

        static PixelCanvas DrawFragment()
        {
            var c = new PixelCanvas(12, 18);
            c.FillPolygon(new[] { V(6, 1), V(10.5f, 8), V(6.5f, 16.5f), V(1.5f, 9) }, C("7a4ad8"), true);
            c.FillPolygon(new[] { V(6, 1), V(6.5f, 16.5f), V(1.5f, 9) }, C("a888ff"), true);
            c.Line(6f, 3f, 6.3f, 15f, 1f, C("e0d0ff"), true);
            c.Outline(C("2a1050"));
            return c;
        }

        static PixelCanvas DrawGlow()
        {
            var c = new PixelCanvas(64, 64);
            for (int y = 0; y < 64; y++)
            {
                for (int x = 0; x < 64; x++)
                {
                    float d = Mathf.Sqrt((x + 0.5f - 32f) * (x + 0.5f - 32f) + (y + 0.5f - 32f) * (y + 0.5f - 32f)) / 32f;
                    float a = Mathf.Clamp01(1f - d);
                    c.Set(x, y, PixelCanvas.WithAlpha(new Color32(255, 255, 255, 255), a * a));
                }
            }
            return c;
        }

        static PixelCanvas DrawMote()
        {
            var c = new PixelCanvas(3, 3);
            var white = new Color32(255, 255, 255, 255);
            for (int y = 0; y < 3; y++)
                for (int x = 0; x < 3; x++)
                    c.Set(x, y, PixelCanvas.WithAlpha(white, x == 1 && y == 1 ? 1f : (x == 1 || y == 1 ? 0.45f : 0f)));
            return c;
        }

        static PixelCanvas DrawVignette()
        {
            var c = new PixelCanvas(128, 128);
            for (int y = 0; y < 128; y++)
            {
                for (int x = 0; x < 128; x++)
                {
                    float dx = (x + 0.5f) / 128f - 0.5f, dy = (y + 0.5f) / 128f - 0.5f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy) / 0.7071f;
                    float a = Mathf.Clamp01((d - 0.55f) / 0.45f);
                    c.Set(x, y, PixelCanvas.WithAlpha(new Color32(0, 0, 0, 255), a * a * (3f - 2f * a)));
                }
            }
            return c;
        }

        // ------------------------------------------------------------------
        // Escenario (tiles de 16x16)
        // ------------------------------------------------------------------

        static PixelCanvas DrawStone(int variant, bool background)
        {
            var c = new PixelCanvas(16, 16);
            Color32 baseColor = background ? C("161b1e") : C("343b3f");
            Color32 dark = background ? C("0c0f11") : C("1c2124");
            Color32 light = background ? C("20272a") : C("4b5459");
            int seamY = variant == 1 ? 8 : 7;
            int seamTopX = variant == 1 ? 11 : 5;
            int seamBottomX = variant == 1 ? 3 : 12;
            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    float n = PixelCanvas.Hash(x, y, 3 + variant);
                    Color32 color = PixelCanvas.Lerp(baseColor, n > 0.5f ? light : dark, Mathf.Abs(n - 0.5f) * 0.6f);
                    bool seam = y == seamY || (y > seamY && x == seamTopX) || (y < seamY && x == seamBottomX);
                    if (seam) color = dark;
                    else if (y == seamY + 1 || (y > seamY && x == seamTopX + 1) || (y < seamY && x == seamBottomX + 1)) color = PixelCanvas.Lerp(color, light, 0.5f);
                    c.Set(x, y, color);
                }
            }
            return c;
        }

        static PixelCanvas DrawGroundTop()
        {
            var c = DrawStone(0, false);
            Color32 moss = C("2d5646"), mossLight = C("4f8a6c"), mossDark = C("1e3b31");
            for (int x = 0; x < 16; x++)
            {
                int depth = 2 + Mathf.FloorToInt(PixelCanvas.Hash(x, 0, 21) * 3f);
                for (int y = 16 - depth; y < 16; y++)
                {
                    c.Set(x, y, y == 15 ? mossLight : (y == 16 - depth ? mossDark : moss));
                }
                if (PixelCanvas.Hash(x, 1, 22) > 0.75f)
                {
                    c.Set(x, 15 - depth, mossDark);
                    if (PixelCanvas.Hash(x, 2, 23) > 0.5f) c.Set(x, 14 - depth, mossDark);
                }
            }
            return c;
        }

        static PixelCanvas DrawPlatform()
        {
            var c = new PixelCanvas(16, 16);
            Color32 wood = C("4a3424"), woodLight = C("6b4a30"), woodDark = C("2a1c12"), nail = C("9aa0a0");
            for (int x = 0; x < 16; x++)
            {
                c.Set(x, 15, woodLight);
                for (int y = 12; y <= 14; y++) c.Set(x, y, x % 8 == 7 ? woodDark : wood);
                c.Set(x, 11, woodDark);
            }
            c.Set(2, 13, nail);
            c.Set(10, 13, nail);
            return c;
        }

        static PixelCanvas DrawGate()
        {
            var c = new PixelCanvas(16, 16);
            Color32 iron = C("2b2d33"), ironLight = C("4a4e57"), rust = C("5a3422");
            foreach (int bx in new[] { 2, 7, 12 })
            {
                for (int y = 0; y < 16; y++)
                {
                    c.Set(bx, y, iron);
                    c.Set(bx + 1, y, ironLight);
                    if (PixelCanvas.Hash(bx, y, 5) > 0.85f) c.Set(bx, y, rust);
                }
            }
            for (int x = 0; x < 16; x++)
            {
                c.Set(x, 4, iron);
                c.Set(x, 5, ironLight);
                c.Set(x, 12, iron);
            }
            return c;
        }

        const string SpikesArt = @"
................
................
................
................
................
................
...p.......p....
...p...p...p....
..pp...p..ppp...
..pP..pPp.pPp...
.pPP..pPp.pPp..p
.pPP.pPPPppPP.pP
pPPPppPPPpPPPppP
PPPPPPPPPPPPPPPP
PPPPPPPPPPPPPPPP
PPPPPPPPPPPPPPPP";

        static PixelCanvas DrawSpikes()
        {
            var palette = Palette("p=d8cfc0", "P=8f5f66");
            return PixelCanvas.FromAscii(SpikesArt, palette, "", false);
        }

        static PixelCanvas DrawWater(bool surface)
        {
            var c = new PixelCanvas(16, 16);
            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    float n = PixelCanvas.Fbm(x / 4f, y / 4f, 2, 4, 11);
                    Color32 color = PixelCanvas.Lerp(C("0a2226"), C("14403f"), n);
                    if (surface && y == 15) color = (x + 1) % 8 < 5 ? C("6fc2ac") : C("3f8f86");
                    else if (surface && y == 14) color = (x + 3) % 8 < 4 ? C("2f7a70") : color;
                    c.Set(x, y, PixelCanvas.WithAlpha(color, 0.93f));
                }
            }
            return c;
        }

        // ------------------------------------------------------------------
        // Fondos (se repiten en horizontal)
        // ------------------------------------------------------------------

        static PixelCanvas DrawSky()
        {
            var c = new PixelCanvas(4, 128);
            Color32 horizon = C("1e3832"), middle = C("0f1f20"), top = C("04080a");
            for (int y = 0; y < 128; y++)
            {
                float t = y / 127f;
                Color32 color = t < 0.35f ? PixelCanvas.Lerp(horizon, middle, t / 0.35f) : PixelCanvas.Lerp(middle, top, (t - 0.35f) / 0.65f);
                for (int x = 0; x < 4; x++) c.Set(x, y, color);
            }
            return c;
        }

        static PixelCanvas DrawMoon()
        {
            var c = new PixelCanvas(96, 96);
            const float r = 30f, halo = 17f;
            for (int y = 0; y < 96; y++)
            {
                for (int x = 0; x < 96; x++)
                {
                    float d = Mathf.Sqrt((x + 0.5f - 48f) * (x + 0.5f - 48f) + (y + 0.5f - 48f) * (y + 0.5f - 48f));
                    if (d <= r)
                    {
                        float craters = PixelCanvas.Fbm(x / 9f, y / 9f, 3, 0, 4);
                        Color32 color = PixelCanvas.Lerp(C("cfe3b8"), C("8aae88"), Mathf.Clamp01((craters - 0.45f) * 2.5f));
                        color = PixelCanvas.Lerp(color, C("6f9478"), Mathf.Clamp01((d - r + 6f) / 6f) * 0.5f);
                        c.Set(x, y, color);
                    }
                    else if (d <= r + halo)
                    {
                        float a = 1f - (d - r) / halo;
                        c.Set(x, y, PixelCanvas.WithAlpha(C("a8d8b0"), a * a * 0.35f));
                    }
                }
            }
            return c;
        }

        // Una silueta colosal que duerme bajo el mar... y abre los ojos.
        static PixelCanvas DrawColossus()
        {
            var c = new PixelCanvas(256, 200);
            Color32 shape = C("0b1517"), front = C("12232a"), rim = C("173033");

            // Alas membranosas con el borde inferior festoneado.
            var leftWing = new[] { V(92, 100), V(62, 150), V(16, 197), V(36, 165), V(14, 158), V(44, 140), V(26, 126), V(58, 118), V(50, 104), V(76, 104) };
            c.FillPolygon(leftWing, shape);
            var rightWing = new Vector2[leftWing.Length];
            for (int i = 0; i < leftWing.Length; i++) rightWing[i] = V(256f - leftWing[i].x, leftWing[i].y);
            c.FillPolygon(rightWing, shape);
            c.Line(92f, 100f, 16f, 197f, 3f, shape);
            c.Line(164f, 100f, 240f, 197f, 3f, shape);

            // Hombros encorvados y cabeza de pulpo alargada.
            c.FillPolygon(new[] { V(36, 0), V(220, 0), V(204, 66), V(172, 102), V(84, 102), V(52, 66) }, shape);
            c.FillEllipse(128f, 142f, 27f, 38f, shape);
            c.FillEllipse(128f, 118f, 22f, 12f, shape);

            // Barba de tentáculos: algo más clara para que se lea sobre el pecho.
            for (int i = 0; i < 7; i++)
            {
                float x0 = 110f + i * 6f, px = x0, py = 120f;
                int segments = 7 + (i * 5) % 4;
                for (int s = 1; s <= segments; s++)
                {
                    float nx = x0 + Mathf.Sin(s * 0.7f + i * 1.9f) * 5f, ny = 120f - s * 7.5f;
                    c.Line(px, py, nx, ny, Mathf.Lerp(7f, 2f, s / (float)segments), front);
                    px = nx;
                    py = ny;
                }
            }
            RimLight(c, rim);
            c.FillCircle(117f, 140f, 2.3f, C("c8ff6a"), true);
            c.FillCircle(139f, 140f, 2.3f, C("c8ff6a"), true);
            for (int y = 0; y < 40; y++)
            {
                for (int x = 0; x < c.Width; x++)
                {
                    var p = c.Get(x, y);
                    if (p.a > 0) c.Set(x, y, PixelCanvas.WithAlpha(p, y / 40f));
                }
            }
            return c;
        }

        static void FillPolygonWrapped(PixelCanvas c, Vector2[] poly, Color32 color)
        {
            foreach (float shift in new[] { -c.Width, 0f, c.Width })
            {
                var moved = new Vector2[poly.Length];
                for (int i = 0; i < poly.Length; i++) moved[i] = V(poly[i].x + shift, poly[i].y);
                c.FillPolygon(moved, color);
            }
        }

        /// <summary>Luz de luna en los bordes derechos de las siluetas (respetando la repetición).</summary>
        static void RimLight(PixelCanvas c, Color32 rim)
        {
            var copy = (Color32[])c.Pixels.Clone();
            for (int y = 0; y < c.Height; y++)
            {
                for (int x = 0; x < c.Width; x++)
                {
                    if (copy[y * c.Width + x].a == 0) continue;
                    int right = (x + 1) % c.Width;
                    bool open = copy[y * c.Width + right].a == 0 || (y + 1 < c.Height && copy[(y + 1) * c.Width + x].a == 0);
                    if (open) c.Set(x, y, rim);
                }
            }
        }

        // Ciudad ciclópea con ángulos que no deberían existir, y el mar a sus pies.
        static PixelCanvas DrawFarCity()
        {
            var c = new PixelCanvas(512, 128);
            var rng = new System.Random(5);
            float R() => (float)rng.NextDouble();
            for (int i = 0; i < 28; i++)
            {
                float x = R() * 512f, w = 6f + R() * 22f, h = 30f + R() * 80f, lean = (R() - 0.5f) * 0.6f * h;
                FillPolygonWrapped(c, new[] { V(x - w / 2f, 18), V(x + w / 2f, 18), V(x + w * 0.3f + lean, 18 + h), V(x - w * 0.3f + lean, 18 + h * 0.9f) }, C("0e1c1e"));
            }
            for (int i = 0; i < 10; i++)
            {
                float x = R() * 512f, w = 20f + R() * 40f, h = 10f + R() * 18f;
                FillPolygonWrapped(c, new[] { V(x - w / 2f, 18), V(x + w / 2f, 18), V(x + w / 2f - 4f, 18 + h), V(x - w / 2f + 6f, 18 + h + 4f) }, C("0e1c1e"));
            }
            RimLight(c, C("1d3532"));
            for (int y = 0; y < 22; y++)
            {
                for (int x = 0; x < 512; x++)
                {
                    Color32 color = C("0a1416");
                    if (y > 3 && PixelCanvas.Hash(x / 3, y, 9) > 0.93f) color = C("2c4a42");
                    c.Set(x, y, color);
                }
            }
            return c;
        }

        static PixelCanvas DrawRuins()
        {
            var c = new PixelCanvas(512, 160);
            Color32 ruin = C("091315");
            var rng = new System.Random(12);
            float R() => (float)rng.NextDouble();

            for (int x = 0; x < 512; x++)
            {
                int top = 10 + Mathf.RoundToInt(PixelCanvas.Fbm(x / 32f, 0.5f, 3, 16, 2) * 26f);
                for (int y = 0; y <= top; y++) c.Set(x, y, ruin);
            }
            for (int i = 0; i < 12; i++)
            {
                float x = R() * 512f, w = 8f + R() * 14f, h = 40f + R() * 90f, lean = (R() - 0.5f) * 30f;
                FillPolygonWrapped(c, new[] { V(x - w / 2f, 10), V(x + w / 2f, 10), V(x + w / 2f + lean, h), V(x - w / 2f + lean - 3f, h - 6f) }, ruin);
            }
            for (int i = 0; i < 4; i++)
            {
                float ax = 64f + i * 128f + (R() - 0.5f) * 40f, outer = 22f + R() * 10f, inner = outer - 7f, baseY = 30f;
                float broken = R() * 3.14f;
                for (int y = 0; y < c.Height; y++)
                {
                    for (int dx = -40; dx <= 40; dx++)
                    {
                        float px = dx + 0.5f, py = y + 0.5f - baseY;
                        if (py < 0f) continue;
                        float d = Mathf.Sqrt(px * px + py * py);
                        float angle = Mathf.Atan2(py, px);
                        if (d >= inner && d <= outer && Mathf.Abs(angle - broken) > 0.5f)
                        {
                            int wx = ((Mathf.RoundToInt(ax) + dx) % 512 + 512) % 512;
                            c.Set(wx, y, ruin);
                        }
                    }
                    if (y < baseY + 2)
                    {
                        for (int k = -1; k <= 1; k += 2)
                        {
                            for (int t = 0; t < 7; t++)
                            {
                                int wx = ((Mathf.RoundToInt(ax + k * (inner + 3.5f)) + t - 3) % 512 + 512) % 512;
                                c.Set(wx, y, ruin);
                            }
                        }
                    }
                }
            }
            RimLight(c, C("17302c"));
            return c;
        }

        static PixelCanvas DrawFog()
        {
            var c = new PixelCanvas(256, 64);
            for (int y = 0; y < 64; y++)
            {
                for (int x = 0; x < 256; x++)
                {
                    float n = PixelCanvas.Fbm(x / 32f, y / 16f, 4, 8, 3);
                    float a = Mathf.Clamp01((n - 0.35f) / 0.45f);
                    a = a * a * (3f - 2f * a) * Mathf.Sin(Mathf.PI * (y + 0.5f) / 64f);
                    c.Set(x, y, PixelCanvas.WithAlpha(new Color32(255, 255, 255, 255), a));
                }
            }
            return c;
        }
    }
}
