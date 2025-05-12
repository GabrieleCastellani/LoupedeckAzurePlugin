namespace Loupedeck.LoupedeckAzurePlugin.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using Microsoft.Azure.Management.Compute.Models;
    using Microsoft.Rest;
    using Microsoft.Rest.Azure.Authentication;

    public class AzureConfig
    {
        public Dictionary<String, AzureConfigInstance> AzureConfigs { get; set; } = new Dictionary<String, AzureConfigInstance>();
    }
    public class AzureConfigInstance
    {
        [JsonIgnore]
        public ServiceClientCredentials _login;

        public bool Login()
        { this._login = ApplicationTokenProvider.LoginSilentAsync(this.TenantId, this.ClientId, this.ClientSecret).Result;
            return this._login != null;
        }
       
        public String ClientId { get; set; } = String.Empty;
        public String ClientSecret { get; set; } = String.Empty;
        public String TenantId { get; set; } = String.Empty;

    }
}
