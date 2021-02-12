/*
 * Copyright 2015 Google Inc. All Rights Reserved.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 *  Unless required by applicable law or agreed to in writing, software
 *  distributed under the License is distributed on an "AS IS" BASIS,
 *  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *  See the License for the specific language governing permissions and
 *  limitations under the License.
 *
 */

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.Util.Store;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using Newtonsoft.Json;
using YouTubeModels;
using YouTubeModels.YoutubeCategories;

namespace Google.Apis.YouTube.Samples
{
    /// <summary>
    /// YouTube Data API v3 sample: upload a video.
    /// Relies on the Google APIs Client Library for .NET, v1.7.0 or higher.
    /// See https://code.google.com/p/google-api-dotnet-client/wiki/GettingStarted
    /// </summary>
    internal class UploadVideo
    {

        DateTime ParseVideoDate(string fileName)
        {
            var fn = Path.GetFileNameWithoutExtension(fileName); //ouP-2021-02-11_12-27-41.avi
            fn = fn.Replace("ouP-", "");
            var date = DateTime.ParseExact(fn, "yyyy-MM-dd_HH-mm-ss", CultureInfo.InvariantCulture);
            return date;
        }
        [STAThread]
        static void Main(string[] args)
        {
            string path = @"C:\Users\alexh\source\repos\alexhiggins732\CsharpMouseKeyboardDesktopLibrary\GlobalMacroRecorder\bin\Release\";
            var file = "ouP-2021-02-11_12-27-41.avi";
            var fileName = Path.Combine(path, file);
            if (args.Length == 0 && System.Diagnostics.Debugger.IsAttached)
            {
                args = new[] { fileName };
            }
            if (args.Length > 0)
            {
                if (!File.Exists(args[0]))
                {
                    Console.WriteLine($"File does not exist {args[0]}");
                    return;
                }
            }
            else
            {
                Console.WriteLine("You must specify a file name");
                return;
            }
            Console.WriteLine("YouTube Data API: Upload Video");
            Console.WriteLine("==============================");

            try
            {
                new UploadVideo().Run(fileName).Wait();
            }
            catch (AggregateException ex)
            {
                foreach (var e in ex.InnerExceptions)
                {
                    Console.WriteLine("Error: " + e.Message);
                }
            }
        }

        private async Task Run(string fileName)
        {
            UserCredential credential;
            using (var stream = new FileStream(@"C:\CredsFolder\youtube_uploader_client_secrets.json", FileMode.Open, FileAccess.Read))
            {
                credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    // This OAuth 2.0 access scope allows an application to upload files to the
                    // authenticated user's YouTube channel, but doesn't allow other types of access.
                    new[] { YouTubeService.Scope.YoutubeUpload },
                    "xela20redna@gmail.com",
                    CancellationToken.None
                );
            }


            var youtubeService = new YouTubeService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = Assembly.GetExecutingAssembly().GetName().Name
            });

            var video = new Video();
            video.Snippet = new VideoSnippet();

            var videoDate = ParseVideoDate(fileName);

            string title = $"COINBASE PRO - {videoDate}";
            var description = $"Data Video for AI Machine Learning Training Bot - COINBASE PRO recording on  {videoDate}";
            video.Snippet.Title = title; // "Default Video Title";
            video.Snippet.Description = description; // "Default Video Description";
            video.Snippet.Tags = new string[] { "Coinbase", "Coinbase Pro" };
            var category = CategoryCollection.GetByName("Science & Technology");
            video.Snippet.CategoryId = $"{category.id}"; //"22"; // See https://developers.google.com/youtube/v3/docs/videoCategories/list
            video.Status = new VideoStatus();
            video.Status.PrivacyStatus = PrivacyStatus.Unlisted;// "unlisted"; // or "private" or "public"
            var filePath = fileName;// @"REPLACE_ME.mp4"; // Replace with path to actual movie file.
            TotalSize = new FileInfo(fileName).Length;
            using (var fileStream = new FileStream(filePath, FileMode.Open))
            {
                var videosInsertRequest = youtubeService.Videos.Insert(video, "snippet,status", fileStream, "video/*");
                videosInsertRequest.ProgressChanged += videosInsertRequest_ProgressChanged;
                videosInsertRequest.ResponseReceived += videosInsertRequest_ResponseReceived;

                await videosInsertRequest.UploadAsync();
            }
        }

        long TotalSize = 0;
        void videosInsertRequest_ProgressChanged(Google.Apis.Upload.IUploadProgress progress)
        {
            switch (progress.Status)
            {
                case UploadStatus.Uploading:
                    Console.WriteLine($"{progress.BytesSent} bytes sent. ({ ((double)progress.BytesSent / TotalSize).ToString("P")})");
                    break;

                case UploadStatus.Failed:
                    Console.WriteLine("An error prevented the upload from completing.\n{0}", progress.Exception);
                    break;
            }
        }

        void videosInsertRequest_ResponseReceived(Video video)
        {
            Console.WriteLine("Video id '{0}' was successfully uploaded.", video.Id);
        }
    }


}
namespace YouTubeModels
{
    public class PrivacyStatus
    {
        public const string Unlisted = "unlisted"; // or "private" or "public"
        public const string Private = "private";
        public const string Public = "public";
    }
    namespace YoutubeCategories
    {


        public class CategoryCollection
        {
            public static CategoryCollection Instance;
            static CategoryCollection()
            {
                var json = File.ReadAllText("youtube-categories.json");
                Instance = JsonConvert.DeserializeObject<CategoryCollection>(json);
            }
            public string kind { get; set; }
            public string etag { get; set; }
            [JsonProperty("items")]
            public Category[] Categories { get; set; }
            public static CategoryCollection Load() => Instance;
            public static Category GetByName(string name)
            {
                var result = Instance.Categories.FirstOrDefault(x => string.Compare(x.snippet.title, name, true) == 0);
                return result;
            }

        }

        public class Category
        {
            public string kind { get; set; }
            public string etag { get; set; }
            public string id { get; set; }
            public Snippet snippet { get; set; }
        }

        public class Snippet
        {
            public string title { get; set; }
            public bool assignable { get; set; }
            public string channelId { get; set; }
        }
    }
}
