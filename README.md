![SimpleScroll — Lists, Grids, and Carousels for Unity uGUI](docs/images/SimpleScroll-banner.png)

**Virtualized lists, grids, and carousels for Unity uGUI.**

Choose a component for your layout, provide the data, and let SimpleScroll reuse the cells as you scroll.

[日本語](README.ja.md) · [Installation](#installation) · [Quick start](#quick-start) · [Demos](#demos)

## Why SimpleScroll?

- **A component for each layout.** Fixed-size lists, variable-size lists, measured layouts, grids, and carousels.
- **Cell virtualization and pooling.** Create cells for the visible region and reuse them as items move out of view.
- **Your data, your prefabs.** Bind through a small data-source interface, with support for multiple cell types.
- **Horizontal or vertical.** Configure the axis, dragging, inertia, and mouse-wheel scrolling in the Inspector.

## Choose your component

| Component | Cell size | Best suited to |
| --- | --- | --- |
| `FixedListScroll` | One fixed size along the scroll axis | Menus, rankings, uniform rows |
| `SizedListScroll` | Size provided for each item | Feeds and lists with known variable sizes |
| `LazyLayoutListScroll` | Measured from each cell's RectTransform | Chat bubbles and content-driven layouts |
| `FixedGridScroll` | One fixed width and height | Inventories, shops, galleries |
| `CarouselScroll` | One fixed size along the scroll axis | Center-snapping cards and looping sliders |

Use `FixedListScroll` when all rows share a size. Use `SizedListScroll` when sizes are available from your data. Use `LazyLayoutListScroll` when the UI must be laid out to determine its size.

## Installation

The package declares Unity **2022.1** as its minimum version. It uses **uGUI**; ensure Unity UI is installed in your project.

In Unity's Package Manager, choose **Add package from git URL** and enter:

```text
https://github.com/kurobon-jp/SimpleScroll.git?path=Assets/
```

## Quick start

### 1. Set up the view

The easiest starting point is the `FixedListScroll` sample. For a custom view:

1. Create a uGUI hierarchy under a Canvas, with a viewport and a child content RectTransform.
2. Add `FixedListScroll` to the scroll-view object and assign **Viewport** and **Content**.
3. Set the scroll axis and cell size. Match the prefab's size along that axis to the configured cell size.
4. Add a `RectMask2D` or `Mask` to the viewport to clip cells. Ensure the view has a raycastable UI Graphic and the scene has an EventSystem for input.

SimpleScroll handles cell positioning. Let it manage placement rather than adding a Layout Group to the content or using a ScrollRect to move the same content. Layout components inside a cell can still be used with `LazyLayoutListScroll`.

### 2. Provide the data

Save this as `SimpleListExample.cs`, attach it to a GameObject, and assign the scroll component and cell prefab. The prefab should have a RectTransform and a child uGUI `Text`.

```csharp
using SimpleScroll;
using UnityEngine;
using UnityEngine.UI;

public class SimpleListExample : MonoBehaviour, IDataSource
{
    [SerializeField] private FixedListScroll _scroll;
    [SerializeField] private GameObject _cellPrefab;
    [SerializeField] private int _count = 100;

    private void Start()
    {
        _scroll.SetDataSource(this);
        _scroll.Refresh();
    }

    public int GetDataCount() => _count;
    public GameObject GetCellView(int index) => _cellPrefab;
    public int GetCellViewType(int index) => 0;

    public void SetData(int index, GameObject cell)
    {
        cell.GetComponentInChildren<Text>().text = $"Item {index}";
    }
}
```

For a real project, bind through your own cell component; the `Text` lookup above keeps the example self-contained.

### 3. Update and navigate

Call `Refresh()` after changing data to rebind visible cells and recalculate the layout. Items are positioned during the scroll component's `LateUpdate`.

```csharp
_scroll.Refresh();
_scroll.SetPositionIndex(20, anchor: 0.5f, smooth: true);
```

For lists, `anchor` specifies alignment within the viewport: `0` for the start, `0.5` for the center, and `1` for the end. The target is constrained by the available scroll range.

To refresh at a normalized position, use `Refresh(0f)` for the start or `Refresh(1f)` for the end. Call `Refresh()` before navigating after a data change so size and offset information is up to date.

#### Refresh parameters

```csharp
void Refresh(float normalizedPosition, bool isRefreshVisibleCells = true)
```

Recalculate the layout and move to the specified normalized position.

| Parameter | Description |
| --- | --- |
| `normalizedPosition` | Position within the available scroll range (`0`–`1`): `0` is the start and `1` is the end. |
| `isRefreshVisibleCells` | When `true` (the default), also rebind data to visible cells. When `false`, keep visible cells and recalculate the layout. Use `true` if their displayed content has changed. |

Use `Refresh(bool isRefreshVisibleCells = true)` to update without specifying a normalized position.

`LazyLayoutListScroll` calculates the total length using estimated sizes for unmeasured cells. For `normalizedPosition` values other than `0` and `1`, the resulting position may differ from the position obtained after measuring all cells. Use `SetPositionIndex` when you want to navigate to a specific item.

## Data sources and cell reuse

| Method | Responsibility |
| --- | --- |
| `GetDataCount()` | Return the number of items. |
| `GetCellView(index)` | Return the prefab for this item. |
| `GetCellViewType(index)` | Return the pool key for this item's cell type; the default is `0`. |
| `SetData(index, cell)` | Bind the item to a created or reused cell. |

For multiple prefabs, return a distinct type ID for each kind of cell and choose the matching prefab in `GetCellView`. Cells with the same type ID share a pool.

A reused cell may have displayed a different item. In `SetData`, update all item-dependent state, including text, images, selection, and callbacks. Replace old item callbacks rather than adding another listener on every bind.

`SizedListScroll` uses `ISizedDataSource`, which adds `GetCellViewSize(index)` for the cell's length along the scroll axis, excluding spacing.

`LazyLayoutListScroll` uses `ILazyLayoutDataSource`. Derive from `LazyLayoutDataSource<T>` for the built-in item collection and size/offset bookkeeping; implement `SetData`, `GetCellView`, and `GetCellViewType`. See [LazyLayoutListScrollDemo.cs](Assets/Samples~/Scripts/LazyLayoutListScrollDemo.cs).

## Layout details

### Grids

`FixedGridScroll.Column` controls the number of cells across the scroll axis: columns for vertical scrolling, rows for horizontal scrolling.

### Carousels

`CarouselScroll` snaps cells to the center and can loop. Navigate with `Next()`, `Prev()`, or `SetPositionIndex(index, smooth)`.

When looping, the position index can be negative or exceed the data count. Use `IDataSource.GetDataIndex(positionIndex)` to map it to a valid data index in **all** index-based data-source methods. `OnSelected` also reports the position index.

`OnReposition` supplies the cell, position index, and signed distance from the viewport center (normalized by half the viewport size) for effects such as scaling. See [CarouselScrollDemo.cs](Assets/Samples~/Scripts/CarouselScrollDemo.cs).

### Measured layouts

`LazyLayoutListScroll` starts with estimated sizes and measures cells as they appear. The content length and scrollbar can change as estimates are replaced by actual sizes. Measurement requires layout rebuilds and offset recalculation; prefer `SizedListScroll` when you already know the sizes.

## Component parameters

These tables describe the Inspector fields. Sizes and spacing use RectTransform local coordinate units.

### Common settings

| Parameter | Description |
| --- | --- |
| **Axis** | Select `Horizontal` or `Vertical`. |
| **Viewport** | The visible area's RectTransform. If unassigned, the component uses its own RectTransform. |
| **Content** | The parent RectTransform for cells. Assign a child of the viewport. |
| **Scrollable** | Accept scroll input, such as the mouse wheel. |
| **Scrolling / Scroll Sensitivity** | Movement per scroll input. Higher values increase movement; for carousels, this affects how many items are advanced. |
| **Draggable** | Accept pointer dragging. |
| **Dragging / Inertia** | Enable inertial movement after releasing a drag. |
| **Dragging / Deceleration** | Inertia deceleration (px/sec²). Higher values stop movement over a shorter distance. Default: `10000`. |
| **Dragging / Max Velocity** | Maximum inertia speed (px/sec). Default: `10000`. |
| **Dragging / Drag Sensitivity** | Drag movement multiplier (`0.1`–`1`). At `1`, movement matches the pointer. |
| **Dragging / Over Scroll** | Allow movement beyond the ends of the scroll range. |
| **On Value Changed** | Event reporting changes in scroll position. The value is a signed scroll coordinate, rather than a normalized position: negative from the start horizontally, positive vertically. |

### Shared list and grid settings

| Parameter | Description |
| --- | --- |
| **Content Padding / Start, End** | Padding before and after the cells along the scroll axis: top/bottom vertically, left/right horizontally. |
| **Space** | Gap between cells. Lists use a single value along the scroll axis; grids use `X` (horizontal) and `Y` (vertical). |
| **Scrollbar** | Optional uGUI Scrollbar. Its value and handle size are synchronized, and its GameObject is hidden when scrolling is unnecessary. Match its direction to the scroll axis, with value `0` at the start. |

### FixedListScroll

| Parameter | Description |
| --- | --- |
| **Cell Size** | Shared cell size along the scroll axis, excluding spacing. Match the prefab's size to this value. |

### LazyLayoutListScroll

| Parameter | Description |
| --- | --- |
| **Estimated Cell Size** | Estimated size along the scroll axis for unmeasured cells. Default: `100`. A value close to actual cell sizes reduces changes in content length and scrollbar size after measurement. Excludes spacing. |

### FixedGridScroll

| Parameter | Description |
| --- | --- |
| **Cell Size / X, Y** | Shared cell width and height, excluding spacing. Match the prefab's size to these values. |
| **Column** | Number of cells across the scroll axis: columns for vertical scrolling, rows for horizontal scrolling. Set to at least `1`. |

### CarouselScroll

| Parameter | Description |
| --- | --- |
| **Cell Size** | Shared cell size along the scroll axis. Match the prefab's size to this value. |
| **Space** | Gap along the scroll axis. The distance between cell centers is `Cell Size + Space`. |
| **Loop** | Enable wrapping from the last item to the first and vice versa. When disabled, movement stops at the first and last cells. |
| **Indicator** | Optional uGUI Toggle template. A copy is created for each data item under the same parent, and `isOn` marks the selected item. The original template is hidden. |

Carousels use center snapping and do not use Content Padding. Keep cell sizes positive, and ensure `Cell Size + Space` is also positive.

## Demos

### FixedListScroll

![](docs/images/FixedListScroll.gif)

### SizedListScroll

![](docs/images/SizedListScroll.gif)

### FixedGridScroll

![](docs/images/FixedGridScroll.gif)

### LazyLayoutListScroll

![](docs/images/LazyLayoutListScroll.gif)

### CarouselScroll

![](docs/images/CarouselScroll.gif)

### CarouselScroll loop

![](docs/images/CarouselScroll_loop.gif)

## License

[MIT](LICENSE)
