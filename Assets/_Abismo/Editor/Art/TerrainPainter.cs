using System.Collections.Generic;
using UnityEngine;

namespace Abismo.EditorTools
{
    /// <summary>Zonas del mundo: cada una tiene su paleta y su arquitectura.</summary>
    public enum Zone { Coast, Ruins, Sanctuary, Reef }

    /// <summary>Paleta y rasgos de una zona.</summary>
    public sealed class TerrainTheme
    {
        public Color32[] Stone;     // sillería (oscuro → claro)
        public Color32[] Cobble;    // adoquines del camino (oscuro → claro), el ocre de Blasphemous
        public Color32[] Earth;     // tierra y cascotes bajo los adoquines
        public Color32[] Moss;      // musgo / algas
        public Color32 Deep;        // color al que se funde la roca en profundidad
        public Color32 Accent;      // detalle (oro, coral, runas)
        public float MossAmount;    // 0..1
        public bool AccentInlays;   // incrustaciones (oro en el santuario, coral en el arrecife)
        public bool GlowingGlyphs;  // runas tenues en las ruinas

        static readonly Dictionary<Zone, TerrainTheme> cache = new Dictionary<Zone, TerrainTheme>();

        public static TerrainTheme For(Zone zone)
        {
            if (!cache.TryGetValue(zone, out var theme))
            {
                theme = Create(zone);
                cache[zone] = theme;
            }
            return theme;
        }

        static TerrainTheme Create(Zone zone)
        {
            switch (zone)
            {
                case Zone.Ruins:
                    return new TerrainTheme
                    {
                        Stone = Ramp.Make("4a5446", 6, 0.1f, 0.25f, 1.6f),
                        Cobble = Ramp.Make("98925a", 6, 0.12f, 0.24f, 1.5f), Earth = Ramp.Make("2c2a22", 4, 0.08f, 0.5f, 1.5f),
                        Moss = Ramp.Make("355e45", 4, 0.1f), Deep = PixelCanvas.Hex("0a0c09"), Accent = PixelCanvas.Hex("4fd6a4"),
                        MossAmount = 0.4f, GlowingGlyphs = true,
                    };
                case Zone.Sanctuary:
                    return new TerrainTheme
                    {
                        Stone = Ramp.Make("4a3c30", 6, 0.1f, 0.25f, 1.6f),
                        Cobble = Ramp.Make("c19a4c", 6, 0.12f, 0.22f, 1.45f), Earth = Ramp.Make("33241c", 4, 0.08f, 0.5f, 1.5f),
                        Moss = Ramp.Make("3d4a30", 4, 0.08f), Deep = PixelCanvas.Hex("0e0907"), Accent = PixelCanvas.Hex("c9a14f"),
                        MossAmount = 0.08f, AccentInlays = true,
                    };
                case Zone.Reef:
                    return new TerrainTheme
                    {
                        Stone = Ramp.Make("332e3a", 6, 0.1f, 0.25f, 1.6f),
                        Cobble = Ramp.Make("7d7a88", 6, 0.12f, 0.24f, 1.5f), Earth = Ramp.Make("221e28", 4, 0.08f, 0.5f, 1.5f),
                        Moss = Ramp.Make("6a3a4a", 4, 0.08f), Deep = PixelCanvas.Hex("0a080d"), Accent = PixelCanvas.Hex("d97a8a"),
                        MossAmount = 0.3f, AccentInlays = true,
                    };
                default:
                    return new TerrainTheme
                    {
                        Stone = Ramp.Make("3e3a36", 6, 0.1f, 0.25f, 1.6f),
                        Cobble = Ramp.Make("b08d52", 6, 0.12f, 0.22f, 1.48f), Earth = Ramp.Make("2e2620", 4, 0.08f, 0.5f, 1.5f),
                        Moss = Ramp.Make("3a6a50", 4, 0.1f), Deep = PixelCanvas.Hex("0c0a0a"), Accent = PixelCanvas.Hex("8fb3a0"),
                        MossAmount = 0.35f,
                    };
            }
        }
    }

    /// <summary>Un trozo de terreno ya pintado (color + normal map) y su posición en casillas.</summary>
    public sealed class TerrainChunk
    {
        public int TileX, TileY, TilesW, TilesH;
        public PixelCanvas Color;
        public PixelCanvas Normal;
    }

