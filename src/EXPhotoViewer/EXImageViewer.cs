using System;
using CoreAnimation;
using CoreGraphics;
using Foundation;
using UIKit;
namespace EXImageViewer
{
    public class EXImageViewer : UIViewController, IUIScrollViewDelegate, IUIAppearanceContainer
    {
        public nfloat BackgroundScale { get; set; }

        public UIColor BackgroundColor { get; set; }

        bool IsClosing { get; set; }

        UIScrollView ZoomeableScrollView { get; set; }

        UIImageView OriginalImageView { get; set; }

        UIImageView TheImageView { get; set; }

        UIView TempViewContainer { get; set; }

        CGRect OriginalImageRect { get; set; }

        UIViewController Controller { get; set; }

        UIViewController SelfController { get; set; }

        public static EXImageViewer ShowImageFrom(UIImageView imageView)
        {
            EXImageViewer viewer = NewViewerFor(imageView);
            viewer.Show();
            return viewer;
        }

        public static EXImageViewer NewViewerFor(UIImageView imageView)
        {
            EXImageViewer viewer = null;
            if (imageView.Image != null)
            {
                viewer = new EXImageViewer();
                viewer.OriginalImageView = imageView;
                viewer.BackgroundScale = 1.0f;
            }

            return viewer;
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            this.View = new UIView(UIScreen.MainScreen.Bounds);
            this.View.BackgroundColor = UIColor.Clear;
            UIScrollView scrollView = new UIScrollView(this.View.Bounds);
            scrollView.MaximumZoomScale = 10.0f;
            scrollView.MinimumZoomScale = 1.0f;
            scrollView.Delegate = this;
            scrollView.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
            this.View.AddSubview(scrollView);
            this.ZoomeableScrollView = scrollView;
            UIImageView imageView = new UIImageView(this.View.Bounds);
            imageView.ClipsToBounds = true;
            imageView.ContentMode = this.OriginalImageView.ContentMode;
            this.ZoomeableScrollView.AddSubview(imageView);
            this.TheImageView = imageView;
        }

        UIViewController RootViewController()
        {
            UIViewController controller = UIApplication.SharedApplication.KeyWindow.RootViewController;
            if (controller.PresentedViewController != null)
            {
                controller = controller.PresentedViewController;
            }

            return controller;
        }

        public void Show()
        {
            if (this.Controller != null) return;

            UIViewController controller = this.RootViewController();
            this.TempViewContainer = new UIView(controller.View.Bounds);
            this.TempViewContainer.BackgroundColor = controller.View.BackgroundColor;
            controller.View.BackgroundColor = UIColor.Black;
            foreach (UIView subView in controller.View.Subviews)
            {
                this.TempViewContainer.AddSubview(subView);
            }
            controller.View.AddSubview(this.TempViewContainer);
            this.Controller = controller;
            this.View.Frame = controller.View.Bounds;
            this.View.BackgroundColor = UIColor.Clear;
            controller.View.AddSubview(this.View);
            this.TheImageView.Image = this.OriginalImageView.Image;
            this.OriginalImageRect = this.OriginalImageView.ConvertRectToView(this.OriginalImageView.Bounds, this.View);
            this.TheImageView.Frame = this.OriginalImageRect;
            NSNotificationCenter.DefaultCenter.AddObserver(this, new ObjCRuntime.Selector("orientationDidChange:"), UIDevice.OrientationDidChangeNotification, null);
            UIView.Animate(0.3, () =>
            {
                this.View.BackgroundColor = this.BackgroundColor != null ? this.BackgroundColor : UIColor.Black;
                this.TempViewContainer.Layer.Transform = CATransform3D.MakeScale(this.BackgroundScale, this.BackgroundScale, this.BackgroundScale);
                this.TheImageView.Frame = this.CenteredOnScreenImage(this.TheImageView.Image);
            }, () =>
            {
                this.AdjustScrollInsetsToCenterImage();
                UITapGestureRecognizer tap = new UITapGestureRecognizer(Close);
                this.View.AddGestureRecognizer(tap);
            });
            this.SelfController = this;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                NSNotificationCenter.DefaultCenter.RemoveObserver(this, UIDevice.OrientationDidChangeNotification, null);
            }
        }
        void Dealloc()
        {
        }

        [Export("orientationDidChange:")]
        void OrientationDidChange(NSNotification note)
        {
            this.TheImageView.Frame = this.CenteredOnScreenImage(this.TheImageView.Image);
            CGRect newFrame = this.RootViewController().View.Bounds;
            this.TempViewContainer.Frame = newFrame;
            this.View.Frame = newFrame;
            this.AdjustScrollInsetsToCenterImage();
        }

