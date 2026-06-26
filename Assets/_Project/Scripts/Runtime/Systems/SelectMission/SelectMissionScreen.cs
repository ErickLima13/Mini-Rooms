using Maneuver.ScreenManager;
using UnityEngine;
using UnityEngine.UIElements;

namespace App.UI.Screens.Behaviour
{
    public class SelectMissionScreen : ScreenBase<SelectMissionViewModel>
    {

        [SerializeField] private VisualTreeAsset _missionCard;

        public override void Initialize()
        {
        }

        protected override void BindViewModel(SelectMissionViewModel viewModel)
        {
        }
    }
}
