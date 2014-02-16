# MonoTouch.UIImageEffects

A native port of the [UIImage categories](https://developer.apple.com/downloads/download.action?path=wwdc_2013/wwdc_2013_sample_code/ios_uiimageeffects.zip) by Apple from WWDC 2013.

## Usage

``` c#
UIImage image = UIImage.FromFile ("cheetah.png")
image = image.ApplyDarkEffect ();
```

### The infamous iOS7 blur

Getting the iOS7 style blur is a bit more tricky but can be achieved with the following code.

``` c#
// Helper method to create an image snapshot of a view
UIImage SnapshotImageWithScale (UIView view, float scale)
{
    UIGraphics.BeginImageContextWithOptions (view.Bounds.Size, false, scale);
    view.DrawViewHierarchy (view.Bounds, true);

    UIImage image = UIGraphics.GetImageFromCurrentImageContext ();
    UIGraphics.EndImageContext ();

    return image;
}

...

// Create snapshot of the parent view (this can be the superview or anything else)
UIImage snapshot = SnapshotImageWithScale (parentView, UIScreen.MainScreen.Scale);

// Blur the snapshot with the specified radius and tint color
snapshot = snapshot.ApplyBlur (6.0f, UIColor.FromWhiteAlpha (0.0f, 0.6f), 0.8f, null);

// Create an UIImageView to display the blurred snapshot
UIImageView snapshotView = new UIImageView {
    Frame = new RectangleF (0, 0, View.Bounds.Width, View.Bounds.Height),
    Image = snapshot,
};
View.AddSubview (snapshotView);
```

## Installation

Either drop in `src/UIImageExtensions.cs` to your project or build a standalone library and reference it.

## Requirements

MonoTouch.UIImageEffects is tested on iOS7, but may be compatible with lower versions.

## Contact

Lukas Lipka

- http://github.com/lipka
- http://twitter.com/lipec
- http://lukaslipka.com

## License

MonoTouch.UIImageEffects is available under the [MIT license](LICENSE). See the LICENSE file for more info.
