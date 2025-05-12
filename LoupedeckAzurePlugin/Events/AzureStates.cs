namespace Loupedeck.LoupedeckAzurePlugin.Events
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class AzureState
    // This class represents the state of an Azure virtual machine.
    {
        public string VMName { get; set; } = string.Empty;
        public string SubscriptionId { get; set; } = string.Empty;
        public string ResourceGroupName { get; set; } = string.Empty;
        public AzureStateType PowerState { get; set; } = AzureStateType.NotFound;

        public string resourceId { get; set; } = string.Empty;
    }
    public enum AzureStateType
    {
      
        NotFound,
        PowerOff,
        PowerOn,
        Changing,

    }
}
