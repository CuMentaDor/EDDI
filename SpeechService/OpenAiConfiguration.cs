using Newtonsoft.Json;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace EddiSpeechService
{
    public class OpenAiConfiguration : INotifyPropertyChanged
    {
        [JsonProperty("enabled")]
        public bool Enabled { get; set; } = true;

        [JsonProperty("endpoint")]
        public string Endpoint { get; set; } = "http://localhost:5050/v1/audio/speech";

        [JsonProperty("voice")]
        public string Voice { get; set; } = "en-US-AriaNeural";

        [JsonProperty("apiKey")]
        public string ApiKey { get; set; } = "your_api_key_here";

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged ( [CallerMemberName] string propertyName = null )
        {
            PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( propertyName ) );
        }
    }
}
