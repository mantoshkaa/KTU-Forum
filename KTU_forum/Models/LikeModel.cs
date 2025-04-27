namespace KTU_forum.Models
{
    public class LikeModel
    {
        public int MessageId { get; set; }
        public MessageModel Message { get; set; }

        public int UserId { get; set; }
        public UserModel User { get; set; }
    }
}
