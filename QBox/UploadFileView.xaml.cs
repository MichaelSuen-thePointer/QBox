using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.WiFiDirect;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.Pickers.Provider;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Web.Http;

// “空白页”项模板在 http://go.microsoft.com/fwlink/?LinkId=234238 上提供

namespace QBox
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class UploadFileView : Page
    {
        private static string BoxEditUrl = "http://box.zjuqsc.com/item/change_item";
        public MainPage rootPage = MainPage.Current;

        public UploadFileView()
        {
            this.InitializeComponent();
            EditView.Visibility = Visibility.Collapsed;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = base.Frame.CanGoBack ? AppViewBackButtonVisibility.Visible : AppViewBackButtonVisibility.Collapsed;
            SystemNavigationManager.GetForCurrentView().BackRequested += MainPage_BackRequested;
            base.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            SystemNavigationManager.GetForCurrentView().BackRequested -= MainPage_BackRequested;
            base.OnNavigatedFrom(e);
        }

        private void MainPage_BackRequested(object sender, BackRequestedEventArgs e)
        {
            base.Frame.GoBack();
        }

        private void UploadedItems_OnSelectionChanged_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListBox listBox = sender as ListBox;
            UploadFile item = listBox.SelectedItem as UploadFile;
            UpdateEditView(item);
        }

        private void UpdateEditView(UploadFile file)
        {
            EditView.Visibility = Visibility.Visible;
            switch (file.Expiration.Expiration)
            {
                case FileExpiration.ExpirationTime.OneHour:
                    PT1H.IsChecked = true;
                    break;
                case FileExpiration.ExpirationTime.OneDay:
                    P1D.IsChecked = true;
                    break;
                case FileExpiration.ExpirationTime.FiveDays:
                    P5D.IsChecked = true;
                    break;
                case FileExpiration.ExpirationTime.TenDays:
                    P10D.IsChecked = true;
                    break;
                case FileExpiration.ExpirationTime.ThirtyDays:
                    P30D.IsChecked = true;
                    break;
                default:
                    throw new ArgumentException("This part cannot be reached!!!");
            }
            NewTokenBox.Text = file.Token;
        }

        private async void SubmitChange_OnClick(object sender, RoutedEventArgs e)
        {
            /*
            UploadFile choseFile = UploadedItems.SelectedItem as UploadFile;
            FileExpiration newExpiration =
            List < KeyValuePair < string, string>> requestContents = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("new_token", NewTokenBox.Text),
                new KeyValuePair<string, string>("old_token", choseFile?.Token),
                new KeyValuePair<string, string>("expiration", "P30D"),
                new KeyValuePair<string, string>("secure_id", choseFile?.SecureId)
            };
            HttpFormUrlEncodedContent formData = new HttpFormUrlEncodedContent(requestContents);

            var responseMessage = await rootPage.BoxClient.PostAsync(new Uri(BoxEditUrl), formData);

            if (responseMessage.StatusCode == HttpStatusCode.Ok)
            {
                ResponseParser.EditResponse response =
                    await ResponseParser.ParseEditResponseAsync(responseMessage.Content);
                if (response.ErrorCode == 0)
                {
                    choseFile.EditInfo(response.Token, response.Expiration);
                }
                else
                {
                    rootPage.NotifyUser(response.ErrorMessage, NotifyType.ErrorMessage);
                }
            }
            else
            {
                rootPage.NotifyUser("网络连接错误", NotifyType.ErrorMessage);
            }
            */
        }
        /*
        string GetRadioButtonSelection()
        {
            if (PT1H.IsChecked)
            {
                return "PT1H";
            }
            if (P1D.IsChecked)
            {
                return "P1D";
            }
            if (P5D.IsChecked)
            {
                return "P5D";
            }
            break;
                case FileExpiration.ExpirationTime.FiveDays:
                    P5D.IsChecked = true;
            break;
                case FileExpiration.ExpirationTime.TenDays:
                    P10D.IsChecked = true;
            break;
                case FileExpiration.ExpirationTime.ThirtyDays:
                    P30D.IsChecked = true;
        }*/
    }

    public class ExpirationBindingConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var expiration = (FileExpiration)value;
            switch (expiration.Expiration)
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
