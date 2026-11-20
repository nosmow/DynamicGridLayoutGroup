# Dynamic Grid Layout Group

Drop-in replacement for uGUI's `GridLayoutGroup` with:

- **Cell spans**: a cell can take several slots along the *Start Axis* (columns if `Horizontal`, rows if `Vertical`) with the `GridLayoutCell` component.
- **Partial line alignment** (`Partial Line Alignment`): e.g. center the last row.
- **Adaptive columns/rows** (`Adaptive Constraint`): the column or row count follows the rect size, clamped to `Min Count`–`Max Count`.
- **Cell sizing** (`Cell Size Mode`): `Preserve`, `Stretch` (fill the width/height) or `FitContent` (each column/row sized to its content).
- **Query API**: `columnCount`, `rowCount`, `GetColumnWidth`, `GetRowHeight`, `TryGetCellCoordinates(index, out Vector2Int)`.
- No allocations per rebuild and a single `TryGetComponent` per child per rebuild.

## Installation

**Git (UPM)**: *Package Manager → + → Install package from git URL…*, or in `Packages/manifest.json`:

```json
"com.frontend.ui-grid-layout": "https://github.com/nosmow/DynamicGridLayoutGroup.git#v1.0.0"
```

`#v1.0.0` pins the version to a tag; without it the default branch is used.

**Tarball (`.tgz`)**: *Package Manager → + → Install package from tarball…*

**`.unitypackage`**: *Assets → Import Package → Custom Package…* It imports the package together with the demo scene.

**Demo**: with UPM, *Package Manager → Dynamic Grid Layout Group → Samples → Demo → Import*.

## Usage

1. Add `Layout > Dynamic Grid Layout Group` (or, on an existing `GridLayoutGroup`, right-click the component → *Convert to Dynamic Grid Layout Group*, which keeps its settings).
2. On children that should take more than one slot, add `Layout > Grid Layout Cell` and set `Span`.

```csharp
grid.partialLineAlignment = GridLineAlignment.Center;
item.GetComponent<GridLayoutCell>().span = 2;
```

### Partial Line Alignment

| Value | Behaviour |
|---|---|
| `None` | Same as `GridLayoutGroup` (packed towards the *Start Corner*). |
| `FollowChildAlignment` | Uses the axis component of `Child Alignment`. Default value. |
| `Start` / `Center` / `End` | Left/center/right (or top/center/bottom on vertical axes) inside the grid block. |

Applies to every line that uses fewer slots than the widest one; without spans that only happens on the last line.

### Adaptive Constraint

| Value | Behaviour |
|---|---|
| `None` | Uses the regular `Constraint` / `Constraint Count`. |
| `Columns` | Columns = as many as fit in the rect width, clamped to `[Min Count, Max Count]`. |
| `Rows` | Rows = as many as fit in the rect height, clamped to `[Min Count, Max Count]`. |

When active it replaces `Constraint` (the inspector hides it). The grid is recalculated automatically when the rect size changes. If not even `Min Count` cells fit, the content overflows. As a layout element it reports `min` = `Min Count` cells and `preferred` = `Max Count` cells (never more than the number of items), so it works with a `ContentSizeFitter` or inside another layout group.

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
| `FitContent` | Variable sizes: each column is as wide as its widest cell and each row as tall as its tallest cell (*preferred* size from `LayoutElement`, `Text`, etc.). |

With `Stretch`, `Cell Size` still decides how many columns or rows fit; then the space is shared between them. It works with `Adaptive Constraint` and with `Fixed Column/Row Count`. With `Flexible` there is no constrained axis and it has no effect. The size actually used is exposed as `resolvedCellSize`.

```csharp
grid.cellSizeMode = GridCellSizeMode.Stretch;
```

#### FitContent (dynamic columns with variable sizes)

- Columns line up like a table: each one is as wide as its widest cell, and each row as tall as its tallest cell. The child is stretched to its cell.
- With `Adaptive Constraint` (or `Flexible`) the grid uses the **largest column/row count whose content fits** in the rect, within `[Min Count, Max Count]`. The search is binary: O(n log range) per pass.
- With `Fixed Column/Row Count` the count is fixed and only the sizes change.
- A spanning cell that doesn't fit in the columns it takes spreads the missing space evenly across them.
- `Cell Size` is only used for cells without a preferred size (e.g. an `Image` without a sprite).
- `GetColumnWidth(i)` and `GetRowHeight(i)` return the computed sizes.

```csharp
grid.adaptiveConstraint = GridAdaptiveConstraint.Columns;
grid.minCount = 2;
grid.maxCount = 8;
grid.cellSizeMode = GridCellSizeMode.FitContent;
// On each child: LayoutElement.preferredWidth / preferredHeight (or a Text/TMP that provides them).
```

## Code structure

Every script is under 200 lines and each class has a single responsibility.

| File | Responsibility |
|---|---|
| `DynamicGridLayoutGroup.cs` | Component: settings, public API and uGUI callbacks. Delegates the math. |
| `GridLineAlignment.cs`, `GridAdaptiveConstraint.cs`, `GridCellSizeMode.cs` | Public enums. |
| `GridSettings.cs` | Immutable snapshot of the settings and rect size for one pass. |
| `GridLayoutSolver.cs` | Layout pass: measure, choose the column/row count, pack and size. |
| `GridCountResolver.cs` | Constraint/adaptive → column/row count and slots per line; cell size in `Stretch`. |
| `GridLayoutInput.cs` | Min/preferred size the grid reports as a layout element. |
| `GridCellPlacement.cs` | Final position: *Start Corner* mirroring and partial-line alignment. |
| `GridChildMetrics.cs` | Children's spans and *preferred* sizes. |
| `GridLines.cs` | Packing cells into lines (`GridCell`). |
| `GridTracks.cs` | Column/row sizes and offsets. |
| `GridLayoutCell.cs` | Per-child span component. |

## Notes

- `Span` is clamped to the slots available per line; if it doesn't fit in the current line it moves to the next one.
- With `Fixed Row Count` + horizontal axis (or `Fixed Column Count` + vertical axis) and spans, slots per line are computed as `ceil(total slots / constraintCount)`; if the spans don't fit there may be more lines than the constraint.
- With `Flexible` the slot count depends on the current rect size (same as Unity).

## License

[MIT](../LICENSE.md).
