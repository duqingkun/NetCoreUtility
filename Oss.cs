using Aliyun.OSS;
using System;
using System.Collections.Generic;
using System.IO;

namespace AliyunHelper
{
    class Oss
    {
        private string _endPoint;
        private string _accessKeyId;
        private string _accessKeySecret;

        private readonly DateTime _gmtDateTime = new DateTime(1970, 1, 1, 0, 0, 0);

        public enum Acl
        {
            Private = CannedAccessControlList.Private,                  //私有
            PublicRead = CannedAccessControlList.PublicRead,            //公共读
            PublicReadWrite = CannedAccessControlList.PublicReadWrite,  //公共读写
        }

        public Oss(string endPoint, string accessKeyId, string accessKeySecret)
        {
            _endPoint = endPoint;
            _accessKeyId = accessKeyId;
            _accessKeySecret = accessKeySecret;
        }

        public void SetEndpoint(string endPoint)
        {
            _endPoint = endPoint;
        }

        public void SetAccessKey(string accessKeyId, string accessKeySecret)
        {
            _accessKeyId = accessKeyId;
            _accessKeySecret = accessKeySecret;
        }

        public string PutFile(string filePath, string bucketName, string folder)
        {
            string ossUrl = "";
            try
            {
                string fileName = $"{Path.GetFileNameWithoutExtension(filePath)}-{(long)(DateTime.Now - _gmtDateTime).TotalMilliseconds}{Path.GetExtension(filePath)}";
                folder = folder.TrimEnd(new[] {'\\', '/'});
                string key = string.IsNullOrEmpty(folder) ? $"{fileName}" 
                    : $"{folder}/{fileName}";
                OssClient client = new OssClient(_endPoint, _accessKeyId, _accessKeySecret);
                client.PutObject(bucketName, key, filePath);
                ossUrl = $"{bucketName}.{_endPoint.TrimEnd(new[] { '\\', '/' })}/{key}";
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }

            return ossUrl;
        }

        public void DeleteFile(string fileUrl, string bucketName)
        {
            string bucketDomain = $"{bucketName}.{_endPoint}/";
            int index = fileUrl.IndexOf(bucketDomain, StringComparison.Ordinal);
            if (index < 0)
            {
                throw new Exception("Invalid file url.");
            }

            index += bucketDomain.Length;

            try
            {
                string key = fileUrl.Substring(index);
                OssClient client = new OssClient(_endPoint, _accessKeyId, _accessKeySecret);
                client.DeleteObject(bucketName, key);
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        public void CreateBucket(string bucketName, Acl acl)
        {
            try
            {
                OssClient client = new OssClient(_endPoint, _accessKeyId, _accessKeySecret);
                client.CreateBucket(bucketName);
                client.SetBucketAcl(bucketName, (CannedAccessControlList)acl);
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        public void DeleteBucket(string bucketName)
        {
            try
            {
                OssClient client = new OssClient(_endPoint, _accessKeyId, _accessKeySecret);
                client.DeleteBucket(bucketName);
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        public List<string> GetFileUrls(string bucketName, string folder)
        {
            List<string> fileUrls = new List<string>();
            try
            {
                folder = folder.TrimEnd(new[] { '\\', '/' }) + "/";
                OssClient client = new OssClient(_endPoint, _accessKeyId, _accessKeySecret);
                ObjectListing result = null;
                string nextMarker = string.Empty;
                do
                {
                    // 每页列举的文件个数通过maxKeys指定，超过指定数将进行分页显示。
                    var listObjectsRequest = new ListObjectsRequest(bucketName)
                    {
                        Prefix = folder,
                        Marker = nextMarker,
                        MaxKeys = 100
                    };
                    result = client.ListObjects(listObjectsRequest);
                    foreach (var summary in result.ObjectSummaries)
                    {
                        string url = summary.Key.Remove(0, folder.Length);
                        if (!string.IsNullOrEmpty(url))
                        {
                            fileUrls.Add($"{bucketName}.{_endPoint.TrimEnd(new[] { '\\', '/' })}/{summary.Key}");
                        }
                    }
                    nextMarker = result.NextMarker;
                } while (result.IsTruncated);
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }

            return fileUrls;
        }
    }
}
