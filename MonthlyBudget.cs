using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using KSP.UI.Screens;
using System;
using MonthlyBudgets_KACWrapper;
using Experience;

namespace severedsolo {
    [KSPAddon(KSPAddon.Startup.SpaceCentre, true)]
    public class MonthlyBudgets : MonoBehaviour {
        public static double lastUpdate = 99999;
        private double budgetInterval;
        private float periodLength = 91.25f;
        private float wages = 5;
        private float vesselCost = 50;
        bool showGUI = false;
        ApplicationLauncherButton ToolbarButton;
        Rect Window = new Rect(20, 100, 240, 50);
        float loanPercentage = 1.0f;
        bool timeDiscrepancyLog = true;
        bool stopTimeWarp;
        bool excessCoversCosts;
        CelestialBody HomeWorld;
        double dayLength;
        double yearLength;

        // provide a common function to load the next funding total.
        private float NextFunding() {
            float rep = Reputation.CurrentRep;

            // if your rep is this low you have basically lost the game.
            if(rep < -250) {
                return 0.0f;
            }

            // it's ... complicated, but based on fitting to desired values.
            float baseFunding = (float) Math.Pow(rep + 250.0, 2.1);
            if(rep < 0) {
                return baseFunding / 40.0f;
            }
            if(rep > 0) {
                return baseFunding / 10.0f;
            }
            // I can't see this happening with floats but whatever
            return baseFunding / 20.0f;
        }

        // this apparently does the budget
        private void Budget(double timeSinceLastUpdate) {
            try {
                float crewCosts = CrewWages();
                float vesselCosts = VesselCosts();
                float totalCosts = crewCosts + vesselCosts;
                float funding = NextFunding();
                double currentFunds = Funding.Instance.Funds;
                double excessCovered = 0.0;

                // if the previous period can cover costs on this one, and the player has enabled it, apply them.
                if(excessCoversCosts) {
                    if(currentFunds >= totalCosts) {
                        excessCovered = totalCosts;
                    } else {
                        excessCovered = currentFunds;
                    }
                }

                // apply all these changes.
                double finalBudget = funding - (totalCosts - excessCovered);
                Funding.Instance.AddFunds(-currentFunds, TransactionReasons.None);
                Funding.Instance.AddFunds(finalBudget, TransactionReasons.None);

                // tell the player what happened.
                String message = "Budget Report";
                message += "\n  Current Funds : " + currentFunds.ToString("C");
                if(excessCoversCosts) {
                    message += "\n  Excess Covered: " + excessCovered.ToString("C");
                }
                message += "\n";
                message += "\n  Crew Costs:     " + crewCosts.ToString("C");
                message += "\n  Vessel Upkeep:  " + vesselCosts.ToString("C");
                message += "\n  Total Costs:    " + totalCosts.ToString("C");
                message += "\n";
                message += "\n  Funding:        " + funding.ToString("C");
                message += "\n";
                message += "\nThis Period's Budget: " + finalBudget.ToString("C");
                AddMessage("Budget Report", message, MessageSystemButton.MessageButtonColor.BLUE, MessageSystemButton.ButtonIcons.MESSAGE);

                // upkeep
                lastUpdate = lastUpdate + budgetInterval;
                if(loanPercentage < 1) {
                    loanPercentage = loanPercentage + 0.1f;
                }

                if(!KACWrapper.AssemblyExists && stopTimeWarp) {
                    TimeWarp.SetRate(0, true);
                }
            } catch {
                Debug.Log("[MonthlyBudgets]: Problem calculating the budget");
            }
        }

        void Awake() {
            DontDestroyOnLoad(this);
            if(!HighLogic.CurrentGame.Parameters.CustomParams<BudgetSettings>().masterSwitch) { Destroy(this); }
            GameEvents.onGameStateSave.Add(OnGameStateSave);
            GameEvents.onGameStateLoad.Add(OnGameStateLoad);
            GameEvents.onGUIApplicationLauncherReady.Add(GUIReady);
            GameEvents.OnGameSettingsApplied.Add(OnGameSettingsApplied);
            GameEvents.onGameSceneSwitchRequested.Add(onGameSceneSwitchRequested);
        }

