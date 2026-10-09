using System.Collections.Generic;
using UnityEngine;

namespace Abismo.EditorTools
{
    /// <summary>
    /// Paredes de fondo de los interiores (casillas ':' del mapa): arquitectura con columnas, arcos ojivales,
    /// nichos y cornisas. Los arcos abiertos dejan ver los fondos con paralaje y los cerrados llevan vidrieras.
    /// </summary>
    public static partial class TerrainPainter
    {
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

                    // En las Ruinas el muro está derrumbado: por encima de su borde roto se ven los fondos.
                    int ruinTop = zone == Zone.Ruins ? RuinTop(wx) : int.MaxValue;
                    if (localY >= ruinTop) continue;

                    Color32 c = Architecture(theme, zone, wx, wy, localY, spanH, out float hgt);
                    if (c.a == 0) continue; // arco abierto: se ve el fondo
                    // Oscuro y algo más apagado arriba: la luz viene del suelo (velas).
                    float k = 0.8f - 0.26f * Mathf.Clamp01(localY / (float)Mathf.Max(1, spanH));
                    c = PixelCanvas.Lerp(theme.Deep, c, k);
                    if (localY >= ruinTop - 2) c = PixelCanvas.Lerp(c, theme.Stone[4], 0.55f); // canto roto que coge la luz
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

        /// <summary>
        /// Arcos abiertos, por los que se ven los fondos (la nave con el coloso, la bruma de R'lyeh): todos en las
        /// Ruinas y uno de cada dos en el Santuario. Los cerrados del Santuario llevan vidriera.
        /// </summary>
        public static bool OpenArch(Zone zone, int bay) => zone == Zone.Ruins || (zone == Zone.Sanctuary && (bay & 1) == 1);

        /// <summary>Altura (px sobre el suelo) del borde roto del muro de las Ruinas, escalonada por hiladas de sillares.</summary>
        static int RuinTop(int wx)
        {
            int col = FloorDiv(wx, 22);
            float n = PixelCanvas.ValueNoise(col * 0.35f, 3.1f, 0, 71);
            float jag = PixelCanvas.Hash(col, 5, 72);
            int top = 118 + Mathf.RoundToInt(n * 120f + jag * 30f);
            return top / 14 * 14 + 2;
        }

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
                    if (OpenArch(zone, bay)) return PixelCanvas.Clear;
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
    }
}
