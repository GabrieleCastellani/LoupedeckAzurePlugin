namespace Loupedeck.LoupedeckAzurePlugin.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Rest.Azure.Authentication;
    using Microsoft.Rest;
    using Microsoft.Azure.Management.Compute;
    using Microsoft.Azure.Management.Compute.Models;
    using Loupedeck.LoupedeckAzurePlugin.Events;

    internal class AzureHelper
    {

        public ComputeManagementClient getCompute(ServiceClientCredentials login, String subcription)
        {
            return new ComputeManagementClient(login)
            {
                SubscriptionId = subcription
            };
        }
        public Microsoft.Rest.Azure.IPage<Microsoft.Azure.Management.Compute.Models.VirtualMachine> listVMs(ServiceClientCredentials login, String subcription)
        {
            var computeClient = this.getCompute( login, subcription);
            var x= computeClient.VirtualMachines.ListAllAsync().Result;
            return x;
            
        }
        public void PowerOn(String vmId, ServiceClientCredentials login, String subcription)
        {
            var computeClient = this.getCompute(login, subcription);
            ExtractResourceGroupAndVmName(vmId, out var resourceGroupName, out var vmName);

            computeClient.VirtualMachines.StartAsync(resourceGroupName, vmName).Wait();


        }
        public void PowerOff(String vmId, ServiceClientCredentials login, String subcription)
        {
            var computeClient = this.getCompute(login, subcription);
            ExtractResourceGroupAndVmName(vmId, out var resourceGroupName, out var vmName);

            computeClient.VirtualMachines.Deallocate(resourceGroupName, vmName);

        }
        public AzureStateType RetrieveVmPowerState(String vmId, ServiceClientCredentials login, String subcription)
        {
            var instanceView = this.GetVmInstanceView(vmId, login, subcription);
            var state = instanceView.Statuses.FirstOrDefault(status =>
                status.Code.StartsWith("PowerState/", StringComparison.OrdinalIgnoreCase));
            // Strip the "PowerState/" prefix from the state code

            var txtstate = state.Code.Substring("PowerState/".Length);


            if (txtstate == "running")
            {
                return AzureStateType.PowerOn;
            }
            else
            {
                return txtstate == "deallocated" ? AzureStateType.PowerOff : AzureStateType.Changing;
            }

        }

        private VirtualMachineInstanceView GetVmInstanceView(String vmId, ServiceClientCredentials login, String subcription)
        {
            ComputeManagementClient computeClient = this.getCompute(login, subcription);
            var instanceView = this.FetchVmInstanceView(vmId, computeClient);
            return instanceView;
        }

        private  VirtualMachineInstanceView FetchVmInstanceView(String vmId, ComputeManagementClient computeClient)
        {

            this.ExtractResourceGroupAndVmName(vmId, out var resourceGroupName, out var vmName);

            // Retrieve the VM instance view to check the current power state.
            return computeClient.VirtualMachines.InstanceViewAsync(resourceGroupName, vmName).Result;
        }

        public void ExtractResourceGroupAndVmName(String vmId, out String resourceGroupName, out String vmName)
        {
            String[] parts = vmId.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
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
