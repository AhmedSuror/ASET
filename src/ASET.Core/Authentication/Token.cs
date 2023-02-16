using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ASET.Core
{
    public enum EtEnvironment
    {
        PreProduction,
        SIT,
        Production
    }
}

namespace ASET.Core.Authentication
{

    public static class Token
    {
        private const string signature = "/connect/token";

        private static HttpClient _httpClient;
        private static Timer _lifeSpanTimer;
        private static AsyncOperation _lifeSpanTimerAsyncOperation;
        private static string _value;
        private static string _type;
        private static int? _expireIn;
        private static DateTime? _generationTime;
        private static DateTime? _expiryTime;
        private static string _scope;
        private static ISecretVault _vault;
        private static EtEnvironment _environment;
        private static string _idBaseURI;
        private static string _serviceBaseURI;

        public static HttpClient HttpClient { get => _httpClient; }
        /// <summary>
        /// The vault that holds user secrets for ETA Authentication.
        /// </summary>
        public static ISecretVault Vault { get => _vault; set => _vault = value; }
        /// <summary>
        /// Gets or sets the current environment for the token.
        /// </summary>
        public static EtEnvironment Environment
        {
            get => _environment;
            set
            {
                _environment = value;
                switch (value)
                {
                    case EtEnvironment.PreProduction:
                        _serviceBaseURI = "https://preprod.invoicing.eta.gov.eg";
                        _idBaseURI = "https://id.preprod.eta.gov.eg";
                        break;
                    case EtEnvironment.SIT:
                        _serviceBaseURI = "https://sit.invoicing.eta.gov.eg";
                        _idBaseURI = "https://id.sit.eta.gov.eg";
                        break;
                    case EtEnvironment.Production:
                        _serviceBaseURI = "https://invoicing.eta.gov.eg";
                        _idBaseURI = "https://id.eta.gov.eg";
                        break;
                }
            }
        }
        /// <summary>
        /// Gets the ETA Service URI for the current environment.
        /// </summary>
        public static string ServiceBaseURI => _serviceBaseURI;
        /// <summary>
        /// Gets the ETA Identity Service URI for the current environment
        /// </summary>
        public static string IdBaseURI => _idBaseURI;
        /// <summary>
        /// Gets the current type of the token (e.g Bearer).
        /// </summary>
        public static string Type => _type;
        /// <summary>
        /// Gets the value of the currently generated token.
        /// </summary>
        public static string Value => _value;
        /// <summary>
        /// Gets the exact date and time of the currently generated token.
        /// </summary>
        public static DateTime? GenerationTime => _generationTime;
        /// <summary>
        /// Gets the exact date and time of when the currently generated token will expire.
        /// </summary>
        public static DateTime? ExpiryTime => _expiryTime;
        /// <summary>
        /// Gets the total seconds that should pass from token generation time for it to be expired.
        /// </summary>
        public static int? ExpireIn => _expireIn;
        /// <summary>
        /// Gets the scope of the currently generated token.
        /// </summary>
        public static string Scope => _scope;

        /// <summary>
        /// Occurs right before generating the token.
        /// </summary>
        public static event EventHandler TokenGenerating;
        /// <summary>
        /// Occurs after the token is successfully generated.
        /// </summary>
        public static event EventHandler TokenGenerated;
        /// <summary>
        /// Occurs right after token expiration.
        /// </summary>
        public static event EventHandler TokenExpired;
        /// <summary>
        /// Occurs when the token is destroyed by the user.
        /// </summary>
        public static event EventHandler TokenDestroyed;
        /// <summary>
        /// Occurs each second passed from the lifetime of the generated token.
        /// </summary>
        public static event EventHandler<LifeSpanEventArgs> LifeSpanChanged;

        /// <summary>
        /// Generate a token. Exception is raised if there is an existing active token.
        /// </summary>
        /// <exception cref="InvalidOperationException" />
        /// <returns></returns>
        public static async Task GenerateAsync()
        {
            if (string.IsNullOrWhiteSpace(_value))
            {
                TokenGenerating?.Invoke(null, EventArgs.Empty);

                if (_httpClient is null) 
                    _httpClient = new HttpClient();

                var values = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("grant_type", "client_credentials"),
                    new KeyValuePair<string, string>("client_id", _vault.GetClientId().Result),
                    new KeyValuePair<string, string>("client_secret", _vault.GetClientSecret1().Result),
                };

                var content = new FormUrlEncodedContent(values);
                content.Headers.Clear();
                content.Headers.Add("Content-Type", "application/x-www-form-urlencoded");

                HttpRequestMessage msg = new HttpRequestMessage(HttpMethod.Post, new Uri(IdBaseURI + signature));
                msg.Content = content;

                var response = await _httpClient.SendAsync(msg);
                var responseContent = await response.Content.ReadAsStringAsync();
                var json = JObject.Parse(responseContent);
                if (response.IsSuccessStatusCode)
                {
                    _generationTime = DateTime.Now;
                    _expireIn = (int)json.GetValue("expires_in");
                    _lifeSpanTimerAsyncOperation = null;
                    _lifeSpanTimerAsyncOperation = AsyncOperationManager.CreateOperation(null); ;

                    _lifeSpanTimer = new Timer(LifeSpanTimerCallBack, (int)_expireIn, 0, 1000);
                    _expiryTime = Convert.ToDateTime(_generationTime).AddSeconds((int)_expireIn);
                    _value = json.GetValue("access_token").ToString();
                    _type = json.GetValue("token_type").ToString();
                    _scope = json.GetValue("scope").ToString();

                    TokenGenerated?.Invoke(null, EventArgs.Empty);
                }
                else
                {
                    string error = json.GetValue("error")?.ToString();
                    string error_description = json.GetValue("error_description")?.ToString();
                    string error_uri = json.GetValue("error_uri")?.ToString();

                    throw new InvalidOperationException($"Error: {error}\nDescription: {error_description}\n URI: {error_uri}");
                }
            }
            else
            {
                throw new InvalidOperationException("A token is already generated and active, " +
                    "you can only call GenerateAsync() again when the token is expired.");
            }
        }

        private static void OnLifeSpanChanged(object state, LifeSpanEventArgs e)
        {
            LifeSpanChanged?.Invoke(null, e);
        }

        private static void LifeSpanTimerCallBack(object state)
        {
            _lifeSpanTimerAsyncOperation.Post(new SendOrPostCallback(delegate
            {
                OnLifeSpanChanged(state, new LifeSpanEventArgs
                {
                    LifeSpan = (int)state,
                    LifeSpanRemaining = (int)((DateTime)_expiryTime - DateTime.Now).TotalSeconds
                });
            }), null);

            if ((int)((DateTime)_expiryTime - DateTime.Now).TotalSeconds == 0)
            {
                _lifeSpanTimer.Dispose();
                _lifeSpanTimerAsyncOperation.PostOperationCompleted(new SendOrPostCallback(delegate
                {
                    Destroy();

                    OnTokenExpired(null, EventArgs.Empty);
                }), null);
            }
        }

        private static void OnTokenExpired(object sender, EventArgs e)
        {
            TokenExpired?.Invoke(sender, e);
        }

        /// <summary>
        /// Destroys the current token.
        /// </summary>
        /// <remarks>Note: The current HttpClient object is not disposed.</remarks>
        public static void Destroy()
        {
            _lifeSpanTimer.Dispose();
            _generationTime = null;
            _expireIn = null;
            _expiryTime = null;
            _value = null;
            _type = null;
            _scope = null;

            TokenDestroyed?.Invoke(null, EventArgs.Empty);
        }
    }
}
