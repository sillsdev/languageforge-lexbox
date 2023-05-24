namespace LexData.Redmine;

public partial class RmMember
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int ProjectId { get; set; }

    public DateTime? CreatedOn { get; set; }

    public bool MailNotification { get; set; }
}
