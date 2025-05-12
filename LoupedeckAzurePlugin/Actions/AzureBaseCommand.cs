namespace Loupedeck.LoupedeckAzurePlugin.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using Loupedeck.LoupedeckAzurePlugin.Events;

    public abstract class AzureBaseCommand: PluginMultistateDynamicCommand
    {
        protected AzureBaseCommand(String groupName) : base() => this.GroupName = groupName;
        protected abstract Boolean EntitiyFilter(String entity_id);

        protected abstract override void RunCommand(String entity_id);

        protected override Boolean OnLoad()
        {
            using (var plugin = base.Plugin as LoupedeckAzurePlugin)
            {
                plugin.StatesReady += this.StatesReady;
                plugin.StateChanged += this.StateChanged;
            }

            return true;
        }
        protected override Boolean OnUnload()
        {
            using (var plugin = base.Plugin as LoupedeckAzurePlugin)
            {
                plugin.StateChanged -= this.StateChanged;
                plugin.StatesReady -= this.StatesReady;
            }

            return true;
        }
        protected LoupedeckAzurePlugin GetPlugin() => (LoupedeckAzurePlugin)base.Plugin;
        protected Dictionary<String, AzureState> GetStates() => this.GetPlugin().States;

        private void StatesReady(Object sender, EventArgs e)
        {
            PluginLog.Verbose($"{this.GroupName}Command.OnLoad() => StatesReady");
            var x = this.GetStates();

            foreach (var kvp in x)
            {
                if (!this.EntitiyFilter(kvp.Key))
                {
                    continue; }

                var state = kvp.Value;
                this.AddParameter(state.resourceId, state.VMName, this.GroupName);
            }

            PluginLog.Info($"[group: {this.GroupName}] [count: {this.GetParameters().Length}]");
        }

        private void StateChanged(Object sender, StateChangedEventArgs e)
        {
            if (!this.EntitiyFilter(e.StateEntity.resourceId))
            { return; }

           // this.ActionImageChanged(e.StateEntity.resourceId);
            var x = (Int32)e.StateEntity.PowerState;
            
            this.SetCurrentState(e.StateEntity.resourceId,x );

        }

        protected override String GetCommandDisplayName(String entity_id, Int32 deviceState, PluginImageSize imageSize)
        {
            if (entity_id.IsNullOrEmpty())
            { return base.GetCommandDisplayName(entity_id,deviceState, imageSize); }

            var states = this.GetStates();

            var FriendlyName = states[entity_id].VMName;
           



            return $"{FriendlyName}";
        }
    }
}
