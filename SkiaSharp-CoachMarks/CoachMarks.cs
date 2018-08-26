using System;
using System.Collections.Generic;
using System.Drawing;
using SkiaSharp;
#if __IOS__
using UIKit;
using NativeView = UIKit.UIView;
using NativeRect = CoreGraphics.CGRect;
using NativeRoot = UIKit.UIViewController;
using SkiaSharp.Views.iOS;
using CoreGraphics;
#elif __ANDROID__
using NativeView = Android.Views.View;
using NativeRect = Android.Graphics.Rect;
using NativeRoot = Android.App.Activity;
using SkiaSharp.Views.Android;
using Android.App;
using Android.Views;
#endif

namespace SkiaSharp.CoachMarks
{
    public interface ICoachMarks
    {
        CoachMarksInstance Create(uint? bgColor = null, Action onTouch = null);
    }


    public class CoachMarks : ICoachMarks
    {
        // TODO, singleton check (pass (this)), cause called twice in ViewDidLayoutSubviews
        public CoachMarksInstance Create(uint? bgColor = null, Action onTouch = null)
        {
            var bgPaint = new SKPaint
            {
                IsAntialias = false,
                Color = bgColor ?? 0x77000000,
                Style = SKPaintStyle.Fill,
            };

            return new CoachMarksInstance(bgPaint, onTouch);
        }
    }


    public class CoachMarksInstance
    {
        private List<ICoachMark> Marks { get; } = new List<ICoachMark>();

        private SKCanvasView _skiaCanvasView = null;

        private readonly Action _onTouch;
        private readonly SKPaint _bgPaint;


        public CoachMarksInstance(SKPaint bgPaint, Action onTouch = null)
        {
            _bgPaint = bgPaint;
            _onTouch = onTouch;

            Marks.Clear();
        }


        public CoachMarksInstance Show(NativeRoot root)
        {
            if (_skiaCanvasView == null)
            {
                float scaleFactor = 1;
#if __IOS__
                //TODO use root (viewcontroller) instead of window
                _skiaCanvasView = new SKCanvasView(UIApplication.SharedApplication.KeyWindow.Bounds);
                
                scaleFactor = (float)_skiaCanvasView.ContentScaleFactor;;
                _skiaCanvasView.BackgroundColor = UIColor.Clear;
                _skiaCanvasView.Opaque = false;
                UIApplication.SharedApplication.KeyWindow.AddSubview(_skiaCanvasView);
                
                //skiaCanvasView.FillParent();
                _skiaCanvasView.TranslatesAutoresizingMaskIntoConstraints = false;
                _skiaCanvasView.Superview.AddConstraints(NSLayoutConstraint.FromVisualFormat("H:|[childView]|", NSLayoutFormatOptions.DirectionLeadingToTrailing, "childView", _skiaCanvasView));
                _skiaCanvasView.Superview.AddConstraints(NSLayoutConstraint.FromVisualFormat("V:|[childView]|", NSLayoutFormatOptions.DirectionLeadingToTrailing, "childView", _skiaCanvasView));

                _skiaCanvasView.UserInteractionEnabled = true;
                _skiaCanvasView.AddGestureRecognizer (new UITapGestureRecognizer(_onTouch ?? _skiaCanvasView.RemoveFromSuperview));
#elif __ANDROID__
                var rootView = (ViewGroup) root.Window.DecorView.RootView;
                
                _skiaCanvasView = new SKCanvasView(root);
                _skiaCanvasView.Touch += ((sender, args) =>
                {
                    if (_onTouch != null)
                        _onTouch?.Invoke();
                    else rootView.RemoveView(_skiaCanvasView);
                }); 
                
                rootView.AddView(_skiaCanvasView, new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent));
#endif
                
                _skiaCanvasView.PaintSurface += (sender, e) =>
                {
                    var canvas = e.Surface.Canvas;
                    var size = e.Info.Size;
    
                    Draw(canvas, size, scaleFactor);
                };
            }

            Invalidate();

            return this;
        }

        public CoachMarksInstance Add(SKRect rect, string text, CoachMarkPosition textPosition = null)
        {
            this.Marks.Add(new TextBaseCoachMark(rect, text, textPosition));
            return this;
        }

        private void Invalidate()
        {
#if __IOS__
            _skiaCanvasView?.SetNeedsDisplay();
#elif __ANDROID__
            _skiaCanvasView?.Invalidate();
#endif
        }

