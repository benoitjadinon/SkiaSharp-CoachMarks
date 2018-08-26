
iOS

```
using SkiaSharp.CoachMarks;
public override void ViewDidLayoutSubviews()
{
    base.ViewDidLayoutSubviews();

	viewModel.CoachMarks
	    .Add(Button.WindowPosition(), viewModel.SomeText)
	    .Show(this);
}
```

Android

```
public override void OnWindowFocusChanged(bool hasFocus)
{
    base.OnWindowFocusChanged(hasFocus);

    viewModel.CoachMarks
        .Add(button.WindowPosition(), viewModel.SomeText)
        .Show(this);
}
```

ViewModel

```
public CoachMarks CoachMarks { get; } 
	= new CoachMarks()
        .Create(bgColor:0x88000000);

public string SomeText { get; } = "test";
```