    /// <summary>
    /// Pinta el terreno de un nivel como una ilustración continua en lugar de baldosas repetidas:
    ///  - camino de adoquines ocres con el canto iluminado y tierra con cascotes debajo (como en Blasphemous),
    ///  - sillería ciclópea que se funde en negro hacia el interior de la roca,
    ///  - musgo y algas que cuelgan, estalactitas y goteos en los techos,
    ///  - paredes de fondo con columnas, arcos y nichos.
    /// Además genera el normal map para que las luces 2D (velas, altares) resbalen por la piedra.
    /// </summary>
    public static class TerrainPainter
    {
        public const int Tile = 32;
        const int Margin = 72; // píxeles extra alrededor de cada trozo para medir distancias a la superficie

        // ------------------------------------------------------------------
        // Roca sólida
        // ------------------------------------------------------------------

        public static List<TerrainChunk> PaintSolid(System.Func<int, int, bool> solid, int tilesW, int tilesH,
                                                    System.Func<int, Zone> zoneAtTile, int chunkTiles = 32)
        {
            var chunks = new List<TerrainChunk>();
            for (int cy = 0; cy < tilesH; cy += chunkTiles)
            {
                for (int cx = 0; cx < tilesW; cx += chunkTiles)
                {
                    int tw = Mathf.Min(chunkTiles, tilesW - cx), th = Mathf.Min(chunkTiles, tilesH - cy);
                    bool any = false;
                    for (int y = cy - 1; y <= cy + th && !any; y++)
                        for (int x = cx; x < cx + tw && !any; x++)
                            if (solid(x, y)) any = true;
                    if (!any) continue;
                    chunks.Add(PaintSolidChunk(solid, cx, cy, tw, th, zoneAtTile));
                }
            }
            return chunks;
        }

        static TerrainChunk PaintSolidChunk(System.Func<int, int, bool> solid, int tileX, int tileY, int tilesW, int tilesH,
                                            System.Func<int, Zone> zoneAtTile)
        {
            int w = tilesW * Tile, h = tilesH * Tile;
            int ox = tileX * Tile - Margin, oy = tileY * Tile - Margin; // origen (px de mundo) del área extendida
            int ew = w + Margin * 2, eh = h + Margin * 2;

            // Máscara sólida a nivel de píxel (1 casilla = 32 px).
            var mask = new bool[ew * eh];
            for (int y = 0; y < eh; y++)
            {
                int ty = FloorDiv(oy + y, Tile);
                for (int x = 0; x < ew; x++)
                {
                    mask[y * ew + x] = solid(FloorDiv(ox + x, Tile), ty);
                }
            }

            // Distancias a la superficie en cada dirección (en píxeles, limitadas).
            var dUp = new short[ew * eh];
            var dDown = new short[ew * eh];
            var dSide = new short[ew * eh];
            const short Far = 999;
            for (int x = 0; x < ew; x++)
            {
                short run = Far;
                for (int y = eh - 1; y >= 0; y--)
                {
                    int i = y * ew + x;
                    run = mask[i] ? (short)Mathf.Min(Far, run + 1) : (short)-1;
                    dUp[i] = run < 0 ? Far : run;
                }
                run = Far;
                for (int y = 0; y < eh; y++)
                {
                    int i = y * ew + x;
                    run = mask[i] ? (short)Mathf.Min(Far, run + 1) : (short)-1;
                    dDown[i] = run < 0 ? Far : run;
                }
            }
            for (int y = 0; y < eh; y++)
            {
                short left = Far, right = Far;
                var fromLeft = new short[ew];
                for (int x = 0; x < ew; x++)
                {
                    int i = y * ew + x;
                    left = mask[i] ? (short)Mathf.Min(Far, left + 1) : (short)-1;
                    fromLeft[x] = left < 0 ? Far : left;
                }
                for (int x = ew - 1; x >= 0; x--)
                {
                    int i = y * ew + x;
                    right = mask[i] ? (short)Mathf.Min(Far, right + 1) : (short)-1;
                    dSide[i] = (short)Mathf.Min(fromLeft[x], right < 0 ? Far : right);
                }
            }

            var dist = Chamfer(mask, ew, eh);

            var color = new PixelCanvas(w, h);
            var height = new float[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int ex = x + Margin, ey = y + Margin, i = ey * ew + ex;
                    if (!mask[i]) continue;
                    int wx = ox + ex, wy = oy + ey; // píxel de mundo
                    var theme = TerrainTheme.For(zoneAtTile(FloorDiv(wx, Tile)));
                    int up = dUp[i], down = dDown[i], side = dSide[i];
                    int depth = dist[i];
                    float hgt;
                    Color32 c;
                    if (up <= 26 && up <= side + 4) c = Cobble(theme, wx, wy, up, out hgt);
                    else if (down <= 10 && down < side) c = Ceiling(theme, wx, wy, down, out hgt);
                    else c = Masonry(theme, wx, wy, depth, side < up && side < down, out hgt);

                    // Se funde en negro con la profundidad (la roca "se pierde" en la oscuridad).
                    float fade = Mathf.Clamp01((depth - 16f) / 44f);
                    fade = fade * fade * (3f - 2f * fade);
                    c = PixelCanvas.Lerp(c, theme.Deep, fade * 0.95f);
                    color.Pixels[y * w + x] = c;
                    height[y * w + x] = hgt;
                }
            }

