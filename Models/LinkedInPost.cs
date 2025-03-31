using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LinkedInEngagement.Models
{
    [Table("LinkedInPosts")]
    public class LinkedInPost
    {
        [Key]
        public int Id { get; set; }

        public string OrganizationId { get; set; }
        public string? PostId { get; set; }
        public string? PostContent { get; set; }
        public DateTime CreatedOn { get; set; }
        public string? ResolvedURL { get; set; }
        public int? PostsCount { get; set; }
        public DateTime DateOfRun { get; set; } = DateTime.UtcNow;
    }

}