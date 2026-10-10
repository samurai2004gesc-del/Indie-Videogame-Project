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
        public Zone Zone;           // zona a la que pertenece (relieves tallados en las Ruinas, charcos en la costa...)

        static readonly Dictionary<Zone, TerrainTheme> cache = new Dictionary<Zone, TerrainTheme>();

        public static TerrainTheme For(Zone zone)
        {
            if (!cache.TryGetValue(zone, out var theme))
            {
                theme = Create(zone);
                theme.Zone = zone;
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
    public static partial class TerrainPainter
    {
        public const int Tile = 32;
        const int Margin = 140; // píxeles extra alrededor de cada trozo para medir distancias a la superficie

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

            // Distancias en cada dirección (0 = vecino inmediato). En un píxel sólido: hasta el aire; en uno de aire:
            // hasta la roca. Sirven para saber si un píxel es suelo, techo o pared y para romper la silueta.
            var up = new short[ew * eh];
            var down = new short[ew * eh];
            var left = new short[ew * eh];
            var right = new short[ew * eh];
            const short Far = 999;
            for (int x = 0; x < ew; x++)
            {
                short run = Far;
                for (int y = eh - 1; y >= 0; y--)
                {
                    int i = y * ew + x;
                    run = y < eh - 1 && mask[i] == mask[i + ew] ? (short)Mathf.Min(Far, run + 1) : (y < eh - 1 ? (short)0 : Far);
                    up[i] = run;
                }
                run = Far;
                for (int y = 0; y < eh; y++)
                {
                    int i = y * ew + x;
                    run = y > 0 && mask[i] == mask[i - ew] ? (short)Mathf.Min(Far, run + 1) : (y > 0 ? (short)0 : Far);
                    down[i] = run;
                }
            }
            for (int y = 0; y < eh; y++)
            {
                short run = Far;
                for (int x = 0; x < ew; x++)
                {
                    int i = y * ew + x;
                    run = x > 0 && mask[i] == mask[i - 1] ? (short)Mathf.Min(Far, run + 1) : (x > 0 ? (short)0 : Far);
                    left[i] = run;
                }
                run = Far;
                for (int x = ew - 1; x >= 0; x--)
                {
                    int i = y * ew + x;
                    run = x < ew - 1 && mask[i] == mask[i + 1] ? (short)Mathf.Min(Far, run + 1) : (x < ew - 1 ? (short)0 : Far);
                    right[i] = run;
                }
            }

            // Silueta visible: la colisión es una rejilla, pero el borde pintado se rompe (sillares que sobresalen o
            // faltan, esquinas desconchadas, techos de roca irregular). Paredes y techos ±3 px; el suelo ±1 px.
            var vis = new bool[ew * eh];
            var kind = new byte[ew * eh]; // en el aire: qué cara ha crecido ahí (1 pared, 2 techo, 3 adoquín, 4 cascote)
            for (int y = 1; y < eh - 1; y++)
            {
                int wy = oy + y;
                for (int x = 1; x < ew - 1; x++)
                {
                    int i = y * ew + x, wx = ox + x;
                    if (mask[i])
                    {
                        bool v = true;
                        int dl = left[i], dr = right[i], du = up[i], dd = down[i];
                        if (dl <= 3 && du > 5 && dd > 3 && dl < -WallOffset(wy, FaceKey(wx - dl, 0))) v = false;
                        if (dr <= 3 && du > 5 && dd > 3 && dr < -WallOffset(wy, FaceKey(wx + dr, 1))) v = false;
                        if (dd <= 3 && dl > dd && dr > dd && dd < -CeilOffset(wx, FloorDiv(wy - dd, Tile))) v = false;
                        if (du == 0 && dl > 1 && dr > 1 && CobbleTop(wx, wy) < 0) v = false;
                        if (du <= 3)
                        {
                            if (dl <= 3 && du + dl < CornerChip(FaceKey(wx - dl, 0), wy + du)) v = false;
                            if (dr <= 3 && du + dr < CornerChip(FaceKey(wx + dr, 1), wy + du)) v = false;
                        }
                        vis[i] = v;
                    }
                    else
                    {
                        byte k = 0;
                        int gr = right[i], gl = left[i], gu = up[i], gd = down[i];
                        if (gr <= 2 && x + gr + 1 < ew)
                        {
                            int s = i + gr + 1;
                            if (up[s] > 5 && down[s] > 3 && gr < WallOffset(wy, FaceKey(wx + gr + 1, 0))) k = 1;
                        }
                        if (k == 0 && gl <= 2 && x - gl - 1 >= 0)
                        {
                            int s = i - gl - 1;
                            if (up[s] > 5 && down[s] > 3 && gl < WallOffset(wy, FaceKey(wx - gl - 1, 1))) k = 1;
                        }
                        if (k == 0 && gu <= 2 && y + gu + 1 < eh)
                        {
                            int s = i + (gu + 1) * ew;
                            if (left[s] > 2 && right[s] > 2 && gu < CeilOffset(wx, FloorDiv(wy + gu + 1, Tile))) k = 2;
                        }
                        if (k == 0 && gd == 0)
                        {
                            int s = i - ew;
                            if (left[s] > 1 && right[s] > 1 && CobbleTop(wx, wy - 1) > 0) k = 3;
                        }
                        // Rincones: un poco de cascote al pie de las paredes y bajo los techos.
                        int gs = Mathf.Min(gl, gr);
                        if (k == 0 && gd + gs <= 2 && gd <= 2) k = 4;
                        if (k == 0 && gu + gs <= 1) k = 4;
                        vis[i] = k != 0;
                        kind[i] = k;
                    }
                }
            }

            var dist = Chamfer(mask, ew, eh);

            var color = new PixelCanvas(w, h);
            var height = new float[w * h];
            var mat = new byte[w * h]; // 1 adoquín, 2 tierra, 3 sillería, 4 techo, 5 cascote
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int ex = x + Margin, ey = y + Margin, i = ey * ew + ex;
                    if (!vis[i]) continue;
                    int wx = ox + ex, wy = oy + ey; // píxel de mundo
                    var theme = TerrainTheme.For(ZoneAt(zoneAtTile, wx, wy));
                    float hgt;
                    Color32 c;
                    byte m;
                    if (!mask[i])
                    {
                        switch (kind[i])
                        {
                            case 1: c = SolidMasonry(theme, wx, wy, 0, 0, out hgt); m = 3; break;
                            case 2: c = Ceiling(theme, wx, wy, 0, out hgt); m = 4; break;
                            case 3: c = Cobble(theme, wx, wy, -1, out hgt); m = 1; break;
                            default: c = Rubble(theme, wx, wy, out hgt); m = 5; break;
                        }
                    }
                    else
                    {
                        int u = up[i], d = down[i], side = Mathf.Min(left[i], right[i]);
                        int depth = dist[i];
                        // Límite irregular entre la capa de superficie y el interior.
                        int skin = 24 + Mathf.RoundToInt(PixelCanvas.ValueNoise(wx / 7f, wy / 7f, 0, 421) * 10f);
                        int earthD = 20 + Mathf.RoundToInt(PixelCanvas.ValueNoise(wx / 6f, 0.3f, 0, 422) * 10f);
                        int crust = 12 + Mathf.RoundToInt(PixelCanvas.ValueNoise(wx / 8f, 0.7f, 0, 423) * 10f);
                        if (u <= earthD) { c = Cobble(theme, wx, wy, u, out hgt); m = (byte)(u < 16 ? 1 : 2); }
                        else if (d <= crust && d < side) { c = Ceiling(theme, wx, wy, d, out hgt); m = 4; }
                        else
                        {
                            // Los sillares de la cara llegan enteros; detrás empieza la roca del interior.
                            MasonryBlock(wx, wy, out _, out int blx, out int bly, out int bbw, out int brh);
                            int bcx = ex - blx + bbw / 2, bcy = ey - bly + brh / 2;
                            int bd = bcx >= 0 && bcx < ew && bcy >= 0 && bcy < eh ? dist[bcy * ew + bcx] : depth;
                            if ((bd <= 30 && depth < 46) || left[i] + right[i] < 72) { c = SolidMasonry(theme, wx, wy, depth, side < u && side < d ? side : 99, out hgt); m = 3; }
                            else { c = DeepRock(theme, wx, wy, out hgt); m = 6; }
                        }
                        if (m == 6 || (m == 2 && u > 18)) c = WithFossils(theme, c, wx, wy, ref hgt);
                        if (m == 2 && depth > skin) { c = DeepRock(theme, wx, wy, out hgt); c = WithFossils(theme, c, wx, wy, ref hgt); m = 6; }

                        // La roca se apaga con la profundidad, pero conserva la estructura.
                        // (la profundidad se limita al margen para que los trozos vecinos empalmen sin costura)
                        float fd = DepthFade(Mathf.Min(depth, Margin));
                        if (m == 3) fd *= 0.65f;
                        c = PixelCanvas.Lerp(c, theme.Deep, fd);
                    }
                    color.Pixels[y * w + x] = c;
                    height[y * w + x] = hgt;
                    mat[y * w + x] = m;
                }
            }

            RimLight(color, height, mat, vis, ew, ox, oy, w, h, zoneAtTile);
            DecorateOpenAir(color, height, vis, mask, ew, eh, ox, oy, w, h, zoneAtTile);
            var normal = NormalsFromHeight(color, height, w, h, 2.2f);
            return new TerrainChunk { TileX = tileX, TileY = tileY, TilesW = tilesW, TilesH = tilesH, Color = color, Normal = normal };
        }

        /// <summary>Zona de un píxel con la frontera entre zonas rota (no una línea vertical perfecta).</summary>
        static Zone ZoneAt(System.Func<int, Zone> zoneAtTile, int wx, int wy)
        {
            Zone a = zoneAtTile(FloorDiv(wx - 40, Tile)), b = zoneAtTile(FloorDiv(wx + 40, Tile));
            if (a == b) return a;
            // Cerca de la frontera: se decide por sillar entero (bloques de una y otra piedra entremezclados).
            MasonryBlock(wx, wy, out int row, out int lx, out _, out int bw, out _);
            float n = PixelCanvas.ValueNoise(row * 0.45f, 3.7f, 0, 351) - 0.5f;
            int j = Mathf.RoundToInt(n * 40f + (PixelCanvas.Hash(wx - lx, row, 352) - 0.5f) * 24f);
            return zoneAtTile(FloorDiv(wx - lx + bw / 2 + j, Tile));
        }

        /// <summary>Clave de una cara vertical: columna de casillas del píxel de roca del borde y hacia dónde mira.</summary>
        static int FaceKey(int faceX, int dir) => FloorDiv(faceX, Tile) * 2 + dir;

        /// <summary>
        /// Desplazamiento (px) del borde visible de una pared respecto a la colisión: positivo sobresale, negativo entra.
        /// Va por hiladas de sillares: cada sillar de la cara asoma o se hunde, las juntas se muerden y algunos sillares
        /// tienen un desconchón.
        /// </summary>
        static int WallOffset(int wy, int faceKey)
        {
            MasonryRow(wy, out int row, out int ly, out int rh);
            float hsh = PixelCanvas.Hash(row, faceKey, 301);
            int p = hsh < 0.1f ? -3 : hsh < 0.26f ? -2 : hsh < 0.42f ? -1 : hsh < 0.56f ? 0 : hsh < 0.74f ? 1 : hsh < 0.9f ? 2 : 3;
            if (ly == 0) p -= 2;                                 // la junta se muerde
            else if (ly == rh - 1 || ly == 1) p -= 1;            // arista redondeada del sillar
            else if (ly == rh - 2 && p > 1) p -= 1;
            float bite = PixelCanvas.Hash(row, faceKey, 302);
            if (bite > 0.8f)
            {
                // Desconchón: un mordisco en forma de cuña.
                int c = 2 + Mathf.FloorToInt(PixelCanvas.Hash(row, faceKey, 303) * (rh - 4));
                int d = Mathf.Abs(ly - c);
                if (d < 4) p = Mathf.Min(p, -3 + d);
            }
            return Mathf.Clamp(p, -3, 3);
        }

        /// <summary>Desplazamiento del borde inferior de un techo: roca áspera con bultos, salientes y mordiscos.</summary>
        static int CeilOffset(int wx, int faceKey)
        {
            float n = PixelCanvas.ValueNoise(wx / 9f, faceKey * 3.1f, 0, 311);
            float m = PixelCanvas.ValueNoise(wx / 3f, faceKey * 1.7f, 0, 312);
            int p = Mathf.RoundToInt((n - 0.5f) * 6f + (m - 0.5f) * 2.4f);
            // Bultos colgantes (roca que gotea) y muescas.
            int cell = FloorDiv(wx, 11);
            float b = PixelCanvas.Hash(cell, faceKey, 313);
            int lx = wx - cell * 11;
            if (b > 0.72f && lx >= 3 && lx <= 8) p = Mathf.Max(p, lx == 3 || lx == 8 ? 1 : lx == 4 || lx == 7 ? 2 : 3);
            else if (b < 0.16f && lx >= 4 && lx <= 7) p = Mathf.Min(p, -2 - (lx == 5 || lx == 6 ? 1 : 0));
            return Mathf.Clamp(p, -3, 3);
        }

        /// <summary>Tamaño (px, en diagonal) del desconchón de la esquina de un saliente.</summary>
        static int CornerChip(int faceKey, int topWy)
        {
            float hsh = PixelCanvas.Hash(faceKey, FloorDiv(topWy, Tile), 305);
            return hsh < 0.4f ? 1 : 2;
        }

        /// <summary>Estado del canto de la primera hilera de adoquines: -1 hundido o junta, 0 normal, +1 asoma un píxel.</summary>
        static int CobbleTop(int wx, int surfaceWy)
        {
            if (InPuddle(wx, surfaceWy, out int pp, out int pl) && pp > 0 && pp < pl - 1) return 0;
            int stone = StoneIndex(wx, surfaceWy * 3, 8, 6, out int pos, out int width);
            float hsh = PixelCanvas.Hash(stone, surfaceWy, 66);
            if (pos == 0) return -1;
            if (hsh > 0.72f) return -1;
            if (hsh < 0.2f && pos >= 2 && pos <= width - 2) return 1;
            return 0;
        }

        /// <summary>Cascotes y tierra apelmazada en los rincones (al pie de las paredes y bajo los techos).</summary>
        static Color32 Rubble(TerrainTheme t, int wx, int wy, out float height)
        {
            float n = PixelCanvas.Hash(FloorDiv(wx, 2), FloorDiv(wy, 2), 361);
            height = 0.5f + n * 0.3f;
            var c = n > 0.66f ? t.Stone[3] : n > 0.33f ? t.Stone[2] : t.Earth[2];
            return PixelCanvas.Lerp(c, t.Deep, 0.2f);
        }

        /// <summary>
        /// Luz de canto sobre la silueta visible: arista clara arriba e izquierda, sombra abajo y a la derecha, y un
        /// contorno oscuro bajo los salientes (nunca negro puro).
        /// </summary>
        static void RimLight(PixelCanvas color, float[] height, byte[] mat, bool[] vis, int ew, int ox, int oy, int w, int h,
                             System.Func<int, Zone> zoneAtTile)
        {
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int ci = y * w + x;
                    if (color.Pixels[ci].a == 0) continue;
                    int i = (y + Margin) * ew + x + Margin;
                    bool airL = !vis[i - 1], airR = !vis[i + 1], airU = !vis[i + ew], airD = !vis[i - ew];
                    if (!(airL || airR || airU || airD || !vis[i - 2 * ew])) continue;
                    var t = TerrainTheme.For(ZoneAt(zoneAtTile, ox + Margin + x, oy + Margin + y));
                    var c = color.Pixels[ci];
                    byte m = mat[ci];
                    if (airD) { c = PixelCanvas.Lerp(c, t.Deep, 0.5f); height[ci] *= 0.6f; }
                    else if (!vis[i - 2 * ew]) c = PixelCanvas.Lerp(c, t.Deep, 0.22f);
                    if (m != 1)
                    {
                        if (airU && !airD) c = PixelCanvas.Lerp(c, t.Stone[5], 0.32f);
                        if (airL && !airD) c = PixelCanvas.Lerp(c, t.Stone[4], 0.28f);
                        if (airR) c = PixelCanvas.Lerp(c, t.Deep, 0.3f);
                    }
                    color.Pixels[ci] = c;
                }
            }
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
        /// Camino de adoquines como en Blasphemous: tres hileras de piedras redondeadas, cada una más hundida y oscura
        /// que la anterior, con el canto superior muy iluminado y juntas oscuras. Desgaste: piedras pulidas y picadas,
        /// grietas, alguna rota, verdín entre las juntas y, en la Costa y el Arrecife, charcos que reflejan el cielo.
        /// Debajo, tierra con cantos, raíces y algún hueso. depthFromTop = -1 es el píxel que asoma sobre el suelo.
        /// </summary>
        static Color32 Cobble(TerrainTheme t, int wx, int wy, int depthFromTop, out float height)
        {
            int surfaceY = wy + depthFromTop;
            var ramp = t.Cobble;
            var mortar = PixelCanvas.Lerp(ramp[0], t.Deep, 0.4f);
            if (depthFromTop < CobbleRows[0] + CobbleRows[1] + CobbleRows[2])
            {
                // Charco sobre los adoquines (Costa y Arrecife).
                if ((t.Zone == Zone.Coast || t.Zone == Zone.Reef) && depthFromTop >= 0 && depthFromTop <= 3 && InPuddle(wx, surfaceY, out int pp, out int pl))
                {
                    bool rim = pp == 0 || pp == pl - 1;
                    if (!(rim && depthFromTop > 1))
                    {
                        height = 0.05f;
                        var sky = PixelCanvas.Lerp(t.Stone[5], new Color32(170, 196, 204, 255), 0.45f);
                        var deepW = PixelCanvas.Lerp(t.Deep, new Color32(30, 44, 58, 255), 0.5f);
                        if (rim) return PixelCanvas.Lerp(ramp[2], deepW, 0.5f);
                        if (depthFromTop == 0) return PixelCanvas.Hash(FloorDiv(wx, 3), surfaceY, 74) > 0.25f ? sky : PixelCanvas.Lerp(sky, deepW, 0.4f);
                        // Reflejo: franjas claras y oscuras, como el cielo y la luna en el agua quieta.
                        float rf = PixelCanvas.ValueNoise(wx / 5f, depthFromTop * 1.7f, 0, 75);
                        if (depthFromTop == 1 && rf > 0.62f) return PixelCanvas.Lerp(sky, deepW, 0.25f);
                        return PixelCanvas.Lerp(deepW, sky, depthFromTop == 1 ? 0.3f : 0.12f + (rf > 0.7f ? 0.15f : 0f));
                    }
                }

                int row = 0, top = 0;
                while (depthFromTop >= top + CobbleRows[row]) { top += CobbleRows[row]; row++; }
                int rowH = CobbleRows[row];
                int stone = StoneIndex(wx + row * 5, surfaceY * 3 + row, 8 - row, 6, out int pos, out int width);
                float tone = PixelCanvas.Hash(stone, surfaceY + row, 61);
                // Algunas piedras están hundidas un píxel y otras asoman (la línea del suelo no es una regla).
                float sh = PixelCanvas.Hash(stone, surfaceY + row, 66);
                int sink = sh > 0.72f ? 1 : (row == 0 && sh < 0.2f && pos >= 2 && pos <= width - 2 ? -1 : 0);
                int localD = depthFromTop - top - sink;
                bool corner = (localD <= 0 || localD >= rowH - 1) && (pos <= 1 || pos >= width - 1);
                float mossN = PixelCanvas.ValueNoise(wx / 4f, surfaceY * 0.37f + row * 2.1f, 0, 76);
                bool mossy = t.MossAmount > 0.05f && mossN > 1f - t.MossAmount * (row == 0 ? 0.55f : 0.9f);
                if (pos == 0 || localD < 0 || corner)
                {
                    height = 0f;
                    if (localD < 0 && row == 0) return PixelCanvas.Lerp(ramp[1], t.Deep, 0.2f);
                    // Verdín entre las juntas.
                    if (mossy && row < 2) return t.Moss[mossN > 1f - t.MossAmount * 0.5f ? 2 : 1];
                    return mortar;
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

                // Piedra rota: le falta la esquina superior derecha.
                float hb = PixelCanvas.Hash(stone, surfaceY + row, 73);
                if (hb < 0.09f && (width - 1 - pos) + localD < 4) { height = 0.1f; return mortar; }
                if (hb < 0.09f && (width - 1 - pos) + localD == 4) idx = Mathf.Max(0, idx - 1);
                // Grieta diagonal.
                if (hb > 0.86f && width > 8)
                {
                    int cx = width / 3 + localD * (hb > 0.93f ? 1 : -1) / 2 + 1;
                    if (pos == cx && localD > 0) { height = 0.2f; return PixelCanvas.Lerp(mortar, ramp[1], 0.3f); }
                    if (pos == cx + 1 && localD > 0) idx = Mathf.Min(5, idx + 1);
                }
                // Desgaste: piedras pulidas por los pasos (mancha clara) y picaduras de 2 px.
                if (tone > 0.45f && tone < 0.6f && row == 0 && localD == 2 && pos > 2 && pos < width - 3) idx = Mathf.Min(5, idx + 1);
                if (localD > 1 && localD < rowH - 1 && PixelCanvas.Hash(FloorDiv(wx, 2), surfaceY + row * 7, 67) > 0.9f) idx--;
                var c = ramp[Mathf.Clamp(idx, 0, ramp.Length - 1)];
                // El verdín trepa por los cantos de las piedras.
                if (mossy && (pos <= 1 || pos >= width - 2 || localD >= rowH - 2) && row < 2 && localD > 0)
                    c = PixelCanvas.Lerp(c, t.Moss[localD <= 1 ? 2 : 1], 0.6f);
                return c;
            }
            return Earth(t, wx, wy, depthFromTop - CobbleRows[0] - CobbleRows[1] - CobbleRows[2], ramp, out height);
        }

        /// <summary>Charcos: tramos de 18-47 px sin ondulación (el agua deja el canto plano).</summary>
        static bool InPuddle(int wx, int surfaceWy, out int pos, out int len)
        {
            int cell = FloorDiv(wx + 37, 112);
            pos = 0;
            len = 0;
            if (PixelCanvas.Hash(cell, surfaceWy, 71) > 0.4f) return false;
            int start = cell * 112 - 37 + 10 + Mathf.FloorToInt(PixelCanvas.Hash(cell, surfaceWy, 72) * 40f);
            len = 18 + Mathf.FloorToInt(PixelCanvas.Hash(cell, surfaceWy, 77) * 30f);
            pos = wx - start;
            return pos >= 0 && pos < len;
        }

        /// <summary>Tierra bajo los adoquines: cantos con su luz arriba, raíces que bajan y algún hueso pequeño.</summary>
        static Color32 Earth(TerrainTheme t, int wx, int wy, int d, Color32[] cobble, out float height)
        {
            var earth = t.Earth;
            height = 0.3f;
            if (d <= 0) return PixelCanvas.Lerp(cobble[0], t.Deep, 0.5f);           // sombra bajo los adoquines
            float n = PixelCanvas.ValueNoise(wx / 4f, wy / 3f, 0, 63);
            int e = d < 3 ? 2 : n > 0.6f ? 2 : n > 0.35f ? 1 : 0;
            // Raíces finas que bajan desde el camino.
            int rc = FloorDiv(wx, 9);
            if (PixelCanvas.Hash(rc, 3, 81) < 0.35f)
            {
                int rx = rc * 9 + 2 + Mathf.FloorToInt(PixelCanvas.Hash(rc, 4, 81) * 5f);
                int len = 4 + Mathf.FloorToInt(PixelCanvas.Hash(rc, 5, 81) * 9f);
                int wob = Mathf.RoundToInt((PixelCanvas.ValueNoise(d * 0.35f, rc, 0, 82) - 0.5f) * 3f);
                if (d < len && wx == rx + wob) { height = 0.6f; return PixelCanvas.Lerp(earth[3], cobble[1], 0.35f); }
            }
            // Cantos: piedras redondeadas de 4-7 px con la luz arriba-izquierda.
            int gx = FloorDiv(wx, 7), gy = FloorDiv(wy, 5);
            float f1 = 99f, f2 = 99f, ox = 0f, oy = 0f;
            int bx = 0, by = 0;
            for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    int cx = gx + dx, cy = gy + dy;
                    float px = cx * 7 + 1 + PixelCanvas.Hash(cx, cy, 83) * 5f, py = cy * 5 + 1 + PixelCanvas.Hash(cx, cy, 84) * 3f;
                    float ddx = (wx - px) / 1.3f, ddy = wy - py, dd = ddx * ddx + ddy * ddy;
                    if (dd < f1) { f2 = f1; f1 = dd; ox = wx - px; oy = wy - py; bx = cx; by = cy; }
                    else if (dd < f2) f2 = dd;
                }
            float hs = PixelCanvas.Hash(bx, by, 85);
            if (d > 1 && hs > 0.5f && Mathf.Sqrt(f1) < 2.6f && Mathf.Sqrt(f2) - Mathf.Sqrt(f1) > 0.8f)
            {
                height = 0.8f;
                int si = oy > 0.8f ? 2 : oy < -0.8f ? 0 : 1;
                if (ox < -1f && si < 2) si++;
                var st = PixelCanvas.Lerp(cobble[si + 1], earth[3], 0.5f);
                return hs > 0.93f ? PixelCanvas.Lerp(st, new Color32(150, 142, 120, 255), 0.4f) : st; // alguno es un hueso
            }
            if (hs <= 0.5f && Mathf.Sqrt(f1) < 1.5f && d > 1) return earth[Mathf.Max(0, e - 1)];   // hueco entre cantos
            return earth[e];
        }

        /// <summary>
        /// Techos: costra de roca en lóbulos (lit desde abajo por las velas: el labio inferior de cada lóbulo coge luz),
        /// con nervios de mineral, manchas húmedas que brillan y la junta oscura entre lóbulos.
        /// </summary>
        static Color32 Ceiling(TerrainTheme t, int wx, int wy, int depthFromBottom, out float height)
        {
            var ramp = t.Stone;
            int gx = FloorDiv(wx, 17), gy = FloorDiv(wy, 9);
            float f1 = 99f, f2 = 99f, ox = 0f, oy = 0f;
            int bx = 0, by = 0;
            for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    int cx = gx + dx, cy = gy + dy;
                    float px = cx * 17 + 2 + PixelCanvas.Hash(cx, cy, 91) * 13f, py = cy * 9 + 1 + PixelCanvas.Hash(cx, cy, 92) * 7f;
                    float ddx = (wx - px) / 1.9f, ddy = (wy - py) * (wy < py ? 0.8f : 1.2f), dd = ddx * ddx + ddy * ddy;
                    if (dd < f1) { f2 = f1; f1 = dd; ox = wx - px; oy = wy - py; bx = cx; by = cy; }
                    else if (dd < f2) f2 = dd;
                }
            float edge = Mathf.Sqrt(f2) - Mathf.Sqrt(f1);
            float hs = PixelCanvas.Hash(bx, by, 93);
            height = 0.5f + Mathf.Clamp01(edge / 3f) * 0.5f;
            float idx = 1.8f + hs * 1.2f;
            if (oy < -1.2f) idx += 1.1f;                 // labio inferior iluminado desde abajo
            else if (oy > 1.5f) idx -= 0.8f;             // parte alta del lóbulo, en sombra
            if (ox < -3f) idx += 0.3f;
            if (depthFromBottom > 5) idx -= (depthFromBottom - 5) * 0.2f;
            // Escamas de roca que cuelgan: pliegue oscuro sobre cada lóbulo y labio claro debajo.
            if (edge < 1.1f && oy >= 0f)
            {
                height = 0.15f;
                return PixelCanvas.Lerp(ramp[0], t.Deep, 0.25f);
            }
            if (edge < 1.1f) idx -= 0.5f;
            else if (edge < 2.2f && oy < 0f) idx += 0.7f;    // arista inferior del lóbulo
            // Nervios de mineral que recorren la roca.
            float vein = Mathf.Abs(PixelCanvas.ValueNoise(wx / 16f, wy / 7f, 0, 94) - 0.5f);
            if (vein < 0.022f) { height = 0.8f; idx = 3.6f; }
            else if (vein < 0.045f && PixelCanvas.ValueNoise(wx / 16f, (wy - 1) / 7f, 0, 94) > 0.5f) idx -= 0.6f;
            var c = ramp[Mathf.Clamp(Mathf.RoundToInt(idx), 0, ramp.Length - 1)];
            // Manchas húmedas con un brillo especular de 1 px.
            float wet = PixelCanvas.ValueNoise(wx / 6f, wy / 10f, 0, 95);
            if (wet > 0.7f)
            {
                c = PixelCanvas.Lerp(c, Ramp.Shadow(c, 0.3f), 0.7f);
                if (oy < -1.2f && PixelCanvas.Hash(wx, wy, 96) > 0.8f) c = PixelCanvas.Lerp(ramp[5], new Color32(220, 230, 225, 255), 0.3f);
            }
            if (t.MossAmount > 0.25f && oy < -1.5f && PixelCanvas.ValueNoise(wx / 5f, wy / 4f, 0, 97) > 0.78f) c = PixelCanvas.Lerp(c, t.Moss[1], 0.6f);
            return c;
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

        /// <summary>Hiladas de la sillería: tramos de 48 px partidos en 2-3 hiladas de alto variable (14-31 px).</summary>
        static void MasonryRow(int wy, out int row, out int ly, out int rh)
        {
            const int G = 48;
            int g = FloorDiv(wy, G), gy = wy - g * G;
            int h0 = 14 + Mathf.FloorToInt(PixelCanvas.Hash(g, 0, 201) * 7f);
            int h1 = 14 + Mathf.FloorToInt(PixelCanvas.Hash(g, 1, 201) * 7f);
            int h2 = G - h0 - h1;
            if (h2 < 12) { h1 += h2; h2 = 0; }
            if (gy < h0) { row = g * 3; ly = gy; rh = h0; }
            else if (gy < h0 + h1) { row = g * 3 + 1; ly = gy - h0; rh = h1; }
            else { row = g * 3 + 2; ly = gy - h0 - h1; rh = h2; }
        }

        /// <summary>Parte una hilera en tramos de minW..minW+rangeW-1 px (el último de cada 256 px absorbe el resto).</summary>
        static int Segment(int x, int key, int minW, int rangeW, out int pos, out int width)
        {
            int shift = Mathf.FloorToInt(PixelCanvas.Hash(key, 1, 207) * 256f);
            int xs = x + shift;
            int block = FloorDiv(xs, 256);
            int start = block * 256, end = start + 256;
            int index = block * 64;
            while (true)
            {
                int wdt = minW + Mathf.FloorToInt(PixelCanvas.Hash(index, key, 208) * rangeW);
                if (end - (start + wdt) < minW) wdt = end - start;
                if (xs < start + wdt)
                {
                    pos = xs - start;
                    width = wdt;
                    return index;
                }
                start += wdt;
                index++;
            }
        }

        // Relieves tallados de las Ruinas ('X' = relieve).
        static readonly string[][] Carvings =
        {
            new[] { // pez
                "..XXXXX.....",
                ".XXXXXXXX..X",
                "XX.XXXXXXXXX",
                "XXXXXXXXXXX.",
                ".XXXXXXXX..X",
                "..XXXXX.....",
            },
            new[] { // ojo
                "...XXXXX...",
                ".XX.....XX.",
                "X...XXX...X",
                "X..XX.XX..X",
                "X...XXX...X",
                ".XX.....XX.",
                "...XXXXX...",
            },
            new[] { // tentáculo
                "...XX...",
                "..XX....",
                "..XX....",
                "...XX...",
                "....XX..",
                "....XX..",
                "...XX...",
                "..XX..X.",
                ".XX..XX.",
                ".XX.XX..",
                "..XXX...",
            },
        };

        // Glifos de R'lyeh (5×5) para las runas grabadas.
        static readonly string[][] Glyphs =
        {
            new[] { ".XXX.", "X.X.X", "..X..", ".XXX.", "..X.." },
            new[] { "X.X..", "XXX..", "..XXX", "..X.X", "..X.." },
            new[] { ".X...", "XXXX.", ".X..X", ".X.X.", "XX..." },
            new[] { "..X..", "X.X.X", ".XXX.", "X.X.X", "..X.." },
            new[] { "XXX.X", "X...X", "X.XXX", "X.X..", "XXX.." },
        };

        static bool CarvingAt(string[] art, int x, int y)
        {
            if (x < 0 || y < 0 || y >= art.Length || x >= art[0].Length) return false;
            return art[art.Length - 1 - y][x] == 'X';
        }

        /// <summary>
        /// Sillería de la roca sólida: sillares grandes con bisel (arista clara arriba-izquierda, sombra abajo-derecha),
        /// desconchones en las esquinas, grietas ramificadas, manchas de humedad y salitre que chorrean, verdín en las
        /// juntas, tono distinto por bloque y, en las Ruinas, algún sillar tallado con un relieve (pez, ojo, tentáculo).
        /// <paramref name="faceDist"/> es la distancia a la cara vertical (99 si no es una cara).
        /// </summary>
        static Color32 SolidMasonry(TerrainTheme t, int wx, int wy, int depth, int faceDist, out float height)
        {
            var ramp = t.Stone;
            MasonryRow(wy, out int row, out int ly, out int rh);
            int id = Segment(wx, row * 13 + 5, 22, 22, out int lx, out int bw);
            float hb = PixelCanvas.Hash(id, row, 211);       // carácter del bloque
            float hb2 = PixelCanvas.Hash(id, row, 212);
            bool face = faceDist < 24;

            // Junta (abajo e izquierda de cada sillar).
            if (ly == 0 || lx == 0)
            {
                height = 0f;
                var mortar = PixelCanvas.Lerp(ramp[0], ramp[1], 0.5f);
                // Verdín y salitre en las juntas.
                float mj = PixelCanvas.ValueNoise(wx / 5f, wy / 4f, 0, 213);
                if (t.MossAmount > 0.05f && mj > 1f - t.MossAmount * 0.6f) return PixelCanvas.Lerp(t.Moss[mj > 1f - t.MossAmount * 0.25f ? 1 : 0], ramp[1], 0.3f);
                if (PixelCanvas.Hash(wx, wy, 214) > 0.94f) return PixelCanvas.Lerp(ramp[3], new Color32(200, 205, 196, 255), 0.25f);
                return mortar;
            }

            int top = rh - 1 - ly;     // px desde la arista superior
            int rgt = bw - 1 - lx;     // px desde la arista derecha
            float idx = 2.5f + (hb - 0.5f) * 1.6f;
            height = 1f;

            // Bisel por bloque.
            if (top == 0) { idx += 1.6f; height = 0.65f; }
            else if (top == 1) { idx += 0.6f; height = 0.9f; }
            if (lx == 1) { idx += 1.0f; height = Mathf.Min(height, 0.7f); }
            else if (lx == 2) idx += 0.3f;
            if (ly == 1) { idx -= 1.2f; height = Mathf.Min(height, 0.6f); }
            else if (ly == 2) idx -= 0.4f;
            if (rgt == 0) { idx -= 1.1f; height = Mathf.Min(height, 0.6f); }
            else if (rgt == 1) idx -= 0.35f;

            // Desconchones en las esquinas (hasta dos por sillar).
            int chip = 0;
            int cs = 2 + Mathf.FloorToInt(hb2 * 4f);
            int corner = Mathf.FloorToInt(PixelCanvas.Hash(id, row, 215) * 6f); // 0-3 una esquina, 4 dos, 5 ninguna
            bool TL() => (lx - 1) + top < cs;
            bool TR() => rgt + top < cs;
            bool BL() => (lx - 1) + (ly - 1) < cs - 1;
            bool BR() => rgt + (ly - 1) < cs - 1;
            if ((corner == 0 || corner == 4) && TL()) chip = 1;
            if ((corner == 1 || corner == 4) && BR()) chip = 2;
            if (corner == 2 && TR()) chip = 1;
            if (corner == 3 && BL()) chip = 2;
            if (chip != 0)
            {
                height = 0.35f;
                return ramp[chip == 1 ? 1 : 0];
            }

            // Textura de grano: manchas de 2-4 px (no ruido suelto) y picaduras.
            float grain = PixelCanvas.ValueNoise(wx / 3f, wy / 2.5f, 0, 31);
            if (grain > 0.76f) idx -= 0.7f;
            else if (grain < 0.2f) idx += 0.6f;
            if (PixelCanvas.Hash(FloorDiv(wx, 2), FloorDiv(wy, 2), 216) > 0.95f && top > 2 && ly > 2) { idx -= 1.2f; height = 0.7f; }

            // Grieta ramificada que baja desde la arista superior.
            if (hb2 < 0.2f && bw > 14 && rh > 12)
            {
                int c0 = 3 + Mathf.FloorToInt(PixelCanvas.Hash(id, row, 217) * (bw - 6));
                int len = rh / 2 + Mathf.FloorToInt(hb * rh * 0.5f);
                if (top < len)
                {
                    int cx = c0 + Mathf.RoundToInt((PixelCanvas.ValueNoise(top * 0.3f, id * 0.37f, 0, 218) - 0.5f) * 5f);
                    int cxn = c0 + Mathf.RoundToInt((PixelCanvas.ValueNoise((top + 1) * 0.3f, id * 0.37f, 0, 218) - 0.5f) * 5f);
                    int bTop = len / 2, dir = hb > 0.5f ? 1 : -1;
                    bool crack = lx == cx || (top + 1 < len && lx > Mathf.Min(cx, cxn) && lx < Mathf.Max(cx, cxn));
                    bool branch = top > bTop && top < bTop + 5 && lx == cx + (top - bTop) * dir && hb > 0.25f;
                    if (crack || branch)
                    {
                        height = 0.15f;
                        return PixelCanvas.Lerp(ramp[0], ramp[1], 0.3f);
                    }
                    if (lx == cx + 1 || (top > bTop && top < bTop + 6 && lx == cx + (top - bTop) * dir + 1)) idx += 0.9f; // labio iluminado
                }
            }

            // Relieve tallado (Ruinas): un panel hundido con la figura en relieve.
            if (t.Zone == Zone.Ruins && face && hb > 0.86f && bw >= 22 && rh >= 13)
            {
                int which = Mathf.FloorToInt(hb2 * 3f) % 3;
                var art = Carvings[which];
                int aw = art[0].Length, ah = art.Length;
                if (aw + 8 > bw || ah + 6 > rh) { art = Carvings[0]; aw = art[0].Length; ah = art.Length; }
                if (aw + 8 <= bw && ah + 6 <= rh)
                {
                    int px0 = 3, px1 = bw - 4, py0 = 3, py1 = rh - 4;
                    if (lx >= px0 && lx <= px1 && ly >= py0 && ly <= py1)
                    {
                        int ax = lx - (bw - aw) / 2, ay = ly - (rh - ah) / 2;
                        bool on = CarvingAt(art, ax, ay);
                        float r = idx - 1.2f; // fondo hundido del panel
                        if (ly == py1 || lx == px0) r -= 0.8f;          // sombra que arroja el marco
                        else if (ly == py0 || lx == px1) r += 0.7f;     // canto inferior-derecho iluminado
                        height = 0.55f;
                        if (on)
                        {
                            r = idx;
                            height = 0.85f;
                            if (!CarvingAt(art, ax, ay + 1) || !CarvingAt(art, ax - 1, ay)) r += 1.1f;
                            else if (!CarvingAt(art, ax, ay - 1) || !CarvingAt(art, ax + 1, ay)) r -= 0.9f;
                        }
                        return ramp[Mathf.Clamp(Mathf.RoundToInt(r), 0, ramp.Length - 1)];
                    }
                }
            }

            // Runas tenues en las ruinas: una fila de glifos grabados con un resto de brillo verdoso.
            if (t.GlowingGlyphs && depth < 40 && hb > 0.8f && hb < 0.86f && bw >= 16 && rh >= 11)
            {
                int n = Mathf.Min(3, (bw - 6) / 6);
                int gx0 = (bw - n * 6 + 1) / 2, gy0 = (rh - 5) / 2;
                int gi = (lx - gx0) / 6, gxx = (lx - gx0) % 6, gyy = ly - gy0;
                if (lx >= gx0 && gi < n && gxx < 5 && gyy >= 0 && gyy < 5)
                {
                    var g = Glyphs[Mathf.FloorToInt(PixelCanvas.Hash(id, gi, 219) * Glyphs.Length) % Glyphs.Length];
                    if (g[4 - gyy][gxx] == 'X') { height = 0.5f; return PixelCanvas.Lerp(t.Accent, ramp[1], 0.6f); }
                    if (gyy < 4 && g[3 - gyy][gxx] == 'X') idx -= 0.8f;   // sombra del surco
                }
            }

            // Detalles de cada zona.
            if (t.Zone == Zone.Sanctuary && t.AccentInlays && face && hb2 > 0.44f && hb2 < 0.56f && bw >= 18 && rh >= 13)
            {
                // Filete de oro embutido a media altura del sillar, con rosetas cada pocos píxeles.
                int fy = rh / 2;
                if (ly == fy && lx > 1 && rgt > 1)
                {
                    height = 0.9f;
                    bool ros = (wx & 7) == 0;
                    return ros ? PixelCanvas.Lerp(t.Accent, new Color32(255, 240, 190, 255), 0.45f) : PixelCanvas.Lerp(t.Accent, ramp[2], 0.15f);
                }
                if (ly == fy + 1 && lx > 1 && rgt > 1 && (wx & 7) == 0) { height = 0.9f; return PixelCanvas.Lerp(t.Accent, ramp[1], 0.4f); }
                if (ly == fy - 1 && lx > 1 && rgt > 1) idx -= 0.9f; // sombra bajo el filete
                if (ly == fy + 1 && lx > 1 && rgt > 1) idx += 0.5f;
            }
            if (t.Zone == Zone.Reef && ly < rh - 3)
            {
                // Percebes (anillo claro con el hueco oscuro) y brotes de coral en la mitad baja de los sillares.
                int cx = FloorDiv(wx, 6), cy = FloorDiv(wy, 5);
                float hc = PixelCanvas.Hash(cx, cy, 231);
                if (hc < 0.16f)
                {
                    float px = cx * 6 + 3f, py = cy * 5 + 2.5f;
                    float d = Mathf.Sqrt((wx + 0.5f - px) * (wx + 0.5f - px) + (wy + 0.5f - py) * (wy + 0.5f - py));
                    if (hc < 0.05f && d < 2.2f)
                    {
                        height = 0.9f;
                        return wy + 0.5f > py ? PixelCanvas.Lerp(t.Accent, new Color32(255, 214, 200, 255), 0.25f) : PixelCanvas.Lerp(t.Accent, ramp[1], 0.4f);
                    }
                    if (d < 0.9f) { height = 0.4f; return PixelCanvas.Lerp(t.Deep, ramp[0], 0.5f); }
                    if (d < 2.1f) { height = 0.85f; idx = wy + 0.5f > py ? 4.2f : 2.6f; }
                }
            }
            if (t.Zone == Zone.Coast && ly <= 3 && lx > 1 && lx < bw - 2)
            {
                // Conchas incrustadas junto a la junta baja.
                int cx = FloorDiv(wx, 5);
                if (PixelCanvas.Hash(cx, row, 232) < 0.07f)
                {
                    int sx = wx - cx * 5;
                    if (sx >= 1 && sx <= 3 && ly >= 1 && ly <= 2 && !(ly == 2 && (sx == 1 || sx == 3)))
                    {
                        height = 0.85f;
                        return ly == 2 || sx == 1 ? new Color32(196, 188, 168, 255) : new Color32(140, 132, 118, 255);
                    }
                }
            }

            var c = ramp[Mathf.Clamp(Mathf.RoundToInt(idx), 0, ramp.Length - 1)];

            // Tono por bloque: unos tiran a verdoso (humedad), otros a cálido.
            if (hb2 > 0.8f) c = PixelCanvas.Lerp(c, t.Moss[1], 0.18f);
            else if (hb2 > 0.62f && hb2 < 0.7f) c = PixelCanvas.Lerp(c, t.Earth[3], 0.2f);

            // Manchas de humedad que chorrean desde las juntas, con borde de salitre.
            float wetCol = PixelCanvas.ValueNoise(wx / 4f, row * 1.3f, 0, 221);
            int wetLen = Mathf.FloorToInt(PixelCanvas.ValueNoise(wx / 2.2f, row * 2.1f, 0, 222) * rh * 1.1f);
            if (wetCol > 0.7f && top <= wetLen)
            {
                c = PixelCanvas.Lerp(c, Ramp.Shadow(c, 0.35f), 0.75f);
                height -= 0.05f;
                if (top == wetLen || top == wetLen - 1) c = PixelCanvas.Lerp(c, new Color32(196, 200, 188, 255), top == wetLen ? 0.28f : 0.12f);
            }
            // Verdín sobre la arista superior de los sillares.
            if (t.MossAmount > 0.05f && top <= 1)
            {
                float mt = PixelCanvas.ValueNoise(wx / 4f, row * 0.9f, 0, 223);
                if (mt > 1f - t.MossAmount * 0.7f) c = PixelCanvas.Lerp(t.Moss[top == 0 ? 2 : 1], c, 0.3f);
            }
            return c;
        }

        /// <summary>Sillar de la sillería que contiene el píxel (hilada, posición dentro del sillar y su tamaño).</summary>
        static int MasonryBlock(int wx, int wy, out int row, out int lx, out int ly, out int bw, out int rh)
        {
            MasonryRow(wy, out row, out ly, out rh);
            return Segment(wx, row * 13 + 5, 22, 22, out lx, out bw);
        }

        /// <summary>
        /// Oscurecimiento con la profundidad: la roca se apaga pero conserva la estructura (≥ ~22 % del brillo de la
        /// paleta hasta unos 160 px dentro; solo muy al fondo baja más).
        /// </summary>
        static float DepthFade(int depth)
        {
            if (depth <= 10) return 0f;
            if (depth < 42) { float k = (depth - 10) / 32f; return 0.66f * k * k * (3f - 2f * k); }
            if (depth < 140) return 0.66f + 0.12f * (depth - 42) / 98f;
            return Mathf.Min(0.88f, 0.78f + 0.1f * (depth - 140) / 160f);
        }

        // Fósiles y restos incrustados en la roca ('X' hueso/concha, 'o' hueco).
        static readonly string[][] Fossils =
        {
            new[] { // cráneo
                "..XXXXX..",
                ".XXXXXXX.",
                "XXXXXXXXX",
                "XooXXXooX",
                "XooXXXooX",
                ".XXX.XXX.",
                "..XXXXX..",
                "..X.X.X..",
            },
            new[] { // amonites
                "..XXXX..",
                ".XooooX.",
                "XoXXXXoX",
                "XoXooXoX",
                "XoXoXXoX",
                "XoXXooX.",
                ".XooXX..",
                "..XX....",
            },
            new[] { // espina de pez
                ".....X.X.X....",
                "X...XXXXXXXXX.",
                "XX.X.X.X.X.XXX",
                ".XXXXXXXXXXXoX",
                "XX.X.X.X.X.XXX",
                "X...XXXXXXXXX.",
                ".....X.X.X....",
            },
            new[] { // costillar
                "XXXXXXXXXXXXX",
                "X.X.X.X.X.X.X",
                "X.X.X.X.X.X.X",
                "X.X.X.X.X.X.X",
                ".X.X.X.X.X.X.",
                ".X.X.X.X.X.X.",
                "..X.X.X.X.X..",
            },
            new[] { // hueso largo
                "XX.........XX",
                "XXXXXXXXXXXXX",
                "XXXXXXXXXXXXX",
                "XX.........XX",
            },
            new[] { // tentáculo fósil con ventosas
                "..........XX",
                ".......XXXX.",
                "....XXXoX...",
                "..XXoXX.....",
                ".XoXX.......",
                "XXX.........",
            },
            new[] { // concha de bivalvo
                "..XXXXX..",
                ".XX.X.XX.",
                "XX.X.X.XX",
                "X.X.X.X.X",
                ".XXXXXXX.",
                "...XXX...",
            },
        };

        /// <summary>¿Hay un fósil en este píxel? Devuelve 0 nada, 1 hueso/concha, 2 hueco, 3 sombra bajo el fósil.</summary>
        static int FossilAt(int wx, int wy, int seed, float density)
        {
            const int CW = 64, CH = 52;
            int cx = FloorDiv(wx, CW), cy = FloorDiv(wy, CH);
            if (PixelCanvas.Hash(cx, cy, seed) > density) return 0;
            var art = Fossils[Mathf.FloorToInt(PixelCanvas.Hash(cx, cy, seed + 1) * Fossils.Length) % Fossils.Length];
            int aw = art[0].Length, ah = art.Length;
            int x0 = cx * CW + 4 + Mathf.FloorToInt(PixelCanvas.Hash(cx, cy, seed + 2) * (CW - aw - 8));
            int y0 = cy * CH + 4 + Mathf.FloorToInt(PixelCanvas.Hash(cx, cy, seed + 3) * (CH - ah - 8));
            bool flip = PixelCanvas.Hash(cx, cy, seed + 4) > 0.5f;
            int lx = wx - x0, ly = wy - y0;
            if (lx < 0 || ly < -1 || lx > aw || ly >= ah) return 0;
            char At(int x, int y)
            {
                if (x < 0 || y < 0 || x >= aw || y >= ah) return '.';
                return art[ah - 1 - y][flip ? aw - 1 - x : x];
            }
            char ch = At(lx, ly);
            if (ch == 'X') return 1;
            if (ch == 'o') return 2;
            if (At(lx - 1, ly + 1) == 'X') return 3;
            return 0;
        }

        /// <summary>
        /// Interior de la roca: estratos ondulados de grandes bloques ciclópeos biselados, conglomerado de cantos y tierra
        /// con raíces, separados por vetas oscuras, con grietas y restos incrustados (cráneos, conchas, espinas de pez,
        /// costillares, tentáculos fósiles). Se pinta con poco contraste: luego se apaga con la profundidad.
        /// </summary>
        static Color32 DeepRock(TerrainTheme t, int wx, int wy, out float height)
        {
            var ramp = t.Stone;
            float wave = (PixelCanvas.ValueNoise(wx / 44f, 0.5f, 0, 401) - 0.5f) * 18f + (PixelCanvas.ValueNoise(wx / 12f, 1.5f, 0, 402) - 0.5f) * 3f;
            int sy = wy + Mathf.RoundToInt(wave);
            const int G = 100;
            int g = FloorDiv(sy, G), gy = sy - g * G;
            int b1 = 22 + Mathf.FloorToInt(PixelCanvas.Hash(g, 0, 403) * 18f);
            int b2 = b1 + 20 + Mathf.FloorToInt(PixelCanvas.Hash(g, 1, 403) * 22f);
            int k = gy < b1 ? 0 : gy < b2 ? 1 : 2;
            int s0 = k == 0 ? 0 : k == 1 ? b1 : b2;
            int sh = (k == 0 ? b1 : k == 1 ? b2 : G) - s0;
            int sl = gy - s0;                       // px desde la base del estrato
            int stratum = g * 3 + k;
            float ht = PixelCanvas.Hash(stratum, 7, 404);
            int type = ht < 0.5f ? 0 : ht < 0.8f ? 1 : 2;
            float idx;
            height = 0.8f;

            // Veta oscura entre estratos, con algún grano de mineral.
            if (sl <= 1)
            {
                height = 0.1f;
                if (PixelCanvas.Hash(FloorDiv(wx, 2), stratum, 405) > 0.93f) return ramp[2];
                return PixelCanvas.Lerp(ramp[0], t.Deep, 0.35f);
            }

            if (type == 0)
            {
                // Bloques ciclópeos.
                int id = Segment(wx, stratum * 7 + 3, 38, 54, out int lx, out int bw);
                int bh = sh - 2, ly = sl - 2, top = bh - 1 - ly, rgt = bw - 1 - lx;
                if (lx == 0) { height = 0.1f; return PixelCanvas.Lerp(ramp[0], t.Deep, 0.25f); }
                float hb = PixelCanvas.Hash(id, stratum, 406);
                idx = 1.9f + (hb - 0.5f) * 1.2f;
                height = 1f;
                if (top == 0) { idx += 1.2f; height = 0.7f; } else if (top == 1) idx += 0.5f;
                if (lx == 1) { idx += 0.8f; height = 0.75f; } else if (lx == 2) idx += 0.3f;
                if (ly == 0) { idx -= 1.4f; height = 0.6f; } else if (ly == 1) idx -= 0.6f;
                if (rgt == 0) { idx -= 1.1f; height = 0.65f; } else if (rgt == 1) idx -= 0.4f;
                // Esquina desconchada.
                int cs = 3 + Mathf.FloorToInt(hb * 5f);
                if (hb > 0.5f && (lx - 1) + top < cs) { height = 0.35f; return ramp[1]; }
                if (hb < 0.3f && rgt + ly < cs - 1) { height = 0.35f; return ramp[0]; }
                // Grieta diagonal que cruza el bloque.
                if (PixelCanvas.Hash(id, stratum, 407) < 0.35f)
                {
                    int c0 = Mathf.FloorToInt(PixelCanvas.Hash(id, stratum, 408) * bw);
                    int along = lx - c0 - top / 2 + Mathf.RoundToInt((PixelCanvas.ValueNoise(top * 0.35f, id, 0, 409) - 0.5f) * 4f);
                    if (along == 0 && top < bh * 3 / 4) { height = 0.15f; return PixelCanvas.Lerp(ramp[0], t.Deep, 0.2f); }
                    if (along == 1 && top < bh * 3 / 4) idx += 0.8f;
                }
                // Grano en manchas grandes (la piedra ciclópea está muy gastada).
                float n = PixelCanvas.ValueNoise(wx / 6f, wy / 5f, 0, 410);
                if (n > 0.7f) idx -= 0.6f; else if (n < 0.25f) idx += 0.5f;
                if (PixelCanvas.Hash(FloorDiv(wx, 2), FloorDiv(wy, 2), 411) > 0.96f) idx -= 1f;
            }
            else if (type == 1)
            {
                // Conglomerado: cantos rodados apretados en una matriz oscura.
                float f1 = 99f, f2 = 99f;
                int bestX = 0, bestY = 0;
                int gx = FloorDiv(wx, 10), gyy = FloorDiv(wy, 8);
                float ox1 = 0f, oy1 = 0f;
                for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int cx = gx + dx, cy = gyy + dy;
                        float px = cx * 10 + 1 + PixelCanvas.Hash(cx, cy, 412) * 8f;
                        float py = cy * 8 + 1 + PixelCanvas.Hash(cx, cy, 413) * 6f;
                        float ddx = (wx - px) / 1.2f, ddy = wy - py;
                        float d = ddx * ddx + ddy * ddy;
                        if (d < f1) { f2 = f1; f1 = d; bestX = cx; bestY = cy; ox1 = wx - px; oy1 = wy - py; }
                        else if (d < f2) f2 = d;
                    }
                float edge = Mathf.Sqrt(f2) - Mathf.Sqrt(f1);
                if (edge < 1.1f) { height = 0.2f; return PixelCanvas.Lerp(t.Earth[0], t.Deep, 0.3f); }
                float hs = PixelCanvas.Hash(bestX, bestY, 414);
                idx = 1.8f + hs * 1.6f;
                height = 0.6f + Mathf.Clamp01(edge / 4f) * 0.4f;
                if (oy1 > 1.5f && ox1 < 1f) idx += 1f;            // luz arriba-izquierda del canto
                else if (oy1 < -1.5f || ox1 > 2.5f) idx -= 0.7f;
                if (edge < 2f) idx -= 0.4f;
                var stone = ramp[Mathf.Clamp(Mathf.RoundToInt(idx), 0, ramp.Length - 1)];
                return hs > 0.75f ? PixelCanvas.Lerp(stone, t.Earth[3], 0.35f) : stone;
            }
            else
            {
                // Tierra apelmazada con raíces.
                float n = PixelCanvas.ValueNoise(wx / 5f, wy / 3f, 0, 415);
                idx = 1.4f + n * 1.4f;
                height = 0.5f + n * 0.2f;
                float rootA = PixelCanvas.ValueNoise(wx / 9f, wy / 26f, 0, 416);
                float rootLine = Mathf.Abs(PixelCanvas.ValueNoise(wx / 7f + rootA * 3f, wy / 9f, 0, 417) - 0.5f);
                if (rootLine < 0.03f && rootA > 0.45f) { height = 0.75f; return PixelCanvas.Lerp(t.Earth[3], ramp[3], 0.3f); }
                var e = t.Earth[Mathf.Clamp(Mathf.RoundToInt(idx), 0, t.Earth.Length - 1)];
                return PixelCanvas.Lerp(e, ramp[1], 0.3f);
            }
            return ramp[Mathf.Clamp(Mathf.RoundToInt(idx), 0, ramp.Length - 1)];
        }

        /// <summary>Superpone los fósiles (con muy poco contraste) sobre la roca del interior.</summary>
        static Color32 WithFossils(TerrainTheme t, Color32 c, int wx, int wy, ref float height)
        {
            int f = FossilAt(wx, wy, 420, 0.42f);
            if (f == 0) f = FossilAt(wx + 31, wy + 23, 430, 0.18f);
            if (f == 0) return c;
            var bone = PixelCanvas.Lerp(t.Stone[4], new Color32(196, 186, 160, 255), 0.35f);
            if (f == 1) { height = 0.9f; return PixelCanvas.Lerp(c, bone, 0.45f); }
            if (f == 2) { height = 0.3f; return PixelCanvas.Lerp(c, t.Deep, 0.4f); }
            return PixelCanvas.Lerp(c, t.Deep, 0.25f);
        }

        /// <summary>Sillería ciclópea: bloques grandes e irregulares con bisel y argamasa (wallFace = cara de la roca).</summary>
        static Color32 Masonry(TerrainTheme t, int wx, int wy, int depth, bool wallFace, out float height)
        {
            // Las caras verticales de la roca usan la sillería detallada; el resto (p. ej. las paredes de fondo) la
            // sillería sencilla de siempre, con poco contraste.
            if (wallFace) return SolidMasonry(t, wx, wy, depth, depth, out height);
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

        /// <summary>
        /// Vida y desgaste al aire: matas de hierba/algas de 2-3 tonos sobre el suelo, cortinas de musgo que cuelgan por
        /// los cantos, estalactitas sombreadas con su gota, raíces que asoman del techo y telarañas en los rincones.
        /// Recorre también el margen del trozo para que lo que nace en el trozo vecino continúe sin cortes.
        /// </summary>
        static void DecorateOpenAir(PixelCanvas color, float[] height, bool[] vis, bool[] mask, int ew, int eh, int ox, int oy,
                                    int w, int h, System.Func<int, Zone> zoneAtTile)
        {
            bool Vis(int ex, int ey) => ex >= 0 && ey >= 0 && ex < ew && ey < eh && vis[ey * ew + ex];
            bool Solid(int ex, int ey) => ex >= 0 && ey >= 0 && ex < ew && ey < eh && mask[ey * ew + ex];
            // Pinta en el aire (no tapa la roca).
            void Put(int ex, int ey, Color32 c, float hgt)
            {
                int x = ex - Margin, y = ey - Margin;
                if (x < 0 || y < 0 || x >= w || y >= h || Vis(ex, ey)) return;
                color.Pixels[y * w + x] = c;
                height[y * w + x] = hgt;
            }
            // Pinta encima de lo que haya (musgo sobre la piedra).
            void Over(int ex, int ey, Color32 c, float hgt)
            {
                int x = ex - Margin, y = ey - Margin;
                if (x < 0 || y < 0 || x >= w || y >= h) return;
                if (!Vis(ex, ey) && color.Pixels[y * w + x].a == 0 && !Vis(ex, ey + 1) && !Vis(ex - 1, ey) && !Vis(ex + 1, ey)) return;
                color.Pixels[y * w + x] = c;
                height[y * w + x] = hgt;
            }

            const int Reach = 30;
            int x0 = Mathf.Max(2, Margin - Reach), x1 = Mathf.Min(ew - 3, Margin + w + Reach);
            int y0 = Mathf.Max(2, Margin - Reach), y1 = Mathf.Min(eh - 3, Margin + h + Reach);
            for (int ey = y0; ey < y1; ey++)
            {
                int wy = oy + ey;
                for (int ex = x0; ex < x1; ex++)
                {
                    int i = ey * ew + ex;
                    if (!vis[i]) continue;
                    bool openAbove = !vis[i + ew], openBelow = !vis[i - ew];
                    if (!openAbove && !openBelow) continue;
                    int wx = ox + ex;
                    var t = TerrainTheme.For(ZoneAt(zoneAtTile, wx, wy));
                    var moss = t.Moss;

                    bool realFloor = Solid(ex, ey - 1) && Solid(ex, ey - 3) && !Solid(ex, ey + 2);
                    if (openAbove && Solid(ex, ey - 2) && (realFloor || PixelCanvas.Hash(wx, wy, 50) < 0.25f))
                    {
                        // Matas de hierba o algas: varias briznas en abanico, base oscura y puntas claras.
                        if (PixelCanvas.Hash(wx, wy, 51) < t.MossAmount * 0.2f)
                        {
                            int blades = 3 + Mathf.FloorToInt(PixelCanvas.Hash(wx, wy, 52) * 4f);
                            int tall = 3 + Mathf.FloorToInt(PixelCanvas.Hash(wx, wy, 53) * 5f);
                            for (int b = 0; b < blades; b++)
                            {
                                float lean = (b - (blades - 1) * 0.5f) * 0.5f + (PixelCanvas.Hash(wx + b, wy, 54) - 0.5f);
                                int len = Mathf.Max(2, tall - Mathf.Abs(b - blades / 2) - Mathf.FloorToInt(PixelCanvas.Hash(wx, b, 55) * 2f));
                                int bx = ex + b - blades / 2;
                                for (int k = 1; k <= len; k++)
                                {
                                    int px = bx + Mathf.RoundToInt(lean * k * k / (float)(len * 2));
                                    int tone = k == len ? 3 : k > len / 2 ? 2 : k > 1 ? 1 : 0;
                                    if ((b & 1) == 1 && tone > 1) tone--;
                                    Put(px, ey + k, moss[Mathf.Clamp(tone, 0, moss.Length - 1)], 0.6f);
                                }
                            }
                        }
                        // Cortina de musgo que cuelga por el canto de un saliente.
                        bool edgeL = !Vis(ex - 1, ey) && !Vis(ex - 1, ey - 3), edgeR = !Vis(ex + 1, ey) && !Vis(ex + 1, ey - 3);
                        if ((edgeL || edgeR) && PixelCanvas.Hash(wx, wy, 56) < t.MossAmount * 1.6f)
                        {
                            int dir = edgeL ? 1 : -1;
                            for (int s = -1; s < 5; s++)
                            {
                                int sx = ex + s * dir;
                                int len = 2 + Mathf.FloorToInt(PixelCanvas.Hash(wx + s, wy, 57) * (s < 2 ? 14f : 6f));
                                for (int k = 1; k <= len; k++)
                                {
                                    int tone = k <= 2 ? 2 : k < len - 1 ? 1 : 0;
                                    if (s == -1) Put(sx, ey - k, moss[Mathf.Max(0, tone - 1)], 0.5f);
                                    else Over(sx, ey - k, moss[tone], 0.5f);
                                }
                            }
                        }
                    }

                    if (openBelow && Solid(ex, ey + 2) && Solid(ex, ey + 6))
                    {
                        // Un elemento por tramo de 9 px de techo.
                        int cell = FloorDiv(wx, 9);
                        int at = cell * 9 + Mathf.FloorToInt(PixelCanvas.Hash(cell, FloorDiv(wy, Tile), 61) * 9f);
                        if (wx != at) continue;
                        float pick = PixelCanvas.Hash(cell, FloorDiv(wy, Tile), 62);
                        float sz = PixelCanvas.Hash(cell, FloorDiv(wy, Tile), 63);
                        if (pick < 0.42f)
                        {
                            // Estalactita: cono sombreado (izquierda clara, derecha oscura) con una gota en la punta.
                            int len = 4 + Mathf.FloorToInt(sz * sz * 15f);
                            int half = len > 11 ? 2 : 1;
                            var st = t.Stone;
                            for (int k = 0; k < len; k++)
                            {
                                int hw = Mathf.RoundToInt(half * (1f - k / (float)len) + 0.3f);
                                for (int dx = -hw; dx <= hw; dx++)
                                {
                                    int tone = dx < 0 ? 3 : dx == 0 ? 2 : 1;
                                    if (hw == 0) tone = k == len - 1 ? 3 : 2;
                                    if (dx == hw && hw > 0) tone = 0;
                                    Put(ex + dx, ey - 1 - k, st[tone], 0.7f - 0.3f * (dx / (float)(hw + 1)));
                                }
                            }
                            if (sz > 0.45f) Put(ex, ey - 1 - len - 2 - Mathf.FloorToInt(sz * 3f), PixelCanvas.WithAlpha(PixelCanvas.Lerp(st[5], new Color32(200, 230, 230, 255), 0.5f), 0.8f), 0.6f);
                        }
                        else if (pick < 0.66f && t.MossAmount > 0.15f)
                        {
                            // Raíz: línea ondulante con un par de raicillas, oscura con una luz a la izquierda.
                            int len = 8 + Mathf.FloorToInt(sz * 18f);
                            var dark = PixelCanvas.Lerp(t.Earth[1], t.Stone[1], 0.3f);
                            var lite = PixelCanvas.Lerp(t.Earth[3], t.Cobble[1], 0.4f);
                            int px = ex;
                            for (int k = 0; k < len; k++)
                            {
                                px = ex + Mathf.RoundToInt((PixelCanvas.ValueNoise(k * 0.22f, cell, 0, 64) - 0.5f) * 6f);
                                Put(px, ey - 1 - k, k % 4 == 1 ? lite : dark, 0.6f);
                                if (k < len / 3) Put(px + 1, ey - 1 - k, dark, 0.55f);
                                if (k == len / 3 || k == (len * 2) / 3)
                                {
                                    int dir = (k & 1) == 0 ? 1 : -1;
                                    for (int r = 1; r <= 3; r++) Put(px + r * dir, ey - 1 - k - r, dark, 0.5f);
                                }
                            }
                        }
                        else if (pick < 0.84f && t.MossAmount > 0.25f)
                        {
                            // Musgo colgante: varios mechones de 2 tonos.
                            int n = 3 + Mathf.FloorToInt(sz * 4f);
                            for (int s = 0; s < n; s++)
                            {
                                int len = 3 + Mathf.FloorToInt(PixelCanvas.Hash(cell, s, 65) * 11f);
                                for (int k = 0; k < len; k++)
                                {
                                    int sway = k > len / 2 && ((cell + s) & 1) == 0 ? 1 : 0;
                                    Put(ex + s - n / 2 + sway, ey - 1 - k, moss[k < 2 ? 2 : k < len - 2 ? 1 : 0], 0.4f);
                                }
                            }
                        }
                    }
                }
            }

            // Telarañas en los rincones techo-pared (esquinas de casilla), en abanico desde la esquina.
            int tx0 = FloorDiv(ox, Tile), tx1 = FloorDiv(ox + ew - 1, Tile), ty0 = FloorDiv(oy, Tile), ty1 = FloorDiv(oy + eh - 1, Tile);
            for (int ty = ty0 + 1; ty < ty1; ty++)
            {
                for (int tx = tx0 + 1; tx < tx1; tx++)
                {
                    int cex = tx * Tile + Tile / 2 - ox, cey = ty * Tile + Tile / 2 - oy;
                    if (Solid(cex, cey) || !Solid(cex, cey + Tile)) continue;
                    for (int side = -1; side <= 1; side += 2)
                    {
                        if (!Solid(cex + side * Tile, cey)) continue;
                        if (PixelCanvas.Hash(tx * 2 + side, ty, 71) > 0.6f) continue;
                        var t = TerrainTheme.For(zoneAtTile(tx));
                        int R = 14 + Mathf.FloorToInt(PixelCanvas.Hash(tx, ty, 72) * 9f);
                        float cx = tx * Tile + (side < 0 ? 0 : Tile) - ox, cy = ty * Tile + Tile - oy; // esquina (px del área extendida)
                        float sx = side < 0 ? 1f : -1f;
                        var thread = PixelCanvas.WithAlpha(PixelCanvas.Lerp(t.Stone[5], new Color32(214, 214, 204, 255), 0.6f), 0.5f);
                        var arc = PixelCanvas.WithAlpha(thread, 0.32f);
                        void Line(float ax, float ay, float bx, float by, Color32 c)
                        {
                            int steps = Mathf.CeilToInt(Mathf.Max(Mathf.Abs(bx - ax), Mathf.Abs(by - ay)));
                            for (int k = 0; k <= steps; k++)
                            {
                                float f = steps == 0 ? 0f : k / (float)steps;
                                Put(Mathf.FloorToInt(ax + (bx - ax) * f), Mathf.FloorToInt(ay + (by - ay) * f), c, 0.5f);
                            }
                        }
                        // Hilos radiales en abanico (de la esquina hacia fuera) y tres vueltas que se combean.
                        float[] angs = { 0f, 0.2f, 0.7f, 1.15f, Mathf.PI * 0.5f };
                        for (int k = 1; k < angs.Length - 1; k++)
                        {
                            angs[k] += (PixelCanvas.Hash(tx * 5 + k, ty, 73) - 0.5f) * 0.25f;
                            float len = R * (0.85f + PixelCanvas.Hash(tx * 5 + k, ty, 74) * 0.25f);
                            Line(cx, cy - 1, cx + sx * Mathf.Cos(angs[k]) * len, cy - 1 - Mathf.Sin(angs[k]) * len, thread);
                        }
                        for (int r = 1; r <= 3; r++)
                        {
                            float rr = R * (0.28f + 0.29f * r);
                            for (int k = 0; k < angs.Length - 1; k++)
                            {
                                float ax = cx + sx * Mathf.Cos(angs[k]) * rr, ay = cy - 1 - Mathf.Sin(angs[k]) * rr;
                                float bx = cx + sx * Mathf.Cos(angs[k + 1]) * rr, by = cy - 1 - Mathf.Sin(angs[k + 1]) * rr;
                                float mx = (ax + bx) * 0.5f, my = (ay + by) * 0.5f;
                                mx += (cx - mx) * 0.12f; my += (cy - 1 - my) * 0.12f; // la seda se combea hacia la esquina
                                Line(ax, ay, mx, my, arc);
                                Line(mx, my, bx, by, arc);
                            }
                        }
                    }
                }
            }
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
