
iOS

```csharp
using SkiaSharp.CoachMarks;

public override void ViewDidLayoutSubviews()
{
    base.ViewDidLayoutSubviews();

    viewModel.CoachMarks
        .Add(Button, viewModel.SomeText)
        .Show(this);
}
```

Android

```csharp
using SkiaSharp.CoachMarks;

public override void OnWindowFocusChanged(bool hasFocus)
{
    base.OnWindowFocusChanged(hasFocus);

    viewModel.CoachMarks
        .Add(button, viewModel.SomeText)
        .Show(this);
}
```

ViewModel

```csharp
public CoachMarksInstance CoachMarks { get; } 
    = new CoachMarks()
        .Create(bgColor:0x88000000);

public string SomeText { get; } = "test";

// also, to avoid putting text references inside views,
// use the extension method inside view controllers
//    viewModel.AddButton(myButton.WindowPosition());
// makes the view code copy-pastable
public void AddButton(SKRect rect)
	=> CoachMarks.Add(rect, SomeText);

```