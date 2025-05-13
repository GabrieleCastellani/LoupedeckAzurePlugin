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

    /// <summary>
    /// Represents a command for controlling Azure Virtual Machines (VMs) from the Loupedeck plugin.
    /// Allows starting and stopping VMs and reflects their current state.
    /// </summary>
    public class AzureVmAction : AzureBaseCommand
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AzureVmAction"/> class.
        /// Sets up the possible VM states for the command.
        /// </summary>
        public AzureVmAction()
            : base("Virtual Machines")
        {
            try
            {
                this.AddState("NotFound", "Not fetched");
                this.AddState("Off", "Vm is turned off");
                this.AddState("On", "Vm is turned on");
                this.AddState("Changing", "Vm is changing");
            }
            catch (Exception ex)
            {
                PluginLog.Error($"[AzureVmAction.ctor] Error initializing states: {ex}");
            }
        }

        /// <summary>
        /// Filters entities to only include Azure Virtual Machines.
        /// </summary>
        /// <param name="entityId">The resource ID of the entity.</param>
        /// <returns>True if the entity is a VM; otherwise, false.</returns>
        protected override Boolean EntityFilter(String entityId)
            => entityId?.Contains("providers/Microsoft.Compute/virtualMachines") == true;

        /// <summary>
        /// Executes the command to start or stop the VM based on its current power state.
        /// </summary>
        /// <param name="entityId">The resource ID of the VM.</param>
        protected override void RunCommand(String entityId)
        {
            var states = this.GetStates();
            if (!states.TryGetValue(entityId, out var state))
            {
                PluginLog.Error($"[AzureVmAction.RunCommand] State not found for entity: {entityId}");
                return;
            }

            var subscriptionId = state.SubscriptionId;
            var plugin = this.GetPlugin();
            if (plugin?.ConfigInstances?.AzureConfigs == null)
            {
                PluginLog.Error("[AzureVmAction.RunCommand] AzureConfigs not available.");
                return;
            }

            if (!plugin.ConfigInstances.AzureConfigs.TryGetValue(subscriptionId, out var config))
            {
                PluginLog.Error($"[AzureVmAction.RunCommand] Config not found for subscription: {subscriptionId}");
                return;
            }

            var azureHelper = new AzureHelper();

            switch (state.PowerState)
            {
                case AzureStateType.PowerOn:
                    this.HandleVmStateChange(entityId, state, azureHelper.PowerOff, config._login, subscriptionId, "Powering off");
                    break;
                case AzureStateType.PowerOff:
                    this.HandleVmStateChange(entityId, state, azureHelper.PowerOn, config._login, subscriptionId, "Powering on");
                    break;
                case AzureStateType.Changing:
                    PluginLog.Info($"[AzureVmAction.RunCommand] VM '{state.VMName}' is already changing state. No action taken.");
                    break;
                default:
                    PluginLog.Info($"[AzureVmAction.RunCommand] VM '{state.VMName}' is in an unexpected state. No action taken.");
                    break;
            }
        }

        /// <summary>
        /// Handles the VM state change operation and triggers a status update.
        /// </summary>
        private void HandleVmStateChange(
            String entityId,
            AzureState state,
            Action<String, ServiceClientCredentials, String> vmAction,
            ServiceClientCredentials login,
            String subscriptionId,
            String actionDescription)
        {
            PluginLog.Info($"[AzureVmAction] {actionDescription} VM: {state.VMName}");
            this.SetCurrentState(entityId, (Int32)AzureStateType.Changing);
            vmAction(entityId, login, subscriptionId);
            this.GetPlugin().TriggerTimer(2);
        }

        /// <summary>
        /// Gets the display name for the command, reflecting the VM's friendly name and current state.
        /// </summary>
        /// <param name="entityId">The resource ID of the VM.</param>
        /// <param name="deviceState">The current state index.</param>
        /// <param name="imageSize">The size of the plugin image.</param>
        /// <returns>A string to display on the device.</returns>
        protected override String GetCommandDisplayName(String entityId, Int32 deviceState, PluginImageSize imageSize)
        {
            if (String.IsNullOrEmpty(entityId))
            {
                return base.GetCommandDisplayName(entityId, deviceState, imageSize);
            }

            var states = this.GetStates();
            if (!states.TryGetValue(entityId, out var state))
            {
                return $"{entityId}\nNot Found";
            }

            var friendlyName = state.VMName;

            return deviceState switch
            {
                0 => $"{friendlyName}\nNot Found",
                1 => $"{friendlyName}\nOff",
                2 => $"{friendlyName}\nOn",
                3 => $"{friendlyName}\nChanging",
                _ => $"{friendlyName}\nUnknown"
            };
        }
    }

}
