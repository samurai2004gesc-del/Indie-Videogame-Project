using System.Collections.Generic;
using UnityEngine;

namespace Abismo.EditorTools
{
    /// <summary>
    /// Paredes de fondo de los interiores (casillas ':' del mapa): arquitectura gótica con pilares, arcos ojivales,
    /// impostas, zócalo y sillería con bisel, manchas de humedad que chorrean y grietas. Los arcos abiertos dejan ver
    /// los fondos con paralaje y los cerrados llevan vidrieras.
    /// Todo se calcula píxel a píxel a partir de las coordenadas de mundo (determinista y sin costuras entre trozos).
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

                    var p = Muro(zone, theme, wx, localY, spanH, ruinTop);
                    if (p.M == MAbierto) continue; // arco abierto: se ve el fondo
                    Color32 c = Colorear(PaletaDe(zone, theme), p);
                    // Oscuro y algo más apagado arriba: la luz viene del suelo (velas).
                    float k = 0.82f - 0.26f * Mathf.Clamp01(localY / (float)Mathf.Max(1, spanH));
                    c = PixelCanvas.Lerp(theme.Deep, c, k);
                    color.Pixels[y * w + x] = c;
                    height[y * w + x] = p.A;
                }
            }
            var normal = NormalsFromHeight(color, height, w, h, 1.8f);
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

        // ------------------------------------------------------------------
        // Píxel de muro: tono en la rampa + material + altura (para el normal map)
        // ------------------------------------------------------------------

        const byte MPiedra = 0, MMusgo = 1, MAcento = 2, MHueco = 3, MTierra = 4, MMadera = 5, MCoral = 6, MRaiz = 7, MAbierto = 255;

        /// <summary>Un píxel de la pared antes de colorearlo: así el desgaste puede oscurecer o teñir sin salirse de la paleta.</summary>
        struct PxMuro
        {
            public int T;     // tono 0..7 (rampa de piedra extendida) o índice en la rampa de su material
            public byte M;    // material
            public byte H;    // humedad: 0 seca, 1 húmeda, 2 empapada (tinte frío)
            public float A;   // altura 0..1 para el normal map
            public PxMuro(int t, float a, byte m = MPiedra) { T = t; A = a; M = m; H = 0; }
        }

        /// <summary>Colores de la pared de fondo de una zona (derivados de su TerrainTheme).</summary>
        sealed class PaletaMuro
        {
            public Color32[] Piedra;   // 8 tonos: 0 grieta casi negra … 7 brillo
            public Color32[] Musgo, Tierra, Madera, Coral, Raiz;
            public Color32 Acento, Hueco, Humedad;
        }

        static readonly Dictionary<Zone, PaletaMuro> paletasMuro = new Dictionary<Zone, PaletaMuro>();

        static PaletaMuro PaletaDe(Zone zone, TerrainTheme t)
        {
            if (paletasMuro.TryGetValue(zone, out var p)) return p;
            p = new PaletaMuro();
            var st = t.Stone;
            int n = st.Length;
            Color32 Muestra(float f)
            {
                float x = Mathf.Clamp01(f) * (n - 1);
                int i = Mathf.Clamp(Mathf.FloorToInt(x), 0, n - 1);
                return PixelCanvas.Lerp(st[i], st[Mathf.Min(i + 1, n - 1)], x - i);
            }
            p.Piedra = new Color32[8];
            p.Piedra[0] = PixelCanvas.Lerp(t.Deep, st[0], 0.5f);
            for (int i = 1; i <= 6; i++) p.Piedra[i] = Muestra((i - 1) / 5f);
            p.Piedra[7] = PixelCanvas.Lerp(st[n - 1], PixelCanvas.Hex("f0e0bc"), 0.32f);
            p.Musgo = t.Moss != null && t.Moss.Length > 0 ? t.Moss : Ramp.Make("3a6a50", 4, 0.1f);
            p.Tierra = t.Earth != null && t.Earth.Length > 0 ? t.Earth : Ramp.Make("2e2620", 4, 0.08f, 0.5f, 1.5f);
            p.Acento = t.Accent;
            p.Hueco = t.Deep;
            p.Humedad = PixelCanvas.Lerp(t.Deep, p.Musgo[0], 0.45f);
            p.Madera = Ramp.Make("5a4230", 6, 0.1f, 0.3f, 1.55f);
            p.Coral = Ramp.Make(t.Accent, 5, 0.1f, 0.35f, 1.25f);
            p.Raiz = Ramp.Make("3c3226", 4, 0.08f, 0.45f, 1.5f);
            paletasMuro[zone] = p;
            return p;
        }

        static Color32 Colorear(PaletaMuro pal, PxMuro p)
        {
            Color32 c;
            switch (p.M)
            {
                case MMusgo: c = pal.Musgo[Mathf.Clamp(p.T, 0, pal.Musgo.Length - 1)]; break;
                case MAcento: c = PixelCanvas.Lerp(pal.Piedra[Mathf.Clamp(p.T, 0, 7)], pal.Acento, 0.55f); break;
                case MHueco: c = PixelCanvas.Lerp(pal.Hueco, pal.Piedra[1], Mathf.Clamp01(p.T * 0.22f)); break;
                case MTierra: c = pal.Tierra[Mathf.Clamp(p.T, 0, pal.Tierra.Length - 1)]; break;
                case MMadera: c = pal.Madera[Mathf.Clamp(p.T, 0, pal.Madera.Length - 1)]; break;
                case MCoral: c = pal.Coral[Mathf.Clamp(p.T, 0, pal.Coral.Length - 1)]; break;
                case MRaiz: c = pal.Raiz[Mathf.Clamp(p.T, 0, pal.Raiz.Length - 1)]; break;
                default: c = pal.Piedra[Mathf.Clamp(p.T, 0, 7)]; break;
            }
            if (p.H > 0) c = PixelCanvas.Lerp(c, pal.Humedad, p.H == 1 ? 0.2f : 0.38f);
            return c;
        }

        // ------------------------------------------------------------------
        // Composición del muro
        // ------------------------------------------------------------------

        /// <summary>Datos del píxel que comparten todas las piezas de arquitectura.</summary>
        struct GeoMuro
        {
            public Zone Z;
            public bool Ruinas;
            public int Wx, Ly, Span, BayI, Bx, Cu, Half, Zh, S;
            public float ATop;
            public int Zona; // respecto al arco: 0 fuera, 1 moldura interior, 2 hueco
            public int RTop; // borde roto de las Ruinas (px sobre el suelo) o int.MaxValue
        }

        static PxMuro Muro(Zone zone, TerrainTheme t, int wx, int ly, int spanH, int ruinTop)
        {
            var g = new GeoMuro { Z = zone, Ruinas = zone == Zone.Ruins, Wx = wx, Ly = ly, Span = spanH, RTop = ruinTop };
            g.BayI = FloorDiv(wx, Bay);
            g.Bx = wx - g.BayI * Bay;
            g.Cu = g.Bx < Bay / 2 ? g.Bx : g.Bx - Bay;
            g.Half = (g.Ruinas ? 52 : 44) / 2;
            g.Zh = g.Ruinas ? 26 : 20;
            g.ATop = ArchTop(spanH);
            g.S = Mathf.RoundToInt(g.ATop - ArchHalfW * 0.9f); // arranque del arco: ahí corre la imposta
            g.Zona = g.ATop > 40f ? ZonaArco(Mathf.Abs(g.Bx - Bay / 2), ly, g.ATop) : 0;

            PxMuro p;
            if (!Columna(ref g, t, out p) && !Arco(ref g, t, out p) && !Ornamentos(ref g, t, out p) && !Molduras(ref g, t, out p))
            {
                p = Pared(ref g, t);
                if (SombraArco(ref g)) { p.T--; p.A -= 0.1f; }
                // Oclusión junto al pilar (más ancha a su derecha: la luz viene de la izquierda).
                if (Mathf.Abs(g.Cu + 0.5f) < g.Half + (g.Cu > 0 ? 3.5f : 1.5f)) { p.T--; p.A -= 0.1f; }
            }
            if (p.M == MAbierto) return p;
            Desgaste(ref p, ref g, t, ruinTop);
            return p;
        }

        /// <summary>
        /// Dónde cae un píxel respecto al arco ojival del tramo (misma geometría de siempre: jambas rectas hasta el
        /// arranque, dos arcos de radio 1,6·semiancho y cierre en ArchTop). 0 fuera, 1 moldura de 5 px por dentro del
        /// borde, 2 hueco (transparente si el arco es abierto).
        /// </summary>
        static int ZonaArco(float dx, int localY, float archTop)
        {
            float archHalfW = ArchHalfW;
            if (!(dx < archHalfW + 6f && localY > 10)) return 0;
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
            if (!insideArch) return 0;
            bool rim;
            float rimDx = dx + 5f;
            if (localY < springY) rim = rimDx >= archHalfW;
            else
            {
                float r = archHalfW * 1.6f, ccx = archHalfW - r;
                float ddx = rimDx - ccx, ddy = localY + 5f - springY;
                rim = !(ddx * ddx + ddy * ddy < r * r && localY + 5f < archTop);
            }
            return rim ? 1 : 2;
        }

        // ------------------------------------------------------------------
        // Columnas
        // ------------------------------------------------------------------

        /// <summary>Altura (px sobre el suelo) de la cornisa que corre por encima de los arcos, o -1 si no cabe.</summary>
        static int Cornisa(ref GeoMuro g)
        {
            if (g.ATop <= 40f) return -1;
            int aw = g.Ruinas ? 14 : 12;
            float spring = g.ATop - ArchHalfW * 0.9f;
            int corn = Mathf.CeilToInt(spring + Mathf.Sqrt((ArcoR + aw) * (ArcoR + aw) - ArcoCx * ArcoCx)) + 6;
            return g.Span > corn + 20 ? corn : -1;
        }

        /// <summary>Tono e iluminación de un fuste cilíndrico: p = 0 borde izquierdo … 1 borde derecho.</summary>
        static void Cilindro(float p, out int t, out float a)
        {
            float c = 2f * p - 1f;
            a = 0.35f + 0.65f * Mathf.Sqrt(Mathf.Clamp01(1f - c * c));
            t = p < 0.12f ? 3 : p < 0.32f ? 5 : p < 0.46f ? 4 : p < 0.68f ? 3 : p < 0.86f ? 2 : 1;
        }

        /// <summary>
        /// Pilar: en el Santuario (y Costa/Arrecife) un pilar gótico de haz de columnillas (fuste central, dos
        /// columnillas y caras planas separadas por escocias), basa con plinto, toros y escocia, anillo a la altura de la
        /// imposta y capitel de hojas que son tentáculos que se enroscan. En las Ruinas, una columna ciclópea estriada de
        /// tambores desalineados, rota por arriba.
        /// </summary>
        static bool Columna(ref GeoMuro g, TerrainTheme t, out PxMuro p)
        {
            p = default;
            if (Mathf.Abs(g.Cu) > g.Half + 6) return false;
            return g.Ruinas ? ColumnaCiclopea(ref g, out p) : PilarGotico(ref g, out p);
        }

        static bool PilarGotico(ref GeoMuro g, out PxMuro p)
        {
            p = default;
            int ly = g.Ly, h = g.Half;
            float uf = g.Cu + 0.5f, au = Mathf.Abs(uf);
            int corn = Cornisa(ref g);
            int capTop = corn > 0 ? corn : g.Span;
            int c0 = capTop - 17;
            int s = g.S;

            // Por encima de la cornisa: pilastra más estrecha con su columnilla.
            if (corn > 0 && ly >= corn + 6)
            {
                if (au >= 16f) return false;
                int ti; float a;
                if (au < 6f) Cilindro((uf + 6f) / 12f, out ti, out a);
                else if (au < 7f) { ti = 1; a = 0.25f; }
                else { ti = uf < -14.5f ? 5 : uf > 14.5f ? 1 : uf > 0 ? 2 : 3; a = 0.5f; }
                p = new PxMuro(ti, a);
                return true;
            }
            if (corn > 0 && ly >= corn) return false; // la cornisa pasa por delante

            // Basa.
            if (ly < 18)
            {
                float w = ly < 8 ? h + 4 : ly < 12 ? h + 3 : ly < 14 ? h + 1 : ly < 17 ? h + 2 : h + 1;
                if (au >= w) return false;
                int ti;
                if (ly < 8)
                {
                    ti = ly == 7 ? 5 : ly == 6 ? 4 : ly == 0 ? 2 : 3;
                    if (uf < -w + 1.5f && ly < 7) ti++;
                    else if (uf > w - 1.5f) ti--;
                    if (ly > 1 && ly < 6 && Mathf.Abs(au - (w - 3f)) < 0.5f) ti--; // chaflán del plinto
                }
                else
                {
                    int[] perfil = { 2, 4, 5, 3, 1, 2, 3, 5, 4, 2 };
                    ti = perfil[ly - 8];
                    if (uf > 9f) ti--;
                    else if (uf < -14f && ti >= 4) ti++;
                }
                float a = ly < 8 ? 0.7f : (ly == 10 || ly == 15) ? 0.95f : (ly == 12 || ly == 13) ? 0.45f : 0.8f;
                p = new PxMuro(Mathf.Clamp(ti, 1, 6), a);
                return true;
            }

            // Capitel: collarino, campana con hojas-tentáculo y ábaco.
            if (ly >= c0 && ly < capTop)
            {
                int yy = ly - c0;
                if (yy < 2)
                {
                    if (au >= h + 1) return false;
                    p = new PxMuro(yy == 0 ? 2 : 4, 0.75f);
                    if (uf > 10f) p.T--;
                    return true;
                }
                if (yy >= 13)
                {
                    if (au >= h + 5) return false;
                    int[] ab = { 2, 3, 5, 6 };
                    int ti = ab[yy - 13];
                    if (uf > h + 3.5f && yy < 16) ti--;
                    p = new PxMuro(ti, 0.85f);
                    return true;
                }
                float q = (yy - 2) / 10f;
                float wb = h + 1 + 4f * q * q;
                if (au >= wb) return false;
                p = CampanaTentaculos(uf, yy - 2);
                return true;
            }

            if (au >= h) return false;
            // Anillo a la altura de la imposta.
            if (ly >= s - 1 && ly <= s + 2)
            {
                int[] perfil = { 2, 4, 5, 3 };
                int ti = perfil[ly - (s - 1)];
                if (uf > 10f) ti--;
                p = new PxMuro(ti, 0.85f);
                return true;
            }
            // Fuste: haz de columnillas.
            {
                int ti; float a;
                if (au < 7f)
                {
                    float pc = (uf + 7f) / 14f;
                    Cilindro(pc, out ti, out a);
                    if (pc > 0.2f && pc < 0.28f) ti = 6; // brillo
                }
                else if (au < 8.5f) { ti = 1; a = 0.25f; }
                else if (au < 15.5f) { Cilindro(uf < 0 ? (uf + 15.5f) / 7f : (uf - 8.5f) / 7f, out ti, out a); a *= 0.85f; if (uf > 0) ti = Mathf.Max(1, ti - 1); }
                else if (au < 16.5f) { ti = 1; a = 0.25f; }
                else { ti = uf < -h + 1.5f ? 5 : uf > h - 1.5f ? 1 : uf > 0 ? 2 : 3; a = 0.5f; }
                if (ly == s - 2) ti--; // sombra del anillo
                // Juntas de los tambores, espaciadas.
                int dj = ((ly - 18) % 46 + 46) % 46;
                if (dj == 0 && ti > 1) ti--;
                else if (dj == 1 && ti < 6 && au < 16.5f) ti++;
                p = new PxMuro(ti, a);
                return true;
            }
        }

        /// <summary>Campana del capitel gótico (yy = 0..10 de abajo arriba): hojas que son tentáculos y se enroscan hacia fuera.</summary>
        static PxMuro CampanaTentaculos(float uf, int yy)
        {
            var fondo = new PxMuro(uf > 6f ? 1 : 2, 0.4f);
            for (int i = -2; i <= 2; i++)
            {
                float b = i * 8f;
                float d = i < 0 ? -1f : i > 0 ? 1f : 0f;
                float yq = yy / 10f;
                if (i == 0)
                {
                    // Tallo central rematado por un ojo.
                    if (yy >= 6)
                    {
                        float ex = uf, ey = yy - 8f;
                        if (Mathf.Abs(ex) < 3f && Mathf.Abs(ey) < 1.5f - Mathf.Abs(ex) * 0.25f)
                            return Mathf.Abs(ex) < 0.8f ? new PxMuro(0, 0.5f) : new PxMuro(Mathf.Abs(ex) < 1.8f ? 4 : 3, 0.7f, MAcento);
                        if (Mathf.Abs(ex) < 3.5f && Mathf.Abs(ey) < 2.5f) return new PxMuro(ey > 0f ? 5 : 2, 0.75f); // párpado
                    }
                    else if (Mathf.Abs(uf) < 1.5f) return new PxMuro(uf < 0f ? 5 : 3, 0.7f);
                    continue;
                }
                float xc = b + d * 4f * yq * yq;
                float hw = 1.8f - yy * 0.08f;
                float off = uf - xc;
                if (yy < 8 && Mathf.Abs(off) < hw)
                {
                    int ti = off < -hw + 1f ? 5 : off > hw - 1f ? 2 : 4;
                    if (d * off > hw - 1f && (yy & 1) == 0) ti = 5; // ventosas
                    return new PxMuro(ti, 0.75f);
                }
                // Voluta: el tentáculo se enrosca en la punta.
                float cx = b + d * 5.5f, cyy = 8.2f;
                float rx = uf - cx, ry = yy - cyy;
                float rr = Mathf.Sqrt(rx * rx + ry * ry);
                if (rr < 2.4f)
                {
                    if (rr < 0.9f) return new PxMuro(0, 0.4f);
                    float l = (rx * LuzX + ry * LuzY) / Mathf.Max(0.5f, rr);
                    return new PxMuro(l > 0.3f ? 5 : l < -0.3f ? 2 : 4, 0.8f);
                }
            }
            return fondo;
        }

        static bool ColumnaCiclopea(ref GeoMuro g, out PxMuro p)
        {
            p = default;
            int ly = g.Ly, h = g.Half, zh = g.Zh;
            float uf = g.Cu + 0.5f, au = Mathf.Abs(uf);
            // Plinto y toro.
            if (ly < zh)
            {
                float w = h + 4;
                if (au >= w) return false;
                int ti = ly == zh - 1 ? 5 : ly == zh - 2 ? 4 : ly == 0 ? 1 : 3;
                if (uf < -w + 1.5f && ly < zh - 1) ti++;
                else if (uf > w - 1.5f) ti--;
                float n = PixelCanvas.ValueNoise(g.Wx * 0.35f, ly * 0.4f, 0, 231);
                if (n > 0.72f) ti--;
                if (ly == 9 && PixelCanvas.Hash(g.BayI, 1, 232) > 0.4f) ti = 1; // grieta horizontal en el plinto
                p = new PxMuro(Mathf.Clamp(ti, 1, 6), 0.7f);
                return true;
            }
            if (ly < zh + 5)
            {
                float w = h + 2;
                if (au >= w) return false;
                int[] perfil = { 2, 4, 5, 3, 2 };
                int ti = perfil[ly - zh];
                if (uf > 12f) ti--;
                if (PixelCanvas.ValueNoise(g.Wx / 3f, 4.5f, 0, 233) > 0.7f) ti--; // toro desportillado
                p = new PxMuro(Mathf.Clamp(ti, 1, 6), 0.85f);
                return true;
            }
            // Tambores: altura variable y cada uno algo desplazado (los terremotos de R'lyeh).
            int y = ly - (zh + 5);
            int drum = 0, start = 0;
            while (true)
            {
                int dh = 26 + Mathf.FloorToInt(PixelCanvas.Hash(g.BayI, drum, 234) * 14f);
                if (y < start + dh) break;
                start += dh;
                drum++;
                if (drum > 40) break;
            }
            int shift = Mathf.FloorToInt(PixelCanvas.Hash(g.BayI, drum, 235) * 3f) - 1;
            float u = uf - shift;
            float aus = Mathf.Abs(u);
            if (aus >= h) return false;
            int dy = y - start;
            float pc = (u + h) / (2f * h);
            Cilindro(pc, out int t0, out float a0);
            // Estrías: 8 canales cóncavos (el lado izquierdo del canal en sombra, el derecho con luz).
            float fl = (u + h) / 6.5f;
            float f = fl - Mathf.Floor(fl);
            int tf = f < 0.16f ? t0 + 1 : f < 0.5f ? t0 - 1 : t0;
            float a = a0 - (f >= 0.16f ? 0.12f * Mathf.Sin((f - 0.16f) / 0.84f * Mathf.PI) : 0f);
            if (dy == 0) { tf = 1; a = 0.3f; }                    // junta entre tambores
            else if (dy == 1) tf++;
            float n2 = PixelCanvas.ValueNoise(g.Wx * 0.3f, ly * 0.3f, 0, 236);
            if (n2 > 0.76f) tf--;                                  // piedra comida
            // Glifos tenues en algún tambor.
            if (PixelCanvas.Hash(g.BayI, drum, 237) > 0.75f && dy > 8 && dy < 16 && f > 0.25f && f < 0.75f)
            {
                int gl = Mathf.FloorToInt(fl);
                if (Glifo(Mathf.FloorToInt((f - 0.25f) * 8f), dy - 9, gl + drum * 3)) return Ok(out p, new PxMuro(3, 0.5f, MAcento));
            }
            p = new PxMuro(Mathf.Clamp(tf, 1, 6), a);
            return true;
        }

        static bool Ok(out PxMuro p, PxMuro v) { p = v; return true; }

        /// <summary>Glifo de 4×6 de la escritura de los Primigenios (x 0..3, y 0..5).</summary>
        static bool Glifo(int x, int y, int seed)
        {
            if (x < 0 || x > 3 || y < 0 || y > 5) return false;
            int k = ((seed % 6) + 6) % 6;
            switch (k)
            {
                case 0: return x == 1 || (y == 5 && x < 3) || (y == 2 && x == 2);
                case 1: return (x == 0 && y > 1) || (y == 1 && x > 0) || (x == 3 && y < 2) || (y == 4 && x == 2);
                case 2: return (y == 0 && x < 3) || (x == 2 && y > 0) || (y == 3 && x < 2) || (x == 0 && y == 4);
                case 3: return (x + y == 5 && x < 4) || (y == 5 && x > 1) || (x == 0 && y < 2);
                case 4: return (y == 5) || (x == 1 && y > 1 && y < 5) || (x == 3 && y == 3);
                default: return (x == 0 || x == 3) && y > 0 || (y == 2 && x > 0 && x < 3);
            }
        }

        // ------------------------------------------------------------------
        // Arco
        // ------------------------------------------------------------------

        const float ArcoR = ArchHalfW * 1.6f;     // radio de cada uno de los dos arcos de la ojiva
        const float ArcoCx = ArchHalfW - ArcoR;   // centro del arco (al otro lado del eje)
        static readonly float ArcoRise = Mathf.Sqrt(ArcoR * ArcoR - ArcoCx * ArcoCx);       // del arranque a la punta de la ojiva
        static readonly float ArcoAngPunta = Mathf.Atan2(ArcoRise, -ArcoCx);

        /// <summary>Luz de la escena para el sombreado "pintado" (arriba a la izquierda).</summary>
        const float LuzX = -0.6f, LuzY = 0.8f;

        /// <summary>Tono de un bocel (moldura redonda): q = -1 lado interior … 1 lado exterior; ndl = cuánto mira a la luz su exterior.</summary>
        static int Bocel(float q, float ndl) => 3 + Mathf.RoundToInt(q * ndl * 2f) + (Mathf.Abs(q) < 0.5f ? 1 : 0);

        /// <summary>
        /// Arco ojival: el hueco y su moldura interior conservan la geometría de siempre (ZonaArco). Alrededor, las
        /// arquivoltas siguen los dos arcos hasta su punta: dovelas con juntas radiales, boceles, guardapolvo con su
        /// sombra y una clave tallada; entre el dintel (el cierre plano del hueco) y la punta queda un tímpano con relieve.
        /// En las jambas, las arquivoltas bajan como columnillas con su capitel bajo la imposta.
        /// </summary>
        static bool Arco(ref GeoMuro g, TerrainTheme t, out PxMuro p)
        {
            p = default;
            if (g.ATop <= 40f || g.Ly <= 10) return false;
            int dx = Mathf.Abs(g.Bx - Bay / 2);
            int side = g.Bx >= Bay / 2 ? 1 : -1;
            int aw = g.Ruinas ? 14 : 12;
            if (dx > ArchHalfW + aw + 2) return false;
            int ly = g.Ly, s = g.S;
            float spring = g.ATop - ArchHalfW * 0.9f;
            float ddx = dx - ArcoCx, ddy = ly - spring;
            float dist = Mathf.Sqrt(ddx * ddx + ddy * ddy);
            bool enArco = ly >= spring;
            // Normal exterior de la moldura (para el sombreado) y cuánto mira a la luz.
            float nx = enArco ? side * ddx / Mathf.Max(1f, dist) : side, ny = enArco ? ddy / Mathf.Max(1f, dist) : 0f;
            float ndl = nx * LuzX + ny * LuzY;

            if (g.Zona == 2)
            {
                if (OpenArch(g.Z, g.BayI)) { p = new PxMuro(0, 0f, MAbierto); return true; }
                p = Nicho(ref g, dx);
                return true;
            }
            if (g.Zona == 1)
            {
                // Moldura interior: bocel en las jambas y el arco; en el cierre plano es el canto del dintel.
                float dIn = !enArco ? ArchHalfW - dx : Mathf.Min(ArcoR - dist, g.ATop - ly);
                bool dintel = enArco && g.ATop - ly < ArcoR - dist;
                // Ruinas: si el muro se ha derrumbado por encima, el dintel se ha caído con él.
                if (dintel && g.Ruinas && g.RTop < g.ATop + 10f) { p = new PxMuro(0, 0f, MAbierto); return true; }
                if (dintel) { nx = 0f; ny = -1f; ndl = ny * LuzY; }
                float q = (3f - dIn) / 2.5f;
                int ti = dIn > 4.2f ? 1 : Bocel(q, dintel ? 0.6f : ndl);
                if (dintel && dIn > 4.2f) ti = 2;
                p = new PxMuro(Mathf.Clamp(ti, 1, 6), 0.55f + 0.35f * (1f - q * q));
                if (!enArco && ly >= s - 6 && ly <= s + 2) p = CapitelJamba(ref g, dx, ly - s);
                else if (!enArco && ly < g.Zh + 7) p = BasaJamba(ly - g.Zh, dIn, side);
                return true;
            }

            // Fuera del hueco: tímpano, arquivoltas o nada.
            float dOut = enArco ? dist - ArcoR : dx - ArchHalfW;
            float apexIn = spring + ArcoRise;
            float apexOut = spring + Mathf.Sqrt((ArcoR + aw) * (ArcoR + aw) - ArcoCx * ArcoCx);
            // Clave: trapecio en la punta que atraviesa las arquivoltas.
            if (ly >= apexIn - 4f && ly < apexOut + 3f)
            {
                float kw = 5f + (ly - (apexIn - 4f)) * 0.15f + (g.Ruinas ? 1.5f : 0f);
                if (dx <= kw) { p = Clave(ref g, dx, side, kw, ly - (apexIn - 4f), apexOut + 3f - (apexIn - 4f)); return true; }
            }
            if (enArco && ly >= g.ATop && dOut < 0f) { p = Timpano(ref g, dx, side, -dOut, ly - g.ATop, t); return true; }
            if (dOut < 0f) dOut = 0f;
            if (dOut >= aw + 1.5f) return false;
            if (dOut >= aw) return false; // la sombra del guardapolvo se aplica sobre el muro (Desgaste)

            // Imposta y capitel de las jambas cruzan las arquivoltas a la altura del arranque.
            if (!enArco && ly >= s - 6 && ly <= s + 2) { p = CapitelJamba(ref g, dx, ly - s); return true; }
            if (enArco && ly <= s + 2) { p = Imposta(ly - s, g.Ruinas, g.Wx, s); return true; }
            if (!enArco && ly < g.Zh + 7) { p = BasaJamba(ly - g.Zh, -dOut, side); return true; }

            int vw0 = 1, vw1 = g.Ruinas ? 11 : 7; // banda de dovelas
            if (dOut < vw0 || (dOut >= vw1 && dOut < vw1 + 1)) { p = new PxMuro(1, 0.3f); return true; } // escocias
            if (dOut < vw1)
            {
                if (enArco) p = Dovela(ref g, dist, ddx, ddy, dOut - vw0, vw1 - vw0, side, ndl);
                else p = SillarJamba(ref g, dOut - vw0, vw1 - vw0, ndl);
                return true;
            }
            // Bocel exterior y guardapolvo.
            float q2 = (dOut - (vw1 + 1)) / (aw - vw1 - 1) * 2f - 1f;
            if (!g.Ruinas && dOut >= aw - 2f)
            {
                int ti = dOut >= aw - 1f ? (ndl > 0.2f ? 5 : 3) : 2;
                p = new PxMuro(ti, dOut >= aw - 1f ? 0.85f : 0.6f);
                return true;
            }
            p = new PxMuro(Mathf.Clamp(Bocel(q2, ndl), 1, 6), 0.55f + 0.3f * (1f - q2 * q2));
            return true;
        }

        /// <summary>Sombra que arroja el guardapolvo del arco sobre el muro (1-2 px por fuera, hacia abajo-derecha).</summary>
        static bool SombraArco(ref GeoMuro g)
        {
            if (g.ATop <= 40f || g.Ly <= g.Zh) return false;
            int dx = Mathf.Abs(g.Bx - Bay / 2);
            int aw = g.Ruinas ? 14 : 12;
            float spring = g.ATop - ArchHalfW * 0.9f;
            float dOut;
            if (g.Ly < spring) dOut = dx - ArchHalfW;
            else
            {
                float ddx = dx - ArcoCx, ddy = g.Ly - spring;
                dOut = Mathf.Sqrt(ddx * ddx + ddy * ddy) - ArcoR;
            }
            float w = g.Bx >= Bay / 2 ? 2f : 1f; // el lado derecho, más sombra
            return dOut >= aw && dOut < aw + w;
        }

        /// <summary>Dovela: cuña de piedra con juntas radiales (hacia el centro de su arco), bisel y algún desperfecto.</summary>
        static PxMuro Dovela(ref GeoMuro g, float dist, float ddx, float ddy, float across, float bandW, int side, float ndl)
        {
            float ang = Mathf.Atan2(ddy, ddx);
            float vw = g.Ruinas ? 13f : 11f;
            float along = (ArcoAngPunta - ang) * (ArcoR + 4f) - (g.Ruinas ? 6.5f : 5f);
            int k = Mathf.FloorToInt(along / vw);
            float fr = along - k * vw;
            float hv = PixelCanvas.Hash(k * 2 + (side > 0 ? 1 : 0), g.BayI, 211);
            if (fr < 1f) return new PxMuro(1, 0.15f);
            // Dovela suelta (Ruinas): junta abierta y la piedra descolgada.
            if (g.Ruinas && hv > 0.82f && fr < 2.2f) return new PxMuro(0, 0.05f, MHueco);
            int ti = 3 + (hv > 0.78f ? 1 : 0) - (hv < 0.2f ? 1 : 0);
            float a = 0.7f;
            if (fr < 2f) { ti++; a = 0.62f; }
            else if (fr >= vw - 1f) { ti--; a = 0.6f; }
            if (across < 1f) { ti--; a = 0.6f; }
            else if (across >= bandW - 1f && ndl > 0.25f) ti++;
            float grain = PixelCanvas.ValueNoise(g.Wx * 0.4f, g.Ly * 0.4f, 0, 212);
            if (grain > 0.78f) ti--;
            // Esquina desportillada en alguna dovela.
            if (hv > 0.5f && hv < 0.62f && fr > vw - 3f && across > bandW - 3f) return new PxMuro(1, 0.3f);
            return new PxMuro(ti, a);
        }

        /// <summary>Sillares de las jambas (la banda de dovelas, ya en vertical), en hiladas como el muro.</summary>
        static PxMuro SillarJamba(ref GeoMuro g, float across, float bandW, float ndl)
        {
            int ch = g.Ruinas ? 28 : 12; // hiladas de las jambas (ciclópeas en las Ruinas)
            int ry = ((g.Ly - g.Zh) % ch + ch) % ch;
            if (ry == 0) return new PxMuro(g.Ruinas ? 1 : 0, 0.15f);
            int row = FloorDiv(g.Ly - g.Zh, ch);
            float hv = PixelCanvas.Hash(row, g.BayI * 2 + (g.Bx > Bay / 2 ? 1 : 0), 213);
            int ti = 3 + (hv > 0.8f ? 1 : 0) - (hv < 0.2f ? 1 : 0);
            if (ry == ch - 1) ti++;
            else if (ry == 1) ti--;
            if (g.Ruinas && PixelCanvas.ValueNoise(g.Wx * 0.4f, g.Ly * 0.3f, 0, 215) > 0.72f) ti--;
            if (across < 1f) ti += ndl > 0f ? 1 : -1;
            else if (across >= bandW - 1f) ti += ndl > 0f ? -1 : 1;
            return new PxMuro(ti, 0.68f);
        }

        /// <summary>Capitel de las jambas: collarino, hojas que se curvan como tentáculos y la imposta como ábaco.</summary>
        static PxMuro CapitelJamba(ref GeoMuro g, int dx, int r)
        {
            if (r >= -1) return Imposta(r, g.Ruinas, g.Wx, g.S);
            if (r == -6) return new PxMuro(2, 0.6f);                 // collarino
            int u = dx % 4;
            int rr = r + 5;                                          // 0..3 de abajo arriba
            if (g.Ruinas)
            {
                // Capitel cúbico, gastado.
                int ti = rr == 3 ? 4 : 3;
                if (PixelCanvas.Hash(dx >> 1, g.BayI, 214) > 0.7f) ti--;
                return new PxMuro(ti, 0.7f);
            }
            bool hoja = u == 1 || u == 2 || (rr == 3 && u == 3);
            if (!hoja) return new PxMuro(rr == 3 ? 2 : 1, 0.45f);
            int tt = rr == 3 ? (u == 3 ? 5 : 4) : (u == 1 ? 4 : 3);
            if (rr == 2 && u == 2) tt = 2; // envés de la voluta
            return new PxMuro(tt, 0.75f);
        }

        /// <summary>Basa de las columnillas de las jambas, sobre el zócalo.</summary>
        static PxMuro BasaJamba(int r, float d, int side)
        {
            int[] perfil = { 2, 4, 5, 3, 1, 3, 4 };
            int ti = perfil[Mathf.Clamp(r, 0, 6)];
            if (side > 0 && r < 4) ti--;
            return new PxMuro(Mathf.Clamp(ti, 1, 6), 0.6f + (r == 2 ? 0.25f : 0f));
        }

        /// <summary>Imposta: canto superior muy iluminado, cara y panza en sombra (r = 0 en el arranque).</summary>
        static PxMuro Imposta(int r, bool ruinas, int wx, int s)
        {
            int[] perfil = { 1, 3, 4, 6 };
            float[] alt = { 0.55f, 0.75f, 0.85f, 0.9f };
            int i = Mathf.Clamp(r + 1, 0, 3);
            var p = new PxMuro(perfil[i], alt[i]);
            if (ruinas && PixelCanvas.Hash(FloorDiv(wx, 5), s, 141) > 0.82f) { p.T -= 1; p.A -= 0.2f; } // mordida
            return p;
        }

        // Relieves de la clave (de arriba abajo). Dígitos = tono del relieve; '.' = fondo rehundido; 'a' = acento.
        static readonly string[] ClaveSigno = {
            "...5...",
            "..454..",
            "5545443",
            ".44a43.",
            "..443..",
            ".43.32.",
            "43...32",
        };
        static readonly string[] ClaveOjo = {
            "..23332..",
            ".3455543.",
            "345a0a543",
            ".3444432.",
            "..22222..",
        };
        static readonly string[] ClaveConcha = {
            "..343..",
            ".45454.",
            "4545454",
            "4545454",
            ".45454.",
            "..343..",
            "...2...",
        };
        static readonly string[] ClavePez = {
            ".34443.",
            "3455543",
            "4a545a4",
            "3455543",
            ".42024.",
            "..343..",
            "...3...",
        };

        /// <summary>Clave: el sillar de la punta, más ancho arriba, con su relieve según la zona.</summary>
        static PxMuro Clave(ref GeoMuro g, int dx, int side, float kw, float v, float kh)
        {
            if (dx > kw - 1f) return new PxMuro(1, 0.4f);                       // junta
            if (v < 1f) return new PxMuro(2, 0.6f);                              // canto inferior en sombra
            int ti = 4;
            if (v >= kh - 2f) ti = 5;
            else if (dx > kw - 2f) ti = side < 0 ? 5 : 2;
            string[] sello = g.Z == Zone.Ruins ? ClaveOjo : g.Z == Zone.Reef ? ClaveConcha : g.Z == Zone.Coast ? ClavePez
                           : (g.BayI & 1) == 0 ? ClaveSigno : ClavePez;
            int sw = sello[0].Length, sh = sello.Length;
            int sx = side * dx + sw / 2;                       // columna del sello
            int sy = Mathf.FloorToInt(kh * 0.55f + sh * 0.5f - v); // fila (de arriba abajo)
            if (sx >= 0 && sx < sw && sy >= 0 && sy < sh)
            {
                char c = sello[sy][sx];
                if (c == '.') return new PxMuro(2, 0.45f);
                if (c == 'a') return g.Z == Zone.Sanctuary || g.Z == Zone.Ruins ? new PxMuro(4, 0.7f, MAcento) : new PxMuro(1, 0.5f);
                return new PxMuro(c - '0', 0.6f + (c - '0') * 0.06f);
            }
            return new PxMuro(ti, 0.72f);
        }

        /// <summary>
        /// Tímpano: el hueco ojival por encima del dintel, con un relieve tallado: el Signo Antiguo entre tentáculos,
        /// dos peces de Dagón con el tridente (Santuario), un ojo que irradia (Ruinas) o una venera (Costa y Arrecife).
        /// e = distancia al borde de la ojiva; v = altura sobre el dintel.
        /// </summary>
        static PxMuro Timpano(ref GeoMuro g, int dx, int side, float e, float v, TerrainTheme t)
        {
            if (e < 1f) return new PxMuro(1, 0.3f);                // escocia del marco
            var fondo = new PxMuro(e < 2f ? 1 : 2, 0.3f);          // campo rehundido
            float x = dx, y = v;
            if (g.Z == Zone.Ruins)
            {
                // Ojo que irradia, erosionado.
                float yc = 17f, hx = 22f, hy = 8.5f;
                float lim = hy * (1f - (x / hx) * (x / hx));
                float dyc = y - yc;
                if (PixelCanvas.ValueNoise(g.Wx / 5f, g.Ly / 5f, 0, 221) > 0.7f) return new PxMuro(1, 0.25f); // relieve perdido
                if (lim > 0f && Mathf.Abs(dyc) < lim + 1.6f)
                {
                    if (Mathf.Abs(dyc) >= lim) return new PxMuro(dyc > 0 ? 5 : 2, 0.7f);         // párpados
                    float ri = Mathf.Sqrt(x * x + dyc * dyc);
                    if (x < 1.2f && Mathf.Abs(dyc) < 5f) return new PxMuro(0, 0.3f);              // pupila de rendija
                    if (ri < 6.5f) return ri > 5.3f ? new PxMuro(2, 0.4f) : new PxMuro(3, 0.5f, MAcento); // iris
                    return new PxMuro(3 + (dyc > 2f ? 1 : 0), 0.55f);                             // globo
                }
                float ang = Mathf.Atan2(dyc, side * x);
                float rr = Mathf.Sqrt(x * x + dyc * dyc);
                float fa = ang * 10f / Mathf.PI;
                fa -= Mathf.Floor(fa);
                if (rr > 12f && dyc > -6f && fa < 0.22f) return new PxMuro(fa < 0.11f ? 4 : 3, 0.55f); // rayos
                return fondo;
            }
            if (g.Z == Zone.Reef || g.Z == Zone.Coast)
            {
                // Venera: costillas radiales desde abajo.
                float rr = Mathf.Sqrt(x * x + (y + 2f) * (y + 2f));
                float ang = Mathf.Atan2(y + 2f, side * x);
                float edge = 31f + 1.5f * Mathf.Abs(Mathf.Sin(ang * 8f));
                if (rr < edge)
                {
                    float fa = ang * 8f / Mathf.PI;
                    fa -= Mathf.Floor(fa);
                    if (rr > edge - 1.5f) return new PxMuro(4, 0.6f);
                    return new PxMuro(fa < 0.45f ? (fa < 0.15f ? 5 : 4) : (fa > 0.85f ? 1 : 2), 0.4f + (fa < 0.45f ? 0.2f : 0f));
                }
                return fondo;
            }
            // Santuario: olas en el registro bajo.
            float ola = 3f + 2f * Mathf.Sin(x * 0.55f);
            if (y < ola) return new PxMuro(y > ola - 1f ? 4 : 3, 0.5f);
            if ((g.BayI & 1) == 0)
            {
                // El Signo Antiguo en un medallón, entre tentáculos.
                float cy = 21f, dyc = y - cy;
                float rr = Mathf.Sqrt(x * x + dyc * dyc);
                if (rr < 13f)
                {
                    if (rr >= 10.5f)
                    {
                        float l = (side * x * LuzX + dyc * LuzY) / Mathf.Max(1f, rr);
                        return new PxMuro(l > 0.35f ? 5 : l < -0.35f ? 2 : 4, 0.7f);
                    }
                    if (Estrella(side * x, dyc, 9.5f, 4f))
                    {
                        if (x < 2.5f && Mathf.Abs(dyc) < 1.5f) return x < 1f ? new PxMuro(0, 0.4f) : new PxMuro(5, 0.7f, MAcento); // ojo
                        bool borde = !Estrella(side * x - 1f, dyc + 1f, 9.5f, 4f);
                        return new PxMuro(borde ? 5 : 4, 0.65f, MAcento);
                    }
                    return new PxMuro(1, 0.3f);
                }
                // Tentáculos que ondulan hacia fuera, con ventosas.
                if (x > 12f)
                {
                    float tdx = x - 12f;
                    float vc = 17f + 5.5f * Mathf.Sin(tdx * 0.21f + 0.6f) - tdx * 0.05f;
                    float th = Mathf.Max(1.6f, 5.5f - tdx * 0.1f);
                    float dv = y - vc;
                    if (Mathf.Abs(dv) < th * 0.5f + 0.5f)
                    {
                        if (dv > th * 0.5f - 0.5f) return new PxMuro(5, 0.7f);
                        if (dv < -th * 0.5f + 0.5f) return new PxMuro(((int)tdx % 3 == 0) ? 5 : 2, 0.6f); // ventosas
                        return new PxMuro(4, 0.68f);
                    }
                    // Segundo tentáculo, más corto, por encima.
                    float vc2 = 30f + 3f * Mathf.Sin(tdx * 0.3f + 2f) - tdx * 0.35f;
                    float th2 = Mathf.Max(1.4f, 4f - tdx * 0.1f);
                    float dv2 = y - vc2;
                    if (tdx < 30f && Mathf.Abs(dv2) < th2 * 0.5f + 0.5f) return new PxMuro(dv2 > 0f ? 5 : 3, 0.65f);
                }
                return fondo;
            }
            // Dos peces de Dagón afrontados y el tridente.
            if (x < 1f && y > 4f && y < 36f) return new PxMuro(side < 0 ? 5 : 3, 0.65f);                    // asta
            if (y > 28f && y < 36f && (Mathf.Abs(x - 5f) < 0.8f)) return new PxMuro(4, 0.6f);               // púas
            if (Mathf.Abs(y - 28.5f) < 0.8f && x < 6f) return new PxMuro(4, 0.6f);                          // travesaño
            {
                float fx = x - 23f, fy = y - 17f;
                float body = (fx * fx) / (14f * 14f) + (fy * fy) / (6.5f * 6.5f);
                if (body < 1f)
                {
                    if (body > 0.78f) return new PxMuro(fy > 0f ? 5 : 2, 0.6f);
                    if (Mathf.Abs(fx + 8f) < 1.3f && Mathf.Abs(fy - 1f) < 1.3f) return new PxMuro(0, 0.4f);   // ojo
                    if (fx < -9.5f) return new PxMuro(fy < -1f ? 2 : 4, 0.6f);                                 // cabeza
                    bool escama = ((Mathf.FloorToInt(fx) + Mathf.FloorToInt(fy) * 2) & 3) == 0;
                    return new PxMuro(escama ? 3 : 4, 0.62f);
                }
                // Cola en abanico y aleta dorsal.
                if (fx > 12f && fx < 21f && Mathf.Abs(fy) < (fx - 12f) * 0.9f + 1f) return new PxMuro(((int)fy & 1) == 0 ? 4 : 3, 0.55f);
                if (fy > 5f && fy < 9f && fx > -6f && fx < 6f && fy - 5f < (6f - Mathf.Abs(fx)) * 0.6f) return new PxMuro(4, 0.55f);
            }
            return fondo;
        }

        /// <summary>¿Está (x, y) dentro de una estrella de cinco puntas con radio exterior ro e interior ri?</summary>
        static bool Estrella(float x, float y, float ro, float ri)
        {
            float r = Mathf.Sqrt(x * x + y * y);
            if (r > ro) return false;
            if (r < ri * 0.8f) return true;
            float a = Mathf.Atan2(y, x) - Mathf.PI * 0.5f;
            float sector = 2f * Mathf.PI / 5f;
            a = Mathf.Repeat(a + sector * 0.5f, sector) - sector * 0.5f; // -36°..36°, 0 en la punta
            a = Mathf.Abs(a);
            // Arista recta de la punta (ro, 0) al vértice interior (ri, 36°).
            float px = r * Mathf.Cos(a), py = r * Mathf.Sin(a);
            float bx = ri * Mathf.Cos(sector * 0.5f), by = ri * Mathf.Sin(sector * 0.5f);
            float cross = (bx - ro) * py - by * (px - ro);
            return cross >= 0f;
        }

        /// <summary>Fondo del nicho de los arcos cerrados: oscuro, con ladrillos apenas insinuados (ahí va la vidriera).</summary>
        static PxMuro Nicho(ref GeoMuro g, int dx)
        {
            int wx = g.Wx, ly = g.Ly;
            int row = FloorDiv(ly, 6);
            int bxk = FloorDiv(wx + (row & 1) * 6, 12);
            bool junta = ly - row * 6 == 0 || wx + (row & 1) * 6 - bxk * 12 == 0;
            int tono = junta ? 0 : (PixelCanvas.Hash(bxk, row, 131) > 0.6f ? 2 : 1);
            // Más hondo junto a la moldura (sombra del intradós) y algo de luz abajo, junto al suelo.
            if (dx > ArchHalfW - 10 || ly > g.ATop - 12) tono = 0;
            else if (ly < 24 && !junta) tono++;
            return new PxMuro(tono, junta ? 0f : 0.08f, MHueco);
        }

        // ------------------------------------------------------------------
        // Ornamentos: cornisa con canecillos, arquería ciega, rosetón, medallones y hornacinas
        // ------------------------------------------------------------------

        /// <summary>El muro liso de cada zona (sillería, mampostería ciclópea o tablazón podrido en la Costa).</summary>
        static PxMuro Pared(ref GeoMuro g, TerrainTheme t)
        {
            if (g.Ruinas) return Ciclopea(g.Wx, g.Ly, t);
            if (g.Z == Zone.Coast) return Tablazon(g.Wx, g.Ly);
            return Silleria(g.Wx, g.Ly, g.Zh, t);
        }

        // Canecillo con cabeza de pez bajo la cornisa (7×9, de arriba abajo).
        static readonly string[] Canecillo = {
            "3555553",
            "2333332",
            "4054504",
            "3445443",
            "3400043",
            ".34243.",
            ".33433.",
            "..343..",
            "...2...",
        };

        static bool Ornamentos(ref GeoMuro g, TerrainTheme t, out PxMuro p)
        {
            p = default;
            if (g.ATop <= 40f) return false;
            int ly = g.Ly;
            int side = g.Bx >= Bay / 2 ? 1 : -1;

            // Marco del rosetón sobre los arcos cerrados altos del Santuario (ahí el constructor pone la vidriera_rosa).
            if (g.Z == Zone.Sanctuary && !OpenArch(g.Z, g.BayI) && g.Span - (g.ATop + 62f) > 64f)
            {
                float rx = g.Bx + 0.5f - Bay / 2f, ry = ly + 0.5f - (g.ATop + 62f);
                float rr = Mathf.Sqrt(rx * rx + ry * ry);
                if (rr < 56f)
                {
                    if (rr < 47f)
                    {
                        // Tracería ciega (solo se ve si falta la vidriera): radios y anillo interior.
                        float ang = Mathf.Atan2(ry, rx) * 8f / Mathf.PI;
                        float fa = ang - Mathf.Floor(ang);
                        bool radio = fa < 0.12f || fa > 0.88f;
                        bool anillo = Mathf.Abs(rr - 22f) < 1.2f || rr < 6f;
                        return Ok(out p, radio || anillo ? new PxMuro(3, 0.5f) : new PxMuro(0, 0.05f, MHueco));
                    }
                    float l = (rx * LuzX + ry * LuzY) / rr;
                    int ti;
                    if (rr < 48f) ti = 1;                                        // sombra interior
                    else if (rr < 51f) ti = Mathf.Clamp(Bocel((rr - 49.5f) / 1.5f, l), 1, 6);
                    else if (rr < 52f) ti = 1;                                   // escocia
                    else if (rr < 54.5f)
                    {
                        // Puntas de diamante (dientes de perro) alrededor del aro exterior.
                        float ang = Mathf.Atan2(ry, rx) * 24f / Mathf.PI;
                        float fa = ang - Mathf.Floor(ang);
                        ti = fa < 0.5f ? (l > 0f ? 5 : 3) : 2;
                    }
                    else ti = l > 0.2f ? 4 : 2;
                    return Ok(out p, new PxMuro(ti, 0.55f + (rr > 48f && rr < 51f ? 0.3f : 0.1f)));
                }
            }

            int corn = Cornisa(ref g);
            if (corn > 0 && ly >= corn && ly < corn + 6)
            {
                // Cornisa: panza oscura, dentículos, cimacio y canto superior iluminado.
                int r = ly - corn;
                int m = ((g.Wx % 4) + 4) % 4;
                int ti = r == 0 ? 1 : r <= 2 ? (m == 0 ? 4 : m == 1 ? 2 : 1) : r == 3 ? 3 : r == 4 ? 5 : 6;
                if (g.Ruinas && PixelCanvas.Hash(FloorDiv(g.Wx, 6), corn, 251) > 0.7f) ti = Mathf.Max(1, ti - 2);
                return Ok(out p, new PxMuro(ti, r == 0 ? 0.5f : 0.7f + r * 0.04f));
            }
            if (!g.Ruinas && corn > 0)
            {
                if (ly >= corn - 9 && ly < corn)
                {
                    int u = ((g.Bx - 4) % 16 + 16) % 16;
                    if (u < 7)
                    {
                        char c = Canecillo[corn - 1 - ly][u];
                        if (c != '.') return Ok(out p, new PxMuro(c - '0', 0.55f + (c - '0') * 0.06f));
                    }
                    var w = Pared(ref g, t);
                    w.T -= ly >= corn - 3 ? 2 : 1; // sombra bajo la cornisa
                    if (u == 7 && corn - 1 - ly < 8) w.T--; // sombra del canecillo a su derecha
                    w.A -= 0.15f;
                    return Ok(out p, w);
                }
                int ha = Mathf.Min(24, g.Span - (corn + 6));
                if (ha >= 16 && ly >= corn + 6 && ly < corn + 6 + ha)
                    return Ok(out p, Arqueria(ref g, ly - (corn + 6), ha, t));
            }

            // Medallones en las enjutas (a cada lado de cada pilar).
            if (g.ATop >= 150f)
            {
                float cxw = Bay / 2f + side * 88f, cyw = g.ATop + 15f;
                float rx = g.Bx + 0.5f - cxw, ry = ly + 0.5f - cyw;
                float rr = Mathf.Sqrt(rx * rx + ry * ry);
                if (rr < 12f) return Ok(out p, Medallon(ref g, rx, ry, rr, t));
            }

            // Hornacinas con estatuas encapuchadas en los muros altos.
            if (!g.Ruinas && corn > 0 && g.Span > corn + 100)
            {
                float cxw = Bay / 2f + side * 88f;
                float rx = g.Bx + 0.5f - cxw;
                int y0 = corn + 40;
                if (Mathf.Abs(rx) < 10f && ly >= y0 - 7 && ly < y0 + 54)
                {
                    if (Hornacina(ref g, rx, ly - y0, t, out p)) return true;
                }
            }
            return false;
        }

        /// <summary>Arquería ciega: lancetas de 12 px entre columnillas, con su alféizar y una moldura arriba (yy de abajo arriba).</summary>
        static PxMuro Arqueria(ref GeoMuro g, int yy, int ha, TerrainTheme t)
        {
            if (yy < 2) return new PxMuro(yy == 1 ? 4 : 2, 0.7f);
            if (yy >= ha - 3) return new PxMuro(yy == ha - 3 ? 1 : yy == ha - 2 ? 3 : 5, 0.75f);
            int u = ((g.Bx % 14) + 14) % 14;
            if (u < 2) return new PxMuro(u == 0 ? 4 : 2, 0.7f);
            float lx = Mathf.Abs(u + 0.5f - 8f);
            const int Ys = 12;
            float ins; // >0 dentro de la lanceta
            if (yy < Ys) ins = 5.2f - lx;
            else ins = 7.7f - Mathf.Sqrt((lx + 2.5f) * (lx + 2.5f) + (yy - Ys) * (yy - Ys));
            if (ins > 0f)
            {
                bool izq = u + 0.5f < 8f;
                int ti = ins < 1f ? (izq || yy >= Ys ? 0 : 1) : yy < 4 ? 2 : 1;
                return new PxMuro(ti, 0.15f, MHueco);
            }
            if (ins > -1.2f) return new PxMuro(u + 0.5f < 8f || yy >= Ys + 4 ? 4 : 2, 0.6f);
            var w = Pared(ref g, t);
            w.A += 0.05f;
            return w;
        }

        /// <summary>Medallón con tracería: cuadrifolio (Santuario), venera (Costa/Arrecife) u ojo erosionado (Ruinas).</summary>
        static PxMuro Medallon(ref GeoMuro g, float rx, float ry, float rr, TerrainTheme t)
        {
            if (rr >= 10.5f)
            {
                var w = Pared(ref g, t);
                if (rx > 0f || ry < 0f) { w.T--; w.A -= 0.1f; } // sombra arrojada
                return w;
            }
            float l = (rx * LuzX + ry * LuzY) / Mathf.Max(0.5f, rr);
            if (rr >= 8.5f) return new PxMuro((l > 0.3f ? 5 : l < -0.3f ? 2 : 4) - (rr > 9.8f ? 1 : 0), 0.8f);
            if (rr >= 7.5f) return new PxMuro(1, 0.35f);
            if (g.Ruinas)
            {
                if (PixelCanvas.ValueNoise(g.Wx / 3f, g.Ly / 3f, 0, 252) > 0.66f) return new PxMuro(1, 0.3f);
                float lim = 3.6f * (1f - (rx / 6.5f) * (rx / 6.5f));
                if (lim > 0f && Mathf.Abs(ry) < lim)
                {
                    float ri = Mathf.Sqrt(rx * rx + ry * ry);
                    if (Mathf.Abs(rx) < 0.8f) return new PxMuro(0, 0.3f);
                    return ri < 2.6f ? new PxMuro(3, 0.5f, MAcento) : new PxMuro(3, 0.55f);
                }
                return new PxMuro(Mathf.Abs(ry) < lim + 1.2f && lim > -1f ? (ry > 0f ? 4 : 2) : 2, 0.4f);
            }
            if (g.Z == Zone.Reef || g.Z == Zone.Coast)
            {
                float ang = Mathf.Atan2(ry + 7f, rx) * 7f / Mathf.PI;
                float fa = ang - Mathf.Floor(ang);
                return new PxMuro(fa < 0.45f ? 4 : 2, 0.5f);
            }
            // Cuadrifolio: cuatro lóbulos rehundidos y las puntas (cúspides) en relieve, con un botón central dorado.
            if (rr < 1.4f) return new PxMuro(5, 0.8f, MAcento);
            float best = 99f;
            for (int k = 0; k < 4; k++)
            {
                float lx = rx - (k == 0 ? 3.4f : k == 1 ? -3.4f : 0f), lyy = ry - (k == 2 ? 3.4f : k == 3 ? -3.4f : 0f);
                best = Mathf.Min(best, Mathf.Sqrt(lx * lx + lyy * lyy));
            }
            if (best < 2.4f) return new PxMuro(1, 0.25f);
            if (best < 3.3f) return new PxMuro(l < 0f ? 4 : 2, 0.45f);
            return new PxMuro(3, 0.55f);
        }

        /// <summary>
        /// Hornacina: nicho ojival con su repisa, un gablete con cardinas encima y una estatua encapuchada dentro
        /// (con tentáculos asomando bajo el hábito). rx = desde el eje; yy = desde la repisa.
        /// </summary>
        static bool Hornacina(ref GeoMuro g, float rx, int yy, TerrainTheme t, out PxMuro p)
        {
            p = default;
            float ax = Mathf.Abs(rx);
            // Repisa (ménsula) bajo el nicho.
            if (yy < 0)
            {
                float w = 7f + yy * 0.6f;
                if (ax >= w) return false;
                p = new PxMuro(yy == -1 ? 5 : yy == -2 ? 3 : rx > 1f ? 1 : 2, 0.7f);
                return true;
            }
            // Gablete: triángulo con el canto iluminado y un pináculo.
            if (yy >= 42)
            {
                float w = (52 - yy) * 0.8f;
                if (yy >= 52) { if (ax < 1f) { p = new PxMuro(5, 0.8f); return true; } return false; }
                if (ax >= w + 1f) return false;
                if (ax >= w) { p = new PxMuro(rx < 0 ? 5 : 2, 0.75f); return true; }
                p = new PxMuro(Mathf.Abs(rx) < 2f && yy < 47 ? 2 : 3, 0.6f); // trifolio insinuado
                return true;
            }
            // Marco del nicho.
            float ins = yy < 32 ? 6f - ax : 10f - Mathf.Sqrt((ax + 4f) * (ax + 4f) + (yy - 32) * (yy - 32));
            if (ins < -1.6f) return false;
            if (ins < 0f) { p = new PxMuro(rx < 0f || yy >= 32 ? 4 : 2, 0.7f); return true; }
            // Interior oscuro y la estatua.
            var fondo = new PxMuro(ins < 1f && rx < 0f ? 0 : yy < 6 ? 2 : 1, 0.1f, MHueco);
            float hx = rx, hy = yy - 27f;
            // Capucha y rostro en sombra.
            if (hx * hx / 9f + hy * hy / 12f < 1f)
            {
                if (hx * hx / 2.6f + (hy + 0.6f) * (hy + 0.6f) / 4f < 1f) { p = new PxMuro(0, 0.2f, MHueco); return true; }
                p = new PxMuro(hx < -1.2f ? 5 : hx > 1.4f ? 2 : 4, 0.7f);
                return true;
            }
            // Hábito con pliegues y manos juntas.
            if (yy >= 3 && yy < 24)
            {
                float w = 2.9f + (24 - yy) * 0.07f;
                if (ax < w)
                {
                    if (yy >= 14 && yy <= 16 && Mathf.Abs(rx - 0.3f) < 1.4f) { p = new PxMuro(5, 0.8f); return true; }
                    int ti = rx < -w + 1f ? 5 : rx > w - 1f ? 2 : 3;
                    if (Mathf.Abs(rx + 0.8f) < 0.5f || Mathf.Abs(rx - 1.6f) < 0.5f) ti = 2; // pliegues
                    p = new PxMuro(ti, 0.65f);
                    return true;
                }
            }
            // Tentáculos que asoman por el bajo.
            if (yy < 4)
            {
                float cx = rx < 0f ? -3.2f : 3.2f;
                float tx = rx - cx, ty = yy - 1.6f;
                if (Mathf.Abs(tx) < 2.2f && Mathf.Abs(ty) < 1.3f - Mathf.Abs(tx) * 0.2f) { p = new PxMuro(ty > 0f ? 4 : 2, 0.6f); return true; }
                if (ax < 2.6f) { p = new PxMuro(3, 0.6f); return true; }
            }
            p = fondo;
            return true;
        }

        /// <summary>Tablazón de madera podrida (Costa): tablas verticales con veta, nudos, clavos oxidados y podredumbre abajo.</summary>
        static PxMuro Tablazon(int wx, int ly)
        {
            int id = Hilada(wx, 3, 9, 5, 241, out int pos, out int w);
            if (pos == 0) return new PxMuro(0, 0.05f, MMadera);
            int cut = Mathf.FloorToInt(PixelCanvas.Hash(id, 1, 242) * 90f);
            if (((ly + cut) % 96) == 0) return new PxMuro(0, 0.1f, MMadera);   // testa entre tablas
            float veta = PixelCanvas.ValueNoise(wx * 0.9f, ly * 0.06f, 0, 243);
            int ti = veta > 0.62f ? 3 : veta < 0.3f ? 1 : 2;
            if (pos == 1) ti++;
            else if (pos == w - 1) ti--;
            // Nudos.
            int nx = FloorDiv(ly, 37);
            if (PixelCanvas.Hash(id, nx, 244) > 0.8f)
            {
                float kx = pos - w * 0.5f, ky = ly - nx * 37 - 18f;
                float kr = kx * kx / 4f + ky * ky / 9f;
                if (kr < 1f) ti = kr < 0.35f ? 0 : 3;
            }
            // Clavos.
            if ((ly % 48) == 10 && (pos == 2 || pos == w - 3)) return new PxMuro(4, 0.8f, MTierra);
            // Podredumbre abajo.
            if (ly < 26 + 10f * PixelCanvas.ValueNoise(wx / 7f, 0.3f, 0, 245)) ti = Mathf.Max(0, ti - 1);
            return new PxMuro(Mathf.Clamp(ti, 0, 5), 0.45f + (pos == 1 ? 0.1f : 0f), MMadera);
        }

        // ------------------------------------------------------------------
        // Molduras horizontales: zócalo e imposta
        // ------------------------------------------------------------------

        static bool Molduras(ref GeoMuro g, TerrainTheme t, out PxMuro p)
        {
            p = default;
            int ly = g.Ly;
            if (ly < g.Zh) { p = Zocalo(ref g); return true; }
            int s = g.S;
            if (g.ATop > 40f && ly >= s - 5 && ly <= s + 2)
            {
                int r = ly - s;
                if (r >= -1)
                {
                    p = Imposta(r, g.Ruinas, g.Wx, s);
                    return true;
                }
                if (r >= -4 && !g.Ruinas)
                {
                    // Dentículos bajo la imposta: dientes de 2 px con su luz a la izquierda y su sombra a la derecha.
                    int m = ((g.Wx % 4) + 4) % 4;
                    if (r == -4) p = new PxMuro(1, 0.3f);
                    else if (m == 0) p = new PxMuro(4, 0.7f);
                    else if (m == 1) p = new PxMuro(2, 0.65f);
                    else p = new PxMuro(0, 0.25f);
                    return true;
                }
                if (r == -5 || (g.Ruinas && r >= -3))
                {
                    var w = Pared(ref g, t);
                    if (r == -5 || r == -2) { w.T -= 1; w.A -= 0.1f; } // sombra arrojada por la imposta
                    p = w;
                    return true;
                }
            }
            return false;
        }

        /// <summary>Zócalo: una hilada de bloques más oscuros y grandes con su moldura (bocel) arriba.</summary>
        static PxMuro Zocalo(ref GeoMuro g)
        {
            int ly = g.Ly, zh = g.Zh;
            int top = zh - 4;
            if (ly >= top)
            {
                int r = ly - top;
                int[] perfil = { 1, 3, 5, 4 };
                float[] alt = { 0.45f, 0.75f, 0.9f, 0.8f };
                var m = new PxMuro(perfil[r], alt[r]);
                if (g.Ruinas && PixelCanvas.ValueNoise(g.Wx / 4f, 1.7f, 0, 151) > 0.68f) { m.T = r == 3 ? 2 : m.T - 1; m.A -= 0.25f; } // bocel desportillado
                return m;
            }
            int minW = g.Ruinas ? 44 : 30, rangeW = g.Ruinas ? 40 : 26;
            int id = Hilada(g.Wx, 7, minW, rangeW, 153, out int pos, out int bw);
            float tone = PixelCanvas.Hash(id, 3, 154);
            if (pos == 0 || ly == 0) return new PxMuro(0, 0.15f);
            int ti = 2 + (tone > 0.75f ? 1 : 0);
            float a = 0.55f;
            if (ly == top - 1 || pos == 1) { ti++; a = 0.5f; }
            else if (ly == 1 || pos == bw - 1) { ti--; a = 0.42f; }
            float grain = PixelCanvas.ValueNoise(g.Wx * 0.4f, ly * 0.5f, 0, 155);
            if (grain > 0.74f) ti--;
            else if (grain < 0.16f) ti++;
            return new PxMuro(ti, a);
        }

        // ------------------------------------------------------------------
        // Sillería
        // ------------------------------------------------------------------

        /// <summary>Divide una hilada en sillares de anchura minW..minW+rangeW-1, desplazada según la hilada.</summary>
        static int Hilada(int wx, int row, int minW, int rangeW, int seed, out int pos, out int width)
        {
            int shift = Mathf.FloorToInt(PixelCanvas.Hash(row, 11, seed) * 61f);
            int x = wx + shift;
            int block = FloorDiv(x, 512);
            int start = block * 512;
            int index = block * 97 + row * 7919;
            while (true)
            {
                int bw = minW + Mathf.FloorToInt(PixelCanvas.Hash(index, row, seed + 1) * rangeW);
                if (x < start + bw || bw <= 0)
                {
                    pos = x - start;
                    width = bw;
                    return index;
                }
                start += bw;
                index++;
            }
        }

        /// <summary>
        /// Sillería de catedral: hiladas de 12 px, sillares con bisel (canto claro arriba-izquierda, sombra
        /// abajo-derecha), variación de tono por pieza, grano en grupos, esquinas desportilladas, alguna marca de
        /// cantero y algún sillar partido o caído.
        /// </summary>
        static PxMuro Silleria(int wx, int ly, int zh, TerrainTheme t)
        {
            const int RowH = 12;
            int yy = ly - zh;
            int row = FloorDiv(yy, RowH);
            int ry = yy - row * RowH;                     // 0 = junta de abajo
            int id = Hilada(wx, row, 22, 20, 161, out int pos, out int bw);
            if (pos == 0 || ry == 0) return new PxMuro(1, 0.15f);
            float hb = PixelCanvas.Hash(id, row, 162);

            // Sillar caído: hueco oscuro con el fondo en sombra y el canto inferior iluminado.
            if (hb > 0.994f && ly > zh + 14)
            {
                if (ry == 1) return new PxMuro(3, 0.1f);
                if (ry >= RowH - 2 || pos == 1) return new PxMuro(0, 0f, MHueco);
                return new PxMuro(1, 0.05f, MHueco);
            }

            int ti = 3 + (hb > 0.86f ? 1 : 0) - (hb < 0.14f ? 1 : 0);
            float a = 0.6f;
            // Esquina desportillada: una mordida triangular en una de las cuatro esquinas.
            int corner = Mathf.FloorToInt(PixelCanvas.Hash(id, row, 163) * 9f); // 0..3 mordida, resto entera
            if (corner < 4)
            {
                int cxp = (corner & 1) == 0 ? pos - 1 : bw - 1 - pos;
                int cyp = (corner & 2) == 0 ? ry - 1 : RowH - 1 - ry;
                int cs = 2 + (corner & 1);
                if (cxp + cyp < cs - 1) return new PxMuro(1, 0.2f);
                if (cxp + cyp == cs - 1) { ti = (corner & 2) != 0 ? ti + 1 : ti - 1; a = 0.45f; }
            }
            // Bisel.
            if (ry == RowH - 1 && pos == 1) { ti += 2; a = 0.5f; }
            else if (ry == RowH - 1 || pos == 1) { ti++; a = 0.5f; }
            else if (ry == 1 || pos == bw - 1) { ti--; a = 0.45f; }
            // Grano en grupos de 2-4 px (no ruido suelto) y poros.
            float grain = PixelCanvas.ValueNoise(wx * 0.38f, ly * 0.55f, 0, 164);
            if (grain > 0.8f) ti--;
            else if (grain < 0.1f) ti++;
            if (ry > 1 && ry < RowH - 1 && pos > 1 && pos < bw - 1 && PixelCanvas.Hash(wx >> 1, ly >> 1, 165) > 0.96f) ti--;
            // Sillar partido por una grieta diagonal.
            if (hb > 0.93f && hb <= 0.994f && bw > 18)
            {
                float along = pos - bw * 0.3f - (ry - RowH * 0.5f) * 1.3f;
                if (Mathf.Abs(along) < 0.6f && ry > 1) return new PxMuro(0, 0.1f);
                if (along >= 0.6f && along < 1.6f && ry > 1) ti++;
            }
            // Marca de cantero (cruz, triángulo o tridente) en algún sillar ancho.
            if (hb > 0.6f && hb < 0.64f && bw > 26 && ry >= 3 && ry <= 9)
            {
                int mx = pos - bw / 2, my = ry - 6;
                int kind = Mathf.FloorToInt(PixelCanvas.Hash(id, 5, 166) * 3f);
                bool on = kind == 0 ? (mx == 0 && Mathf.Abs(my) <= 2) || (my == 1 && Mathf.Abs(mx) <= 1)
                        : kind == 1 ? (my == -2 && Mathf.Abs(mx) <= 2) || (Mathf.Abs(mx) == my + 1 && my >= -1 && my <= 1) || (my == 2 && mx == 0)
                        : (my >= -2 && my <= 2 && mx == 0) || (my == 2 && Mathf.Abs(mx) == 2) || (my == 1 && Mathf.Abs(mx) == 2) || (my == 0 && Mathf.Abs(mx) <= 2);
                if (on) { ti -= 1; a = 0.45f; }
            }
            return new PxMuro(ti, a);
        }

        /// <summary>
        /// Mampostería ciclópea (Ruinas): grandes piedras poligonales encajadas (celdas de Voronoi: aristas rectas),
        /// redondeadas por la erosión, con bisel según hacia dónde mira cada arista, picaduras y glifos tenues.
        /// </summary>
        static PxMuro Ciclopea(int wx, int ly, TerrainTheme t)
        {
            const float Cw = 40f, Ch = 28f;
            int cx = Mathf.FloorToInt(wx / Cw), cy = Mathf.FloorToInt(ly / Ch);
            float bestD = 1e9f, secD = 1e9f;
            float b1x = 0, b1y = 0, b2x = 0, b2y = 0;
            int bestId = 0;
            for (int j = -1; j <= 1; j++)
            {
                for (int i = -1; i <= 1; i++)
                {
                    int ix = cx + i, iy = cy + j;
                    float sx = (ix + 0.15f + PixelCanvas.Hash(ix, iy, 171) * 0.7f) * Cw;
                    float sy = (iy + 0.15f + PixelCanvas.Hash(ix, iy, 172) * 0.7f) * Ch;
                    float ddx = wx + 0.5f - sx, ddy = ly + 0.5f - sy;
                    float d = ddx * ddx * 0.8f + ddy * ddy;
                    if (d < bestD)
                    {
                        secD = bestD; b2x = b1x; b2y = b1y;
                        bestD = d; b1x = sx; b1y = sy; bestId = ix * 7349 + iy * 151;
                    }
                    else if (d < secD) { secD = d; b2x = sx; b2y = sy; }
                }
            }
            // Distancia (aprox. en píxeles) a la arista entre las dos piedras más cercanas.
            float nx = b2x - b1x, ny = b2y - b1y;
            float len = Mathf.Max(0.001f, Mathf.Sqrt(nx * nx * 0.8f * 0.8f + ny * ny));
            float edge = (secD - bestD) / (2f * len);
            if (edge < 0.85f) return new PxMuro(0, 0.1f);
            float hb = PixelCanvas.Hash(bestId, 3, 173);
            int ti = 3 + (hb > 0.8f ? 1 : 0) - (hb < 0.22f ? 1 : 0);
            float a = 0.25f + 0.5f * Mathf.Clamp01(edge / 4.5f);
            if (edge < 2.6f)
            {
                float dl = (nx * -0.6f + ny * 0.8f) / Mathf.Max(0.001f, Mathf.Sqrt(nx * nx + ny * ny));
                if (dl > 0.25f) ti++;          // arista que mira arriba-izquierda: coge la luz
                else if (dl < -0.25f) ti--;    // arista en sombra
                if (edge < 1.6f && dl < -0.25f) ti--;
            }
            float grain = PixelCanvas.ValueNoise(wx * 0.3f, ly * 0.42f, 0, 174);
            if (grain > 0.74f) ti--;
            else if (grain < 0.15f) ti++;
            // Musgo en los cantos de arriba de las piedras.
            if (edge < 2.6f && ny > 0f && PixelCanvas.Hash(wx >> 1, ly, 176) < t.MossAmount * 0.55f) return new PxMuro(edge < 1.6f ? 2 : 1, 0.55f, MMusgo);
            // Glifos tallados (algunos aún brillan) en alguna piedra.
            if (hb > 0.9f && edge > 3.5f)
            {
                int gx = Mathf.FloorToInt(wx + 0.5f - b1x) + 2, gy = Mathf.FloorToInt(ly + 0.5f - b1y) + 3;
                if (Glifo(gx, gy, bestId))
                    return hb > 0.965f ? new PxMuro(3, 0.4f, MAcento) : new PxMuro(1, 0.35f);
            }
            // Picaduras: cavidades de 2-3 px con el labio de abajo iluminado.
            int pcx = FloorDiv(wx, 7), pcy = FloorDiv(ly, 6);
            if (PixelCanvas.Hash(pcx, pcy, 175) > 0.86f && edge > 3f)
            {
                int lx = wx - pcx * 7, lyy = ly - pcy * 6;
                if (lx >= 2 && lx <= 4 && lyy >= 2 && lyy <= 3) ti = lyy == 3 ? ti - 2 : ti - 1;
                else if (lx >= 2 && lx <= 4 && lyy == 1) ti++;
            }
            return new PxMuro(ti, a);
        }

        // ------------------------------------------------------------------
        // Desgaste: humedad que chorrea, salitre, grietas, verdín y el borde roto de las Ruinas
        // ------------------------------------------------------------------

        static void Desgaste(ref PxMuro p, ref GeoMuro g, TerrainTheme t, int ruinTop)
        {
            if (p.M == MHueco) return;
            int wx = g.Wx, ly = g.Ly;
            float moss = t.MossAmount;

            // Raíces y musgo que cuelgan del borde roto de las Ruinas.
            if (g.Ruinas && Colgajos(wx, ly, moss, out PxMuro raiz)) { p = raiz; return; }

            // Borde roto de las Ruinas: canto fracturado que coge la luz, con mordiscos.
            if (g.Ruinas && ruinTop != int.MaxValue)
            {
                int dTop = ruinTop - 1 - ly;
                if (dTop <= 2)
                {
                    float bite = PixelCanvas.Hash(wx >> 1, ruinTop, 181);
                    if (dTop == 0 && bite > 0.62f) { p = new PxMuro(0, 0f, MAbierto); return; }
                    if (dTop == 1 && bite > 0.86f) { p = new PxMuro(0, 0f, MAbierto); return; }
                    if (dTop == 0 || (dTop == 1 && bite > 0.62f)) { p.T = 5; p.A = 0.9f; }
                    else if (dTop == 1 || (dTop == 2 && bite > 0.86f)) { p.T = Mathf.Max(p.T, 4); p.A = 0.8f; }
                    // Musgo sobre el borde.
                    if (dTop <= 1 && PixelCanvas.Hash(wx, 3, 182) < moss * 0.9f) { p.M = MMusgo; p.T = dTop == 0 ? 3 : 2; }
                    return;
                }
            }

            // Grietas que se ramifican.
            if (Grieta(wx, ly, g.BayI, g.Span, g.Ruinas, out bool labio))
            {
                p.T = 0; p.M = MPiedra; p.A = 0.05f; p.H = 0;
                return;
            }
            if (labio) p.T += 1;

            // Humedad que sube del suelo (capilaridad) con su línea de salitre.
            float damp = 16f + 18f * PixelCanvas.ValueNoise(wx / 29f, 0.5f, 0, 183) + 5f * PixelCanvas.ValueNoise(wx / 6f, 1.5f, 0, 184);
            if (ly < damp - 1) p.H = (byte)Mathf.Max(p.H, ly < damp * 0.45f ? 2 : 1);
            else if (ly < damp + 0.5f && PixelCanvas.Hash(wx >> 1, 7, 185) > 0.45f && p.M == MPiedra) p.T += 1; // salitre

            // Chorreones bajo las molduras (imposta, techo, borde roto): franjas verticales que se afinan.
            int src = int.MaxValue;
            int corn = Cornisa(ref g);
            if (g.ATop > 40f && ly < g.S - 5) src = g.S - 5;
            else if (corn > 0 && ly < corn - 9) src = corn - 9;
            else if (ly < g.Span) src = g.Span;
            if (g.Ruinas && ruinTop < src) src = ruinTop;
            int dist = src - ly;
            if (dist > 0 && src != int.MaxValue)
            {
                int cell = FloorDiv(wx, 6);
                float hc = PixelCanvas.Hash(cell, src, 186);
                if (hc < (g.Ruinas ? 0.42f : 0.3f))
                {
                    float len = 14f + PixelCanvas.Hash(cell, src, 187) * (g.Ruinas ? 70f : 55f);
                    float cxw = cell * 6 + 2.5f + PixelCanvas.Hash(cell, src, 188) * 1.5f;
                    float wob = (PixelCanvas.ValueNoise(ly * 0.11f, cell, 0, 189) - 0.5f) * 2f;
                    float fade = 1f - dist / len;
                    float hw = 0.6f + 1.6f * fade;
                    if (fade > 0f && Mathf.Abs(wx + 0.5f - cxw - wob) < hw)
                    {
                        bool tail = fade < 0.18f && ((wx + ly) & 1) == 0;
                        if (!tail)
                        {
                            p.T -= 1;
                            if (fade > 0.55f) p.H = 2;
                            else p.H = (byte)Mathf.Max(p.H, 1);
                            if (dist <= 2 && moss > 0.2f && p.M == MPiedra) { p.M = MMusgo; p.T = 1; }
                        }
                    }
                }
            }

            // Verdín: musgo sobre las molduras (imposta y zócalo) en mechones de 1-3 px.
            if (moss > 0.05f && p.M == MPiedra)
            {
                int top = -1;
                if (ly >= g.Zh && ly < g.Zh + 3) top = g.Zh;
                else if (g.ATop > 40f && ly >= g.S + 3 && ly < g.S + 6) top = g.S + 3;
                else if (corn > 0 && ly >= corn + 6 && ly < corn + 9) top = corn + 6;
                if (top >= 0)
                {
                    int tuft = Mathf.FloorToInt(PixelCanvas.Hash(wx >> 1, top, 190) * 4f) - (PixelCanvas.Hash(wx >> 2, top, 191) < moss * 1.4f ? 0 : 9);
                    if (ly - top < tuft) { p.M = MMusgo; p.T = ly - top == 0 ? 2 : 1; p.A = 0.6f; }
                }
            }

            if (g.Z == Zone.Reef) Percebes(ref p, ref g);
            else if (g.Z == Zone.Coast) Redes(ref p, ref g);
        }

        /// <summary>Raíces (marrones, gruesas) y hebras de musgo que cuelgan del borde roto de las Ruinas.</summary>
        static bool Colgajos(int wx, int ly, float moss, out PxMuro p)
        {
            p = default;
            int col = FloorDiv(wx, 22);
            for (int c = col - 1; c <= col + 1; c++)
            {
                if (PixelCanvas.Hash(c, 3, 261) > 0.55f) continue;
                int top = RuinTop(c * 22);
                float d = top - 1 - ly;
                float len = 18f + PixelCanvas.Hash(c, 5, 263) * 75f;
                if (d < 0f || d > len) continue;
                float x0 = c * 22 + 4 + PixelCanvas.Hash(c, 4, 262) * 14f;
                float xc = x0 + 2.2f * Mathf.Sin(d * 0.13f + c) + d * (PixelCanvas.Hash(c, 6, 264) - 0.5f) * 0.3f;
                float th = d < len * 0.55f ? 1f : 0.5f;
                float off = wx + 0.5f - xc;
                if (off > -th && off < th)
                {
                    p = new PxMuro(off < 0f && th > 0.6f ? 2 : d > len - 3f ? 0 : 1, 0.75f, MRaiz);
                    return true;
                }
                // Raicillas laterales.
                int k = Mathf.FloorToInt(d) % 11;
                if (k >= 5 && k <= 7 && d < len - 6f)
                {
                    float dir = (Mathf.FloorToInt(d / 11f) & 1) == 0 ? 1f : -1f;
                    float o2 = (off - dir * (th + (k - 4))) * dir;
                    if (Mathf.Abs(o2) < 0.5f) { p = new PxMuro(1, 0.6f, MRaiz); return true; }
                }
            }
            // Hebras de musgo, finas y más numerosas.
            int cell = FloorDiv(wx, 5);
            if (PixelCanvas.Hash(cell, 9, 265) < moss * 0.6f)
            {
                int top = RuinTop(wx);
                float d = top - 1 - ly;
                float len = 4f + PixelCanvas.Hash(cell, 10, 266) * 18f;
                int xs = cell * 5 + 1 + Mathf.FloorToInt(PixelCanvas.Hash(cell, 11, 267) * 3f) + (d > len * 0.6f ? 1 : 0);
                if (d >= 0f && d < len && wx == xs) { p = new PxMuro(d > len - 2f ? 0 : 1, 0.5f, MMusgo); return true; }
            }
            return false;
        }

        /// <summary>Arrecife: percebes en racimos (más abajo, más densos), conchas incrustadas y coral que trepa desde el suelo.</summary>
        static void Percebes(ref PxMuro p, ref GeoMuro g)
        {
            if (p.M == MAbierto) return;
            int wx = g.Wx, ly = g.Ly;
            // Coral ramificado que sube desde el zócalo.
            int cc = FloorDiv(wx, 11);
            if (PixelCanvas.Hash(cc, 1, 271) < 0.35f)
            {
                float hgt = 10f + PixelCanvas.Hash(cc, 2, 272) * 30f;
                float x0 = cc * 11 + 5.5f;
                float xc = x0 + 1.5f * Mathf.Sin(ly * 0.2f + cc);
                if (ly < hgt && Mathf.Abs(wx + 0.5f - xc) < (ly < hgt * 0.5f ? 1f : 0.6f)) { p = new PxMuro(ly > hgt - 2f ? 4 : 2, 0.7f, MCoral); return; }
                for (int b = 0; b < 2; b++)
                {
                    float yb = hgt * (0.35f + b * 0.25f);
                    float dir = b == 0 ? -1f : 1f;
                    float dyb = ly - yb;
                    if (dyb >= 0f && dyb < hgt * 0.4f && Mathf.Abs(wx + 0.5f - (xc + dir * dyb * 0.7f)) < 0.6f) { p = new PxMuro(dyb > hgt * 0.4f - 2f ? 4 : 3, 0.65f, MCoral); return; }
                }
            }
            // Percebes.
            float dens = Mathf.Clamp01(0.6f - ly / 150f);
            int bx = FloorDiv(wx, 5), by = FloorDiv(ly, 5);
            if (PixelCanvas.Hash(bx, by, 273) < dens)
            {
                float px = bx * 5 + 1.2f + PixelCanvas.Hash(bx, by, 274) * 2.6f, py = by * 5 + 1.2f + PixelCanvas.Hash(bx, by, 275) * 2.6f;
                float dx = wx + 0.5f - px, dy = ly + 0.5f - py;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                if (r < 1.9f) { p = r < 0.8f ? new PxMuro(0, 0.3f) : new PxMuro((dx * LuzX + dy * LuzY) > 0f ? 6 : 3, 0.8f); return; }
            }
            // Conchas incrustadas.
            int sx = FloorDiv(wx, 13), sy = FloorDiv(ly, 11);
            if (PixelCanvas.Hash(sx, sy, 276) < 0.1f)
            {
                float cx = sx * 13 + 6.5f, cy = sy * 11 + 4f;
                float dx = wx + 0.5f - cx, dy = ly + 0.5f - cy;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                if (dy > -0.5f && r < 3.8f)
                {
                    float fa = Mathf.Atan2(dy, dx) * 4f / Mathf.PI;
                    fa -= Mathf.Floor(fa);
                    p = new PxMuro(r > 3f ? 4 : fa < 0.5f ? 3 : 1, 0.7f, MCoral);
                }
                else if (dy <= -0.5f && dy > -1.6f && Mathf.Abs(dx) < 1.5f) p = new PxMuro(2, 0.6f, MCoral); // charnela
            }
        }

        /// <summary>Costa: redes de pesca colgadas bajo la imposta, con su comba y nudos.</summary>
        static void Redes(ref PxMuro p, ref GeoMuro g)
        {
            if (p.M == MAbierto || PixelCanvas.Hash(g.BayI, g.Bx < Bay / 2 ? 0 : 1, 281) > 0.6f) return;
            int half = (g.Bx / 128) * 128;
            float u = (g.Bx - half) / 128f;
            float sag = 9f * Mathf.Sin(u * Mathf.PI);
            int yy = Mathf.RoundToInt(g.Ly + sag);
            if (yy < g.S - 48 || yy > g.S - 4) return;
            int a = ((g.Wx + yy) % 7 + 7) % 7, b = ((g.Wx - yy) % 7 + 7) % 7;
            if (a == 0 && b == 0) p = new PxMuro(4, 0.7f, MMadera);
            else if (a == 0 || b == 0) p = new PxMuro(3, 0.6f, MMadera);
        }

        /// <summary>Grietas: unas pocas por tramo, que bajan serpenteando y se ramifican. El labio derecho coge luz.</summary>
        static bool Grieta(int wx, int ly, int bay, int spanH, bool ruinas, out bool labio)
        {
            labio = false;
            int n = 2;
            for (int b = bay - 1; b <= bay + 1; b++)
            {
                for (int i = 0; i < n; i++)
                {
                    float hx = PixelCanvas.Hash(b, i, 201);
                    if (hx < (ruinas ? 0.3f : 0.4f)) continue;
                    float x0 = b * Bay + PixelCanvas.Hash(b, i, 202) * Bay;
                    float y0 = 30f + PixelCanvas.Hash(b, i, 203) * Mathf.Max(20f, spanH - 50f);
                    float len = (ruinas ? 40f : 26f) + PixelCanvas.Hash(b, i, 204) * (ruinas ? 80f : 60f);
                    float drift = (PixelCanvas.Hash(b, i, 205) - 0.5f) * 0.5f;
                    if (Rama(wx, ly, x0, y0, len, drift, b * 13 + i, ruinas, out bool l)) return true;
                    labio |= l;
                    // Rama secundaria desde un tercio del recorrido.
                    float yb = y0 - len * 0.35f;
                    float xb = x0 + (y0 - yb) * drift + Desvio(y0 - yb, b * 13 + i);
                    float d2 = drift + (drift >= 0f ? 0.7f : -0.7f);
                    if (Rama(wx, ly, xb, yb, len * 0.4f, d2, b * 13 + i + 7, false, out l)) return true;
                    labio |= l;
                }
            }
            return false;
        }

        static float Desvio(float d, int seed) => (PixelCanvas.ValueNoise(d / 6f, seed, 0, 206) - 0.5f) * 6f;

        static bool Rama(int wx, int ly, float x0, float y0, float len, float drift, int seed, bool gruesa, out bool labio)
        {
            labio = false;
            float d = y0 - ly;
            if (d < 0f || d > len) return false;
            float xc = x0 + d * drift + Desvio(d, seed);
            float off = wx + 0.5f - xc;
            float w = gruesa && d < len * 0.3f ? 1.1f : 0.55f;
            if (off > -w && off < w) return true;
            if (off >= w && off < w + 1f) labio = true;
            return false;
        }
    }
}
