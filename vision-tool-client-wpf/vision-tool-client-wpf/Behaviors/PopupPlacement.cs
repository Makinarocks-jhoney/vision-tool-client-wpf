// Behaviors/PopupPlacement.cs
using System;
using System.Windows;
using System.Windows.Controls.Primitives;

namespace vision_tool_client_wpf.Behaviors
{
    public static class PopupPlacement
    {
        // 오른쪽 정렬 + 아래 배치 활성화
        public static readonly DependencyProperty RightAlignBelowProperty =
            DependencyProperty.RegisterAttached(
                "RightAlignBelow", typeof(bool), typeof(PopupPlacement),
                new PropertyMetadata(false, OnRightAlignBelowChanged));

        public static void SetRightAlignBelow(DependencyObject obj, bool value) => obj.SetValue(RightAlignBelowProperty, value);
        public static bool GetRightAlignBelow(DependencyObject obj) => (bool)obj.GetValue(RightAlignBelowProperty);

        // 버튼 아래 여백(px)
        public static readonly DependencyProperty GapProperty =
            DependencyProperty.RegisterAttached(
                "Gap", typeof(double), typeof(PopupPlacement),
                new PropertyMetadata(4.0, OnGapChanged));

        public static void SetGap(DependencyObject obj, double value) => obj.SetValue(GapProperty, value);
        public static double GetGap(DependencyObject obj) => (double)obj.GetValue(GapProperty);

        // 윈도우 이동/리사이즈 추적
        public static readonly DependencyProperty TrackWindowProperty =
            DependencyProperty.RegisterAttached(
                "TrackWindow", typeof(bool), typeof(PopupPlacement),
                new PropertyMetadata(false, OnTrackWindowChanged));

        public static void SetTrackWindow(DependencyObject obj, bool value) => obj.SetValue(TrackWindowProperty, value);
        public static bool GetTrackWindow(DependencyObject obj) => (bool)obj.GetValue(TrackWindowProperty);

        // 내부: 핸들러 보관
        private class HandlerBag
        {
            public Window? Win;
            public EventHandler? LocationChangedHandler;      // Window.LocationChanged
            public SizeChangedEventHandler? SizeChangedHandler; // Window.SizeChanged
            public EventHandler? StateChangedHandler;         // Window.StateChanged
        }

        private static readonly DependencyProperty HandlersProperty =
            DependencyProperty.RegisterAttached(
                "Handlers", typeof(HandlerBag), typeof(PopupPlacement), new PropertyMetadata(null));

        private static void OnRightAlignBelowChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not Popup popup) return;

            if ((bool)e.NewValue)
            {
                popup.Placement = PlacementMode.Custom;
                popup.CustomPopupPlacementCallback = BuildCallback(popup);

                // 열릴 때/크기변경 때 재배치
                popup.Opened += Popup_OpenedOrSizeChanged;
                popup.Closed += Popup_Closed;
                AttachSizeHandlers(popup, attach: true);
            }
            else
            {
                popup.Opened -= Popup_OpenedOrSizeChanged;
                popup.Closed -= Popup_Closed;
                AttachSizeHandlers(popup, attach: false);
                popup.CustomPopupPlacementCallback = null;
            }
        }

        private static void OnGapChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not Popup popup) return;
            if (GetRightAlignBelow(popup))
            {
                popup.CustomPopupPlacementCallback = BuildCallback(popup);
                Nudge(popup);
            }
        }

        private static void OnTrackWindowChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not Popup popup) return;

            if ((bool)e.NewValue)
            {
                popup.Loaded += Popup_LoadedForWindowTrack;
                popup.Unloaded += Popup_UnloadedForWindowTrack;
            }
            else
            {
                popup.Loaded -= Popup_LoadedForWindowTrack;
                popup.Unloaded -= Popup_UnloadedForWindowTrack;
                RemoveWindowHooks(popup);
            }
        }

        private static void Popup_LoadedForWindowTrack(object? sender, RoutedEventArgs e) => HookWindow((Popup)sender!, hook: true);
        private static void Popup_UnloadedForWindowTrack(object? sender, RoutedEventArgs e) => HookWindow((Popup)sender!, hook: false);

        private static void HookWindow(Popup popup, bool hook)
        {
            var win = Window.GetWindow(popup.PlacementTarget ?? popup);
            if (win == null) return;

            if (hook)
            {
                // 팝업 인스턴스 캡처한 정확한 시그니처의 핸들러
                EventHandler onLocationChanged = (s, e) => Nudge(popup);
                SizeChangedEventHandler onSizeChanged = (s, e) => Nudge(popup);
                EventHandler onStateChanged = (s, e) => Nudge(popup);

                // 안전하게 로드 후 구독
                win.Dispatcher.BeginInvoke(new Action(() =>
                {
                    win.LocationChanged += onLocationChanged;
                    win.SizeChanged += onSizeChanged;
                    win.StateChanged += onStateChanged;
                    Nudge(popup); // 최초 보정
                }), System.Windows.Threading.DispatcherPriority.Loaded);

                // 핸들러 보관
                var bag = new HandlerBag
                {
                    Win = win,
                    LocationChangedHandler = onLocationChanged,
                    SizeChangedHandler = onSizeChanged,
                    StateChangedHandler = onStateChanged
                };
                popup.SetValue(HandlersProperty, bag);

                // 열릴 때 한 번 더 보정
                popup.Opened += Popup_OpenedOrSizeChanged;
            }
            else
            {
                RemoveWindowHooks(popup);
            }
        }

        private static void RemoveWindowHooks(Popup popup)
        {
            if (popup.GetValue(HandlersProperty) is not HandlerBag bag || bag.Win is null) return;
            var win = bag.Win;

            if (bag.LocationChangedHandler != null)
                win.LocationChanged -= bag.LocationChangedHandler;
            if (bag.SizeChangedHandler != null)
                win.SizeChanged -= bag.SizeChangedHandler;
            if (bag.StateChangedHandler != null)
                win.StateChanged -= bag.StateChangedHandler;

            popup.ClearValue(HandlersProperty);
        }

        private static void Popup_OpenedOrSizeChanged(object? sender, EventArgs e)
        {
            if (sender is Popup p) Nudge(p);
        }

        private static void Popup_Closed(object? sender, EventArgs e)
        {
            if (sender is Popup p) Nudge(p); // 환경에 따라 유용
        }

        private static void AttachSizeHandlers(Popup popup, bool attach)
        {
            if (popup.Child is FrameworkElement fe)
            {
                if (attach) fe.SizeChanged += Popup_OpenedOrSizeChanged;
                else fe.SizeChanged -= Popup_OpenedOrSizeChanged;
            }
            if (popup.PlacementTarget is FrameworkElement target)
            {
                if (attach) target.SizeChanged += Popup_OpenedOrSizeChanged;
                else target.SizeChanged -= Popup_OpenedOrSizeChanged;
            }
        }

        // Custom 콜백: 오른쪽 정렬 + 아래로 Gap
        private static CustomPopupPlacementCallback BuildCallback(Popup popup)
        {
            double gap = GetGap(popup);
            return (popupSize, targetSize, offset) =>
            {
                var p = new Point(targetSize.Width - popupSize.Width, targetSize.Height + gap);
                return new[] { new CustomPopupPlacement(p, PopupPrimaryAxis.Horizontal) };
            };
        }

        // 살짝 흔들어 재배치
        private static void Nudge(Popup popup)
        {
            if (!popup.IsOpen) return;
            var x = popup.HorizontalOffset;
            popup.HorizontalOffset = x + 0.1;
            popup.HorizontalOffset = x;
        }
    }
}
