namespace AADB2CExtensionModifier.Models
{
    public class SignInLogModel
    {
        public string CreatedDateTime { get; set; } = string.Empty;
        public string CreatedDateTimeUtc { get; set; } = string.Empty;
        public string CreatedDateTimeLocal { get; set; } = string.Empty;
        public string UserDisplayName { get; set; } = string.Empty;
        public string AppDisplayName { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string ErrorCode { get; set; } = string.Empty;
        public string FailureReason { get; set; } = string.Empty;
        public string AdditionalDetails { get; set; } = string.Empty;
        public string ConditionalAccessStatus { get; set; } = string.Empty;
        public string RiskDetail { get; set; } = string.Empty;
        public string RiskLevel { get; set; } = string.Empty;
        public string ClientAppUsed { get; set; } = string.Empty;
        public string ResourceDisplayName { get; set; } = string.Empty;
    }
}
