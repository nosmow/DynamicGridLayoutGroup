# Dynamic Grid Layout Group — Documentation

Drop-in replacement for uGUI's `GridLayoutGroup` with cell spans, partial-line alignment, an adaptive column/row count and three cell sizing modes.

## Contents

1. [Installation](#installation)
2. [Getting started](#getting-started)
3. [How it works](#how-it-works)
4. [Options](#options)
   - [Partial Line Alignment](#partial-line-alignment)
   - [Adaptive Constraint](#adaptive-constraint)
   - [Cell Size Mode](#cell-size-mode)
5. [Common setups](#common-setups)
6. [Scripting API](#scripting-api)
7. [Package structure](#package-structure)
8. [Limitations](#limitations)

## Installation

**Git (UPM)**: *Package Manager → + → Install package from git URL…*

```
https://github.com/nosmow/DynamicGridLayoutGroup.git#v1.0.0
```

Or in `Packages/manifest.json`:

```json
"com.frontend.ui-grid-layout": "https://github.com/nosmow/DynamicGridLayoutGroup.git#v1.0.0"
```

`#v1.0.0` pins the version to a tag; change it to update. Without it the default branch is used.

**Tarball (`.tgz`)**: download it from *Releases*, then *Package Manager → + → Install package from tarball…*

**`.unitypackage`**: download it from *Releases*, then *Assets → Import Package → Custom Package…* It imports the package together with the demo scene.

**Demo scene** (optional): *Package Manager → Dynamic Grid Layout Group → Samples → Demo → Import*. It has one panel per feature; resize the Game view to see the grids adapt.

Requires Unity 2021.3+ and `com.unity.ugui`.

## Getting started

1. On a UI object, **Add Component → Layout → Dynamic Grid Layout Group**.
   On an existing `GridLayoutGroup`, right-click the component → **Convert to Dynamic Grid Layout Group** (its settings are kept).
2. Add the items as children, just like with Unity's grid.
3. To make a child take several columns, add **Layout → Grid Layout Cell** to it and raise its `Span`.

```csharp
using Frontend.UI;

var grid = GetComponent<DynamicGridLayoutGroup>();
grid.adaptiveConstraint = GridAdaptiveConstraint.Columns; // column count follows the width
grid.minCount = 2;
grid.maxCount = 6;
grid.cellSizeMode = GridCellSizeMode.Stretch;              // fill the width
grid.partialLineAlignment = GridLineAlignment.Center;      // center the last row

item.AddComponent<GridLayoutCell>().span = 2;              // this child takes 2 columns
```

## How it works

### Lines and slots

The grid places its children in **lines** along the *Start Axis*. With `Start Axis = Horizontal` lines are rows and each line is split into **slots** (columns); with `Vertical` it's the other way round.

```
Start Axis = Horizontal, 4 slots per line

        slot 0   slot 1   slot 2   slot 3
line 0  [  A  ] [      B (span 2)  ] [ C ]
line 1  [  D  ] [  E  ] [      F (span 2) ]
line 2          [  G  ] [  H  ]             ← partial line, centered
```

- Each child takes `span` consecutive slots (1 by default).
- If a child doesn't fit in what's left of the line, it moves to the next one.
- A line that uses fewer slots than the widest one is a **partial line**. `Partial Line Alignment` decides where it goes.

### A layout pass

Unity calls the grid whenever its size or its children change. On every pass:

1. **Measure**: read each child's `span` (a single `TryGetComponent` per child). In `FitContent` it also reads the child's *preferred* size (`LayoutElement`, `Text`, TMP…).
2. **Choose how many columns/rows**:
   - `Fixed Column/Row Count`: the given number.
   - `Adaptive Constraint`: as many as fit in the rect using `Cell Size`, clamped to `[Min Count, Max Count]`.
   - `FitContent`: the largest count whose content fits, found with a binary search.
3. **Pack**: distribute the children into lines and slots.
4. **Size the tracks** (columns and rows) according to `Cell Size Mode`.
5. **Position**: apply padding, `Child Alignment`, the *Start Corner* (which may reverse the axes) and the partial-line alignment.

The grid also reports its own size (min/preferred), so it works inside a `ScrollRect` with a `ContentSizeFitter` or inside other layout groups. Buffers are reused between passes, so rebuilding the layout doesn't allocate garbage.

## Options

Besides Unity's usual grid settings (`Padding`, `Cell Size`, `Spacing`, `Start Corner`, `Start Axis`, `Child Alignment`, `Constraint`), the inspector adds:

| Option | Values | What it does |
|---|---|---|
| **Partial Line Alignment** | `None`, `FollowChildAlignment`, `Start`, `Center`, `End` | Where partial lines go. |
| **Adaptive Constraint** | `None`, `Columns`, `Rows` | When set, replaces `Constraint`: the column or row count changes with the rect size. |
| **Min / Max Count** | ≥ 1 | Allowed range while `Adaptive Constraint` is set. |
| **Cell Size Mode** | `Preserve`, `Stretch`, `FitContent` | Fixed size, stretch to fill, or size from content. |

### Partial Line Alignment

| Value | Behaviour |
|---|---|
| `None` | Same as `GridLayoutGroup` (packed towards the *Start Corner*). |
| `FollowChildAlignment` | Uses the axis component of `Child Alignment`. Default value. |
| `Start` / `Center` / `End` | Left/center/right (or top/center/bottom on vertical axes) inside the grid block. |

Applies to every line that uses fewer slots than the widest one; without spans that only happens on the last line.

```csharp
grid.partialLineAlignment = GridLineAlignment.Center;
```

### Adaptive Constraint

| Value | Behaviour |
|---|---|
| `None` | Uses the regular `Constraint` / `Constraint Count`. |
| `Columns` | Columns = as many as fit in the rect width, clamped to `[Min Count, Max Count]`. |
| `Rows` | Rows = as many as fit in the rect height, clamped to `[Min Count, Max Count]`. |

- When active it replaces `Constraint` (the inspector hides it) and the grid is recalculated when the rect size changes.
- If not even `Min Count` cells fit, the content overflows.
- As a layout element it reports `min` = `Min Count` cells and `preferred` = `Max Count` cells (never more than the number of items), so it works with a `ContentSizeFitter` or inside another layout group.

```csharp
grid.adaptiveConstraint = GridAdaptiveConstraint.Columns;
grid.minCount = 2;
grid.maxCount = 6;
```

### Cell Size Mode

| Value | Behaviour |
|---|---|
| `Preserve` | Cells keep `Cell Size`; leftover space is distributed by `Child Alignment`. |
| `Stretch` | Cells grow (or shrink) along the constrained axis to fill the rect: the width for columns, the height for rows. |
| `FitContent` | Variable sizes: each column is as wide as its widest cell and each row as tall as its tallest cell. |

**Stretch**

- `Cell Size` still decides how many columns or rows fit; then the space is shared between them.
- Works with `Adaptive Constraint` and with `Fixed Column/Row Count`. With `Flexible` there is no constrained axis and it has no effect.
- The size actually used is exposed as `resolvedCellSize`.

**FitContent** (dynamic columns with variable sizes)

- Columns line up like a table. Each child is stretched to its cell.
- Sizes come from the children's *preferred* size: a `LayoutElement`, a `Text`/TMP, etc. `Cell Size` is only used for children without one (e.g. an `Image` without a sprite).
- With `Adaptive Constraint` (or `Flexible`) the grid uses the **largest column/row count whose content fits** in the rect, within `[Min Count, Max Count]`. The search is binary: O(n log range) per pass.
- With `Fixed Column/Row Count` the count is fixed and only the sizes change.
- A spanning cell that doesn't fit in the columns it takes spreads the missing space evenly across them.

```csharp
grid.adaptiveConstraint = GridAdaptiveConstraint.Columns;
grid.minCount = 2;
grid.maxCount = 8;
grid.cellSizeMode = GridCellSizeMode.FitContent;
// On each child: LayoutElement.preferredWidth / preferredHeight (or a Text/TMP that provides them).
```

## Common setups

| I want… | Settings |
|---|---|
| An inventory with 3 to 6 columns that fills the width | `Adaptive Constraint = Columns`, `Min 3`, `Max 6`, `Cell Size Mode = Stretch` |
| A gallery with a centered last row | `Fixed Column Count`, `Partial Line Alignment = Center` |
| Wider featured cards | `GridLayoutCell.span = 2` on those cards |
| Tags or buttons of different sizes aligned in columns | `Adaptive Constraint = Columns`, `Cell Size Mode = FitContent`, and a `LayoutElement` (or `Text`) on each child |

## Scripting API

Namespace `Frontend.UI`.

**`DynamicGridLayoutGroup`** (inherits `GridLayoutGroup`)

| Member | Description |
|---|---|
| `partialLineAlignment` | `GridLineAlignment` used for partial lines. |
| `adaptiveConstraint` | `GridAdaptiveConstraint`: `None`, `Columns` or `Rows`. |
| `minCount` / `maxCount` | Range for the adaptive constraint. Setting `minCount` above `maxCount` raises `maxCount`. |
| `cellSizeMode` | `GridCellSizeMode`: `Preserve`, `Stretch` or `FitContent`. |
| `columnCount` / `rowCount` | Columns and rows produced by the last layout pass. |
| `GetColumnWidth(int)` / `GetRowHeight(int)` | Actual size of a column/row in the last pass (0 if out of range). |
| `resolvedCellSize` | Cell size used in `Preserve`/`Stretch`. |
| `TryGetCellCoordinates(int, out Vector2Int)` | Logical (column, row) of a laid-out child, before start-corner mirroring. |

**`GridLayoutCell`**

| Member | Description |
|---|---|
| `span` | Slots taken along the start axis (≥ 1). Changing it rebuilds the layout. |

## Package structure

```
├── package.json
├── README.md · CHANGELOG.md · LICENSE.md
├── Documentation~/                ← this document
├── Runtime/                       ← the component (included in builds)
│   ├── DynamicGridLayoutGroup.cs  ← component: settings, API and uGUI callbacks
│   ├── GridLayoutCell.cs          ← optional per-child span component
│   ├── GridLayoutSolver.cs        ← layout pass: measure, choose count, pack, size
│   ├── GridCountResolver.cs       ← how many columns/rows and slots per line
│   ├── GridLayoutInput.cs         ← min/preferred size reported by the grid
│   ├── GridCellPlacement.cs       ← final position of each cell
│   ├── GridChildMetrics.cs        ← children's spans and preferred sizes
│   ├── GridLines.cs               ← packing into lines and slots
│   ├── GridTracks.cs              ← column/row sizes and offsets
│   ├── GridSettings.cs            ← settings snapshot for one pass
│   └── Grid*.cs (enums)           ← GridLineAlignment, GridAdaptiveConstraint, GridCellSizeMode
├── Editor/                        ← editor only (not included in builds)
│   ├── DynamicGridLayoutGroupEditor.cs  ← inspector
│   └── GridLayoutGroupConversion.cs     ← "Convert to Dynamic Grid Layout Group" menu
└── Samples~/Demo/                 ← demo scene (imported from the Package Manager)
```

The component only holds the settings and talks to uGUI; all the math lives in separate plain C# classes, each under 200 lines with a single responsibility.

## Limitations

- A `span` larger than the slots of a line is clamped to the available slots.
- With `Fixed Row Count` and a horizontal axis (or `Fixed Column Count` and a vertical axis), slots per line are `ceil(total slots / constraintCount)`; if the spans don't fit there can be more lines than requested.
- With `Flexible` the slot count depends on the current rect size (same as Unity).
- `Stretch` has no effect with `Constraint = Flexible`.
- In `FitContent`, the binary search assumes that more columns take more width. With very irregular spans it may pick one column fewer than the optimum.

---

[MIT](../LICENSE.md) © nosmow · [Changelog](../CHANGELOG.md) · [Back to README](../README.md)
