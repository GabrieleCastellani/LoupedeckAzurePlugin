namespace Loupedeck.LoupedeckAzurePlugin.Events
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class StateChangedEventArgs(AzureState State) : EventArgs
    {
        public AzureState StateEntity { get; } = State;
        

        public static StateChangedEventArgs Create(AzureState State) => new(State);
    }
}
