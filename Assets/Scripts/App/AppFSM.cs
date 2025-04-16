using App.AppStates;
using Common;

public class AppFSM : FSM
{
    private readonly BootState _bootState;
    private readonly StartedState _startedState;
    private readonly VideoShowingState _videoShowingState;
    private readonly ConnectToWiFiState _connectToWiFiState;
    private readonly ContentDownloadState _contentDownloadState;
    private readonly WaitingForContentState _waitingForContentState;

    public AppFSM(
        BootState bootState,
        StartedState startedState,
        VideoShowingState videoShowingState,
        ConnectToWiFiState connectToWiFiState,
        ContentDownloadState contentDownloadState,
        WaitingForContentState waitingForContentState)
    {
        _bootState = bootState;
        _startedState = startedState;
        _videoShowingState = videoShowingState;

        _connectToWiFiState = connectToWiFiState;

        _contentDownloadState = contentDownloadState;
        _waitingForContentState = waitingForContentState;

        InitializeStates();
    }

    public FsmStateBase GetCurrentState() => StateBaseCurrent;

    private void InitializeStates()
    {
        AddState(_bootState);
        AddState(_startedState);
        AddState(_videoShowingState);
        AddState(_connectToWiFiState);
        AddState(_contentDownloadState);
        AddState(_waitingForContentState);

        SetState<BootState>();
    }
}