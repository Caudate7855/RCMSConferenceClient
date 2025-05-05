using CustomDebug;
using Photon.Pun;
using Photon.Realtime;

namespace Network
{
    public class NetworkManager : MonoBehaviourPunCallbacks
    {
        private void Start()
        {
            PhotonNetwork.ConnectUsingSettings();
            
            CreateRoom("Test",3);
            
            JoinRoom("Test");
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

        public override void OnJoinedLobby()
        {
            Debug.Log("Вошли в лобби");
        }
        
        public override void OnCreatedRoom()
        {
            Debug.Log("Комната создана: " + PhotonNetwork.CurrentRoom.Name);
        }

        public override void OnJoinedRoom()
        {
            Debug.Log("Подключились к комнате: " + PhotonNetwork.CurrentRoom.Name);
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