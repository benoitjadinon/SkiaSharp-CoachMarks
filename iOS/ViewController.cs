using System;
using SkiaSharp.CoachMarks;
using UIKit;

namespace SkiaSharpCoachMarks.iOS
{
    public partial class ViewController : UIViewController
    {
        int count = 1;

        public ViewController(IntPtr handle) : base(handle)
        {
        }
        
        public override void ViewDidLayoutSubviews()
        {
            base.ViewDidLayoutSubviews();

            new CoachMarks()
                .Create(bgColor:0x88000000)
                .Add(Button, "test")
                .Show(this);
        }


        public override void DidReceiveMemoryWarning()
        {
            base.DidReceiveMemoryWarning();
            // Release any cached data, images, etc that aren't in use.		
        }
    }
}
