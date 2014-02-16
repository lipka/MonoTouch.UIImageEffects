using System;
using System.Drawing;

using MonoTouch.UIKit;
using MonoTouch.CoreGraphics;
using System.Runtime.InteropServices;

namespace MonoTouch.UIImageEffects
{
	public static class UIImageExtensions
	{
		struct vImage_Buffer {    
			public System.IntPtr data;
			public uint height;
			public uint width;
			public uint rowBytes;
		};

		[DllImport (Constants.AccelerateImageLibrary)]
		static extern int vImageBoxConvolve_ARGB8888 (ref vImage_Buffer src, ref vImage_Buffer dest, IntPtr tempBuffer, uint srcOffsetToROI_X, uint srcOffsetToROI_Y, uint kernel_height, uint kernel_width, byte[] backgroundColor, uint flags);

		[DllImport (Constants.AccelerateImageLibrary)]
		static extern int vImageMatrixMultiply_ARGB8888 (ref vImage_Buffer src, ref vImage_Buffer dest, short[] matrix, int divisor, IntPtr pre_bias, IntPtr post_bias, uint flags);

		public static UIImage ApplyLightEffect (this UIImage image)
		{
			UIColor tintColor = UIColor.FromWhiteAlpha (1.0f, 0.3f);
			return ApplyBlur (image, 30f, tintColor, 1.8f, null);
		}

		public static UIImage ApplyExtraLightEffect (this UIImage image)
		{
			UIColor tintColor = UIColor.FromWhiteAlpha (0.97f, 0.82f);
			return ApplyBlur (image, 20f, tintColor, 1.8f, null);
		}

		public static UIImage ApplyDarkEffect (this UIImage image)
		{
			UIColor tintColor = UIColor.FromWhiteAlpha (0.11f, 0.73f);
			return ApplyBlur (image, 20f, tintColor, 1.8f, null);
		}
			
		public static UIImage ApplyTintEffectWithColor(this UIImage image, UIColor tintColor)
		{
			const float EFFECT_COLOR_ALPHA = 0.6f;
			UIColor effectColor = tintColor;
			int componentCount = tintColor.CGColor.NumberOfComponents;
			if (componentCount == 2) {
				float b, a;
				if (tintColor.GetWhite (out b, out a)) {
					effectColor = UIColor.FromWhiteAlpha (b, EFFECT_COLOR_ALPHA);
				}
			} else {
				float r, g, b, a;
				try {
					tintColor.GetRGBA (out r, out g, out b, out a);
					effectColor = UIColor.FromRGBA (r, g, b, EFFECT_COLOR_ALPHA);
				} catch {
				}
			}
			return ApplyBlur (image, 10f, effectColor, -1.0f, null);
		}