        void Start() {
            KACWrapper.InitKACWrapper();
            Debug.Log(KACWrapper.KAC.Alarms.Count);
        }

        void Update() {
            if(HighLogic.CurrentGame.Mode != Game.Modes.CAREER) { return; }
            // what's this for?
            if(lastUpdate == 99999) { return; }

            double time = (Planetarium.GetUniversalTime());

            while(lastUpdate > time) {
                lastUpdate = lastUpdate - budgetInterval;
                if(timeDiscrepancyLog) {
                    timeDiscrepancyLog = false;
                }
            }

            double timeSinceLastUpdate = time - lastUpdate;

            if(timeSinceLastUpdate >= budgetInterval) {
                Budget(timeSinceLastUpdate);
            }

            if(KACWrapper.AssemblyExists && stopTimeWarp) {
                if(!KACWrapper.APIReady) { return; }
                KACWrapper.KACAPI.KACAlarmList alarms = KACWrapper.KAC.Alarms;
                if(alarms.Count == 0) { return; }
                for(int i = 0; i < alarms.Count; i++) {
                    string s = alarms[i].Name;
                    if(s == "Next Budget") { return; }
                }
                KACWrapper.KAC.CreateAlarm(KACWrapper.KACAPI.AlarmTypeEnum.Raw, "Next Budget", lastUpdate + budgetInterval);
            }
        }

        void OnDestroy() {
            GameEvents.onGameStateSave.Remove(OnGameStateSave);
            GameEvents.onGameStateLoad.Remove(OnGameStateLoad);
            GameEvents.onGUIApplicationLauncherReady.Remove(GUIReady);
            GameEvents.OnGameSettingsApplied.Remove(OnGameSettingsApplied);
            GameEvents.onGameSceneSwitchRequested.Remove(onGameSceneSwitchRequested);
        }

        private float NextBudget() {
            return CrewWages() + VesselCosts();
        }

        private float CrewWages() {
            // fucking tourists, who the fuck thought this was good gameplay?
            IEnumerable<ProtoCrewMember> crew = HighLogic.CurrentGame.CrewRoster.Crew.Where(p => p.type != ProtoCrewMember.KerbalType.Tourist);
            // we're not differentiating between assigned and unassigned, nor do we care about experience.
            return wages * crew.Count();
        }

        private float VesselCosts() {
            // I have no idea if this is bad linq.  vOv
            IEnumerable<Vessel> vessels = FlightGlobals.Vessels.Where(v => v.vesselType != VesselType.Debris && v.vesselType != VesselType.Flag && v.vesselType != VesselType.SpaceObject && v.vesselType != VesselType.Unknown && v.vesselType != VesselType.EVA);
            return vessels.Count() * vesselCost;
        }

        private void OnGameStateLoad(ConfigNode node) {
            if(!float.TryParse(node.GetValue("EmergencyFunding"), out loanPercentage)) {
                loanPercentage = 1.0f;
            }

            periodLength = HighLogic.CurrentGame.Parameters.CustomParams<BudgetSettings>().periodLength;
            wages = HighLogic.CurrentGame.Parameters.CustomParams<BudgetSettings>().wages;
            if(HomeWorld == null) {
                PopulateHomeWorldData();
            }
            budgetInterval = periodLength * dayLength;
            vesselCost = HighLogic.CurrentGame.Parameters.CustomParams<BudgetSettings>().vesselCost;
            if(!double.TryParse(node.GetValue("LastBudgetUpdate"), out lastUpdate)) {
                lastUpdate = budgetInterval * 1000;
            }
            timeDiscrepancyLog = true;
            stopTimeWarp = HighLogic.CurrentGame.Parameters.CustomParams<BudgetSettings>().stopTimewarp;
            excessCoversCosts = HighLogic.CurrentGame.Parameters.CustomParams<BudgetSettings>().excessCoversCosts;
        }

        private void OnGameStateSave(ConfigNode savedNode) {
            savedNode.AddValue("LastBudgetUpdate", lastUpdate);
            savedNode.AddValue("EmergencyFunding", loanPercentage);
        }

