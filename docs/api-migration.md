# SimpleScrollのAPI移行

移動管理はScrollerへ集約し、利用者には目的別の小さなAPIを提供する。1.xでは以下の旧APIをObsolete警告付きで維持する。削除は2.0で予定し、1.x内では削除しない。

## 停止とCarouselのスナップ

全Scrollで現在位置に止める場合はStopを使う。速度、目標位置、セル追従を解除し、セルの途中でもその位置を維持する。

```csharp
scroll.Stop();
```

Carouselで最寄りセルへ移動する場合は、停止とは独立した操作として指定する。

```csharp
carousel.SnapToNearest();       // 最寄りセルへスムーズに移動
carousel.SnapToNearest(false); // 即時移動
```

旧StopScrollは1.xの互換入口として残す。ListとGridでは停止し、Carouselでは従来の速度方向を考慮したセル選択とスナップを維持する。新しいSnapToNearestは現在位置から最寄りセルを選ぶため、慣性の速度方向を考慮する旧動作とは選択結果が異なる場合がある。

## レイアウト更新と位置変更

位置指定付きRefreshは正式APIとして維持し、非推奨化しない。更新と表示位置を一度に指定し、セルの更新前に移動先を決定する。

```csharp
scroll.Refresh(1f, false); // 表示中のセルを維持し、更新後は末尾を表示
```

用途に応じて3つのAPIを使い分ける。

| API | 用途 |
| --- | --- |
| Refresh(normalizedPosition, isRefreshVisibleCells) | データ・レイアウト更新と表示位置を一度に指定する |
| Refresh(isRefreshVisibleCells) | 位置を指定せずデータ・レイアウトを更新する |
| SetNormalizedPosition(normalizedPosition) | 再バインドせず、位置だけを変更する |

RefreshとSetNormalizedPositionを別々に呼ぶことを移行要件にしない。同じタイミングで連続して呼ぶ場合はセル更新を挟まないが、呼び出しをまたぐ実行タイミングを利用者に管理させない。

位置だけを変える場合はSetNormalizedPositionだけを呼ぶ。セルの再バインドやRefreshは実行しない。0が先頭、1が末尾で、範囲外は0から1へ制限される。移動中の目標とセル追従は解除する。有限のレイアウト範囲を初期化するため、データを初めて設定した直後は先にRefreshを呼ぶ。

LazyLayoutは可視セルの実測後にも正規化位置を補正する。ループするCarouselには有限範囲がないため、正規化位置指定は適用されない。

## Scrollを継承する場合

目標位置を毎フレーム渡す旧UpdatePositionは、移動の開始とフレーム更新に分ける。

```csharp
// 移動を要求するとき
MoveToPosition(position, smooth: true);

// 毎フレームの更新はBaseScrollのLateUpdateが実行する。
// 独自のLateUpdateで更新する場合だけUpdatePosition()を呼ぶ。
```

旧UpdatePositionの引数付きオーバーロードは、目標を更新して新しい更新処理へ転送する。既存の継承コードは警告付きでコンパイルできる。

新しいStopではOnStopScrollを呼ばない。独自の追従情報の解除はOnScrollInterruptedで行う。このフックは停止、別の移動要求、ユーザー操作、コンポーネントの無効化でも呼ばれる。旧OnStopScrollは旧StopScrollの互換動作に限定する。

## 継続して使うAPIと保留する変更

SetDataSource、Refreshの両オーバーロード、SetPositionIndex、SetScrollPosition、位置参照プロパティ、操作可否プロパティ、状態イベント、CarouselのNextとPrevは維持する。役割が明確なAPIを名前だけの理由で非推奨にしない。

SetPositionIndexとSetScrollPositionの引数設計はoffsetとduration対応時にまとめて決め、移行を繰り返さない。Carouselのindexはループ上の位置、その他のScrollのindexはデータindexとして扱う。位置指定の単位はRectTransformと同じで、現在の直接座標は縦が正、横が負の方向へ末尾に進む。

Range、VisibleRange、OnVisibleRangeChanged、Repositionは、非連続な可視index表現の置換先が確定するまで非推奨化しない。IndexedDictionaryも1.xでは公開を維持し、内部化は2.0の互換性見直しで判断する。