		public static UIImage ApplyBlur (this UIImage image, float blurRadius, UIColor tintColor, float saturationDeltaFactor, UIImage maskImage)
		{
			if (image.Size.Width < 1 || image.Size.Height < 1) {
				Console.WriteLine ("*** error: invalid size: ({0} x .{1}). Both dimensions must be >= 1: {2}", image.Size.Width, image.Size.Height, image);
				return null;
			}
			if (image.CGImage == null) {
				Console.WriteLine ("*** error: image must be backed by a CGImage: {0}", image);
				return null;
			}
			if (maskImage != null && maskImage.CGImage == null) {
				Console.WriteLine ("*** error: maskImage must be backed by a CGImage: {0}", maskImage);
				return null;
			}

			RectangleF imageRect = new RectangleF (PointF.Empty, image.Size);
			UIImage effectImage = image;

			bool hasBlur = blurRadius > float.Epsilon;
			bool hasSaturationChange = Math.Abs (saturationDeltaFactor - 1.0f) > float.Epsilon;
			if (hasBlur || hasSaturationChange) {
				UIGraphics.BeginImageContextWithOptions (image.Size, false, UIScreen.MainScreen.Scale);
				CGContext effectInContext = UIGraphics.GetCurrentContext ();
				effectInContext.ScaleCTM (1.0f, -1.0f);
				effectInContext.TranslateCTM (0.0f, -image.Size.Height);
				effectInContext.DrawImage (imageRect, image.CGImage);

				CGBitmapContext effectInContextAsBitmapContext = effectInContext.AsBitmapContext ();

				vImage_Buffer effectInBuffer;
				effectInBuffer.data = effectInContextAsBitmapContext.Data;
				effectInBuffer.width = (uint)effectInContextAsBitmapContext.Width;
				effectInBuffer.height = (uint)effectInContextAsBitmapContext.Height;
				effectInBuffer.rowBytes = (uint)effectInContextAsBitmapContext.BytesPerRow;

				UIGraphics.BeginImageContextWithOptions (image.Size, false, UIScreen.MainScreen.Scale);
				CGContext effectOutContext = UIGraphics.GetCurrentContext ();

				CGBitmapContext effectOutContextAsBitmapContext = effectOutContext.AsBitmapContext ();

				vImage_Buffer effectOutBuffer;
				effectOutBuffer.data = effectOutContextAsBitmapContext.Data;
				effectOutBuffer.width = (uint)effectOutContextAsBitmapContext.Width;
				effectOutBuffer.height = (uint)effectOutContextAsBitmapContext.Height;
				effectOutBuffer.rowBytes = (uint)effectOutContextAsBitmapContext.BytesPerRow;

				if (hasBlur) {
					float inputRadius = blurRadius * UIScreen.MainScreen.Scale;
					uint radius = (uint)Math.Floor (inputRadius * 3.0 * Math.Sqrt (2 * Math.PI) / 4 + 0.5);
					if (radius % 2 != 1) {
						radius += 1;
					}
					const uint kvImageEdgeExtend = 8;
					vImageBoxConvolve_ARGB8888 (ref effectInBuffer, ref effectOutBuffer, IntPtr.Zero, 0, 0, radius, radius, null, kvImageEdgeExtend);
					vImageBoxConvolve_ARGB8888 (ref effectOutBuffer, ref effectInBuffer, IntPtr.Zero, 0, 0, radius, radius, null, kvImageEdgeExtend);
					vImageBoxConvolve_ARGB8888 (ref effectInBuffer, ref effectOutBuffer, IntPtr.Zero, 0, 0, radius, radius, null, kvImageEdgeExtend);
				}
				bool effectImageBuffersAreSwapped = false;
				if (hasSaturationChange) {
					float s = saturationDeltaFactor;
					float[] floatingPointSaturationMatrix = new float[] {
						0.0722f + 0.9278f * s,  0.0722f - 0.0722f * s,  0.0722f - 0.0722f * s,  0f,
						0.7152f - 0.7152f * s,  0.7152f + 0.2848f * s,  0.7152f - 0.7152f * s,  0f,
						0.2126f - 0.2126f * s,  0.2126f - 0.2126f * s,  0.2126f + 0.7873f * s,  0f,
						0f,                     0f,                     0f,                     1f,
					};
					const int divisor = 256;
					uint matrixSize = (uint)floatingPointSaturationMatrix.Length;
					short[] saturationMatrix = new short[matrixSize];
					for (uint i = 0; i < matrixSize; ++i) {
						saturationMatrix [i] = (short)Math.Round (floatingPointSaturationMatrix [i] * divisor);
					}
					if (hasBlur) {
						const uint kvImageNoFlags = 0;
						vImageMatrixMultiply_ARGB8888 (ref effectOutBuffer, ref effectInBuffer, saturationMatrix, divisor, IntPtr.Zero, IntPtr.Zero, kvImageNoFlags);
						effectImageBuffersAreSwapped = true;
					} else {
						const uint kvImageNoFlags = 0;
						vImageMatrixMultiply_ARGB8888 (ref effectInBuffer, ref effectOutBuffer, saturationMatrix, divisor, IntPtr.Zero, IntPtr.Zero, kvImageNoFlags);
					}
				}
				if (!effectImageBuffersAreSwapped) {
					effectImage = UIGraphics.GetImageFromCurrentImageContext ();
				}
				UIGraphics.EndImageContext ();

				if (effectImageBuffersAreSwapped) {
					effectImage = UIGraphics.GetImageFromCurrentImageContext ();
				}
				UIGraphics.EndImageContext ();
			}

			UIGraphics.BeginImageContextWithOptions (image.Size, false, UIScreen.MainScreen.Scale);
			CGContext outputContext = UIGraphics.GetCurrentContext ();
			outputContext.ScaleCTM (1.0f, -1.0f);
			outputContext.TranslateCTM (0, -image.Size.Height);

			outputContext.DrawImage (imageRect, image.CGImage);

			if (hasBlur) {
				outputContext.SaveState ();
				if (maskImage != null) {
					outputContext.ClipToMask (imageRect, maskImage.CGImage);
				}
				outputContext.DrawImage (imageRect, effectImage.CGImage);
				outputContext.RestoreState ();
			}

			if (tintColor != null) {
				outputContext.SaveState ();
				outputContext.SetFillColorWithColor (tintColor.CGColor);
				outputContext.FillRect (imageRect);
				outputContext.RestoreState ();
			}

			UIImage outputImage = UIGraphics.GetImageFromCurrentImageContext ();
			UIGraphics.EndImageContext ();

			return outputImage;
		}
	}
}

