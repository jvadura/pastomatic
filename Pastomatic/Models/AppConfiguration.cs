namespace Pastomatic.Models
{
    public class HotkeyConfig
    {
        public string Key { get; set; } = "Insert";
        public bool Suppress { get; set; } = true;
    }

    public class ImageConfig
    {
        public double MaxMegapixels { get; set; } = 2.0;
        public string SaveFolder { get; set; } = @"E:\claude\images";
        public string ClipboardFormat { get; set; } = "Read {wslpath}";
    }

    public class VisionConfig
    {
        public string Endpoint { get; set; } = "http://10.0.0.244:8000/v1";
        public string Model { get; set; } = "qwen2.5-vl-72b-instruct";
        public string ApiKey { get; set; } = "";
        public int MaxTokens { get; set; } = 4096;
        public double Temperature { get; set; } = 0.1;
        public int TimeoutSeconds { get; set; } = 120;
        public string SystemPrompt { get; set; } = "You are an expert image analyst. Provide an extremely detailed description of what you see in this image.";
    }

    public class UiConfig
    {
        public bool CloseOnFocusLoss { get; set; } = true;
        public int SuccessDisplayMs { get; set; } = 1500;
    }
}
