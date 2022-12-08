using UnityEngine;

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

        public override void OnEnabled()
        {
            scores = scores.OrderBy(kvp => kvp.Value).ToDictionary(key => key.Key, value => value.Value);
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
            scores = scores.OrderBy(kvp => kvp.Value).ToDictionary(key => key.Key, value => value.Value);
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
            scores = scores.OrderBy(kvp => kvp.Value).ToDictionary(key => key.Key, value => value.Value);
            
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
                    plr.SetRole((RoleType) Server.Host.ReferenceHub.characterClassManager.FindRandomIdUsingDefinedTeam(Team.SCP));
                }
            }
        }

        private void printDictionary()
        {
            foreach (var kvp in scores)
            {
                Log.Debug($"{kvp.Key} | {kvp.Value}");
            }
        }
    }
}