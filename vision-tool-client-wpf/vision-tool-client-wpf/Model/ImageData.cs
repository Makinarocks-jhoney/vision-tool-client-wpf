using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace vision_tool_client_wpf.Model
{
    class ImageData
    {
        public string Id { get; set; }
        public string ImageName { get; set; }
        public string ImageSource { get; set; }
        public string? thumbnailsSource { get; set; }
        public string? cloundSource { get; set; }
        public string IdCamera { get; set; }
        public string Camera { get; set; }
        public DateTime? CreateAt { get; set; }
        public string? Line { get; set; }
        public DateTime? AIDecisionDate { get; set; }
        public string? AIResult { get; set; }
        public float? AIScore { get; set; }
        public DateTime? ManualDecisionDate { get; set; }
        public string? ManualWorkerId { get; set; }
        public string? ManualResult { get; set; }
        public DateTime? FinalDecisionDate { get; set; }
        public string Status { get; set; }
        public List<Comment>? Comments { get; set; } = new List<Comment>();
        public List<UserActionLog>? UserLogs { get; set; } = new List<UserActionLog>();
    }

    public class Comment
    {
        public int X { get; set; }
        public int Y { get; set; }
        public string Text { get; set; }
        public string UserId { get; set; } // Thêm: ID người tạo/sửa comment
        public DateTime Timestamp { get; set; } // Thêm: Thời gian tạo/sửa comment
    }
    public class UserActionLog
    {
        public string UserId { get; set; }
        public string Action { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
