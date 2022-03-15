using RestSharp;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Communication;
using System.Linq;
using System.Text.Json;

namespace Worker
{
    public static class WorkerUtility
    {
        private static string apiKey = "acc_d1ec4977786a38d";
        private static string apiSecret = "e8662515892944c5fe29832fd16b48e2";

        private static string uploadApi = "https://api.imagga.com/v2/uploads";
        private static string gettingTagsApi = "https://api.imagga.com/v2/tags";

        public static async Task<TaggingJobResult> GetImageTags(string imageUri, int jobId)
        {
            try 
            {
                string basicAuthValue = System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(String.Format("{0}:{1}", apiKey, apiSecret)));

                var uploadResponse = UploadImage(imageUri, basicAuthValue).Result;

                var uploadId = uploadResponse.RootElement.GetProperty("result").GetProperty("upload_id").ToString();

                var taggingResponse = GetTags(uploadId, basicAuthValue);

                return new TaggingJobResult(taggingResponse.Result, FinalState.Succeeded, string.Empty, jobId);
            } catch (Exception e)
            {
                return new TaggingJobResult(string.Empty, FinalState.Error, e.Message.ToString(), jobId);
            }
        }

        public static List<TagModel> PrepareTags(List<string> results, int targetedConfidence = 50)
        {
            List<TagModel> tags = new List<TagModel>();

            foreach (var result in results)
            {
                var yourObject = System.Text.Json.JsonDocument.Parse(result);
                var temp = yourObject.RootElement.GetProperty("result").GetProperty("tags");
                int x = temp.GetArrayLength();
                for (int i = 0; i < x; i++)
                {
                    var k = temp[i];
                    var model = new TagModel(Double.Parse(k.GetProperty("confidence").ToString()), k.GetProperty("tag").GetProperty("en").ToString());

                    if (model.Confidence > targetedConfidence)
                    {
                        if (tags.Any(a => a.Tag == model.Tag))
                        {
                            var conf = tags.Find(a => a.Tag == model.Tag).Confidence;
                            if (conf < model.Confidence)
                            {
                                tags.Find(a => a.Tag == model.Tag).Confidence = Math.Round(model.Confidence, 2);
                            }
                        }
                        else
                        {
                            tags.Add(model);
                        }
                    }
                }
            }

            return tags;
        }

        private static async Task<JsonDocument> UploadImage(string imageUri, string basicAuthValue)
        {
            var uploadClient = new RestClient(uploadApi);
            var uploadRequest = new RestRequest();
            uploadRequest.Method = Method.Post;
            uploadRequest.AddHeader("Authorization", String.Format("Basic {0}", basicAuthValue));
            uploadRequest.AddFile("image", imageUri);

            RestResponse uploadResponse = await uploadClient.ExecuteAsync(uploadRequest);

            return JsonDocument.Parse(uploadResponse.Content);
        }

        private static async Task<string> GetTags(string uploadId, string basicAuthValue)
        {
            var client = new RestClient(gettingTagsApi);

            var request = new RestRequest();
            request.Method = Method.Get;
            request.AddParameter("image_upload_id", uploadId);
            request.AddHeader("Authorization", String.Format("Basic {0}", basicAuthValue));

           // RestResponse response = null;
            var response = await client.ExecuteAsync(request);

            ValidateTaggingResponse(response);

           return response.Content.ToString();
        }

        private static void ValidateTaggingResponse(RestResponse response)
        {
            try
            {
                var result = JsonDocument.Parse(response.Content);
                var temp = result.RootElement.GetProperty("result").GetProperty("tags");
            }
            catch(Exception e)
            {
                throw e;
            }
        }
    }
}
