using System;
using Foundation;
using UIKit;

namespace EXImageViewer.Demo
{
    public partial class MainViewController : UICollectionViewController
    {
        protected MainViewController(IntPtr handle) : base(handle)
        {
        }

        public override nint GetItemsCount(UICollectionView collectionView, nint section)
        {
            // even number so we display 2 columns
            return 8;
        }

        public override UICollectionViewCell GetCell(UICollectionView collectionView, Foundation.NSIndexPath indexPath)
        {
            return collectionView.DequeueReusableCell("ThumbCollectionCell", indexPath) as ThumbCollectionCell;
        }

        public override void ItemSelected(UICollectionView collectionView, Foundation.NSIndexPath indexPath)
        {
        }
    }
}
