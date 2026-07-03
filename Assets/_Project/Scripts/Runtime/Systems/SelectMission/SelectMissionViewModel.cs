using Cysharp.Threading.Tasks;
using Maneuver.ScreenManager;
using R3;
using UnityEngine;

namespace App.UI.Screens.Behaviour
{
    public class SelectMissionViewModel : ScreenViewModelBase
    {

        private IChangeScene _changeScene;

        public ReactiveProperty<int> MissionCount { get; } = new();

        public ReactiveCommand<ScenesInGame> PlayLevelCommand { get; } = new();

        public SelectMissionViewModel(IChangeScene changeScene)
        {
            _changeScene = changeScene;

            TrackDisposable(PlayLevelCommand.Subscribe(scene => LoadLevel(scene)));
        }

        public async UniTask GenerateCards()
        {
            await UniTask.WaitForSeconds(5);

            MissionCount.Value = 5;
        }

        private void LoadLevel(ScenesInGame scenes)
        {
            _changeScene.LoadScene(scenes);
        }
  
    }
}