            DecorateOpenAir(color, height, mask, ew, ox, oy, w, h, zoneAtTile);
            var normal = NormalsFromHeight(color, height, w, h, 2.2f);
            return new TerrainChunk { TileX = tileX, TileY = tileY, TilesW = tilesW, TilesH = tilesH, Color = color, Normal = normal };
        }

        /// <summary>Distancia aproximada (en píxeles) de cada píxel sólido al aire más cercano (chamfer 3-4).</summary>
        static int[] Chamfer(bool[] mask, int w, int h)
        {
            const int Inf = 1 << 20;
            var d = new int[w * h];
            for (int i = 0; i < d.Length; i++) d[i] = mask[i] ? Inf : 0;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int i = y * w + x;
                    if (d[i] == 0) continue;
                    int v = d[i];
                    if (x > 0) v = Mathf.Min(v, d[i - 1] + 3);
                    if (y > 0)
                    {
                        v = Mathf.Min(v, d[i - w] + 3);
                        if (x > 0) v = Mathf.Min(v, d[i - w - 1] + 4);
                        if (x < w - 1) v = Mathf.Min(v, d[i - w + 1] + 4);
                    }
                    d[i] = v;
                }
            }
            for (int y = h - 1; y >= 0; y--)
            {
                for (int x = w - 1; x >= 0; x--)
                {
                    int i = y * w + x;
                    if (d[i] == 0) continue;
                    int v = d[i];
                    if (x < w - 1) v = Mathf.Min(v, d[i + 1] + 3);
                    if (y < h - 1)
                    {
                        v = Mathf.Min(v, d[i + w] + 3);
                        if (x < w - 1) v = Mathf.Min(v, d[i + w + 1] + 4);
                        if (x > 0) v = Mathf.Min(v, d[i + w - 1] + 4);
                    }
                    d[i] = v;
                }
            }
            for (int i = 0; i < d.Length; i++) d[i] = Mathf.Min(d[i] / 3, 999);
            return d;
        }

        /// <summary>
        /// Camino de adoquines como en Blasphemous: tres hileras de piedras redondeadas ocres, cada una más hundida y
        /// oscura que la anterior, con el canto superior muy iluminado, juntas oscuras, alguna piedra hundida que rompe
        /// la línea del suelo y, debajo, tierra con cascotes y algún hueso.
        /// </summary>
        static Color32 Cobble(TerrainTheme t, int wx, int wy, int depthFromTop, out float height)
        {
            int surfaceY = wy + depthFromTop;
            var ramp = t.Cobble;
            var mortar = PixelCanvas.Lerp(ramp[0], t.Deep, 0.4f);
            if (depthFromTop < CobbleRows[0] + CobbleRows[1] + CobbleRows[2])
            {
                int row = 0, top = 0;
                while (depthFromTop >= top + CobbleRows[row]) { top += CobbleRows[row]; row++; }
                int rowH = CobbleRows[row];
                int stone = StoneIndex(wx + row * 5, surfaceY * 3 + row, 8 - row, 6, out int pos, out int width);
                float tone = PixelCanvas.Hash(stone, surfaceY + row, 61);
                // Algunas piedras están hundidas un píxel (la línea del suelo no es una regla).
                int sink = PixelCanvas.Hash(stone, surfaceY + row, 66) > 0.72f ? 1 : 0;
                int localD = depthFromTop - top - sink;
                bool corner = (localD <= 0 || localD >= rowH - 1) && (pos <= 1 || pos >= width - 1);
                if (pos == 0 || localD < 0 || corner)
                {
                    height = 0f;
                    return localD < 0 && row == 0 ? PixelCanvas.Lerp(ramp[1], t.Deep, 0.2f) : mortar;
                }
                float u = (pos - width * 0.5f) / (width * 0.5f);
                float v = (localD - rowH * 0.45f) / (rowH * 0.55f);
                height = Mathf.Clamp01(1f - (u * u + v * v) * 0.6f);
                int idx;
                if (localD == 0) idx = 5;
                else if (localD == 1) idx = 4;
                else if (localD == rowH - 1) idx = 1;
                else if (localD == rowH - 2) idx = 2;
                else idx = 3;
                if (pos == 1 && localD > 0 && localD < rowH - 1) idx = Mathf.Min(5, idx + 1);       // canto izquierdo, iluminado
                if (pos >= width - 2 && localD > 0) idx = Mathf.Max(1, idx - 1);                    // canto derecho, en sombra
                if (tone > 0.75f) idx = Mathf.Min(5, idx + (localD > 0 ? 1 : 0));
                else if (tone < 0.25f) idx = Mathf.Max(1, idx - 1);
                idx -= row + sink;                                                                  // hileras de abajo, hundidas
                // Desgaste: algún píxel picado en el centro de la piedra.
                if (localD > 1 && localD < rowH - 2 && PixelCanvas.Hash(wx, wy, 67) > 0.93f) idx--;
                // Musgo o verdín en algunas piedras.
                if (t.MossAmount > 0.2f && localD <= 1 && PixelCanvas.Hash(stone, 7 + row, 62) < t.MossAmount * 0.3f && (pos + localD) % 2 == 0)
                    return t.Moss[Mathf.Clamp(2 + localD - row, 0, t.Moss.Length - 1)];
                return ramp[Mathf.Clamp(idx, 0, ramp.Length - 1)];
            }
            // Tierra oscura con cascotes y huesos.
            height = 0.3f;
            int d = depthFromTop - CobbleRows[0] - CobbleRows[1] - CobbleRows[2];
            float n = PixelCanvas.ValueNoise(wx / 4f, wy / 3f, 0, 63);
            var earth = t.Earth;
            if (d == 0) return mortar;
            int e = d < 3 ? 2 : n > 0.6f ? 2 : n > 0.35f ? 1 : 0;
            // Cascotes: piedras sueltas de 3-5 px con su luz arriba.
            int cellX = FloorDiv(wx, 6), cellY = FloorDiv(wy, 5);
            if (d > 2 && PixelCanvas.Hash(cellX, cellY, 64) > 0.8f)
            {
                int lx = wx - cellX * 6, ly = wy - cellY * 5;
                if (lx >= 1 && lx <= 4 && ly >= 1 && ly <= 3 && !((lx == 1 || lx == 4) && (ly == 1 || ly == 3)))
                {
                    height = 0.8f;
                    return PixelCanvas.Lerp(ly == 3 ? ramp[2] : ramp[1], earth[3], 0.45f);
                }
            }
            if (PixelCanvas.Hash(wx / 7, wy / 3, 65) > 0.985f && d > 3) return PixelCanvas.Hex("8a8270"); // hueso
            return earth[e];
        }

        /// <summary>Alto (px) de cada hilera de adoquines, de arriba abajo.</summary>
        static readonly int[] CobbleRows = { 6, 5, 5 };

        /// <summary>Divide una fila en piedras de anchura minW..minW+rangeW-1.</summary>
        static int StoneIndex(int wx, int key, int minW, int rangeW, out int posInStone, out int stoneWidth)
        {
            int shift = Mathf.FloorToInt(PixelCanvas.Hash(key, 1, 3) * 40f);
            int x = wx + shift;
            int block = FloorDiv(x, 256);
            int start = block * 256;
            int index = block * 64;
            while (true)
            {
                int width = minW + Mathf.FloorToInt(PixelCanvas.Hash(index, key, 4) * rangeW);
                if (x < start + width)
                {
                    posInStone = x - start;
                    stoneWidth = width;
                    return index;
                }
                start += width;
                index++;
            }
        }

        /// <summary>Sillería ciclópea: bloques grandes e irregulares con bisel y argamasa.</summary>
        static Color32 Masonry(TerrainTheme t, int wx, int wy, int depth, bool wallFace, out float height)
        {
            int rowH = 14;
            int row = FloorDiv(wy, rowH);
            int rowJitter = Mathf.FloorToInt(PixelCanvas.Hash(row, 0, 21) * 6f) - 3;
            int localY = wy - row * rowH;
            int offset = Mathf.FloorToInt(PixelCanvas.Hash(row, 1, 22) * 30f);
            int x = wx + offset;
            int col = FloorDiv(x, 28);
            int blockW = 20 + Mathf.FloorToInt(PixelCanvas.Hash(col, row, 23) * 18f);
            int localX = x - col * 28;
            bool mortarH = localY == 0 || localY == rowH + rowJitter - 1;
            bool mortarV = localX == 0 || (localX == blockW % 28 && blockW < 28);
            var ramp = t.Stone;
            if (mortarH || mortarV)
            {
                height = 0f;
                return ramp[1];
            }
            float tone = PixelCanvas.Hash(col, row, 24);
            int idx = 2 + (tone > 0.6f ? 1 : 0) - (tone < 0.2f ? 1 : 0);
            height = 1f;
            // Bisel: arriba e izquierda más claros, abajo y derecha más oscuros.
            if (localY == rowH - 2 || localX == 1) { idx++; height = 0.8f; }
            else if (localY == 1 || localX == 27) { idx--; height = 0.6f; }
            // Textura granulada en grupos (no ruido suelto).
            float grain = PixelCanvas.ValueNoise(wx / 3f, wy / 3f, 0, 31);
            if (grain > 0.74f) idx--;
            else if (grain < 0.18f) idx++;
            // Runas tenues en las ruinas.
            if (t.GlowingGlyphs && depth < 40 && PixelCanvas.Hash(col, row, 25) > 0.93f && localX > 8 && localX < 18 && localY > 3 && localY < 10)
            {
                if ((localX + localY) % 3 == 0) return PixelCanvas.Lerp(t.Accent, ramp[2], 0.55f);
            }
            if (wallFace) idx = Mathf.Min(idx + (depth < 3 ? 1 : 0), ramp.Length - 1);
            return ramp[Mathf.Clamp(idx, 0, ramp.Length - 1)];
        }

        /// <summary>Techos: roca áspera y oscura.</summary>
        static Color32 Ceiling(TerrainTheme t, int wx, int wy, int depthFromBottom, out float height)
        {
            float n = PixelCanvas.Fbm(wx / 6f, wy / 6f, 3, 0, 41);
            int idx = depthFromBottom < 2 ? 1 : 2;
            if (n > 0.62f) idx++;
            if (n < 0.35f) idx--;
            height = n;
            return t.Stone[Mathf.Clamp(idx, 0, t.Stone.Length - 1)];
        }

        /// <summary>Musgo que cuelga por los bordes, briznas sobre el suelo y estalactitas bajo los techos.</summary>
        static void DecorateOpenAir(PixelCanvas color, float[] height, bool[] mask, int ew, int ox, int oy, int w, int h,
                                    System.Func<int, Zone> zoneAtTile)
        {
            bool SolidAt(int x, int y)
            {
                int ex = x + Margin, ey = y + Margin;
                return mask[ey * ew + ex];
            }
            void Put(int x, int y, Color32 c, float hgt)
            {
                if (x < 0 || y < 0 || x >= w || y >= h) return;
                if (SolidAt(x, y)) return;
                color.Pixels[y * w + x] = c;
                height[y * w + x] = hgt;
            }

            for (int x = 0; x < w; x++)
            {
                int wx = ox + Margin + x;
                var theme = TerrainTheme.For(zoneAtTile(FloorDiv(wx, Tile)));
                for (int y = 0; y < h; y++)
                {
                    int wy = oy + Margin + y;
                    bool here = SolidAt(x, y);
                    if (!here) continue;
                    bool openAbove = y + 1 < h + Margin && !SolidAt(x, Mathf.Min(y + 1, h + Margin - 1));
                    bool openBelow = y - 1 >= -Margin && !SolidAt(x, Mathf.Max(y - 1, -Margin));

                    // Briznas de algas / musgo sobre el suelo.
                    if (openAbove && PixelCanvas.Hash(wx, wy, 51) < theme.MossAmount * 0.55f)
                    {
                        int tall = 1 + Mathf.FloorToInt(PixelCanvas.Hash(wx, wy, 52) * 4f);
                        for (int k = 1; k <= tall; k++)
                        {
                            int sway = k > 2 && PixelCanvas.Hash(wx, k, 53) > 0.5f ? 1 : 0;
                            Put(x + sway, y + k, theme.Moss[Mathf.Clamp(3 - k / 2, 0, theme.Moss.Length - 1)], 0.6f);
                        }
                    }
                    // Musgo que cuelga por los bordes de las paredes (y algas).
                    if (openAbove && PixelCanvas.Hash(wx / 5, wy, 54) < theme.MossAmount * 0.35f)
                    {
                        for (int side = -1; side <= 1; side += 2)
                        {
                            if (x + side < 0 || x + side >= w || SolidAt(x + side, y)) continue;
                            int len = 3 + Mathf.FloorToInt(PixelCanvas.Hash(wx, wy, 55) * 12f);
                            for (int k = 0; k < len; k++) Put(x + side, y - k, theme.Moss[k < len - 2 ? 1 : 0], 0.5f);
                        }
                    }
                    // Estalactitas y raíces bajo los techos.
                    if (openBelow && PixelCanvas.Hash(wx, wy, 56) < 0.09f)
                    {
                        int len = 2 + Mathf.FloorToInt(PixelCanvas.Hash(wx, wy, 57) * 10f);
                        int width = len > 7 ? 2 : 1;
                        for (int k = 1; k <= len; k++)
                        {
                            for (int dx = 0; dx < width; dx++)
                            {
                                if (k > len - 2 && dx > 0) continue;
                                Put(x + dx, y - k, theme.Stone[k < len / 2 ? 1 : 0], 0.7f);
                            }
                        }
                    }
                    else if (openBelow && theme.MossAmount > 0.3f && PixelCanvas.Hash(wx, wy, 58) < 0.05f)
                    {
                        int len = 4 + Mathf.FloorToInt(PixelCanvas.Hash(wx, wy, 59) * 14f);
                        for (int k = 1; k <= len; k++) Put(x + (k > len / 2 && wx % 2 == 0 ? 1 : 0), y - k, theme.Moss[0], 0.4f);
                    }
                }
            }
        }

        // ------------------------------------------------------------------
        // Paredes de fondo: arquitectura (columnas, arcos, nichos)
        // ------------------------------------------------------------------

        public static List<TerrainChunk> PaintBackWall(System.Func<int, int, bool> back, int tilesW, int tilesH,
                                                       System.Func<int, Zone> zoneAtTile, int chunkTiles = 32)
        {
            var chunks = new List<TerrainChunk>();
            for (int cy = 0; cy < tilesH; cy += chunkTiles)
            {
                for (int cx = 0; cx < tilesW; cx += chunkTiles)
                {
                    int tw = Mathf.Min(chunkTiles, tilesW - cx), th = Mathf.Min(chunkTiles, tilesH - cy);
                    bool any = false;
                    for (int y = cy; y < cy + th && !any; y++)
                        for (int x = cx; x < cx + tw && !any; x++)
                            if (back(x, y)) any = true;
                    if (!any) continue;
                    chunks.Add(PaintBackChunk(back, cx, cy, tw, th, zoneAtTile));
                }
            }
            return chunks;
        }

        static TerrainChunk PaintBackChunk(System.Func<int, int, bool> back, int tileX, int tileY, int tilesW, int tilesH,
                                           System.Func<int, Zone> zoneAtTile)
        {
            int w = tilesW * Tile, h = tilesH * Tile;
            var color = new PixelCanvas(w, h);
            var height = new float[w * h];
            // Para cada casilla: dónde empieza y acaba el hueco de fondo en su columna.
            var floorOf = new int[tilesW * tilesH];
            var ceilOf = new int[tilesW * tilesH];
            for (int tx = 0; tx < tilesW; tx++)
            {
                for (int ty = 0; ty < tilesH; ty++)
                {
                    int gx = tileX + tx, gy = tileY + ty;
                    if (!back(gx, gy)) continue;
                    int f = gy, c = gy;
                    while (back(gx, f - 1)) f--;
                    while (back(gx, c + 1)) c++;
                    floorOf[ty * tilesW + tx] = f;
                    ceilOf[ty * tilesW + tx] = c;
                }
            }
            for (int y = 0; y < h; y++)
            {
                int wy = tileY * Tile + y;
                int ty = FloorDiv(wy, Tile);
                for (int x = 0; x < w; x++)
                {
                    int wx = tileX * Tile + x;
                    int tx = FloorDiv(wx, Tile);
                    if (!back(tx, ty)) continue;
                    var zone = zoneAtTile(tx);
                    var theme = TerrainTheme.For(zone);

                    // Altura del hueco de fondo en esta columna (para colocar arcos y capiteles).
                    int li = (ty - tileY) * tilesW + (tx - tileX);
                    int floorTile = floorOf[li], ceilTile = ceilOf[li];
                    int localY = wy - floorTile * Tile;
                    int spanH = (ceilTile - floorTile + 1) * Tile;

                    Color32 c = Architecture(theme, zone, wx, wy, localY, spanH, out float hgt);
                    // Oscuro y algo más apagado arriba: la luz viene del suelo (velas).
                    float k = 0.8f - 0.26f * Mathf.Clamp01(localY / (float)Mathf.Max(1, spanH));
                    c = PixelCanvas.Lerp(theme.Deep, c, k);
                    color.Pixels[y * w + x] = c;
                    height[y * w + x] = hgt;
                }
            }
            var normal = NormalsFromHeight(color, height, w, h, 1.6f);
            return new TerrainChunk { TileX = tileX, TileY = tileY, TilesW = tilesW, TilesH = tilesH, Color = color, Normal = normal };
        }

        const int Bay = 256;          // una columna cada 8 casillas; un arco entre cada dos
        const float ArchHalfW = 72f;

        static float ArchTop(int spanH) => Mathf.Min(spanH - 24f, 200f);

        /// <summary>Un arco del muro de fondo con sitio para una vidriera.</summary>
        public struct ArchSlot
        {
            public int X;       // px de mundo: eje del arco
            public int FloorY;  // px de mundo: suelo del hueco
            public int Top;     // px sobre el suelo donde cierra el arco
            public int Span;    // px de alto del hueco (de suelo a techo)
            public Zone Zone;
        }

        /// <summary>
        /// Arcos completos (mismo suelo y techo a lo ancho de la vidriera) del muro de fondo: el constructor coloca en
        /// ellos vidrieras que brillan y sus rayos de luz.
        /// </summary>
        public static List<ArchSlot> ArchSlots(System.Func<int, int, bool> back, int tilesW, int tilesH, System.Func<int, Zone> zoneAtTile)
        {
            var slots = new List<ArchSlot>();
            int tilesPerBay = Bay / Tile;
            for (int bay = 0; bay * tilesPerBay < tilesW; bay++)
            {
                int axis = bay * Bay + Bay / 2, tx = axis / Tile;
                for (int ty = 0; ty < tilesH; ty++)
                {
                    if (!back(tx, ty) || back(tx, ty - 1)) continue; // buscamos el suelo de cada hueco
                    int top = ty;
                    while (top + 1 < tilesH && back(tx, top + 1)) top++;
                    bool whole = true;
                    for (int dx = -2; dx <= 1 && whole; dx++)
                    {
                        int x = tx + dx;
                        if (!back(x, ty) || back(x, ty - 1) || !back(x, top) || (top + 1 < tilesH && back(x, top + 1))) whole = false;
                    }
                    int spanH = (top - ty + 1) * Tile;
                    if (whole && ArchTop(spanH) > 150f)
                        slots.Add(new ArchSlot { X = axis, FloorY = ty * Tile, Top = Mathf.RoundToInt(ArchTop(spanH)), Span = spanH, Zone = zoneAtTile(tx) });
                    ty = top;
                }
            }
            return slots;
        }

        static Color32 Architecture(TerrainTheme t, Zone zone, int wx, int wy, int localY, int spanH, out float height)
        {
            int bay = FloorDiv(wx, Bay);
            int bx = wx - bay * Bay;          // 0..255 dentro del tramo
            int columnW = zone == Zone.Ruins ? 52 : 44;
            int center = Bay / 2;
            var ramp = t.Stone;

            // Columna (fuste con estrías, basa y capitel).
            int distToColumn = Mathf.Abs(bx - 0) < Mathf.Abs(bx - Bay) ? bx : Bay - bx;
            if (distToColumn < columnW / 2)
            {
                int u = distToColumn; // 0 en el eje
                bool baseZone = localY < 14, capital = localY > spanH - 18 && spanH > 64;
                int half = columnW / 2 + (baseZone || capital ? 4 : 0);
                if (u < half)
                {
                    float round = 1f - u / (float)half; // 1 en el centro
                    height = round;
                    int idx = 3 + Mathf.RoundToInt(round * 2f) - (bx > Bay / 2 ? 1 : 0);
                    if (!baseZone && !capital && u % 6 == 3) idx--; // estrías
                    if (baseZone && (localY == 13 || localY == 6)) idx = 1;
                    if (capital && (localY == spanH - 18 || localY == spanH - 10)) idx = 1;
                    return ramp[Mathf.Clamp(idx, 0, ramp.Length - 1)];
                }
            }

            // Arco ojival entre columnas, con un nicho oscuro.
            float archHalfW = ArchHalfW;
            float archTop = ArchTop(spanH);
            float dx = Mathf.Abs(bx - center);
            if (archTop > 40f && dx < archHalfW + 6f && localY > 10)
            {
                // Ojiva: dos arcos que se cruzan.
                float springY = archTop - archHalfW * 0.9f;
                bool insideArch;
                if (localY < springY) insideArch = dx < archHalfW;
                else
                {
                    float r = archHalfW * 1.6f;
                    float ccx = archHalfW - r; // centro del arco opuesto
                    float ddx = dx - ccx, ddy = localY - springY;
                    insideArch = ddx * ddx + ddy * ddy < r * r && localY < archTop;
                }
                if (insideArch)
                {
                    bool rim = false;
                    // Moldura del arco: 4 px por dentro del borde.
                    float rimDx = dx + 5f;
                    if (localY < springY) rim = rimDx >= archHalfW;
                    else
                    {
                        float r = archHalfW * 1.6f, ccx = archHalfW - r;
                        float ddx = rimDx - ccx, ddy = localY + 5f - springY;
                        rim = !(ddx * ddx + ddy * ddy < r * r && localY + 5f < archTop);
                    }
                    if (rim)
                    {
                        height = 0.8f;
                        return ramp[3];
                    }
                    height = 0.05f;
                    // Fondo del nicho: oscuro (ahí van las vidrieras, ver ArchSlots), con sillares apenas insinuados.
                    var niche = Masonry(t, wx, wy, 99, false, out _);
                    return PixelCanvas.Lerp(t.Deep, PixelCanvas.Lerp(ramp[0], niche, 0.3f), 0.55f);
                }
            }

            // Muro de sillares con una cornisa horizontal.
            if (localY % 96 == 88 || localY % 96 == 89)
            {
                height = 0.9f;
                return ramp[3];
            }
            // Muro de fondo con poco contraste para no competir con la acción.
            var wall = Masonry(t, wx, wy, 99, false, out height);
            return PixelCanvas.Lerp(wall, ramp[2], 0.45f);
        }

        // ------------------------------------------------------------------
        // Normal map a partir de un mapa de alturas
        // ------------------------------------------------------------------

        static PixelCanvas NormalsFromHeight(PixelCanvas color, float[] height, int w, int h, float strength)
        {
            var normal = new PixelCanvas(w, h);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int i = y * w + x;
                    if (color.Pixels[i].a == 0) continue;
                    float hl = height[y * w + Mathf.Max(0, x - 1)], hr = height[y * w + Mathf.Min(w - 1, x + 1)];
                    float hd = height[Mathf.Max(0, y - 1) * w + x], hu = height[Mathf.Min(h - 1, y + 1) * w + x];
                    var n = new N3((hl - hr) * strength, (hd - hu) * strength, 1f).Normalized();
                    normal.Pixels[i] = new Color32(
                        (byte)Mathf.Clamp(Mathf.RoundToInt((n.x * 0.5f + 0.5f) * 255f), 0, 255),
                        (byte)Mathf.Clamp(Mathf.RoundToInt((n.y * 0.5f + 0.5f) * 255f), 0, 255),
                        (byte)Mathf.Clamp(Mathf.RoundToInt((n.z * 0.5f + 0.5f) * 255f), 0, 255), 255);
                }
            }
            return normal;
        }

        static int FloorDiv(int a, int b) => a >= 0 ? a / b : (a - b + 1) / b;
    }
}
