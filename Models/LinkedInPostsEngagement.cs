using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LinkedInEngagement.Models
{
    [Table("LinkedInPostDetails")]
    public class LinkedInPostsEngagement
	{
        [Key]
        public int Id { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string OrganizationId { get; set; }

        public int LinkedInPostId { get; set; }

        public int? TotalLikes { get; set; }
        public int? Impressions { get; set; }
        public int? TotalComments { get; set; }
        public int? TotalShares { get; set; }

        public DateOnly DateOfRun { get; set; }
    }
}

