namespace Loupedeck.LoupedeckAzurePlugin.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Loupedeck;

    using Azure.Core;
    using Microsoft.Azure.Management.Compute;
    using Microsoft.Rest;
    using Microsoft.IdentityModel.Clients.ActiveDirectory;
    using System.Runtime.CompilerServices;
    using Microsoft.Rest.Azure.Authentication;
    using Loupedeck.LoupedeckAzurePlugin.Helpers;
    using System.Xml.Linq;
    using Loupedeck.LoupedeckAzurePlugin.Events;

    public class AzureVmAction : AzureBaseCommand
    {
        private readonly List<string> _vmIds = new List<string>();
     

        public AzureVmAction()
            : base("Virtual Machines")
        {
            try
            {
                // Authenticate using service principal credentials

                this.AddState("NotFound", "Not fetched");
                this.AddState("Off", "Vm is turned off");
                this.AddState("On", "Vm is turned on");
                this.AddState("Changhing", "Vm is changing");
                





            }
            catch (Exception ex)
            {
                PluginLog.Error($"Error {ex.Message}");
            }
        }

        protected override Boolean EntitiyFilter(String entity_id) => entity_id.Contains("providers/Microsoft.Compute/virtualMachines");



        // Method executed when the user presses the "Accendi VM" button
        protected override void RunCommand(string entity_id)

        {
            var states = this.GetStates();
            var state = states[entity_id];
            var subsID = states[entity_id].SubscriptionId;

            var i = this.GetPlugin().ConfigInstances.AzureConfigs[subsID];

            var ah = new AzureHelper();

            if (state.PowerState == AzureStateType.PowerOn)
            {
                PluginLog.Info($"Powering off VM: {state.VMName}");
                this.SetCurrentState(entity_id, (Int32)AzureStateType.Changing);
                ah.PowerOff(entity_id, i._login, subsID);
                this.GetPlugin().TriggerTimer(2);
                return;
            }
            else if (state.PowerState == AzureStateType.PowerOff)
            {
                PluginLog.Info($"Powering on VM: {state.VMName}");
                this.SetCurrentState(entity_id, (Int32)AzureStateType.Changing);
                ah.PowerOn(entity_id, i._login, subsID);
                
                this.GetPlugin().TriggerTimer(2);
                return;

            }
            else if (state.PowerState == AzureStateType.Changing)
            {
                PluginLog.Info($"The VM is in an unexpected state: No action taken.");
                return;
            }



            //var ah = new AzureHelper();
            //// Authenticate using service principal credentials
            //var currentPowerState = ah.RetrieveVmPowerState(vmId);

            //if (currentPowerState == 0)
            //{
            //    ah.PowerOn(vmId);
            //    this.SetCurrentState(vmId, 1);
            //}
            //else if (currentPowerState == 1)
            //{
            //    ah.PowerOff(vmId);
            //    this.SetCurrentState(vmId, 0);
            //}
            //else
            //{
            PluginLog.Info($"The VM is in an unexpected state: No action taken.");
            //}
            //return;
        }

        protected override String GetCommandDisplayName(String entity_id, Int32 deviceState, PluginImageSize imageSize)
        {
            if (entity_id.IsNullOrEmpty())
            { return base.GetCommandDisplayName(entity_id, deviceState, imageSize); }

            var states = this.GetStates();

            var FriendlyName = states[entity_id].VMName;
            var State = states[entity_id].PowerState;

            switch (deviceState)
            {
                case 0:
                    return $"{FriendlyName}\nNot Found";
                case 1:
                    return $"{FriendlyName}\nOff";
                case 2:
                    return $"{FriendlyName}\nOn";
                case 3:
                    return $"{FriendlyName}\nChanging";

                default:
                    break;
            }

            PluginLog.Info($"{FriendlyName} Status display name updated to: {deviceState}");

            return $"{FriendlyName}\nxxx";
        }


    }

}
