namespace Loupedeck.LoupedeckAzurePlugin
{
    using System;
    using System.Linq;
    using System.Windows.Markup;

    using Loupedeck.LoupedeckAzurePlugin.Events;
    using Loupedeck.LoupedeckAzurePlugin.Helpers;

    // This class contains the plugin-level logic of the Loupedeck Azure plugin.
    public class LoupedeckAzurePlugin : Plugin
    {
        // Gets a value indicating whether this is an API-only plugin.
        public override Boolean UsesApplicationApiOnly => true;

        // Gets a value indicating whether this is a Universal plugin or an Application plugin.
        public override Boolean HasNoApplication => true;

        // Timer used to periodically update the status of Azure virtual machines.
        private Timer StatusUpdateTimer;

        // Holds the Azure configuration instances loaded from the configuration file.
        public AzureConfig ConfigInstances { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LoupedeckAzurePlugin"/> class.
        /// Sets up logging and plugin resources.
        /// </summary>
        public LoupedeckAzurePlugin()
        {
            // Initialize the plugin log.
            PluginLog.Init(this.Log);

            // Initialize the plugin resources.
            PluginResources.Init(this.Assembly);
        }

        /// <summary>
        /// Called when the plugin is loaded.
        /// Loads configuration, logs in to Azure, starts polling for VM status, and sets up the timer.
        /// </summary>
        public override void Load()
        {
            this.ConfigInstances = ReadConfig();
            if (this.ConfigInstances == null)
            {
                PluginLog.Error($"Configuration file is missing or unreadable.");
                this.OnPluginStatusChanged(Loupedeck.PluginStatus.Error, "Configuration could not be read.", "https://github.com/ssss", "Help");
                return;
            }
            // Log in to Azure for each configured subscription.
            foreach (var c in this.ConfigInstances.AzureConfigs)
            {
                c.Value.Login();
            }
            // Initial poll of all VMs.
            this.listAllVM();
            this.StatesReady?.Invoke(this, EventArgs.Empty);

            // Start the timer to poll VM status every 20 minutes, with an initial delay of 2 seconds.
            this.StatusUpdateTimer = new Timer(
                callback: _ => this.listAllVM(),
                state: null,
                dueTime: (int)TimeSpan.FromSeconds(2).TotalMilliseconds,
                period: (int)TimeSpan.FromMinutes(20).TotalMilliseconds
            );
        }

        /// <summary>
        /// Triggers the status update timer to run after a specified number of seconds.
        /// </summary>
        /// <param name="seconds">Delay in seconds before the timer callback is invoked.</param>
        public void TriggerTimer(int seconds)
        {
            if (this.StatusUpdateTimer == null)
            {
                PluginLog.Warning($"Timer is not initialized.");
                return;
            }
            this.StatusUpdateTimer.Change(
                dueTime: (int)TimeSpan.FromSeconds(seconds).TotalMilliseconds,
                period: (int)TimeSpan.FromMinutes(20).TotalMilliseconds
            );
        }

        // Stores the current state of all tracked Azure virtual machines, keyed by resource ID.
        public Dictionary<String, AzureState> States = new Dictionary<String, AzureState>();

        // Event raised when all VM states are ready after initial load or refresh.
        public event EventHandler<EventArgs> StatesReady;

        // Event raised when the state of a VM changes.
        public event EventHandler<StateChangedEventArgs> StateChanged;

        /// <summary>
        /// Polls all configured Azure subscriptions for virtual machines and updates their states.
        /// If any VM is in a "Changing" state, triggers a quicker refresh.
        /// </summary>
        public void listAllVM()
        {
            PluginLog.Info($"Azure Polling Started");
            var ah = new AzureHelper();
            Boolean quickRefresh = false;
            foreach (var c in this.ConfigInstances.AzureConfigs)
            {
                // Retrieve the list of VMs for the current subscription.
                var virtualMachines = ah.ListVMs(c.Value._login, c.Key);
                foreach (var vm in virtualMachines)
                {
                    var s = new AzureState();
                    s.SubscriptionId = c.Key;
                    ah.ExtractResourceGroupAndVmName(vm.Id, out var resourceGroupName, out var vmName);
                    s.ResourceGroupName = resourceGroupName;
                    s.VMName = vm.Name;

                    s.PowerState = ah.RetrieveVmPowerState(vm.Id, c.Value._login, c.Key);
                    s.resourceId = vm.Id;

                    this.UpdateState(s);
                    // If any VM is changing state, schedule a quicker refresh.
                    if (s.PowerState == AzureStateType.Changing)
                    {
                        quickRefresh = true;
                    }
                }
            }
            // If a VM is changing, poll again in 20 seconds.
            if (quickRefresh)
            {
                this.TriggerTimer(20);
            }
        }

        /// <summary>
        /// Updates the state of a VM in the States dictionary and raises the StateChanged event.
        /// </summary>
        /// <param name="state">The new state of the VM.</param>
        public void UpdateState(AzureState state)
        {
            if (this.States.ContainsKey(state.resourceId))
            {
                this.States[state.resourceId] = state;
            }
            else
            {
                this.States.Add(state.resourceId, state);
            }
            this.StateChanged?.Invoke(this, new StateChangedEventArgs(state));
        }

        /// <summary>
        /// Called when the plugin is unloaded.
        /// </summary>
        public override void Unload()
        {
            // Cleanup logic can be added here if needed.
        }

        /// <summary>
        /// Reads the Azure configuration from the user's profile directory.
        /// </summary>
        /// <returns>The loaded AzureConfig instance, or null if not found or unreadable.</returns>
        private static AzureConfig ReadConfig()
        {
            var DEFAULT_PATH = Path.Combine(".loupedeck", "azure");
            var UserProfilePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var ConfigFile = Path.Combine(UserProfilePath, DEFAULT_PATH, "azure.json");

            if (!IoHelpers.FileExists(ConfigFile))
            {
                PluginLog.Error($"Configuration file is missing or unreadable.");
                return null;
            }

            var Config = JsonHelpers.DeserializeAnyObjectFromFile<AzureConfig>(ConfigFile);

            return Config;
        }
    }
}
