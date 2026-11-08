# Kineret flight scene - mini map asset pack

Everything the mini map needs, as approved by the client. Every size is given as a
fraction of **D**, the on-screen diameter of the mini map circle, so the whole thing
scales with one number. The design was signed off at **D = 260 px at 1080p**
(520 px on the 3840x2160 kiosk).

`MM_Reference.png` is the target: it was rendered from the files in this folder and
nothing else, so if your build does not look like it, the difference is in the wiring.
`MM_Reference_InGame.jpg` shows it composited into a real 4K frame, bottom right.


## 1. Files

```
KineretMinimapBase.png     4922 x 5231  the whole terrain, stylised, square pixels
KineretMinimapBase.json    its world-space placement - read this before anything else
Sprites/MM_Mask.png        1024    white disc, for the UI Mask
Sprites/MM_Bezel.png       1024    dark ring + light rim, transparent middle
Sprites/MM_Glider.png       256    the aircraft, nose up
Sprites/MM_Marker.png       128    destination dot, white core + dark ring, tintable
Sprites/MM_NorthArrow.png    72x28 arrowhead, drawn pointing up
Sprites/MM_NorthLetter.png   56x64 the Hebrew letter tsadi, upright
Sprites/MM_LineSolid.png     16    stretch for the flown leg
Sprites/MM_DashCurrent.png  128x20 tile horizontally for the current leg
Sprites/MM_DashFuture.png   128x16 tile horizontally for the next leg
Sprites/MM_Spec.json        every number below, machine readable
MM_Reference.png            what it should look like
MM_Reference_InGame.jpg     the same, in a real frame
```


## 2. Unity import settings

**KineretMinimapBase.png** - Texture Type `Default`, **Max Size 8192** (the default
2048 would quietly throw away three quarters of it), Generate Mip Maps **on** (the map
is minified about 6x on screen; without mips it crawls), Wrap Mode `Clamp`,
Filter `Trilinear`, Compression `High Quality` / BC7.

**All sprites** - Texture Type `Sprite (2D and UI)`, Generate Mip Maps **off**,
Filter `Bilinear`, Compression `None` (they are tiny), Alpha Is Transparency **on**.
`MM_DashCurrent` and `MM_DashFuture` additionally need Wrap Mode `Repeat`, and an
Image with Image Type `Tiled`.


## 3. Where the map sits in the world

This is the only part that can go subtly wrong, so it is stated exactly.
From `KineretMinimapBase.json`:

```
metresPerPixel  25
worldXMin       5641.62        worldXMax  128691.62
worldZMin       -1975.98        worldZMax  128799.02
```

```csharp
// world XZ -> texture UV (Unity UV, v upwards)
float u = (worldX - worldXMin) / (worldXMax - worldXMin);
float v = (worldZ - worldZMin) / (worldZMax - worldZMin);
```

The rect is the texture's **outer edge**, not the centre of the first and last pixel,
so this still holds if the importer downscales the texture.

The map was built straight from `StreamingAssets/KineretTerrain/DEMData` (all 1558
tiles) using the terrain tile placement the game itself uses, not from lat/lon.
Checked against the engine's own Kineret mask (`KineretWaterShore.png`):
**IoU 0.9906, zero offset**. Do not re-derive this from latitude and longitude - the
lat/lon calibration in this project is roughly a kilometre out.


## 4. Hierarchy

```
MiniMap               RectTransform D x D
 +- Backing           Image, MM_Mask, colour #080D11   opaque disc behind everything
 +- MapClip           RectTransform D x D, Mask (Show Mask Graphic off)
 |   +- MapImage      Image, MM_Mask                  the mask shape itself
 |   +- MapRaw        RawImage, the basemap. Side = D * 1.4143, centred. Rotates.
 +- Routes            three Images, see section 6
 +- Markers           one Image per destination, MM_Marker, tinted
 +- Bezel             Image, MM_Bezel                 never rotates
 +- NorthArrow        Image, MM_NorthArrow            rotates with the bearing
 +- NorthLetter       Image, MM_NorthLetter           moves round, stays upright
 +- Glider            Image, MM_Glider                dead centre, never rotates
```

`MapRaw` is `sqrt(2) * D` on a side so that it still covers the circle at any rotation.


## 5. Heading up

The aircraft is fixed in the centre and always points up the screen; the map turns
under it. `heading` is the aircraft's compass bearing in degrees, that is
`transform.eulerAngles.y`.

```csharp
const float COVERAGE_M = 42900f;   // metres ACROSS the circle, see section 7
float diag = 1.41422f;             // MapRaw is sqrt(2)*D so it still covers when turned

// 1. the window of the world we want, centred on the aircraft
float spanM = COVERAGE_M * diag;
float uw = spanM / (worldXMax - worldXMin);
float vh = spanM / (worldZMax - worldZMin);
float u  = (plane.position.x - worldXMin) / (worldXMax - worldXMin);
float v  = (plane.position.z - worldZMin) / (worldZMax - worldZMin);
mapRaw.uvRect = new Rect(u - uw * 0.5f, v - vh * 0.5f, uw, vh);

// 2. turn it so the heading points up
mapRaw.rectTransform.localEulerAngles = new Vector3(0f, 0f, heading);
```

A destination, or anything else at a world position, goes on the circle like this:

```csharp
static Vector2 ToMiniMap(Vector3 world, Vector3 plane, float heading,
                         float D, float coverageM)
{
    float dx = world.x - plane.x, dz = world.z - plane.z;
    float dist = Mathf.Sqrt(dx * dx + dz * dz);
    float ang  = Mathf.Atan2(dx, dz) - heading * Mathf.Deg2Rad;  // bearing relative to nose
    float r    = dist / coverageM * D;                           // coverage is the DIAMETER
    return new Vector2(r * Mathf.Sin(ang), r * Mathf.Cos(ang));  // UI anchoredPosition
}
```

