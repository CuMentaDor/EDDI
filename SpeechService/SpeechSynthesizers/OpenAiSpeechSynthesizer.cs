using NAudio.Wave;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using Utilities;

namespace EddiSpeechService.SpeechSynthesizers
{
    public sealed class OpenAiSpeechSynthesizer : IDisposable
    {
        private readonly HttpClient httpClient = new HttpClient();

        public OpenAiSpeechSynthesizer ( ref HashSet<VoiceDetails> voiceStore, SpeechServiceConfiguration configuration )
        {
            var openAiConfig = configuration.OpenAiConfiguration;
            if ( !openAiConfig.Enabled )
            {
                return;
            }

            try
            {
                if ( !string.IsNullOrEmpty( openAiConfig.ApiKey ) )
                {
                    httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue( "Bearer", openAiConfig.ApiKey );
                }

                var baseUri = new Uri(openAiConfig.Endpoint);
                var allVoicesUri = new Uri(baseUri, "/v1/voices/all");
                var audioVoicesUri = new Uri(baseUri, "/v1/audio/voices");

                var allVoicesResponse = httpClient.GetStringAsync(allVoicesUri).Result;
                var allVoices = JsonConvert.DeserializeObject<AllVoicesResponse>(allVoicesResponse);

                foreach ( var voice in allVoices.voices )
                {
                    voiceStore.Add( new VoiceDetails( voice.name, voice.gender, new CultureInfo( voice.language ), "OpenAI" ) );
                }

                var audioVoicesResponse = httpClient.GetStringAsync(audioVoicesUri).Result;
                var audioVoices = JsonConvert.DeserializeObject<AudioVoicesResponse>(audioVoicesResponse);

                foreach ( var alias in audioVoices.voices )
                {
                    var originalVoice = allVoices.voices.FirstOrDefault(v => v.name == alias.name);
                    if ( originalVoice != null )
                    {
                        voiceStore.Add( new VoiceDetails( alias.id, originalVoice.gender, new CultureInfo( originalVoice.language ), "OpenAI" ) );
                    }
                }
            }
            catch ( System.Exception ex )
            {
                Logging.Error( "Failed to fetch OpenAI voices", ex );
            }
        }

        public class AllVoicesResponse
        {
            public Voice[] voices { get; set; }
        }

        public class Voice
        {
            public string gender { get; set; }
            public string language { get; set; }
            public string name { get; set; }
        }

        public class AudioVoicesResponse
        {
            public Alias[] voices { get; set; }
        }

        public class Alias
        {
            public string id { get; set; }
            public string name { get; set; }
        }

        public Stream Speak ( VoiceDetails voiceDetails, string speech, SpeechServiceConfiguration configuration )
        {
            var openAiConfig = configuration.OpenAiConfiguration;
            if ( !openAiConfig.Enabled )
            {
                return null;
            }

            var request = new
            {
                voice = voiceDetails.name,
                input = speech,
                speed =  CalculateSpeed(configuration.Rate),
                response_format = "mp3"
            };

            var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");

            if ( !string.IsNullOrEmpty( openAiConfig.ApiKey ) )
            {
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue( "Bearer", openAiConfig.ApiKey );
            }

            var response = httpClient.PostAsync(openAiConfig.Endpoint, content).Result;

            if ( response.IsSuccessStatusCode )
            {
                using ( var mp3Stream = response.Content.ReadAsStreamAsync().Result )
                using ( var reader = new Mp3FileReader( mp3Stream ) )
                {
                    var wavStream = new MemoryStream();
                    WaveFileWriter.WriteWavFileToStream( wavStream, reader );
                    wavStream.Position = 0;
                    return wavStream;
                }
            }
            else
            {
                var error = response.Content.ReadAsStringAsync().Result;
                Logging.Error( "OpenAI TTS request failed: " + error );
                return null;
            }
        }

        private double CalculateSpeed ( int rate ) => 1.0 + ( rate / 10.0 ) * ( rate >= 0 ? 1.0 : 0.75 );

        public void Dispose ()
        {
            httpClient?.Dispose();
        }
    }
}
