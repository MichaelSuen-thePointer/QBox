using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// “空白页”项模板在 http://go.microsoft.com/fwlink/?LinkId=234238 上提供

namespace QBox
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class UploadFileView : Page
    {
        public BoxClient Client;

        public UploadFileView()
        {
            this.InitializeComponent();
            this.Client = MainPage.Client;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {


        }

        private void UploadedItemList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            throw new NotImplementedException();
        }
    }

    public class ExpirationTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var expiration = (FileExpiration.ExpirationTime)value;
            switch (expiration)
            {
                case FileExpiration.ExpirationTime.OneHour:
                    return "一小时";
                case FileExpiration.ExpirationTime.OneDay:
                    return "一天";
                case FileExpiration.ExpirationTime.FiveDays:
                    return "五天";
                case FileExpiration.ExpirationTime.TenDays:
                    return "十天";
                case FileExpiration.ExpirationTime.ThirtyDays:
                    return "三十天";
                default:
                    throw new ArgumentException($"Invalid Expiration of {expiration}");
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return true;
        }
    }
}
