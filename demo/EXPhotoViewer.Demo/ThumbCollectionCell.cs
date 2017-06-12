using System;
using System.Net.Http;
using System.Threading.Tasks;
using Foundation;
using Newtonsoft.Json.Linq;
using UIKit;

namespace EXImageViewer.Demo
{
    [Register("ThumbCollectionCell")]
    public partial class ThumbCollectionCell : UICollectionViewCell
    {
        public ThumbCollectionCell(IntPtr handle)
            : base(handle)
        {
        }

        public override void AwakeFromNib()
        {
            base.AwakeFromNib();

            ImageView.Image = UIImage.LoadFromData(NSData.FromUrl(new NSUrl("https://unsplash.it/300?random")));
            ContentView.AddGestureRecognizer(new UITapGestureRecognizer(() =>
            {
                if (ImageView != null && ImageView.Image != null)
                {
                    EXImageViewer.ShowImageFrom(this.ImageView);
                }
            }));
        }
    }
}
