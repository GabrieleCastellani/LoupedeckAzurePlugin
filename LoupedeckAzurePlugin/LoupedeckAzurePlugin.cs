namespace Loupedeck.LoupedeckAzurePlugin
{
    using System;
    using System.Linq;
    using System.Windows.Markup;

    using Loupedeck.LoupedeckAzurePlugin.Events;
    using Loupedeck.LoupedeckAzurePlugin.Helpers;

    // This class contains the plugin-level logic of the Loupedeck plugin.

    public class LoupedeckAzurePlugin : Plugin
    {
        // Gets a value indicating whether this is an API-only plugin.
        public override Boolean UsesApplicationApiOnly => true;

        // Gets a value indicating whether this is a Universal plugin or an Application plugin.
        public override Boolean HasNoApplication => true;

        private Timer StatusUpdateTimer;

        public AzureConfig ConfigInstances { get; private set; }

        // Initializes a new instance of the plugin class.
        public LoupedeckAzurePlugin()
        {
            // Initialize the plugin log.
            PluginLog.Init(this.Log);

            // Initialize the plugin resources.
            PluginResources.Init(this.Assembly);
        }

        // This method is called when the plugin is loaded.
        public override void Load()
        {
            this.ConfigInstances = ReadConfig();
            if (this.ConfigInstances == null)
            {
                PluginLog.Error($"Configuration file is missing or unreadable.");
                this.OnPluginStatusChanged(Loupedeck.PluginStatus.Error, "Configuration could not be read.", "https://github.com/ssss", "Help");
                return;
            }
            foreach (var c in this.ConfigInstances.AzureConfigs)
            {
                c.Value.Login();
            }
            this.listAllVM();
            this.StatesReady?.Invoke(this, EventArgs.Empty);

            // Corrected Timer instantiation
            this.StatusUpdateTimer = new Timer(
                callback: _ => this.listAllVM(),
                state: null,
                dueTime: (int)TimeSpan.FromSeconds(2).TotalMilliseconds,
                period: (int)TimeSpan.FromMinutes(60).TotalMilliseconds
            );
        }
        public void TriggerTimer(int seconds)
        {
            this.StatusUpdateTimer.Change(
                dueTime: (int)TimeSpan.FromSeconds(seconds).TotalMilliseconds,
                period: (int)TimeSpan.FromMinutes(60).TotalMilliseconds
            );
        }
        public Dictionary<String, AzureState> States = new Dictionary<String, AzureState>();
        public event EventHandler<EventArgs> StatesReady;
        public event EventHandler<StateChangedEventArgs> StateChanged;
        public void listAllVM()
        {
            PluginLog.Info($"Azure Polling Started");
            var ah = new AzureHelper();
            
            foreach (var c in this.ConfigInstances.AzureConfigs)
            {
                var virtualMachines = ah.listVMs(c.Value._login, c.Key);
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

                }
            }
        }
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
            if (state.PowerState == AzureStateType.Changing)
            {
                this.TriggerTimer(5);
                
            }
        }

        // This method is called when the plugin is unloaded.
        public override void Unload()
        {
        }
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
    } // Added missing closing brace for the class
}
