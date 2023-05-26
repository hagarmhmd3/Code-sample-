using System.ComponentModel.DataAnnotations.Schema;

namespace DentistPortal_API.Model
{
    public class Tool
    {
        public Guid Id { get; set; }
        public string ToolName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public double ToolPrice { get; set; }
        public string SellerLocation { get; set; } = string.Empty;
        public string ContactNumber { get; set; } = string.Empty;
        public Guid SellerIdDoctor { get; set; }
        public bool IsActive { get; set; }
        public string ToolStatus { get; set; } = string.Empty;
        [ForeignKey("SellerIdDoctor")]
        public Dentist Dentist { get; set; } = new();
    }
}
