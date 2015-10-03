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
    static class ResponseParser
    {
        public struct UploadResponse
        {
            public int ErrorCode;
            public string Token;
            public FileExpiration Expiration;
            public string SecureId;
        }

        public struct EditResponse
        {
            public int ErrorCode;
            public string Token;
            public string Url;
            public FileExpiration Expiration;
        }

        public static async Task<UploadResponse> ParseUploadResponseAsync(IHttpContent response)
        {
            string json = await response.ReadAsStringAsync();
            var jsonObject = JsonObject.Parse(json);
            string token = jsonObject["data"].GetObject()["token"].GetString();
            string secureId = jsonObject["data"].GetObject()["secure_id"].GetString();
            int errorCode = (int)jsonObject["err"].GetNumber();
            string expiration = jsonObject["expiration"].GetString();
            return new UploadResponse
            {
                Token = token,
                ErrorCode = errorCode,
                Expiration = new FileExpiration(expiration),
                SecureId = secureId
            };
        }

        public static async Task<EditResponse> ParseEditResponseAsync(IHttpContent response)
        {
            string json = await response.ReadAsStringAsync();
            var jsonObject = JsonObject.Parse(json);
            int errorCode = (int)jsonObject["status"].GetNumber();
            string newToken = jsonObject["new_token"].GetString();
            string url = jsonObject["url"].GetString();
            string expiration = jsonObject["expiration"].GetString();
            return new EditResponse
            {
                ErrorCode = errorCode,
                Token = newToken,
                Url = url,
                Expiration = new FileExpiration(expiration)
            };
        }
    }
}