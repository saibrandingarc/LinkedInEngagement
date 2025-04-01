using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LinkedInEngagement.Models
{
    [Table("ClientDetails")]
    public class ClientDetail
	{
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [StringLength(255)]
        public string? Name { get; set; }

        public string? PublicURL { get; set; }

        public string? PropertyId { get; set; }

        public Guid? ClientUID { get; set; }

        public string? SearchConsoleId { get; set; }

        public string? CFPBName { get; set; }

        public DateTime? WebsiteLaunchDate { get; set; }

        public string? ZohoAccountId { get; set; }

        public string? YtChannelId { get; set; }
        
        public string? LinkedinId { get; set; }

        public DateOnly? LinkedInRunDate { get; set; }

        public bool? MarkettingClient { get; set; }
    }
}

