using UnityEngine;

namespace severedsolo {
    class BudgetSettings : GameParameters.CustomParameterNode {
        public enum BudgetDifficulty {
            EASY,
            MEDIUM,
            HARD,
        }
        public override string Title { get { return "Monthly Budget Options"; } }
        public override GameParameters.GameMode GameMode { get { return GameParameters.GameMode.CAREER; } }
        public override string Section { get { return "Monthly Budgets"; } }
        public string DisplaySection { get { return Section; } }
        public override int SectionOrder { get { return 1; } }
        public override bool HasPresets { get { return true; } }
        public bool autoPersistance = true;
        public bool newGameOnly = false;
        // This is probably going to be the only one available later, we'll move to config files because sliders are shit.
        [GameParameters.CustomParameterUI("Mod enabled?")]
        public bool masterSwitch = true;
        public bool ContractInterceptor = true;
        [GameParameters.CustomParameterUI("Stop Timewarp on budget?", toolTip = "Will also add KAC alarm if applicable")]
        public bool stopTimewarp = false;
        [GameParameters.CustomParameterUI("Excess Covers Costs?", toolTip = "If there is budget left over from the current period will it cover costs on the next?")]
        public bool excessCoversCosts = false;
        [GameParameters.CustomFloatParameterUI("Period Length", minValue = 1, maxValue = 427, toolTip = "Length of the budgeting period in homeworld days.")]
        public float periodLength = 91.25f;
        [GameParameters.CustomFloatParameterUI("Astronaut Wages per Period", minValue = 1, maxValue = 20)]
        public float wages = 5;
        [GameParameters.CustomFloatParameterUI("Vessel Maintenance Cost per Period", minValue = 1, maxValue = 1000, toolTip = "Costs per active vessel in the tracking station per period. Some vessel types not counted.")]
        public float vesselCost = 50;

        public override void SetDifficultyPreset(GameParameters.Preset preset) {
            Debug.Log("[MonthlyBudgets]: Setting difficulty preset");
            switch(preset) {
                case GameParameters.Preset.Easy:
                    wages = 4;
                    vesselCost = 40;
                    break;
                case GameParameters.Preset.Normal:
                    wages = 5;
                    vesselCost = 50;
                    break;
                case GameParameters.Preset.Moderate:
                    wages = 7.5f;
                    vesselCost = 75;
                    break;
                case GameParameters.Preset.Hard:
                    wages = 10;
                    vesselCost = 100;
                    break;
            }
        }
    }
}
