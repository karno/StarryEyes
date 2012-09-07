using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Codeplex.OAuth
{
    public abstract class OAuthBase
    {
        private static readonly Random random = new Random();

        public string ConsumerKey { get; private set; }
        public string ConsumerSecret { get; private set; }

        public OAuthBase(string consumerKey, string consumerSecret)
        {
            Guard.ArgumentNull(consumerKey, "key");
            Guard.ArgumentNull(consumerSecret, "consumerSecret");

            this.ConsumerKey = consumerKey;
            this.ConsumerSecret = consumerSecret;
        }

        private string GenerateSignature(Uri uri, MethodType methodType, Token token, IEnumerable<Parameter> parameters)
        {
            var hmacKeyBase = ConsumerSecret.UrlEncode() + "&" + ((token == null) ? "" : token.Secret).UrlEncode();
            using (var hmacsha1 = new HMACSHA1(Encoding.UTF8.GetBytes(hmacKeyBase)))
            {
                var stringParameter = parameters.OrderBy(p => p.Key)
                   .ThenBy(p => p.Value)
                   .ToQueryParameter();
                var signatureBase = methodType.ToUpperString() +
                    "&" + uri.GetComponents(UriComponents.SchemeAndServer | UriComponents.Path, UriFormat.Unescaped).UrlEncode() +
                    "&" + stringParameter.UrlEncode();

                var hash = hmacsha1.ComputeHash(Encoding.UTF8.GetBytes(signatureBase));
                return Convert.ToBase64String(hash).UrlEncode();
            }
        }

        protected string BuildAuthorizationHeader(IEnumerable<Parameter> parameters)
        {
            Guard.ArgumentNull(parameters, "parameters");

            return "OAuth " + parameters.Select(p => p.Key + "=" + p.Value.Wrap("\"")).ToString(",");
        }

        protected ParameterCollection ConstructBasicParameters(string url, MethodType methodType, Token token = null, params Parameter[] optionalParameters)
        {
            Guard.ArgumentNull(optionalParameters, "optionalParameters");

            return ConstructBasicParameters(url, methodType, token, optionalParameters.AsEnumerable());
        }

        protected ParameterCollection ConstructBasicParameters(string url, MethodType methodType, Token token, IEnumerable<Parameter> optionalParameters)
        {
            Guard.ArgumentNull(url, "url");
            Guard.ArgumentNull(methodType, "methodType");
            Guard.ArgumentNull(optionalParameters, "optionalParameters");

            var parameters = new ParameterCollection
            {
                { "oauth_consumer_key", ConsumerKey },
                { "oauth_nonce", random.Next() },
                { "oauth_timestamp", DateTime.UtcNow.ToUnixTime() },
                { "oauth_signature_method", "HMAC-SHA1" },
                { "oauth_version", "1.0" }
            };
            if (token != null) parameters.Add("oauth_token", token.Key);

            var signature = GenerateSignature(new Uri(url), methodType, token, parameters.Concat(optionalParameters));
            parameters.Add("oauth_signature", signature);

            return parameters;
        }
    }
}