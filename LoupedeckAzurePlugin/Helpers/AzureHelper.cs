namespace Loupedeck.LoupedeckAzurePlugin.Helpers
{
    using System;
    using System.Linq;
    using Microsoft.Rest.Azure.Authentication;
    using Microsoft.Rest;
    using Microsoft.Azure.Management.Compute;
    using Microsoft.Azure.Management.Compute.Models;
    using Loupedeck.LoupedeckAzurePlugin.Events;



    internal class AzureHelper
    {

        public ComputeManagementClient GetCompute(ServiceClientCredentials login, String subscription)
        {
            return new ComputeManagementClient(login)
            {
                SubscriptionId = subscription
            };
        }
        public Microsoft.Rest.Azure.IPage<VirtualMachine> ListVMs(ServiceClientCredentials login, String subscription)
        {
            var computeClient = this.GetCompute(login, subscription);
            var x = computeClient.VirtualMachines.ListAllAsync().Result;
            return x;

        }
        public void PowerOn(String vmId, ServiceClientCredentials login, String subscription)
        {
            var computeClient = this.GetCompute(login, subscription);
            this.ExtractResourceGroupAndVmName(vmId, out var resourceGroupName, out var vmName);

            computeClient.VirtualMachines.BeginStart(resourceGroupName, vmName);


        }
        public void PowerOff(String vmId, ServiceClientCredentials login, String subscription)
        {
            var computeClient = this.GetCompute(login, subscription);
            this.ExtractResourceGroupAndVmName(vmId, out var resourceGroupName, out var vmName);

            computeClient.VirtualMachines.BeginDeallocate(resourceGroupName, vmName);

        }
        public AzureStateType RetrieveVmPowerState(String vmId, ServiceClientCredentials login, String subscription)
        {
            var instanceView = this.GetVmInstanceView(vmId, login, subscription);
            var state = instanceView.Statuses.FirstOrDefault(status =>
                status.Code.StartsWith("PowerState/", StringComparison.OrdinalIgnoreCase));
            // Strip the "PowerState/" prefix from the state code
            if (state == null)
            {
                return AzureStateType.NotFound;
            }
            var txtstate = state.Code.Substring("PowerState/".Length);


            return txtstate == "running" ? AzureStateType.PowerOn : txtstate == "deallocated" ? AzureStateType.PowerOff : AzureStateType.Changing;

        }

        private VirtualMachineInstanceView GetVmInstanceView(String vmId, ServiceClientCredentials login, String subscription)
        {
            ComputeManagementClient computeClient = this.GetCompute(login, subscription);
            var instanceView = this.FetchVmInstanceView(vmId, computeClient);
            return instanceView;
        }

        private VirtualMachineInstanceView FetchVmInstanceView(String vmId, ComputeManagementClient computeClient)
        {

            this.ExtractResourceGroupAndVmName(vmId, out var resourceGroupName, out var vmName);

            // Retrieve the VM instance view to check the current power state.
            return computeClient.VirtualMachines.InstanceViewAsync(resourceGroupName, vmName).Result;
        }

        public void ExtractResourceGroupAndVmName(String vmId, out String resourceGroupName, out String vmName)
        {
            var parts = vmId.Split(new Char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 8)
            {
                throw new ArgumentException("The provided VM ID format is not valid.");
            }
            // parts[3] is the resource group name and parts[7] is the VM name.
            resourceGroupName = parts[3];
            vmName = parts[7];
        }
    }
}
