namespace SCPQueueSystem
{
    using System.Collections.Generic;
    using System.Linq;
    using Exiled.API.Features;
    using Exiled.Events.EventArgs;

    public class Plugin : Plugin<Config>
    {
        // Create the dictionary
        public Dictionary<Player, int> scores = new Dictionary<Player, int>();

        public override void OnEnabled()
        {
            scores.OrderBy(kvp => kvp.Value);
            Exiled.Events.Handlers.Server.RoundStarted += ServerOnRoundStarted;
            Exiled.Events.Handlers.Player.Verified += PlayerOnVerified;
            Exiled.Events.Handlers.Map.GeneratorActivated += MapOnGeneratorActivated;
            Exiled.Events.Handlers.Server.RoundEnded += ServerOnRoundEnded;
            Exiled.Events.Handlers.Player.Escaping += PlayerOnEscaping;
            Exiled.Events.Handlers.Player.Died += PlayerOnDied;
        }

        private void PlayerOnVerified(VerifiedEventArgs ev)
        {
            var ordered = scores.OrderBy(kvp => kvp.Value);

            if (!scores.ContainsKey(ev.Player))
            {
                scores.Add(ev.Player, 0);
            }

            if (Config.DisplayPlacementOnSpectator)
            {
                ev.Player.ShowHint(
                    $"You have <color=red>{scores[ev.Player]}</color> tickets\nYour placement is <color=red>{getIndex(ev.Player)}</color>",
                    10f);
            }
        }

        private void ServerOnRoundEnded(RoundEndedEventArgs ev)
        {
            foreach (KeyValuePair<Player, int> kvp in scores)
            {
                if (kvp.Key.LeadingTeam == ev.LeadingTeam)
                {
                    Log.Debug($"{kvp.Key.Nickname} rewarded for winning");
                    scores[kvp.Key] += Config.TicketsPerWin;
                }
            }
        }

        private void PlayerOnEscaping(EscapingEventArgs ev)
        {
            Log.Debug($"{ev.Player.Nickname} rewarded for escaping!");
            scores[ev.Player] += Config.TicketsPerEscape;
        }

        private void MapOnGeneratorActivated(GeneratorActivatedEventArgs ev)
        {
            foreach (KeyValuePair<Player, int> kvp in scores)
            {
                if (kvp.Key.IsNTF)
                {
                    Log.Debug($"{kvp.Key.Nickname} rewarded for enabling the generator");
                    scores[kvp.Key] += Config.TicketsPerGeneratorActivated;
                }
            }
        }

        private void PlayerOnDied(DiedEventArgs ev)
        {

            if (ev.Killer != null)
            {
                if (ev.Target.IsScp && ev.Target.Role != RoleType.Scp0492)
                {
                    foreach (KeyValuePair<Player, int> kvp in scores)
                    {
                        if ((kvp.Key.IsNTF || kvp.Key.Role.Type == RoleType.Scientist) &&
                            (ev.Killer.IsNTF || ev.Killer.Role.Type == RoleType.Scientist) ||
                            (kvp.Key.IsCHI || kvp.Key.Role.Type == RoleType.ClassD) &&
                            (ev.Killer.IsCHI || ev.Killer.Role.Type == RoleType.ClassD))
                        {
                            Log.Debug($"{kvp.Key.Nickname} rewarded for containing an scp");
                            scores[kvp.Key] += Config.TicketsPerSCPRecontained;
                        }
                    }
                }
                else
                {
                    Log.Debug($"{ev.Killer.Nickname} rewarded for a kill");
                    scores[ev.Killer] += Config.TicketsPerKill;
                }
            }

            if (Config.DisplayPlacementOnSpectator)
                ev.Target.ShowHint(
                    $"You have <color=red>{scores[ev.Target]}</color> tickets\nYour placement is <color=red>{getIndex(ev.Target)}</color>",
                    10f);
        }

        private void ServerOnRoundStarted()
        {
            foreach (KeyValuePair<Player, int> plr in scores)
            {
                if (!Player.List.Contains(plr.Key))
                {
                    scores.Remove(plr.Key);
                }
            }
        }

        public override void OnDisabled()
        {
            scores.Clear();
        }

        int getIndex(Player plr)
        {
            int count = 1;
            foreach (var kvp in scores)
            {
                if (kvp.Key == plr)
                    return count;    
                ++count;
            }

            return 4321;
        }
    }
}