using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Popups;
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
    public sealed partial class StartPage : Page
    {
        private MainPage rootPage = MainPage.Current;
        private CancellationTokenSource cancellationToken;

        private IBuffer fileBuffer;
        private string fullName;

        private string boxUploadUrl = "http://box.zjuqsc.com/item/add_item";
        private string BoxGetUrl = "http://box.zjuqsc.com/item/get";

        public StartPage()
        {
            this.InitializeComponent();
            ButtonStateControl(PageState.Nothing);
            ButtonStateControl(PageState.HasSaved);

            cancellationToken = new CancellationTokenSource();
            TokenBox.Text = "";
            ButtonStateControl(PageState.Nothing);
            ButtonStateControl(PageState.HasSaved);
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

        private async void UploadButton_Click(object sender, RoutedEventArgs e)
        {
            FileOpenPicker openPicker = new FileOpenPicker
            {
                ViewMode = PickerViewMode.Thumbnail,
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary
            };
            openPicker.FileTypeFilter.Clear();
            openPicker.FileTypeFilter.Add("*");
            var file = await openPicker.PickSingleFileAsync();
            await UploadFile(file);
        }

        private void UploadProgressHandler(HttpProgress progress)
        {
            if (progress.TotalBytesToSend == null) return;
            Progress.Value = (100.0 * progress.BytesSent / progress.TotalBytesToSend) ?? 0;
            BytesReceived.Text = $"已上传: {(100.0 * progress.BytesSent / progress.TotalBytesToSend),3}%";
        }

        private void DownloadProgressHandler(HttpProgress progress)
        {
            BytesReceived.Text = $"已下载: {progress.BytesReceived}B";
        }

        private enum PageState
        {
            Nothing,
            Uploading,
            Uploaded,
            Downloading,
            Downloaded,
            WaitingSave,
            HasSaved
        }

        private void ButtonStateControl(PageState state)
        {
            switch (state)
            {
                case PageState.Nothing:
                    DownloadButton.IsEnabled = true;
                    UploadButton.IsEnabled = true;
                    CancelDownladButton.IsEnabled = false;
                    CancelUploadButton.IsEnabled = false;
                    break;
                case PageState.Downloading:
                    DownloadButton.IsEnabled = false;
                    UploadButton.IsEnabled = false;
                    CancelDownladButton.IsEnabled = true;
                    CancelUploadButton.IsEnabled = false;
                    break;
                case PageState.Downloaded:
                    CancelDownladButton.IsEnabled = false;
                    break;
                case PageState.Uploading:
                    DownloadButton.IsEnabled = false;
                    UploadButton.IsEnabled = false;
                    CancelDownladButton.IsEnabled = false;
                    CancelUploadButton.IsEnabled = true;
                    break;
                case PageState.Uploaded:
                    DownloadButton.IsEnabled = true;
                    UploadButton.IsEnabled = true;
                    CancelDownladButton.IsEnabled = false;
                    CancelUploadButton.IsEnabled = false;
                    break;
                case PageState.WaitingSave:
                    DownloadButton.IsEnabled = true;
                    UploadButton.IsEnabled = true;
                    CancelDownladButton.IsEnabled = false;
                    CancelUploadButton.IsEnabled = false;
                    SaveButton.IsEnabled = true;
                    break;
                case PageState.HasSaved:
                    SaveButton.IsEnabled = false;
                    break;
                default:
                    throw new Exception("No such state.");
            }
        }

        private void CancelUploadButton_Click(object sender, RoutedEventArgs e)
        {
            cancellationToken.Cancel();
            cancellationToken.Dispose();
            cancellationToken = new CancellationTokenSource();
        }

        private async void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            if (TokenBox.Text == string.Empty)
            {
                rootPage.NotifyUser("请先输入代码", NotifyType.ErrorMessage);
                return;
            }
            if (fileBuffer != null)
            {
                var messageDialog = new ContentDialog();
                messageDialog.IsPrimaryButtonEnabled = true;
                messageDialog.PrimaryButtonText = "是";
                messageDialog.SecondaryButtonText = "否";
                messageDialog.Content = "有尚未保存的文件，继续下载新闻将丢弃未保存的文件，是否继续？";
                var result = await messageDialog.ShowAsync();
                if (result != ContentDialogResult.Primary)
                {
                    return;
                }
                else
                {
                    fileBuffer = null;
                    fullName = null;
                    ButtonStateControl(PageState.HasSaved);
                }
            }
            IProgress<HttpProgress> progress = new Progress<HttpProgress>(DownloadProgressHandler);
            var downloadOperation = rootPage.BoxClient.GetAsync(new Uri(BoxGetUrl + "/" + TokenBox.Text));
            rootPage.NotifyUser("正在下载", NotifyType.StatusMessage);
            ButtonStateControl(PageState.Downloading);
            try
            {
                var downloadResponse = await downloadOperation.AsTask(cancellationToken.Token, progress);
                if (downloadResponse.StatusCode == HttpStatusCode.NotFound)
                {
                    rootPage.NotifyUser("文件不存在", NotifyType.ErrorMessage);
                    ButtonStateControl(PageState.Nothing);
                }
                else
                {
                    string contentDisposition = downloadResponse.Content.Headers["Content-Disposition"];
                    int first = contentDisposition.IndexOf('\"');
                    int second = contentDisposition.IndexOf('\"', first + 1);
                    fullName = contentDisposition.Substring(first + 1, second - first - 1);

                    rootPage.NotifyUser($"文件下载完成，文件名：{fullName}，请点击保存", NotifyType.StatusMessage);
                    ButtonStateControl(PageState.Downloaded);
                    ButtonStateControl(PageState.WaitingSave);
                    fileBuffer = await downloadResponse.Content.ReadAsBufferAsync();
                }
            }
            catch (TaskCanceledException)
            {
                rootPage.NotifyUser("下载已被终止", NotifyType.ErrorMessage);
                ButtonStateControl(PageState.Nothing);
            }
            catch (Exception ex)
            {
                rootPage.NotifyUser("错误：" + ex.Message, NotifyType.ErrorMessage);
                ButtonStateControl(PageState.Nothing);
            }
            finally
            {
                Progress.Value = 0;
                TokenBox.Text = string.Empty;
            }
        }

        private void CancelDownladButton_Click(object sender, RoutedEventArgs e)
        {
            cancellationToken.Cancel();
            cancellationToken.Dispose();
            cancellationToken = new CancellationTokenSource();
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (fileBuffer == null)
            {
                rootPage.NotifyUser("没有文件尚未保存", NotifyType.ErrorMessage);
            }
            else
            {
                int dotIndex = fullName.LastIndexOf('.');
                var extName = fullName.Substring(dotIndex);

                var savePicker = new FileSavePicker();
                savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                savePicker.FileTypeChoices.Add(new KeyValuePair<string, IList<string>>("file", new List<string> { extName }));

                savePicker.SuggestedFileName = fullName;

                var localStorage = await savePicker.PickSaveFileAsync();
                try
                {
                    var writeStream = await localStorage.OpenTransactedWriteAsync();
                    await writeStream.Stream.WriteAsync(fileBuffer);
                    await writeStream.CommitAsync();
                    writeStream.Dispose();
                    rootPage.NotifyUser($"文件已保存，文件名：{fullName}", NotifyType.StatusMessage);
                    fileBuffer = null;
                    fullName = null;
                    ButtonStateControl(PageState.HasSaved);
                }
                catch (NullReferenceException)
                {

                }
            }
        }

        private async void Grid_Drop(object sender, DragEventArgs e)
        {
            var elements = await e.DataView.GetStorageItemsAsync();
            foreach (var file in elements)
            {
                if (file != null)
                {
                    await UploadFile(file as StorageFile);
                    Task.Delay(500);
                }
            }
        }

        private async Task UploadFile(IStorageFile file)
        {
            IProgress<HttpProgress> progress = new Progress<HttpProgress>(UploadProgressHandler);
            try
            {
                var fileContent = await file.OpenAsync(FileAccessMode.Read);
                ButtonStateControl(PageState.Uploading);
                var fileStream = new HttpStreamContent(fileContent);
                HttpMultipartFormDataContent formData = new HttpMultipartFormDataContent { { fileStream, "file", file.Path } };
                var asyncTask = rootPage.BoxClient.PostAsync(new Uri(boxUploadUrl), formData).AsTask(cancellationToken.Token, progress);
                ResponseParser.UploadResponse result;
                try
                {
                    rootPage.NotifyUser("正在上传", NotifyType.StatusMessage);
                    var responseMessage = await asyncTask;
                    Debugger.Break();
                    result = await ResponseParser.ParseUploadResponseAsync(responseMessage.Content);
                    var newFile = new UploadedFile(file, DateTime.Now, result.SecureId, result.Token, result.Expiration);
                    rootPage.UploadFileList.Add(newFile);
                    rootPage.NotifyUser("上传成功，信息已保存在已上传列表中", NotifyType.StatusMessage);
                    Debugger.Break();
                    ButtonStateControl(PageState.Uploaded);
                }
                catch (TaskCanceledException)
                {
                    rootPage.NotifyUser("上传已被终止", NotifyType.ErrorMessage);
                }
                catch (Exception ex)
                {
                    rootPage.NotifyUser("错误：" + ex.Message, NotifyType.ErrorMessage);
                    Debugger.Break();
                }
                finally
                {
                    Progress.Value = 0;
                    ButtonStateControl(PageState.Nothing);
                }
            }
            catch (NullReferenceException)
            {

            }
        }

        private void Grid_OnDragOver(object sender, DragEventArgs e)
        {
            //设置操作类型
            e.AcceptedOperation = DataPackageOperation.Copy;

            //设置提示文字
            e.DragUIOverride.Caption = "拖放此处即可添加文件 o(^▽^)o";

            ////是否显示拖放时的文字 默认为true
            //e.DragUIOverride.IsCaptionVisible = true;

            ////是否显示文件图标，默认为true
            //e.DragUIOverride.IsContentVisible = true;

            ////Caption 前面的图标是否显示。默认为 true
            //e.DragUIOverride.IsGlyphVisible = true;

            ////自定义文件图标，可以设置一个图标
            //e.DragUIOverride.SetContentFromBitmapImage(new BitmapImage(new Uri("ms-appx:///Assets/copy.jpg")));
        }
    }
}
