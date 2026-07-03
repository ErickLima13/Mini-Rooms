using UnityEngine;
using Zenject;

public class GameContext : MonoInstaller
{
    public override void InstallBindings()
    {

        Container.Bind<IChangeScene>().To<ChangeScene>().AsSingle().NonLazy();

    }
}