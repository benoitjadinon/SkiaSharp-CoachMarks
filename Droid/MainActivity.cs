using Android.App;
using Android.Widget;
using Android.OS;
using Android.Views;
using SkiaSharp.CoachMarks;

namespace SkiaSharpCoachMarks.Droid
{
    [Activity(Label = "SkiaSharp-CoachMarks", MainLauncher = true, Icon = "@mipmap/icon")]
    public class MainActivity : Activity
    {
        private Button button;
        
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);


            button = FindViewById<Button>(Resource.Id.myButton);
        }

        public override void OnWindowFocusChanged(bool hasFocus)
        {
            base.OnWindowFocusChanged(hasFocus);

            new CoachMarks()
                .Create(bgColor:0x88000000)
                .Add(button, "test")
                .Show(this);
        }
    }
}