        public void OnGUI() {
            if(showGUI) {
               Window = GUILayout.Window(65468754, Window, GUIDisplay, "MonthlyBudgets", GUILayout.Width(200));
            }
        }

        public void GUIReady() {
            if(HighLogic.CurrentGame.Mode != Game.Modes.CAREER || HighLogic.LoadedScene == GameScenes.MAINMENU) { return; }
            if(ToolbarButton == null) {
                ToolbarButton = ApplicationLauncher.Instance.AddModApplication(GUISwitch, GUISwitch, null, null, null, null, ApplicationLauncher.AppScenes.ALWAYS, GameDatabase.Instance.GetTexture("MonthlyBudgets/Icon", false));
            }
        }

        void PopulateHomeWorldData() {
            HomeWorld = FlightGlobals.GetHomeBody();
            dayLength = HomeWorld.solarDayLength;
            yearLength = HomeWorld.orbit.period;
        }

        void GUIDisplay(int windowID) {
            if(HighLogic.CurrentGame.Mode != Game.Modes.CAREER) {
                GUILayout.Label("MonthlyBudgets is only available in Career Games");
                return;
            }
            if(HomeWorld == null) { PopulateHomeWorldData(); }

            float costs = NextBudget();
            float estimatedBudget = NextFunding();
            if(estimatedBudget < 0) {
                estimatedBudget = 0;
            }

            double nextUpdateRaw = lastUpdate + budgetInterval;
            double nextUpdateRefine = nextUpdateRaw / dayLength;
            int year = 1;
            int day = 1;
            while(nextUpdateRefine > yearLength / dayLength) {
                year = year + 1;
                nextUpdateRefine = nextUpdateRefine - (yearLength / dayLength);
            }
            day = day + (int) nextUpdateRefine;
            GUILayout.Label("Next Budget Due: Y " + year + " D " + day);
            GUILayout.Label("Estimated Budget: $" + estimatedBudget);
            GUILayout.Label("Current Costs: $" + costs);
            double loanAmount = Math.Round((costs / 5) * loanPercentage, 0);
            int RepLoss = (int) Reputation.CurrentRep / 20;
            if(loanAmount > 0) {
                if(GUILayout.Button("Apply for Emergency Funding (" + loanAmount + ")")) {
                    Reputation.Instance.AddReputation(-RepLoss, TransactionReasons.None);
                    Funding.Instance.AddFunds(loanAmount, TransactionReasons.None);
                    loanPercentage = loanPercentage - 0.1f;
                }
            }
            GUI.DragWindow();
        }


        public void GUISwitch() {
            if(showGUI) {
                showGUI = false;
            } else {
                showGUI = true;
            }
        }

        void OnGameSettingsApplied() {
            periodLength = HighLogic.CurrentGame.Parameters.CustomParams<BudgetSettings>().periodLength;
            wages = HighLogic.CurrentGame.Parameters.CustomParams<BudgetSettings>().wages;
            if(HomeWorld == null) { PopulateHomeWorldData(); }
            budgetInterval = periodLength * dayLength;
            vesselCost = HighLogic.CurrentGame.Parameters.CustomParams<BudgetSettings>().vesselCost;
            stopTimeWarp = HighLogic.CurrentGame.Parameters.CustomParams<BudgetSettings>().stopTimewarp;
            excessCoversCosts = HighLogic.CurrentGame.Parameters.CustomParams<BudgetSettings>().excessCoversCosts;
        }

        void onGameSceneSwitchRequested(GameEvents.FromToAction<GameScenes, GameScenes> data) {
            if(ToolbarButton == null) { return; }
            ApplicationLauncher.Instance.RemoveModApplication(ToolbarButton);
            showGUI = false;
        }

        public static void AddMessage(String title, String text, MessageSystemButton.MessageButtonColor color, MessageSystemButton.ButtonIcons icon) {
            MessageSystem.Message m = new MessageSystem.Message(title, text.ToString(), color, icon);
            MessageSystem.Instance.AddMessage(m);
        }
    }
}