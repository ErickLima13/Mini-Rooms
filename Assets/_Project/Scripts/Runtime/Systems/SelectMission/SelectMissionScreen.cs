using Cysharp.Threading.Tasks;
using Maneuver.ScreenManager;
using R3;
using UnityEngine;
using UnityEngine.UIElements;

namespace App.UI.Screens.Behaviour
{
    public class SelectMissionScreen : ScreenBase<SelectMissionViewModel>
    {
        private Button _exitButton;
        private VisualElement _scrollContent;


        [SerializeField] private VisualTreeAsset _missionCard;

        public override void Initialize()
        {
            _exitButton = _root.Q<Button>("closeButton");
            _scrollContent = _root.Q("scrollContent");  
        }

        protected override void BindViewModel(SelectMissionViewModel viewModel)
        {
            TrackBinding(viewModel.MissionCount.Subscribe(GenerateMissionCard));

        }

        public override void Show()
        {
            base.Show();
            ViewModel.GenerateCards().Forget();
        }

        private void GenerateMissionCard(int cards)
        {
            if (cards <= 0) return;

            _scrollContent.Clear();

            for (int i = 0; i < cards; i++)
            {
                TemplateContainer container = _missionCard.Instantiate();

                _scrollContent.Add(container);

            }
        }
    }
}