The north arrow is the same thing with a bearing of zero:

```csharp
float north = -heading * Mathf.Deg2Rad;
Vector2 dir = new Vector2(Mathf.Sin(north), Mathf.Cos(north));
arrow.rectTransform.anchoredPosition  = dir * (0.4755f * D);
arrow.rectTransform.localEulerAngles  = new Vector3(0f, 0f, heading);   // points outward
letter.rectTransform.anchoredPosition = dir * (0.4465f * D);
// the letter is NOT rotated - it stays upright wherever it ends up
```


## 6. The three legs

| leg | sprite | Image Type | colour | width |
|---|---|---|---|---|
| already flown | `MM_LineSolid` | Simple | #8CA6B6 at alpha 225/255 | 0.00667 x D |
| flying now | `MM_DashCurrent` | Tiled | #4FC3F7 | 0.00893 x D |
| still to come | `MM_DashFuture` | Tiled | #8CA6B6 at alpha 205/255 | 0.00667 x D |

Each is a rotated rectangle from one point to the next. The blue dashed leg starts at
the **aircraft**, not at the previous destination, so it always shows where you are
heading. Nothing else marks the current target - that was the client's decision.


## 7. Zoom

The circle covers a fixed **42.9 km across**. That comes from one rule: the furthest
of the three destinations has to sit at **0.385 x D** from the centre, which keeps it
clear of the bezel. To compute it per run instead:

```csharp
float coverageM = Mathf.Max(furthestDestinationMetres / 0.385f, 20000f);
```

Note that this is the **diameter**, not the radius.


## 8. Numbers

| what | fraction of D | px at D=260 (1080p) | px at D=520 (4K) | note |
|---|---|---|---|---|
| Minimap circle, outer diameter | 1.0000 | 260.0 | 520.0 | D |
| Bezel depth (dark ring) | 0.0660 | 17.2 | 34.3 | band |
| Rim stroke width | 0.0067 | 1.7 | 3.5 | rimWidth |
| Destination marker, outer diameter | 0.0610 | 15.9 | 31.7 | MM_Marker size |
| Glider sprite, square side | 0.1120 | 29.1 | 58.2 | MM_Glider size |
| North arrow sprite, width | 0.0713 | 18.5 | 37.1 | MM_NorthArrow width |
| North arrow sprite, height | 0.0266 | 6.9 | 13.8 | MM_NorthArrow height |
| North arrow, centre radius | 0.4755 | 123.6 | 247.2 | from circle centre |
| North letter, ink height | 0.0306 | 8.0 | 15.9 | MM_NorthLetter height |
| North letter, centre radius | 0.4465 | 116.1 | 232.2 | from circle centre |
| Flown leg, line width | 0.0067 | 1.7 | 3.5 |  |
| Current leg, line width | 0.0089 | 2.3 | 4.6 |  |
| Future leg, line width | 0.0067 | 1.7 | 3.5 |  |
| Current leg, dash period | 0.0520 | 13.5 | 27.0 | one tile |
| Future leg, dash period | 0.0480 | 12.5 | 25.0 | one tile |


## 9. Colours

| role | hex | alpha |
|---|---|---|
| backing disc | #080D11 | 255 |
| bezel | #090F14 | 185 |
| rim | #E2F0F8 | 165 |
| destination, collected | #4AE834 | 255 |
| destination, not yet reached | #A9BDCA | 255 |
| current leg | #4FC3F7 | 255 |
| flown leg | #8CA6B6 | 225 |
| future leg | #8CA6B6 | 205 |
| north arrow and letter | #FFEEAA | 255 |
| glider | #EAF6FD | 255 |

`MM_Bezel`, `MM_Mask` and `MM_Glider` already carry their colours - leave their
Image.color white. `MM_Marker`, the three line sprites, `MM_NorthArrow` and
`MM_NorthLetter` are white and are meant to be tinted through Image.color. The
marker's dark ring survives tinting because it is already almost black.


## 10. Still open

- **West edge of the map.** 24% of the DEM samples hold a filler value of 310.522 m
  instead of survey data - a band 25 to 40 km wide down the west side. Painted as
  terrain it reads as a real, suspiciously flat plateau, so it is filled with the
  backing colour #080D11 instead. The flag is read **per sample, not per tile**, and the
  boundary that falls out is the actual Mediterranean coastline - Haifa Bay and the
  Carmel headland are both recognisable - so there is no tile staircase. It is cleaned
  up with a small open/close, smoothed with a blur-and-rethreshold, and feathered by
  7 px purely as anti-aliasing. Note the game terrain there is still that flat
  plateau, so the map shows sea where the world has featureless ground - against the
  warm land tone it reads convincingly as sea. Nothing on the
  three-destination route goes near it - Eshkol, the westernmost destination, is about
  27 km clear. Say the word if you would rather see it as blue water than as a cut.
- **Outside the texture.** Wrap Mode `Clamp` repeats the edge pixels, and the outer
  edge is backing colour already, so it smears the backing colour. Good enough.
- **Position on screen.** Bottom right is what the reference frame shows. Bottom left
  works equally well. The top of the screen does not - it collides with the timer and
  the altitude ladder.
- **Labels and interest points.** Nothing is labelled at the moment, and only the
  three destinations are drawn.
- **Contrast of the two neutral legs.** The route colours were picked against the
  darkened version of the map. On the warm sand tone the flown leg and the next leg,
  both #8CA6B6, are faint; the blue current leg is fine. Something around `#44545F` holds
  up much better. Not changed here, because the colours above are what was approved.
