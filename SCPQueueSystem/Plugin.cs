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
        // TODO: If dict doesn't work, use https://stackoverflow.com/questions/289/how-do-you-sort-a-dictionary-by-value
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
            scores = SortDictionary();
            Exiled.Events.Handlers.Server.RoundStarted += ServerOnRoundStarted;
            Exiled.Events.Handlers.Player.Verified += PlayerOnVerified;
            Exiled.Events.Handlers.Map.GeneratorActivated += MapOnGeneratorActivated;
            Exiled.Events.Handlers.Server.RoundEnded += ServerOnRoundEnded;
            Exiled.Events.Handlers.Player.Escaping += PlayerOnEscaping;
            Exiled.Events.Handlers.Player.Died += PlayerOnDied;
            Exiled.Events.Handlers.Player.ChangingRole += PlayerOnChangingRole;
        }

        private void PlayerOnChangingRole(ChangingRoleEventArgs ev)
        {
            Timing.CallDelayed(1f, () =>
            {
                if (ev.Player.IsScp)
                    scores[ev.Player] = 0;
            });
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
                    $"You have <color=red>{scores[ev.Player]}</color> tickets\nYour placement is <color=red>{getIndex(ev.Player)}</color>",
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
            scores = SortDictionary();
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
                    if (!ev.Killer.IsScp)
                    {
                        Log.Debug($"{ev.Killer.Nickname} rewarded for a kill");
                        scores[ev.Killer] += Config.TicketsPerKill;
                    }
                    else if (ev.Killer.Role == RoleType.Scp0492)
                    {
                        Log.Debug($"{ev.Killer.Nickname} rewarded for a kill");
                        scores[ev.Killer] += Config.TicketsPerZombieKill;
                    }
                    
                }
            }

            if (Config.DisplayPlacementOnSpectator)
                ev.Target.ShowHint(
                    $"You have <color=red>{scores[ev.Target]}</color> tickets\nYour placement is <color=red>{getIndex(ev.Target)}</color>",
                    10f);
        }

        private void ServerOnRoundStarted()
        {
            scores = SortDictionary();
            
            foreach (KeyValuePair<Player, int> plr in scores)
            {
                if (!Player.List.Contains(plr.Key))
                {
                    scores.Remove(plr.Key);
                }
            }

            Timing.CallDelayed(1f, () =>
            {
                Log.Debug("Setting up scps...");
                foreach (Player plr in Player.List)
                {
                    if (plr.IsScp)
                        plr.SetRole((RoleType) Server.Host.ReferenceHub.characterClassManager.FindRandomIdUsingDefinedTeam(Team.CDP));
                }

                if (Player.List.Count() > 19)
                {
                    setScpsWithBehaviour(4); // Contains that CCM method
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

        Player getPlayerFromIndex(int value)
        {
            int count = 0;
            foreach (var kvp in scores)
            {
                if (count == value)
                    return kvp.Key;    
                ++count;
            }

            throw new NullReferenceException();
        }

        private void setScpsWithBehaviour(int amountOfPlayers)
        {
            for (int x = 0; x < amountOfPlayers; x++)
            {
                Player plr = getPlayerFromIndex(x);
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

        private Dictionary<Player, int> SortDictionary()
        {
            Log.Debug("Sorting...");
            var ordered = scores.OrderBy(x => x.Value).ToDictionary(pair => pair.Key, pair => pair.Value);

            foreach (var kvp in ordered)
            {
                Log.Debug($"{kvp.Key.Nickname} | {kvp.Value}");
            }
            return ordered;
        }
    }
}