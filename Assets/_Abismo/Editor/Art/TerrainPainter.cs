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
        public Color32[] Slab;      // losas del suelo
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
                        Stone = Ramp.Make("34433b", 6, 0.1f, 0.25f, 1.6f), Slab = Ramp.Make("46574c", 6, 0.1f, 0.3f, 1.55f),
                        Moss = Ramp.Make("355e45", 4, 0.1f), Deep = PixelCanvas.Hex("060a09"), Accent = PixelCanvas.Hex("4fd6a4"),
                        MossAmount = 0.55f, GlowingGlyphs = true,
                    };
                case Zone.Sanctuary:
                    return new TerrainTheme
                    {
                        Stone = Ramp.Make("4a3f35", 6, 0.1f, 0.25f, 1.6f), Slab = Ramp.Make("5e5144", 6, 0.1f, 0.3f, 1.55f),
                        Moss = Ramp.Make("3d4a30", 4, 0.08f), Deep = PixelCanvas.Hex("0b0807"), Accent = PixelCanvas.Hex("c9a14f"),
                        MossAmount = 0.12f, AccentInlays = true,
                    };
                case Zone.Reef:
                    return new TerrainTheme
                    {
                        Stone = Ramp.Make("2f3138", 6, 0.1f, 0.25f, 1.6f), Slab = Ramp.Make("3d3f48", 6, 0.1f, 0.3f, 1.55f),
                        Moss = Ramp.Make("6a3a4a", 4, 0.08f), Deep = PixelCanvas.Hex("07070a"), Accent = PixelCanvas.Hex("d97a8a"),
                        MossAmount = 0.35f, AccentInlays = true,
                    };
                default:
                    return new TerrainTheme
                    {
                        Stone = Ramp.Make("3b474b", 6, 0.1f, 0.25f, 1.6f), Slab = Ramp.Make("505d5f", 6, 0.1f, 0.3f, 1.55f),
                        Moss = Ramp.Make("3a6a50", 4, 0.1f), Deep = PixelCanvas.Hex("07090b"), Accent = PixelCanvas.Hex("8fb3a0"),
                        MossAmount = 0.75f,
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
    ///  - suelo de losas irregulares con grietas y bordes iluminados (como las catedrales de Blasphemous),
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
                    if (up <= 13 && up <= side + 3) c = Slab(theme, wx, wy, up, out hgt);
                    else if (down <= 10 && down < side) c = Ceiling(theme, wx, wy, down, out hgt);
                    else c = Masonry(theme, wx, wy, depth, side < up && side < down, out hgt);

                    // Se funde en negro con la profundidad (la roca "se pierde" en la oscuridad).
                    float fade = Mathf.Clamp01((depth - 8f) / 56f);
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

        /// <summary>Losas del suelo: anchos irregulares, borde superior iluminado, juntas y grietas.</summary>
        static Color32 Slab(TerrainTheme t, int wx, int wy, int depthFromTop, out float height)
        {
            int surfaceY = wy + depthFromTop; // fila del borde superior
            int slab = SlabIndex(wx, surfaceY, out int posInSlab, out int slabWidth);
            float tone = PixelCanvas.Hash(slab, surfaceY, 5);
            int thickness = 9 + Mathf.FloorToInt(PixelCanvas.Hash(slab, surfaceY, 6) * 4f);
            int sink = PixelCanvas.Hash(slab, surfaceY, 7) > 0.8f ? 1 : 0; // alguna losa algo hundida
            int d = depthFromTop - sink;
            height = 1f;
            var ramp = t.Slab;

            if (d < 0) { height = 0.2f; return ramp[0]; }
            bool joint = posInSlab == 0;
            if (joint || d >= thickness)
            {
                height = 0f;
                return d >= thickness && d < thickness + 1 ? ramp[0] : PixelCanvas.Lerp(ramp[0], t.Deep, 0.4f);
            }
            int idx;
            if (d == 0) idx = 5;
            else if (d == 1) idx = 4;
            else idx = 3 - (d > thickness - 3 ? 1 : 0);
            if (posInSlab == 1) idx = Mathf.Max(1, idx - 1);
            if (posInSlab == slabWidth - 1) idx = Mathf.Min(5, idx + (d < 2 ? 0 : 1));
            if (tone > 0.7f) idx = Mathf.Min(5, idx + (d > 1 ? 1 : 0));
            else if (tone < 0.25f) idx = Mathf.Max(1, idx - 1);

            // Grieta diagonal dentro de algunas losas.
            if (PixelCanvas.Hash(slab, surfaceY, 9) > 0.62f)
            {
                int crackX = Mathf.FloorToInt(PixelCanvas.Hash(slab, surfaceY, 10) * (slabWidth - 4)) + 2;
                if (posInSlab == crackX + (d / 2) * (PixelCanvas.Hash(slab, 0, 11) > 0.5f ? 1 : -1) && d > 0)
                {
                    height = 0.35f;
                    return ramp[1];
                }
            }
            // Incrustaciones (oro en el santuario, coral en el arrecife).
            if (t.AccentInlays && d == 3 && slab % 5 == 0 && posInSlab > 2 && posInSlab < slabWidth - 3 && (posInSlab % 3) == 1)
                return PixelCanvas.Lerp(t.Accent, ramp[3], 0.35f);
            return ramp[Mathf.Clamp(idx, 0, ramp.Length - 1)];
        }

        static int SlabIndex(int wx, int surfaceY, out int posInSlab, out int slabWidth)
        {
            // Recorremos losas de ancho 10-24 px desde un origen desplazado según la altura de la superficie.
            int shift = Mathf.FloorToInt(PixelCanvas.Hash(surfaceY, 1, 3) * 40f);
            int x = wx + shift;
            int block = FloorDiv(x, 256);
            int start = block * 256;
            int index = block * 64;
            while (true)
            {
                int width = 10 + Mathf.FloorToInt(PixelCanvas.Hash(index, surfaceY, 4) * 15f);
                if (x < start + width)
                {
                    posInSlab = x - start;
                    slabWidth = width;
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
                    float k = 0.42f - 0.18f * Mathf.Clamp01(localY / (float)Mathf.Max(1, spanH));
                    c = PixelCanvas.Lerp(theme.Deep, c, k);
                    color.Pixels[y * w + x] = c;
                    height[y * w + x] = hgt;
                }
            }
            var normal = NormalsFromHeight(color, height, w, h, 1.6f);
            return new TerrainChunk { TileX = tileX, TileY = tileY, TilesW = tilesW, TilesH = tilesH, Color = color, Normal = normal };
        }

        static Color32 Architecture(TerrainTheme t, Zone zone, int wx, int wy, int localY, int spanH, out float height)
        {
            const int Bay = 256;              // una columna cada 8 casillas
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
            float archHalfW = 72f;
            float archTop = Mathf.Min(spanH - 24f, 200f);
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
                    // Fondo del nicho: muy oscuro, con una ventana tenue en el santuario.
                    if (zone == Zone.Sanctuary && dx < 10f && localY > springY - 20f && localY < archTop - 18f)
                        return PixelCanvas.Lerp(t.Deep, t.Accent, 0.12f);
                    return PixelCanvas.Lerp(t.Deep, ramp[0], 0.5f);
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
