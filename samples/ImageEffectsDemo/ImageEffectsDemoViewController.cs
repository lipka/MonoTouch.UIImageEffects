using System;
using System.Drawing;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using MonoTouch.UIImageEffects;

namespace ImageEffectsDemo
{
	public partial class ImageEffectsDemoViewController : UIViewController
	{
		public override void ViewDidLoad ()
		{
			base.ViewDidLoad ();
			
			UIImageView i = new UIImageView ();
			i.Frame = new RectangleF (0, 0, View.Bounds.Size.Width, View.Bounds.Size.Height);
			i.Image = UIImage.FromFile ("cheetah.png").ApplyDarkEffect ();
			View.AddSubview (i);
		}
	}
}

