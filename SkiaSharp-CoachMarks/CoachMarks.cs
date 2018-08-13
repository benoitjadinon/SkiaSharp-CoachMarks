using System;
using System.Collections.Generic;
using System.Drawing;

using SkiaSharp;

#if __IOS__
using CoreGraphics;
using SkiaSharp.Views.iOS;
using UIKit;
#elif __ANDROID__
using SkiaSharp.Views.Android;
#endif

namespace SkiaSharp.CoachMarks
{
    public interface ICoachMarks
    {
        CoachMarksInstance Create(SKPaint bgPaint = null, Action onTouch = null);
    }
    

    public class CoachMarks : ICoachMarks
    {
        private SKPaint _bgPaint = new SKPaint {
            IsAntialias = false,
            Color = 0x77000000,
            Style = SKPaintStyle.Fill,
        };

        public CoachMarksInstance Create(SKPaint bgPaint = null, Action onTouch = null)
            => new CoachMarksInstance(bgPaint ?? _bgPaint, onTouch);
    }
    

    public class CoachMarksInstance
    {
        private List<ICoachMark> Marks { get; } = new List<ICoachMark>();
        
        private readonly Action _onTouch;

        private SKPaint _bgPaint;

        
        public CoachMarksInstance(SKPaint bgPaint, Action onTouch = null)
        {
            _bgPaint = bgPaint;
            _onTouch = onTouch;

            Marks.Clear();
        }
        
        
        public CoachMarksInstance Show()
        {
            #if __IOS__
                if (skiaCanvasView == null)
                {
                    skiaCanvasView = new SKCanvasView(UIApplication.SharedApplication.KeyWindow.Bounds);
                    skiaCanvasView.BackgroundColor = UIColor.Clear;
                    skiaCanvasView.Opaque = false;
                    UIApplication.SharedApplication.KeyWindow.AddSubview(skiaCanvasView);
                    
                    //skiaCanvasView.FillParent();
                    skiaCanvasView.TranslatesAutoresizingMaskIntoConstraints = false;
                    skiaCanvasView.Superview.AddConstraints(NSLayoutConstraint.FromVisualFormat("H:|[childView]|", NSLayoutFormatOptions.DirectionLeadingToTrailing, "childView", skiaCanvasView));
                    skiaCanvasView.Superview.AddConstraints(NSLayoutConstraint.FromVisualFormat("V:|[childView]|", NSLayoutFormatOptions.DirectionLeadingToTrailing, "childView", skiaCanvasView));
    
                    skiaCanvasView.UserInteractionEnabled = true;
                    skiaCanvasView.AddGestureRecognizer (new UITapGestureRecognizer(_onTouch ?? skiaCanvasView.RemoveFromSuperview));
                    skiaCanvasView.PaintSurface += (sender, e) =>
                    {
                        var canvas = e.Surface.Canvas;
                        var size = e.Info.Size;
        
                        Draw(canvas, size, (float)skiaCanvasView.ContentScaleFactor);
                    };
                }
            #elif __ANDROID__
                //TODO
            #endif
            
            Invalidate();

            return this;
        }

        private SKCanvasView skiaCanvasView = null;

        public CoachMarksInstance Add(RectangleF rect, string text, CoachMarkPosition textPosition = null)
        {
            this.Marks.Add(new TextBaseCoachMark(rect, text, textPosition));
            return this;
        }

        private void Invalidate()
        {
#if __IOS__
            skiaCanvasView.SetNeedsDisplay();
#elif __ANDROID__
            skiaCanvasView.Invalidate();
#endif
        }

        private void Draw(SKCanvas canvas, SKSize size, float scale = 1)
        {
            canvas.Clear();

            // clipping
            foreach (var mark in Marks)
                mark.DrawHole(canvas, mark.Rect.Scale(scale).ToSKRect(), scale);

            // clipping reset
            canvas.ResetMatrix();
            
            // background 
            canvas.DrawRect(0, 0, size.Width, size.Height, _bgPaint);

            // mark texts
            foreach (var mark in this.Marks)
                mark.Draw(canvas, mark.Rect.Scale(scale).ToSKRect(), scale);
        }
    }

    
    public interface ICoachMarkHole
    {
        void DrawHole(SKCanvas canvas, SKRect size, float scale = 1);
    }
    
    
    public interface ICoachMark : ICoachMarkHole
    {
        RectangleF Rect { get; }
        ICoachMarkHole Hole { get; }
        void Draw(SKCanvas canvas, SKRect size, float scale = 1);
    }
    
    
    public abstract class BaseCoachMark : ICoachMark
    {
        public RectangleF Rect { get; }
        public ICoachMarkHole Hole { get; }
        
        public BaseCoachMark(RectangleF rect, ICoachMarkHole hole = null)
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
                canvas.ClipRoundRect(new SKRoundRect(new SKRect(rect.Left, rect.Top, rect.Right, rect.Bottom), Roundness, Roundness), SKClipOperation.Difference);
        }
    }

    public class CoachMarkHoleCircle : ICoachMarkHole
    {
        public void DrawHole(SKCanvas canvas, SKRect rect, float scale = 1)
        {
            using (SKPath path = new SKPath())
            {
                path.AddCircle(rect.Left+rect.Width/2, rect.Top+rect.Height/2, rect.Width/2);
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

        public TextBaseCoachMark(RectangleF rect, string text, CoachMarkPosition textPosition = null) 
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
        public static CoachMarkPosition Above = new CoachMarkPosition(v:-1);
        public static CoachMarkPosition Below = new CoachMarkPosition(v:+1);

        public static CoachMarkPosition Left  = new CoachMarkPosition(h:-1);
        public static CoachMarkPosition Right = new CoachMarkPosition(h:+1);

        public int H { get; } = 0;
        public int V { get; } = 0;
        
        public CoachMarkPosition(int h = 0, int v = 0)
        {
            H = h;
            V = v;
        }
    }

    public enum CoachMarkHoleTypes
    {
        Rectangle,
        Circle,
    }

    public static class DrawingExtensions
    {
        public static Rectangle Scale(this Rectangle @this, float scale)
        {
            return new Rectangle
            (
                (int)(@this.X * scale),
                (int)(@this.Y * scale),
                (int)(@this.Width * scale),
                (int)(@this.Height * scale)
            );
        }
        
        public static RectangleF Scale(this RectangleF @this, float scale)
        {
            return new RectangleF
            (
                @this.X * scale,
                @this.Y * scale,
                @this.Width * scale,
                @this.Height * scale
            );
        }
    }
    
    public static class ViewExtensions{
#if __IOS__
        public static RectangleF WindowPosition(this UIView @this)
        {
            var safe = @this.SafeAreaInsets;
            return @this.Superview?.ConvertRectToView(@this.Frame, toView: null).FromNative() ?? RectangleF.Empty;
        }

        public static RectangleF FromNative(this CGRect @this) 
            => RectangleF.FromLTRB((float)@this.Left, (float)@this.Top, (float)@this.Right, (float)@this.Bottom);
        
#elif __ANDROID__

#endif
    }
}