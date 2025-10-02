using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vision_tool_client_wpf.Settings
{
    public class MinIOConfig
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Type { get; set; } = "Minio";
        public string ServiceUrl { get; set; } = "https://s3.platform-prototype.mrxrunway.ai/great";
        public string ServiceStorageUrl { get; set; } = "produce";
        public string AccessKey { get; set; } = "RUNWAY-SANGWOO-7A8216DF";
        public string SecretKey { get; set; } = "3e41c89af72702e3806e5727c0cc4eab3feab62c22c02454d1a69f8cf2cff2e7";
        public string BucketName { get; set; } = "great";
        public bool UseSSL => ServiceUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
    }
}
