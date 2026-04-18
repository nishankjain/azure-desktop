---
applyTo: "**/*.cs, **/*.xaml"
description: "App-specific design decisions, patterns, and conventions for Azure Desktop"
---

# Azure Desktop — App Design Decisions

## Layout & Spacing
- Content area padding: `24,5,24,24` (left, top, right, bottom) — centralized in MainWindow
- Breadcrumb bottom margin: 24px (separates breadcrumb from page content)
- Page titles: FontSize 20, FontWeight SemiBold, Margin `0,0,0,16`
- No per-page padding — all controlled by MainWindow content Grid
- Nav pane top padding: 3px (from `NavigationViewPaneContentGridMargin` `-1,3`)
- Content top padding: 5px (= nav pane 3px + item button 2px, for alignment)

## Title Bar
- Height: 48px (`PreferredHeightOption.Tall`)
- Button order: Back > Hamburger > Home
- Button width: 48px each
- Icon size: 14px for all title bar FontIcons
- Caption button hover: `#20FFFFFF`, press: `#10FFFFFF`, transparent default
- Hamburger always visible, back/home contextual
- `ExtendsContentIntoTitleBar` with custom `AppTitleBar`

## Navigation
- All pages receive `NavigationContext` as parameter — no tuples, no `SubscriptionItem` directly
- Breadcrumbs show **hierarchy only**, not navigation — use nav sidebar items for navigation between views
- `NavigationContext` fields: `Subscription`, `ResourceGroupName`, `ResourceGroupLocation`, `Resource`, `Section`, `DetailItemName`, `Feature`, `ResourceProvider`, `PageLabel`
- `NavigationContext.BuildBreadcrumbChain()` derives breadcrumbs automatically from populated fields
- `BreadcrumbControl` is in MainWindow, not in individual pages — pages never touch breadcrumbs
- Nav items by scope:
  - Subscription: Overview, Resource Groups, Tags, Locks, Preview Features
  - Resource Group: Overview, Resources, Tags, Locks
  - Resource: Overview (+ type-specific sections), Tags, Locks
  - AppGw: Overview + 12 section items, Tags, Locks

## Breadcrumb
- Font: 24px SemiBold, `LineHeight="24"`
- Text margin: `0,-3,0,0` (cancels BreadcrumbBarItem template's hardcoded `Padding="1,3"`)
- Chevron: 16px, padding `12,6,12,0`
- Chevron color: `BreadcrumbBarNormalForegroundBrush` = `TextFillColorTertiary`
- Ellipsis: middle dots `···` (U+00B7), same font/margin as text
- Ellipsis uses custom `ControlTemplate` — no background highlight on hover, foreground change only
- Max 3 visible items; earlier items collapse to ellipsis `MenuFlyout`

## Icons
- Nav pane SVG icons: 16×16 `ImageIcon` in 16×16 icon box (`NavigationViewItemOnLeftIconBoxWidth/Height`)
- SVG viewBoxes trimmed to actual artwork bounds (removes internal whitespace for consistent visual weight)
- App icon: Azure A logo, all MSIX scale variants (100/125/150/200/400), ICO embedded via `<ApplicationIcon>`
- `AppWindow.SetIcon()` for Alt+Tab/Win+Tab visibility

## Loading Indicators
- Central page-level `ProgressRing`: 20×20, right-aligned in breadcrumb row (MainWindow)
- MainWindow detects `ViewModel.IsLoading` via `ILoadable` interface + `PropertyChanged` subscription
- ViewModels that load data on navigation implement `ILoadable`
- Inline operation indicators (`IsSearching`, `IsUpdating`, `IsToggling`) remain per-page near the action
- Subsection title indicators (16px next to section headings) remain per-page

## Page Lifecycle
- Every page with async `OnNavigatedTo` has a `CancellationTokenSource _cts` field
- Created in `OnNavigatedTo` (after `base.OnNavigatedTo`), cancelled+disposed in `OnNavigatedFrom`
- `_cts.Token` passed to all API calls — cancels pending requests when user navigates away
- All pages use `OnNavigatedTo`, not `Page_Loaded`

## List Styling
- `CardListViewItem` style: custom `ControlTemplate` with rounded corners, `BrushTransition` 200ms hover
- Uses `ControlFillColorSecondary`/`Tertiary` for PointerOver/Pressed states
- All list pages use `<StaticResource ResourceKey="CardListViewItem" />` for item containers

## Code Patterns
- ViewModels are **Transient** (registered in DI), services are **Singleton**
- `OperationManager` is **Singleton** for global notification tracking
- `BreadcrumbControl` UserControl encapsulates all breadcrumb logic
- `ResourceIconResolver` maps Azure resource types to SVG icon paths
- Nav footer: Operations (bell with `InfoBadge` + attached flyout), Settings
