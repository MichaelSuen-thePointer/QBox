using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Foundation;
using Windows.Storage;
using Windows.Web.Http;
using HttpStatusCode = Windows.Web.Http.HttpStatusCode;

namespace QBox
{
    /*
    public class BoxClient : IDisposable
    {
        private static string BoxUploadUrl = "http://box.zjuqsc.com/item/add_item";
        private static string BoxEditUrl = "http://box.zjuqsc.com/item/change_item";
        private static string BoxGetUrl = "http://box.zjuqsc.com/item/get";

        private readonly HttpClient BoxHttpClient;
        public List<UploadFile> UploadFileList { get; }
        public List<DownloadFile> DownloadFileList { get; }
        public BoxClient()
        {
            BoxHttpClient = new HttpClient();
            UploadFileList = new List<UploadFile>();
        }

        public async void UploadFileAsync(StorageFile file)
        {
            var fileContent = await file.OpenAsync(FileAccessMode.Read);
            HttpStreamContent fileStream = new HttpStreamContent(fileContent);
            HttpMultipartFormDataContent formData = new HttpMultipartFormDataContent { { fileStream, "file", file.Path } };
            var asyncOperation = BoxHttpClient.PostAsync(new Uri(BoxUploadUrl), formData);
            int index = UploadFileList.AddEntry(file, DateTime.Now, asyncOperation);
            UploadFileList[index].StartUpload();
        }

        public async void EditFileInfoAsync(int entryIndex, string newToken, string newExpiration)
        {
            List<KeyValuePair<string, string>> requestContents = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("new_token", newToken),
                new KeyValuePair<string, string>("old_token", UploadFileList[entryIndex].Token),
                new KeyValuePair<string, string>("expiration", newExpiration),
                new KeyValuePair<string, string>("secure_id", UploadFileList[entryIndex].SecureId)
            };
            HttpFormUrlEncodedContent formData = new HttpFormUrlEncodedContent(requestContents);

            var responseMessage = await BoxHttpClient.PostAsync(new Uri(BoxEditUrl), formData);

            if (responseMessage.StatusCode == HttpStatusCode.Ok)
            {
                ResponseParser.EditResponse response =
                    await ResponseParser.ParseEditResponseAsync(responseMessage.Content);
                if (response.ErrorCode == 0)
                {
                    UploadFileList[entryIndex].EditInfo(response.Token, response.Expiration);
                }
            }
        }

        public void DownloadFileAsync(string token)
        {
            var downloadOperation = BoxHttpClient.GetAsync(new Uri(BoxGetUrl + "/" + token));
            var index = DownloadFileList.AddEntry(token, downloadOperation);
            DownloadFileList[index].StartDownload();
        }

        ~BoxClient()
        {
            BoxHttpClient.Dispose();
        }

        public void Dispose()
        {
            BoxHttpClient.Dispose();
        }
    }
    */
}