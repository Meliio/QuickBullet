namespace QuickBullet.Models
{
    public class ConfigSettings
    {
        public string Name { get; set; } = string.Empty;
        public string AdditionalInfo { get; set; } = string.Empty;
        public IEnumerable<CustomInput> CustomInputs { get; set; } = Array.Empty<CustomInput>();
        public IEnumerable<InputRule> InputRules { get; set; } = Array.Empty<InputRule>();
    }
}
