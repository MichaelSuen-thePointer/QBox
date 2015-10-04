using System;
using System.Collections.Generic;
using Windows.UI.Xaml.Controls;

namespace QBox
{
    public partial class MainPage : Page
    {
        public readonly List<PageItem> pages = new List<PageItem>
        {
            new PageItem() {Title = "主页", ClassType = typeof(StartPage)},
            new PageItem() {Title = "已上传文件", ClassType = typeof(UploadFileView)},
            //new PageItem() {Title = "已下载文件", ClassType = typeof(DownloadFileView)}
        };
    }

    public class PageItem
    {
        public string Title { get; set; }
        public Type ClassType { get; set; }
    }
}