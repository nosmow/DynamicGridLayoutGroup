# Dynamic Grid Layout Group

A `GridLayoutGroup` for uGUI (Unity) that adapts to its content and to the screen size. It works like Unity's grid and adds what it's missing:

- **Spanning cells**: an item can take 2, 3… columns (or rows).
- **Centered last row** (or aligned left/right) when it isn't full.
- **Adaptive columns/rows**: between a minimum and a maximum, depending on the available width (or height).
- **Three ways to size cells**: fixed size, stretched to fill the width, or each column/row sized to its content.

Requires Unity 2021.3+ and `com.unity.ugui`. MIT licensed.

## Installation

**Package Manager → + → Install package from git URL…**

```
https://github.com/nosmow/DynamicGridLayoutGroup.git?path=/Assets/Frontend.UI.GridLayout#v1.0.0
```

`#v1.0.0` pins the version; change the number to update. You can also download the `.unitypackage` or `.tgz` from *Releases*.

**Demo scene** (optional): *Package Manager → Dynamic Grid Layout Group → Samples → Demo → Import*. It has one panel per feature; resize the Game view to see the grids adapt.

## Quick start

1. On a UI object, **Add Component → Layout → Dynamic Grid Layout Group**.
   If you already have a `GridLayoutGroup`, right-click it → **Convert to Dynamic Grid Layout Group**: its settings are kept.
2. Add the items as children, just like with Unity's grid.
3. To make a child span several columns, add **Layout → Grid Layout Cell** to it and raise its `Span`.

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

### Concepts

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
- A line that uses fewer slots than the widest one is a **partial line**. `Partial Line Alignment` decides whether it goes to the start, the center or the end.

### A layout pass

Unity calls the grid whenever its size or its children change. On every pass:

1. **Measure**: read each child's `span` (a single `TryGetComponent` per child). In `FitContent` it also reads the child's *preferred* size (`LayoutElement`, `Text`, TMP…).
2. **Choose how many columns/rows**:
   - `Fixed Column/Row Count`: the given number.
   - `Adaptive Constraint`: as many as fit in the rect using `Cell Size`, clamped to `[Min Count, Max Count]`.
   - `FitContent`: the largest count whose content fits, found with a binary search.
3. **Pack**: distribute the children into lines and slots (diagram above).
4. **Size the tracks** (columns and rows):
   - `Preserve`: all of them are `Cell Size`.
   - `Stretch`: columns (or rows) share the available space.
   - `FitContent`: each column is as wide as its widest child, each row as tall as its tallest child.
5. **Position**: apply padding, `Child Alignment`, the *Start Corner* (which may reverse the axes) and the partial-line alignment.

The grid also reports its own size (min/preferred), so it works inside a `ScrollRect` with a `ContentSizeFitter` or inside other layout groups.

### New inspector options

| Option | Values | What it does |
|---|---|---|
| **Partial Line Alignment** | `None`, `FollowChildAlignment`, `Start`, `Center`, `End` | Where partial lines go. `None` behaves like Unity's grid. |
| **Adaptive Constraint** | `None`, `Columns`, `Rows` | When set, replaces `Constraint`: the column or row count changes with the rect size. |
| **Min / Max Count** | ≥ 1 | Allowed range while `Adaptive Constraint` is set. |
| **Cell Size Mode** | `Preserve`, `Stretch`, `FitContent` | Fixed size, stretch to fill, or size from content. |

### Common setups

| I want… | Settings |
|---|---|
| An inventory with 3 to 6 columns that fills the width | `Adaptive Constraint = Columns`, `Min 3`, `Max 6`, `Cell Size Mode = Stretch` |
| A gallery with a centered last row | `Fixed Column Count`, `Partial Line Alignment = Center` |
| Wider featured cards | `GridLayoutCell.span = 2` on those cards |
| Tags or buttons of different sizes aligned in columns | `Adaptive Constraint = Columns`, `Cell Size Mode = FitContent`, and a `LayoutElement` (or `Text`) on each child |

### API

```csharp
int columns = grid.columnCount;                // result of the last pass
int rows = grid.rowCount;
float width = grid.GetColumnWidth(0);          // actual size of each column/row
float height = grid.GetRowHeight(1);
Vector2 cell = grid.resolvedCellSize;          // size used in Preserve/Stretch
grid.TryGetCellCoordinates(index, out Vector2Int coords); // (column, row) of a child
```

## Package layout

```
Assets/Frontend.UI.GridLayout/
├── package.json
├── Runtime/                       ← the component (included in builds)
│   ├── DynamicGridLayoutGroup.cs  ← component: settings, API and uGUI callbacks
│   ├── GridLayoutCell.cs          ← optional per-child span component
│   ├── GridLayoutSolver.cs        ← steps 1-4 of the layout pass
│   ├── GridCountResolver.cs       ← how many columns/rows and slots per line
│   ├── GridLayoutInput.cs         ← min/preferred size reported by the grid
│   ├── GridCellPlacement.cs       ← step 5: final position of each cell
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

The component only holds the settings and talks to uGUI; all the math lives in separate plain C# classes. Buffers are reused between passes, so rebuilding the layout doesn't allocate garbage.

## Limitations

- A `span` larger than the slots of a line is clamped to the available slots.
- With `Fixed Row Count` and a horizontal axis (or `Fixed Column Count` and a vertical axis), if the spans don't fit there can be more lines than requested.
- `Stretch` has no effect with `Constraint = Flexible`: there is no constrained axis to fill.
- In `FitContent`, the binary search assumes that more columns take more width. With very irregular spans it may pick one column fewer than the optimum.

Detailed docs for every option: [`Assets/Frontend.UI.GridLayout/README.md`](Assets/Frontend.UI.GridLayout/README.md) · Changes: [`CHANGELOG.md`](Assets/Frontend.UI.GridLayout/CHANGELOG.md)

## License

[MIT](LICENSE) © nosmow
