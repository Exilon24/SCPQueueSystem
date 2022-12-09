namespace SCPQueueSystem
{
    using Exiled.API.Interfaces;
    using System.ComponentModel;
    
    public class Config : IConfig
    {
        [Description("If the plugin is enabled or not")]
        public bool IsEnabled { get; set; } = true;
        
        [Description("Should the current amount of player tickets be shown on death")]
        public bool DisplayPlacementOnSpectator { get; set; } = true;
        
        [Description("How much tickets should the NTF earn when they activate a generator")]
        public int TicketsPerGeneratorActivated { get; set; } = 3;
        
        [Description("How many tickets shoud a player earn for a kill")]
        public int TicketsPerKill { get; set; } = 1;
        
        [Description("How many tickets should a player earn for escaping")]
        public int TicketsPerEscape { get; set; } = 10;
        
        [Description("How many tickets should the team that recontains a SCP earn")]
        public int TicketsPerSCPRecontained { get; set; } = 7;
        
        [Description("How many tickets should a player get for winning")]
        public int TicketsPerWin { get; set; } = 10;

        [Description("How many tickets should a zombie get for killing a player")]
        public int TicketsPerZombieKill { get; set; } = 8;
    }
}