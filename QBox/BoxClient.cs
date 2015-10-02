using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Foundation;
using Windows.Foundation.Diagnostics;
using Windows.Storage;
using Windows.UI.Xaml.Controls;
using Windows.Web.Http;
using QBox.Annotations;

namespace QBox
{

    public class FileExpiration
    {
        public enum ExpirationTime
        {
            OneHour, OneDay, FiveDays, TenDays, ThirtyDays
        }

        public readonly ExpirationTime Expiration;
        public readonly TimeSpan ExpirationSpan;
        public FileExpiration(string expiration)
        {
            switch (expiration)
            {
                case "PT1H":
                    Expiration = ExpirationTime.OneHour;
                    ExpirationSpan = new TimeSpan(0, 1, 0, 0);
                    break;
                case "P1D":
                    Expiration = ExpirationTime.OneDay;
                    ExpirationSpan = new TimeSpan(1, 0, 0, 0);
                    break;
                case "P5D":
                    Expiration = ExpirationTime.FiveDays;
                    ExpirationSpan = new TimeSpan(5, 0, 0, 0);
                    break;
                case "P10D":
                    Expiration = ExpirationTime.TenDays;
                    ExpirationSpan = new TimeSpan(10, 0, 0, 0);
                    break;
                case "P30D":
                    Expiration = ExpirationTime.ThirtyDays;
                    ExpirationSpan = new TimeSpan(30, 0, 0, 0);
                    break;
                default:
                    throw new ArgumentException($"Invalid Expiration of {expiration}");
            }
        }
    }

    public class UploadFile
    {
        public readonly string FileName;
        public readonly DateTime UploadTime;
        public readonly string SecureID;
        public string Token { get; private set; }
        public FileExpiration Expiration { get; private set; }

        public UploadFile(StorageFile file, DateTime uploadTime, string secureID, string token, string expiration)
        {
            FileName = file.Name;
            UploadTime = uploadTime;
            SecureID = secureID;
            Token = token;
            Expiration = new FileExpiration(expiration);
        }

        public bool IsExpired()
        {
            return DateTime.Now - UploadTime > Expiration.ExpirationSpan;
        }

        public void EditInfo(string token, string expiration)
        {
            Token = token;
            Expiration = new FileExpiration(expiration);
        }

        public override bool Equals(object file)
        {
            if (!(file is UploadFile))
            {
                return false;
            }
            return ((UploadFile)file).Token == this.Token;
        }

        public override int GetHashCode()
        {
            return this.SecureID.GetHashCode();
        }

        public override string ToString()
        {
            return $"File: {FileName}, Upload Time: {UploadTime}, Expiration Time Span: {Expiration.ExpirationSpan}, Token: {Token}";
        }
    }

    public static class UploadedFileListExtension
    {
        public static void AddEntry(this List<UploadFile> fileList, StorageFile file, DateTime uploadTime, string secureID, string token, string expiration)
        {
            var newEntry = new UploadFile(file, uploadTime, secureID, token, expiration);
            if (!fileList.Exists((element) => element.Equals(newEntry)))
            {
                fileList.Add(newEntry);
            }
        }
        public static void ClearExpired(this List<UploadFile> fileList)
        {
            foreach (var element in fileList.Where(element => element.IsExpired()))
            {
                fileList.Remove(element);
            }
        }
    }

    public struct UploadResponse
    {
        public int ErrorCode;
        public string Token;
        public string Expiration;
        public string SecureId;
    }

    public struct EditResponse
    {
        public int ErrorCode;
        public string Token;
        public string Url;
    }

    public class BoxClient : IDisposable, INotifyPropertyChanged
    {
        private static string BoxUploadUrl = "http://box.zjuqsc.com/item/add_item";
        private static string BoxEditUrl = "http://box.zjuqsc.com/item/change_item";
        private readonly HttpClient _boxHttpClient;
        public List<UploadFile> FileList { get; }
        private double _Progress;

        public BoxClient()
        {
            _boxHttpClient = new HttpClient();
            FileList = new List<UploadFile>();
        }

        public async void UploadFileAsync(StorageFile file)
        {
            var fileContent = await file.OpenAsync(FileAccessMode.Read);
            HttpStreamContent fileStream = new HttpStreamContent(fileContent);
            HttpMultipartFormDataContent formData = new HttpMultipartFormDataContent {{fileStream, "file", file.Path}};
            var progress = _boxHttpClient.PostAsync(new Uri(BoxUploadUrl), formData);
            progress.Progress = UploadProgressCallback;

            var responseMessage = await progress;

            if (responseMessage.StatusCode == HttpStatusCode.Ok)
            {
                UploadResponse response = await ParseUploadResponseAsync(responseMessage.Content);
                FileList.AddEntry(file, DateTime.Now, response.SecureId, response.Token, response.Expiration);
            }
        }

        public async void EditFileInfoAsync(int entryIndex, string newToken, string newExpiration)
        {
            List<KeyValuePair<string, string>> requestContents = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("new_token", newToken),
                new KeyValuePair<string, string>("old_token", FileList[entryIndex].Token),
                new KeyValuePair<string, string>("expiration", newExpiration),
                new KeyValuePair<string, string>("secure_id", FileList[entryIndex].SecureID)
            };
            HttpFormUrlEncodedContent formData = new HttpFormUrlEncodedContent(requestContents);

            var responseMessage = await _boxHttpClient.PostAsync(new Uri(BoxEditUrl), formData);

            if (responseMessage.StatusCode == HttpStatusCode.Ok)
            {
                UploadResponse response = await ParseUploadResponseAsync(responseMessage.Content);
                FileList[entryIndex].EditInfo(response.Token, response.Expiration);
            }
        }

        public void UploadProgressCallback(IAsyncOperationWithProgress<HttpResponseMessage, HttpProgress> operation,
            HttpProgress progress)
        {
            if (progress.TotalBytesToSend != null)
            {
                Progress = (double) ((double) progress.BytesSent/progress.TotalBytesToSend)*100;
            }
        }

        public async Task<UploadResponse> ParseUploadResponseAsync(IHttpContent response)
        {
            string json = await response.ReadAsStringAsync();
            var jsonObject = JsonObject.Parse(json);
            string token = jsonObject["data"].GetObject()["token"].GetString();
            string secureId = jsonObject["data"].GetObject()["secure_id"].GetString();
            int errorCode = (int) jsonObject["err"].GetNumber();
            string expiration = jsonObject["expiration"].GetString();
            return new UploadResponse
            {
                Token = token,
                ErrorCode = errorCode,
                Expiration = expiration,
                SecureId = secureId
            };
        }

        public async Task<EditResponse> ParseEditResponseAsync(IHttpContent response)
        {
            string json = await response.ReadAsStringAsync();
            var jsonObject = JsonObject.Parse(json);
            int errorCode = (int) jsonObject["status"].GetNumber();
            string newToken = jsonObject["new_token"].GetString();
            string url = jsonObject["url"].GetString();
            return new EditResponse {ErrorCode = errorCode, Token = newToken, Url = url};
        }

        ~BoxClient()
        {
            _boxHttpClient.Dispose();
        }

        public void Dispose()
        {
            _boxHttpClient.Dispose();
        }

        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public double Progress
        {
            get { return _Progress; }
            set
            {
                _Progress = value;
                OnPropertyChanged(nameof(Progress));
            }
        }
    }
}