        private void Draw(SKCanvas canvas, SKSize size, float scale = 1)
        {
            canvas.Clear();

            // clipping
            foreach (var mark in Marks)
                mark.DrawHole(canvas, mark.Rect.Scale(scale), scale);

            // clipping reset
            canvas.ResetMatrix();

            // background 
            canvas.DrawRect(0, 0, size.Width, size.Height, _bgPaint);

            // mark texts
            foreach (var mark in this.Marks)
                mark.Draw(canvas, mark.Rect.Scale(scale), scale);
        }
    }


    public interface ICoachMarkHole
    {
        void DrawHole(SKCanvas canvas, SKRect size, float scale = 1);
    }


    public interface ICoachMark : ICoachMarkHole
    {
        SKRect Rect { get; }
        ICoachMarkHole Hole { get; }
        void Draw(SKCanvas canvas, SKRect size, float scale = 1);
    }


    public abstract class BaseCoachMark : ICoachMark
    {
        public SKRect Rect { get; }
        public ICoachMarkHole Hole { get; }

        public BaseCoachMark(SKRect rect, ICoachMarkHole hole = null)
        {
            Rect = rect;
            Hole = hole ?? new CoachMarkHoleRect();
        }

        public virtual void DrawHole(SKCanvas canvas, SKRect size, float scale = 1)
            => Hole.DrawHole(canvas, size, scale);

        public abstract void Draw(SKCanvas canvas, SKRect size, float scale = 1);
    }


    public class CoachMarkHoleRect : ICoachMarkHole
    {
        public float Roundness { get; set; } = 0;

        public void DrawHole(SKCanvas canvas, SKRect rect, float scale = 1)
        {
            if (Roundness <= 0)
                canvas.ClipRect(new SKRect(rect.Left, rect.Top, rect.Right, rect.Bottom), SKClipOperation.Difference);
            else
                canvas.ClipRoundRect(
                    new SKRoundRect(new SKRect(rect.Left, rect.Top, rect.Right, rect.Bottom), Roundness, Roundness),
                    SKClipOperation.Difference);
        }
    }

    public class CoachMarkHoleCircle : ICoachMarkHole
    {
        public void DrawHole(SKCanvas canvas, SKRect rect, float scale = 1)
        {
            using (SKPath path = new SKPath())
            {
                path.AddCircle(rect.Left + rect.Width / 2, rect.Top + rect.Height / 2, rect.Width / 2);
                canvas.ClipPath(path, SKClipOperation.Difference);
            }
        }
    }


    public class TextBaseCoachMark : BaseCoachMark
    {
        private SKPaint _textPaint = new SKPaint
        {
            IsAntialias = true,
            Color = 0xFF00FF00,
            TextSize = 40,
            FakeBoldText = true
            //Typeface = 
        };

        public string Text { get; }
        public CoachMarkPosition TextPosition { get; }

        public TextBaseCoachMark(SKRect rect, string text, CoachMarkPosition textPosition = null)
            : base(rect)
        {
            Text = text;
            TextPosition = textPosition ?? CoachMarkPosition.Below;
        }

        public override void Draw(SKCanvas canvas, SKRect size, float scale = 1)
        {
            switch (TextPosition.V)
            {
                case -1:
                    canvas.DrawText(Text, size.Left, size.Top, _textPaint);
                    break;
                case 0:
                    canvas.DrawText(Text, size.Left, size.Top + size.Height, _textPaint);
                    break;
                case +1:
                    _textPaint.GetFontMetrics(out var metrics, scale);
                    canvas.DrawText(Text, size.Left, size.Top + size.Height + metrics.XHeight, _textPaint);
                    break;
            }
        }
    }

    public class CoachMarkPosition
    {
        public static CoachMarkPosition Above = new CoachMarkPosition(v: -1);
        public static CoachMarkPosition Below = new CoachMarkPosition(v: +1);

        public static CoachMarkPosition Left = new CoachMarkPosition(h: -1);
        public static CoachMarkPosition Right = new CoachMarkPosition(h: +1);

        public int H { get; } = 0;
        public int V { get; } = 0;

        public CoachMarkPosition(int h = 0, int v = 0)
        {
            H = h;
            V = v;
        }
    }

    public enum CoachMarkHoleTypes //TODO
    {
        Rectangle,
        Circle,
    }

    public static class DrawingExtensions
    {
        public static SKRect Scale(this SKRect @this, float scale)
        {
            return new SKRect
            (
                @this.Left * scale,
                @this.Top * scale,
                @this.Width * scale,
                @this.Height * scale
            );
        }
    }


    public static class ViewExtensions
    {
        public static SKRect WindowPosition(this NativeView @this)
        {
            var rect = new NativeRect();
#if __IOS__
            //TODO : recurive parent call to get root view ?
            rect = (NativeRect)@this.Superview?.ConvertRectToView(@this.Frame, toView: null);//TODO @this.SafeAreaInsets
#elif __ANDROID__
            @this.GetGlobalVisibleRect(rect);
#endif
            return SKRect.Create((float) rect.Left, (float) rect.Top, (float) rect.Right, (float) rect.Bottom);
        }
        
    }
}