using UnityEngine;
using Contracts;
using System;

namespace severedsolo {
    [KSPAddon(KSPAddon.Startup.SpaceCentre, true)]
    class ContractInterceptor : MonoBehaviour {
        bool disableContracts = true;

        public void Awake() {
            if(!HighLogic.CurrentGame.Parameters.CustomParams<BudgetSettings>().masterSwitch) {
                Destroy(this);
            }
            DontDestroyOnLoad(this);
            GameEvents.Contract.onOffered.Add(onOffered);
            GameEvents.OnGameSettingsApplied.Add(onSettings);
            GameEvents.onGameStateLoad.Add(onLoaded);
        }

        private void onLoaded(ConfigNode data) {
            disableContracts = HighLogic.CurrentGame.Parameters.CustomParams<BudgetSettings>().ContractInterceptor;
        }

        private void onSettings() {
            disableContracts = HighLogic.CurrentGame.Parameters.CustomParams<BudgetSettings>().ContractInterceptor;
        }

        public void OnDestroy() {
            GameEvents.Contract.onOffered.Remove(onOffered);
        }

        // don't you dare ever award any funds for a contract, kill it with nuclear fucking fire.
        // We're also not going to convert any funds to rep, you only get the stated rep.
        private void onOffered(Contract contract) {
            if(HighLogic.CurrentGame.Mode != Game.Modes.CAREER || !disableContracts) { return; }
            contract.FundsAdvance = 0;
            contract.FundsCompletion = 0;
        }
    }
}
