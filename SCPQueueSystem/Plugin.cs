namespace SCPQueueSystem
{
    using System.Collections.Generic;
    using System.Linq;
    using Exiled.API.Features;
    using Exiled.Events.EventArgs;
    using System;
    using MEC;

    public class Plugin : Plugin<Config>
    {
        // Create the dictionary
        private Dictionary<Player, int> scores = new Dictionary<Player, int>();
        private readonly Random rand = new Random();

        private List<RoleType> scps = new List<RoleType>()
        {
            RoleType.Scp049,
            RoleType.Scp079,
            RoleType.Scp096,
            RoleType.Scp106,
            RoleType.Scp173,
            RoleType.Scp93953,
            RoleType.Scp93989
        };

        public override void OnEnabled()
        {
            Exiled.Events.Handlers.Server.RoundStarted += ServerOnRoundStarted;
            Exiled.Events.Handlers.Player.Verified += PlayerOnVerified;
            Exiled.Events.Handlers.Map.GeneratorActivated += MapOnGeneratorActivated;
            Exiled.Events.Handlers.Server.RoundEnded += ServerOnRoundEnded;
            Exiled.Events.Handlers.Player.Escaping += PlayerOnEscaping;
            Exiled.Events.Handlers.Player.Died += PlayerOnDied;
        }

        public override void OnDisabled()
        {
            scores.Clear();
            Exiled.Events.Handlers.Server.RoundStarted -= ServerOnRoundStarted;
            Exiled.Events.Handlers.Player.Verified -= PlayerOnVerified;
            Exiled.Events.Handlers.Map.GeneratorActivated -= MapOnGeneratorActivated;
            Exiled.Events.Handlers.Server.RoundEnded -= ServerOnRoundEnded;
            Exiled.Events.Handlers.Player.Escaping -= PlayerOnEscaping;
            Exiled.Events.Handlers.Player.Died -= PlayerOnDied;
        }


        private void PlayerOnVerified(VerifiedEventArgs ev)
        {

            if (!scores.ContainsKey(ev.Player))
            {
                scores.Add(ev.Player, 0);
            }

            if (Config.DisplayPlacementOnSpectator)
            {
                ev.Player.ShowHint(
                    $"You have <color=red>{scores[ev.Player]}</color> tickets\nYour placement is <color=red>{getindex(ev.Player)}</color>",
                    10f);
            }
        }

        private void ServerOnRoundEnded(RoundEndedEventArgs ev)
        {
            Log.Debug("Clearing scps list...");
            scps.Clear();
            scps = new List<RoleType>()
            {
                RoleType.Scp049,
                RoleType.Scp079,
                RoleType.Scp096,
                RoleType.Scp106,
                RoleType.Scp173,
                RoleType.Scp93953,
                RoleType.Scp93989
            };

            Log.Debug("Adding victory tickets");
            foreach (var kvp in scores)
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
            foreach (var kvp in scores)
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
                    foreach (var kvp in scores)
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
                    if (!ev.Killer.IsScp)
                    {
                        Log.Debug($"{ev.Killer.Nickname} rewarded for a kill");
                        scores[ev.Killer] += Config.TicketsPerKill;
                    }
                    else if (ev.Killer.Role == RoleType.Scp0492)
                    {
                        Log.Debug($"{ev.Killer.Nickname} rewarded for a kill");
                        scores[ev.Killer] += Config.TicketsPerKill;
                    }

                }
            }

            if (Config.DisplayPlacementOnSpectator)
                ev.Target.ShowHint(
                    $"You have <color=red>{scores[ev.Target]}</color> tickets\nYour placement is <color=red>{getindex(ev.Target)}</color>",
                    10f);
        }

        private void ServerOnRoundStarted()
        {
            foreach (var plr in scores)
            {
                if (!Player.List.Contains(plr.Key))
                {
                    scores.Remove(scores.First(x => x.Value == plr.Value).Key);
                }
            }

            Timing.CallDelayed(1f, () =>
            {
                Log.Debug("Setting up scps...");
                foreach (Player plr in Player.List)
                {
                    if (plr.IsScp)
                        plr.SetRole((RoleType)Server.Host.ReferenceHub.characterClassManager
                            .FindRandomIdUsingDefinedTeam(Team.CDP));
                }

                if (Player.List.Count() > 19)
                {
                    setScpsWithBehaviour(4);
                }
                else if (Player.List.Count() > 14)
                {
                    setScpsWithBehaviour(3);
                }
                else if (Player.List.Count() > 7)
                {
                    setScpsWithBehaviour(2);
                }
                else
                {
                    setScpsWithBehaviour(1);
                }
            });
        }

        private int getindex(Player plr)
        {
            var orderedList = new List<KeyValuePair<Player, int>>(scores.OrderByDescending(kvp => kvp.Value));
            int x = 0;
            foreach (var kvp in orderedList)
            {
                if (kvp.Key == plr)
                    return x;
            }

            Log.Error("Player not in list!");
            return 66934;
        }
        

        private void setScpsWithBehaviour(int amountOfPlayers)
        {
            var orderedList = new List<KeyValuePair<Player, int>>(scores.OrderByDescending(x => x.Value));
            for (int x = 0; x < amountOfPlayers; x++)
            {
                Player plr = orderedList[x].Key;
                if (!plr.IsScp)
                {
                    reroll:
                    RoleType desiredRole = scps[rand.Next(scps.Count)];
                    if ((desiredRole == RoleType.Scp079 && scps.Count == 7) || desiredRole != RoleType.Scp079)
                    {
                        plr.SetRole(desiredRole);
                        scps.Remove(desiredRole);
                    }
                    else
                        goto reroll;
                }
            }
        }
    }
}