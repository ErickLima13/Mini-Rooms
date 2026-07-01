using Cysharp.Threading.Tasks;
using Maneuver.ScreenManager;
using R3;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace App.UI.Screens.Behaviour
{


    public class SelectMissionViewModel : ScreenViewModelBase
    {
        public ReactiveProperty<int> MissionCount { get; } = new();


        public async UniTask GenerateCards()
        {
            await UniTask.WaitForSeconds(5);

            MissionCount.Value = 5;
        }
    }
}
