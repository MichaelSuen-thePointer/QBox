using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Foundation;
using Windows.Storage;
using Windows.Web.Http;
using QBox.Annotations;
using HttpStatusCode = Windows.Web.Http.HttpStatusCode;

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

        public override string ToString()
        {
            switch (Expiration)
            {
                case ExpirationTime.OneHour:
                    return "PT1H";
                case ExpirationTime.OneDay:
                    return "P1D";
                case ExpirationTime.FiveDays:
                    return "P5D";
                case ExpirationTime.TenDays:
                    return "P10D";
                case ExpirationTime.ThirtyDays:
                    return "P30D";
                default:
                    throw new ArgumentException($"Invalid Expiration of {Expiration}");
            }
        }
    }

    public class UploadFile : INotifyPropertyChanged
    {
        public readonly string FileName;
        public readonly DateTime UploadTime;

        private double _progress;
        public double Progress
        {
            get { return _progress; }
            private set
            {
                _progress = value;
                OnPropertyChanged(nameof(Progress));
            }
        }

        private string _secureId;

        public string SecureId
        {
            get
            {
                return _secureId;
            }
            private set
            {
                _secureId = value;
                OnPropertyChanged(nameof(SecureId));
            }
        }

        private string _token;
        public string Token
        {
            get
            {
                return _token;
            }
            private set
            {
                _token = value;
                OnPropertyChanged(nameof(Token));
            }
        }

        private FileExpiration _expiration;
        public FileExpiration Expiration
        {
            get
            {
                return _expiration;
            }
            private set
            {
                _expiration = value;
                OnPropertyChanged(nameof(Expiration));
            }
        }

        public readonly IAsyncOperationWithProgress<HttpResponseMessage, HttpProgress> UploadOperation;
        public UploadFile(StorageFile file, DateTime uploadTime, IAsyncOperationWithProgress<HttpResponseMessage, HttpProgress> uploadOperation)
        {
            FileName = file.Name;
            UploadTime = uploadTime;
            Progress = 0;
            UploadOperation = uploadOperation;
            UploadOperation.Progress = (message, progress) =>
            {
                if (progress.TotalBytesToSend == null) return;
                Progress = (100.0 * progress.BytesSent / progress.TotalBytesToSend) ?? 0;
            };
        }

        public async void StartUpload()
        {
            var uploadResponse = await UploadOperation;
            ResponseParser.UploadResponse response = await ResponseParser.ParseUploadResponseAsync(uploadResponse.Content);
            SecureId = response.SecureId;
            Token = response.Token;
            Expiration = response.Expiration;
        }

        public void CancelUpload()
        {
            if (UploadOperation.Status == AsyncStatus.Started)
            {
                UploadOperation.Cancel();
            }
        }

        public bool IsExpired()
        {
            return DateTime.Now - UploadTime > Expiration.ExpirationSpan;
        }

        public void EditInfo(string newToken, FileExpiration newExpiration)
        {
            Token = newToken;
            Expiration = newExpiration;
        }

        public override int GetHashCode()
        {
            return this.SecureId.GetHashCode();
        }

        public override string ToString()
        {
            return
                $"File: {FileName}, Upload Time: {UploadTime}, Expiration Time Span: {Expiration.ExpirationSpan}, Token: {Token}";
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class DownloadFile
    {
        public readonly string Token;
        public bool Finished;
        public bool Saved;
        public readonly IAsyncOperationWithProgress<HttpResponseMessage, HttpProgress> DownloadOperation;
        public HttpResponseMessage Response;
        public StorageFile LocalStorage { get; private set; }
        public string FullName;
        public DownloadFile(string token, IAsyncOperationWithProgress<HttpResponseMessage, HttpProgress> operation)
        {
            Token = token;
            Finished = false;
            Saved = false;
            DownloadOperation = operation;
        }

        public async void StartDownload()
        {
            Response = await DownloadOperation;
            Finished = true;
            string contentDisposition = Response.Headers["Content-Disposition"];
            int first = contentDisposition.IndexOf('\"');
            int second = contentDisposition.IndexOf('\"', first + 1);
            FullName = contentDisposition.Substring(first + 1, second - first - 1);
        }

        public void CancelDownload()
        {
            if (Finished)
            {
                return;
            }
            DownloadOperation.Cancel();
        }

        public async void SaveFile()
        {
            if (Saved)
            {
                return;
            }
            var fileBuffer = await Response.Content.ReadAsBufferAsync();
            int dotIndex = FullName.LastIndexOf('.');
            var extName = FullName.Substring(dotIndex);
            var savePicker = new Windows.Storage.Pickers.FileSavePicker();
            savePicker.SuggestedStartLocation =
                Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
            savePicker.FileTypeChoices.Add(new KeyValuePair<string, IList<string>>("file", new List<string> { extName }));

            savePicker.SuggestedFileName = FullName;

            LocalStorage = await savePicker.PickSaveFileAsync();
            var writeStream = await LocalStorage.OpenTransactedWriteAsync();
            await writeStream.Stream.WriteAsync(fileBuffer);
            await writeStream.CommitAsync();
            Saved = true;
        }

        public bool IsValid()
        {
            return LocalStorage.IsAvailable;
        }
    }

    public static class FileListExtension
    {
        public static int AddEntry(this List<UploadFile> fileList, StorageFile file, DateTime uploadTime, IAsyncOperationWithProgress<HttpResponseMessage, HttpProgress> operation)
        {
            var newEntry = new UploadFile(file, uploadTime, operation);
            fileList.Add(newEntry);
            return fileList.Count() - 1;
        }

        public static void ClearExpired(this List<UploadFile> fileList)
        {
            foreach (var element in fileList.Where(element => element.IsExpired()))
            {
                fileList.Remove(element);
            }
        }

        public static int AddEntry(this List<DownloadFile> fileList, string token, IAsyncOperationWithProgress<HttpResponseMessage, HttpProgress> operation)
        {
            var newEntry = new DownloadFile(token, operation);
            fileList.Add(newEntry);
            return fileList.Count() - 1;
        }

        public static void ClearInvalid(this List<DownloadFile> fileList)
        {
            foreach (var element in fileList.Where(element => !element.IsValid()))
            {
                fileList.Remove(element);
            }
        }
    }
}