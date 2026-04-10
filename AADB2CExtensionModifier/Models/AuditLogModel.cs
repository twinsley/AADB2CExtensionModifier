namespace AADB2CExtensionModifier.Models
{
    public class AuditLogModel
    {
        public string ActivityDateTime { get; set; } = string.Empty;
        public string ActivityDateTimeUtc { get; set; } = string.Empty;
        public string ActivityDateTimeLocal { get; set; } = string.Empty;
        public string ActivityDisplayName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string InitiatedBy { get; set; } = string.Empty;
        public string TargetResources { get; set; } = string.Empty;
        public string Result { get; set; } = string.Empty;
        public string ResultReason { get; set; } = string.Empty;
        public string ModifiedProperties { get; set; } = string.Empty;
        public string CorrelationId { get; set; } = string.Empty;
    }
}
