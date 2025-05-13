namespace Loupedeck.LoupedeckAzurePlugin.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using Loupedeck.LoupedeckAzurePlugin.Events;

    /// <summary>
    /// Abstract base class for Azure-related multistate dynamic commands.
    /// Handles event subscription, state filtering, and parameter management for Azure VMs.
    /// </summary>
    public abstract class AzureBaseCommand : PluginMultistateDynamicCommand
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AzureBaseCommand"/> class.
        /// </summary>
        /// <param name="groupName">The group name for this command.</param>
        protected AzureBaseCommand(String groupName) : base() => this.GroupName = groupName;

        /// <summary>
        /// Determines whether the specified entity should be included by this command.
        /// </summary>
        /// <param name="entity_id">The resource ID of the entity.</param>
        /// <returns>True if the entity should be included; otherwise, false.</returns>
        protected abstract Boolean EntityFilter(String entity_id);

        /// <summary>
        /// Executes the command for the specified entity.
        /// </summary>
        /// <param name="entity_id">The resource ID of the entity.</param>
        protected abstract override void RunCommand(String entity_id);

        /// <summary>
        /// Called when the command is loaded. Subscribes to plugin events.
        /// </summary>
        /// <returns>True if loading succeeded; otherwise, false.</returns>
        protected override Boolean OnLoad()
        {
            // Subscribe to plugin events for state updates.
            using (var plugin = base.Plugin as LoupedeckAzurePlugin)
            {
                plugin.StatesReady += this.StatesReady;
                plugin.StateChanged += this.StateChanged;
            }

            return true;
        }

        /// <summary>
        /// Called when the command is unloaded. Unsubscribes from plugin events.
        /// </summary>
        /// <returns>True if unloading succeeded; otherwise, false.</returns>
        protected override Boolean OnUnload()
        {
            // Unsubscribe from plugin events to avoid memory leaks.
            using (var plugin = base.Plugin as LoupedeckAzurePlugin)
            {
                plugin.StateChanged -= this.StateChanged;
                plugin.StatesReady -= this.StatesReady;
            }

            return true;
        }

        /// <summary>
        /// Gets the plugin instance as <see cref="LoupedeckAzurePlugin"/>.
        /// </summary>
        protected LoupedeckAzurePlugin GetPlugin() => (LoupedeckAzurePlugin)base.Plugin;

        /// <summary>
        /// Gets the current Azure VM states from the plugin.
        /// </summary>
        protected Dictionary<String, AzureState> GetStates() => this.GetPlugin().States;

        /// <summary>
        /// Handles the StatesReady event by adding parameters for all filtered entities.
        /// </summary>
        private void StatesReady(Object sender, EventArgs e)
        {
            PluginLog.Verbose($"{this.GroupName}Command.OnLoad() => StatesReady");
            var states = this.GetStates();

            foreach (var kvp in states)
            {
                if (!this.EntityFilter(kvp.Key))
                    continue;

                var state = kvp.Value;
                this.AddParameter(state.resourceId, state.VMName, this.GroupName);
            }

            PluginLog.Info($"[group: {this.GroupName}] [count: {this.GetParameters().Length}]");
        }

        /// <summary>
        /// Handles the StateChanged event by updating the current state for the affected entity.
        /// </summary>
        private void StateChanged(Object sender, StateChangedEventArgs e)
        {
            if (!this.EntityFilter(e.StateEntity.resourceId))
            {
                return;
            }

            // Update the current state for the entity.
            var stateIndex = (Int32)e.StateEntity.PowerState;
            this.SetCurrentState(e.StateEntity.resourceId, stateIndex);
        }

        /// <summary>
        /// Gets the display name for the command parameter.
        /// </summary>
        /// <param name="entity_id">The resource ID of the entity.</param>
        /// <param name="deviceState">The current device state index.</param>
        /// <param name="imageSize">The image size for the display.</param>
        /// <returns>The display name for the command.</returns>
        protected override String GetCommandDisplayName(String entity_id, Int32 deviceState, PluginImageSize imageSize)
        {
            if (entity_id.IsNullOrEmpty())
            {
                return base.GetCommandDisplayName(entity_id, deviceState, imageSize);
            }

            var states = this.GetStates();
            var friendlyName = states[entity_id].VMName;

            return $"{friendlyName}";
        }
    }
}
