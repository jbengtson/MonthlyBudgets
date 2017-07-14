using UnityEngine;

namespace severedsolo {
    [KSPAddon(KSPAddon.Startup.SpaceCentre, true)]
    public class DeadKerbalPenaliser : MonoBehaviour {
        public void Awake() {
            if(!HighLogic.CurrentGame.Parameters.CustomParams<BudgetSettings>().masterSwitch) {
                Destroy(this);
            }
            DontDestroyOnLoad(this);
            GameEvents.onCrewKilled.Add(onCrewKilled);
        }

        public void OnDestroy() {
            GameEvents.onCrewKilled.Remove(onCrewKilled);
        }

        private void onCrewKilled(EventReport evnt) {
            if(HighLogic.CurrentGame.Mode != Game.Modes.CAREER) { return; }
            
            Reputation.Instance.AddReputation(-50, TransactionReasons.None);
            MonthlyBudgets.AddMessage("Astronaut Lost!", "The loss of " + evnt.sender + " is terrible news for the world and our agency. New safety measures should be implemented to prevent further losses.", KSP.UI.Screens.MessageSystemButton.MessageButtonColor.RED, KSP.UI.Screens.MessageSystemButton.ButtonIcons.ALERT);
        }
    }
}
