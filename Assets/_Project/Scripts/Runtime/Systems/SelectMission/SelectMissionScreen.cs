using Cysharp.Threading.Tasks;
using Maneuver.ScreenManager;
using R3;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UIElements;

namespace App.UI.Screens.Behaviour
{
    public class SelectMissionScreen : ScreenBase<SelectMissionViewModel>
    {
        private Button _exitButton;
        private VisualElement _scrollContent;


        [SerializeField] private List<ScenesInGame> _scenesInGame = new();

        [SerializeField] private VisualTreeAsset _missionCard;

        public override void Initialize()
        {
            _exitButton = _root.Q<Button>("closeButton");
            _scrollContent = _root.Q("scrollContent");

            _scenesInGame = new List<ScenesInGame>((ScenesInGame[])Enum.GetValues(typeof(ScenesInGame)));


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
                int index = i;

                TemplateContainer container = _missionCard.Instantiate();

                Button btn = container.Q<Button>("PlayMissionButton");

                btn.text = _scenesInGame[i].ToString();

                btn.clicked += () => ViewModel.PlayLevelCommand.Execute(_scenesInGame[index]);

                _scrollContent.Add(container);

            }
        }
    }
}
