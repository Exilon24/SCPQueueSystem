using System.Collections;

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
        private SortedList<int, Player> scores = new SortedList<int, Player>();

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
            Exiled.Events.Handlers.Player.ChangingRole += PlayerOnChangingRole;
        }

        private void PlayerOnChangingRole(ChangingRoleEventArgs ev)
        {
            Timing.CallDelayed(1f, () =>
            {
                if (ev.Player.IsScp)
                {

                }
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

            if (!scores.ContainsValue(ev.Player))
            {
                scores.Add(0, ev.Player);
            }

            if (Config.DisplayPlacementOnSpectator)
            {
                ev.Player.ShowHint(
                    $"You have <color=red>{scores.First(x => x.Value == ev.Player).Key}</color> tickets\nYour placement is <color=red>{scores.IndexOfValue(ev.Player)}</color>",
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
                if (kvp.Value.LeadingTeam == ev.LeadingTeam)
                {
                    Log.Debug($"{kvp.Value.Nickname} rewarded for winning");
                    int index = scores.IndexOfValue(kvp.Value);
                    AddPlayerScore(kvp.Value, Config.TicketsPerWin);
                }
            }
        }

        private void PlayerOnEscaping(EscapingEventArgs ev)
        {
            Log.Debug($"{ev.Player.Nickname} rewarded for escaping!");
            AddPlayerScore(ev.Player, Config.TicketsPerWin);
        }

        private void MapOnGeneratorActivated(GeneratorActivatedEventArgs ev)
        {
            foreach (var kvp in scores)
            {
                if (kvp.Value.IsNTF)
                {
                    Log.Debug($"{kvp.Value.Nickname} rewarded for enabling the generator");
                    AddPlayerScore(kvp.Value, Config.TicketsPerGeneratorActivated);
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
                        if ((kvp.Value.IsNTF || kvp.Value.Role.Type == RoleType.Scientist) &&
                            (ev.Killer.IsNTF || ev.Killer.Role.Type == RoleType.Scientist) ||
                            (kvp.Value.IsCHI || kvp.Value.Role.Type == RoleType.ClassD) &&
                            (ev.Killer.IsCHI || ev.Killer.Role.Type == RoleType.ClassD))
                        {
                            Log.Debug($"{kvp.Value.Nickname} rewarded for containing an scp");
                            AddPlayerScore(kvp.Value, Config.TicketsPerSCPRecontained);
                        }
                    }
                }
                else
                {
                    if (!ev.Killer.IsScp)
                    {
                        Log.Debug($"{ev.Killer.Nickname} rewarded for a kill");
                        AddPlayerScore(ev.Killer, Config.TicketsPerKill);
                    }
                    else if (ev.Killer.Role == RoleType.Scp0492)
                    {
                        Log.Debug($"{ev.Killer.Nickname} rewarded for a kill");
                        AddPlayerScore(ev.Killer, Config.TicketsPerZombieKill);
                    }

                }
            }

            if (Config.DisplayPlacementOnSpectator)
                ev.Target.ShowHint(
                    $"You have <color=red>{scores.First(x => x.Value == ev.Target)}</color> tickets\nYour placement is <color=red>{scores.IndexOfValue(ev.Killer)}</color>",
                    10f);
        }

        private void ServerOnRoundStarted()
        {
            foreach (var plr in scores)
            {
                if (!Player.List.Contains(plr.Value))
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
        

        private void setScpsWithBehaviour(int amountOfPlayers)
        {
            for (int x = 0; x < amountOfPlayers; x++)
            {
                Player plr = scores[x];
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

        private void AddPlayerScore(Player plr, int amount)
        {
            scores.Remove(scores.First(x => x.Value == plr).Key);
            scores.Add(scores.First(x => x.Value == plr).Key + Config.TicketsPerWin, plr);
        }
    }
}