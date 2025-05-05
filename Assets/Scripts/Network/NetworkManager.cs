using Cysharp.Threading.Tasks;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using Debug = CustomDebug.Debug;

namespace Network
{
    public class NetworkManager : MonoBehaviourPunCallbacks
    {
        [SerializeField] private bool _isHost;
        [SerializeField] private GameObject _clientPrefab;
        
        private void Start()
        {
            PhotonNetwork.ConnectUsingSettings();
        }

        public void CreateRoom(string roomName, int maxRoomClientsCapacity)
        {
            var roomOptions = new RoomOptions();
            roomOptions.MaxPlayers = maxRoomClientsCapacity;

            PhotonNetwork.CreateRoom(roomName, roomOptions);
        }

        public void JoinRoom(string roomName)
        {
            PhotonNetwork.JoinRoom(roomName);
        }
        
        public override void OnConnectedToMaster()
        {
            Debug.Log("Подключено к Photon Master");
            PhotonNetwork.JoinLobby();
        }

        public override async void OnJoinedLobby()
        {
            Debug.Log("Вошли в лобби");

            await UniTask.Delay(5000);

            if (_isHost)
            {
                CreateRoom("Test",3);
            }
            
            await UniTask.Delay(5000);
            JoinRoom("Test");
        }
        
        public override void OnCreatedRoom()
        {
            Debug.Log("Комната создана: " + PhotonNetwork.CurrentRoom.Name);
        }

        public override void OnJoinedRoom()
        {
            Debug.Log("Подключились к комнате: " + PhotonNetwork.CurrentRoom.Name);
            
            PhotonNetwork.Instantiate(_clientPrefab.name, new Vector3(0,0,0), Quaternion.identity);
        }

        public override void OnCreateRoomFailed(short returnCode, string message)
        {
            Debug.LogWarning("Не удалось создать комнату: " + message);
        }

        public override void OnJoinRoomFailed(short returnCode, string message)
        {
            Debug.LogWarning("Не удалось подключиться к комнате: " + message);
        }
    }
}