        public void Close()
        {
            if (!this.IsClosing)
            {
                this.IsClosing = true;
                CGRect absoluteCGRect = this.View.ConvertRectFromView(this.TheImageView.Frame, this.TheImageView.Superview);
                this.ZoomeableScrollView.ContentOffset = CGPoint.Empty;
                this.ZoomeableScrollView.ContentInset = UIEdgeInsets.Zero;
                this.TheImageView.Frame = absoluteCGRect;
                UIView.Animate(0.3, () =>
                    {
                        this.TheImageView.Frame = this.OriginalImageRect;
                        this.View.BackgroundColor = UIColor.Clear;
                        this.TempViewContainer.Layer.Transform = CATransform3D.Identity;
                    }, () =>
                    {
                        this.OriginalImageView.Image = this.TheImageView.Image;
                        this.Controller.View.BackgroundColor = this.TempViewContainer.BackgroundColor;
                        foreach (UIView subView in this.TempViewContainer.Subviews)
                        {
                            this.Controller.View.AddSubview(subView);
                        }
                        this.View.RemoveFromSuperview();
                        this.TempViewContainer.RemoveFromSuperview();
                        this.IsClosing = false;
                    });
                this.SelfController = null;
            }

        }

        CGRect CenteredOnScreenImage(UIImage image)
        {
            CGSize imageSize = this.ImageSizesizeThatFitsForImage(this.TheImageView.Image);
            CGPoint imageOrigin = new CGPoint(this.View.Frame.Size.Width / 2.0 - imageSize.Width / 2.0, this.View.Frame.Size.Height / 2.0 - imageSize.Height / 2.0);
            return new CGRect(imageOrigin.X, imageOrigin.Y, imageSize.Width, imageSize.Height);
        }

        CGSize ImageSizesizeThatFitsForImage(UIImage image)
        {
            if (image == null) return CGSize.Empty;

            CGSize imageSize = image.Size;
            nfloat ratio = (nfloat)Convert.ToDouble(Math.Min(this.View.Frame.Size.Width / imageSize.Width, this.View.Frame.Size.Height / imageSize.Height));
            return new CGSize(imageSize.Width * ratio, imageSize.Height * ratio);
        }

        UIView ViewForZoomingInScrollView(UIScrollView scrollView)
        {
            return this.TheImageView;
        }

        void AdjustScrollInsetsToCenterImage()
        {
            CGSize imageSize = this.ImageSizesizeThatFitsForImage(this.TheImageView.Image);
            this.ZoomeableScrollView.ZoomScale = 1.0f;
            this.TheImageView.Frame = new CGRect(0, 0, imageSize.Width, imageSize.Height);
            this.ZoomeableScrollView.ContentSize = this.TheImageView.Frame.Size;
            CGRect innerFrame = this.TheImageView.Frame;
            CGRect scrollerBounds = this.ZoomeableScrollView.Bounds;
            CGPoint myScrollViewOffset = this.ZoomeableScrollView.ContentOffset;
            if ((innerFrame.Size.Width < scrollerBounds.Size.Width) || (innerFrame.Size.Height < scrollerBounds.Size.Height))
            {
                nfloat tempx = this.TheImageView.Center.X - (scrollerBounds.Size.Width / 2);
                nfloat tempy = this.TheImageView.Center.Y - (scrollerBounds.Size.Height / 2);
                myScrollViewOffset = new CGPoint(tempx, tempy);
            }

            UIEdgeInsets anEdgeInset = new UIEdgeInsets(0.0f, 0.0f, 0.0f, 0.0f);
            if (scrollerBounds.Size.Width > innerFrame.Size.Width)
            {
                anEdgeInset.Left = (scrollerBounds.Size.Width - innerFrame.Size.Width) / 2;
                anEdgeInset.Right = -anEdgeInset.Left;
            }

            if (scrollerBounds.Size.Height > innerFrame.Size.Height)
            {
                anEdgeInset.Top = (scrollerBounds.Size.Height - innerFrame.Size.Height) / 2;
                anEdgeInset.Bottom = -anEdgeInset.Top;
            }

            this.ZoomeableScrollView.ContentOffset = myScrollViewOffset;
            this.ZoomeableScrollView.ContentInset = anEdgeInset;
        }

        void ScrollViewDidZoom(UIScrollView scrollView)
        {
            UIView view = this.TheImageView;
            CGRect innerFrame = view.Frame;
            CGRect scrollerBounds = scrollView.Bounds;
            CGPoint myScrollViewOffset = scrollView.ContentOffset;
            if ((innerFrame.Size.Width < scrollerBounds.Size.Width) || (innerFrame.Size.Height < scrollerBounds.Size.Height))
            {
                nfloat tempx = view.Center.X - (scrollerBounds.Size.Width / 2);
                nfloat tempy = view.Center.Y - (scrollerBounds.Size.Height / 2);
                myScrollViewOffset = new CGPoint(tempx, tempy);
            }

            UIEdgeInsets anEdgeInset = new UIEdgeInsets(0.0f, 0.0f, 0.0f, 0.0f);
            if (scrollerBounds.Size.Width > innerFrame.Size.Width)
            {
                anEdgeInset.Left = (scrollerBounds.Size.Width - innerFrame.Size.Width) / 2;
                anEdgeInset.Right = -anEdgeInset.Left;
            }

            if (scrollerBounds.Size.Height > innerFrame.Size.Height)
            {
                anEdgeInset.Top = (scrollerBounds.Size.Height - innerFrame.Size.Height) / 2;
                anEdgeInset.Bottom = -anEdgeInset.Top;
            }

            UIView.Animate(0.3, () =>
               {
                   scrollView.ContentOffset = myScrollViewOffset;
                   scrollView.ContentInset = anEdgeInset;
               });
        }

    }
}