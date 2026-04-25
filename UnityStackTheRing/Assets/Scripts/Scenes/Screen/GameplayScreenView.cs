namespace HyperCasualGame.Scripts.Scenes.Screen
{
    using Cysharp.Threading.Tasks;
    using GameFoundationCore.Scripts.Signals;
    using GameFoundationCore.Scripts.UIModule.ScreenFlow.BaseScreen.Presenter;
    using GameFoundationCore.Scripts.UIModule.ScreenFlow.BaseScreen.View;
    using HyperCasualGame.Scripts.Level;
    using HyperCasualGame.Scripts.StateMachines.Game;
    using HyperCasualGame.Scripts.StateMachines.Game.States;
    using TMPro;
    using UniT.Logging;
    using UnityEngine;
    using UnityEngine.UI;

    public class GameplayScreenView : BaseView
    {
        [field: SerializeField] public Button BtnHome { get; private set; }
        [field: SerializeField] public TMP_InputField LevelInput { get; private set; }
    }
    [ScreenInfo(nameof(GameplayScreenView))]
    public class GameplayScreenPresenter : BaseScreenPresenter<GameplayScreenView>
    {
        #region Inject
        private readonly GameStateMachine gameStateMachine;
        private readonly ILevelManager levelManager;

        public GameplayScreenPresenter(
            SignalBus signalBus,
            ILoggerManager loggerManager,
            GameStateMachine gameStateMachine,
            ILevelManager levelManager
        ) : base(signalBus, loggerManager)
        {
            this.gameStateMachine = gameStateMachine;
            this.levelManager = levelManager;
        }
        #endregion

        protected override void OnViewReady()
        {
            base.OnViewReady();
            this.View.BtnHome.onClick.AddListener(this.OnClickHome);
            if (this.View.LevelInput != null)
            {
                this.View.LevelInput.onSubmit.AddListener(this.OnSubmitLevel);
                this.View.LevelInput.onEndEdit.AddListener(this.OnSubmitLevel);
            }
        }

        public override UniTask BindData()
        {
            if (this.View.LevelInput != null)
            {
                this.View.LevelInput.text = this.levelManager.CurrentLevel.ToString();
            }

            return UniTask.CompletedTask;
        }


        private void OnClickHome()
        {
            this.gameStateMachine.TransitionTo<GameHomeState>();
        }

        private void OnSubmitLevel(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            if (!int.TryParse(value, out var levelNumber))
            {
                return;
            }

            this.levelManager.LoadLevel(levelNumber).Forget();
        }
    }
}