using System;
using System.Collections.Generic;
using Windows.UI.Xaml.Controls;

namespace QBox
{
    public partial class MainPage : Page
    {
        public readonly List<PageItem> pages = new List<PageItem>
        {
            new PageItem() {Title = "Upload Files", ClassType = typeof(UploadFileView)},
            new PageItem() {Title = "Download Files", ClassType = typeof(DownloadFile)}
        };
    }

    public class PageItem
    {
        public string Title { get; set; }
        public Type ClassType { get; set; }
    }
}