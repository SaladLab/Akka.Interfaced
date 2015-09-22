using System.Collections;
using UnityEngine;
using SlimUnityChat.Interface;
using System.Collections.Generic;
using Akka.Interfaced;
using System;

public class ChatScene : MonoBehaviour, IUserEventObserver
{
    public ControlPanel ControlPanel;
    public GameObject ContentPanel;
    public GameObject ChatPanelTemplate;

    private bool _isBusy;
    private string _currentRoomName;

    private class RoomItem
    {
        public ChatPanel ChatPanel;
        public OccupantRef Occupant;
        public int ObserverId;
    }
    private Dictionary<string, RoomItem> _roomItemMap = new Dictionary<string, RoomItem>();

    void Start()
    {
        ApplicationComponent.TryInit();
        UiManager.Initialize();

        ControlPanel.LogoutButtonClicked = OnLogoutButtonClick;
        ControlPanel.RoomButtonClicked = OnRoomButtonClick;
        ControlPanel.RoomItemSelected = OnRoomItemClick;

        ChatPanelTemplate.SetActive(false);
    }

    void Update()
    {
        CheckLoginedOrTryToLogin();
    }

    void CheckLoginedOrTryToLogin()
    {
        if (G.User != null || _isBusy)
            return;

        _isBusy = true;
        StartCoroutine(ProcessLogin());
    }

    IEnumerator ProcessLogin()
    {
        try
        {
            var loginDialog = UiManager.Instance.ShowModalRoot<LoginDialog>();
            yield return StartCoroutine(loginDialog.WaitForHide());

            if (loginDialog.ReturnValue != null)
            {
                var result = (Tuple<string, int>)loginDialog.ReturnValue;
                ControlPanel.SetUserName(result.Item1);
                G.Comm.AddObserver(result.Item2, this);
                yield return StartCoroutine(ProcessEnterRoom(G.User, "#general"));
            }
        }
        finally
        {
            _isBusy = false;
        }
    }

    void OnRoomTextClick()
    {
        if (G.User == null || _isBusy)
            return;

        _isBusy = true;
        StartCoroutine(ProcessSelectRoomAndEnter());
    }

    private void OnLogoutButtonClick()
    {
        // TODO:IMPL
    }

    private void OnRoomButtonClick()
    {
        if (G.User == null || _isBusy)
            return;

        _isBusy = true;
        StartCoroutine(ProcessSelectRoomAndEnter());
    }

    IEnumerator ProcessSelectRoomAndEnter()
    {
        try
        {
            var t1 = G.User.GetRoomList();
            yield return t1.WaitHandle;

            if (t1.Exception != null)
            {
                Debug.Log(t1.Exception.ToString());
                yield break;
            }

            var roomDialog = UiManager.Instance.ShowModalRoot<RoomDialog>(
                new RoomDialog.Argument
                {
                    CurrentRoomName = _currentRoomName,
                    RoomList = t1.Result
                });
            yield return StartCoroutine(roomDialog.WaitForHide());

            if (roomDialog.ReturnValue == null)
                yield break;

            var roomName = (string)roomDialog.ReturnValue;
            if (_roomItemMap.ContainsKey(roomName))
            {
                OnRoomItemClick(roomName);
                yield break;
            }

            yield return StartCoroutine(ProcessEnterRoom(G.User, roomName));
        }
        finally
        {
            _isBusy = false;
        }
    }

    private IEnumerator ProcessEnterRoom(UserRef user, string roomName)
    {
        // Try to enter the room

        var observerId = G.Comm.IssueObserverId();
        var t1 = user.EnterRoom(roomName, observerId);
        yield return t1.WaitHandle;

        if (t1.Status != TaskStatus.RanToCompletion)
        {
            Debug.LogError(t1.Exception.ToString());
            yield break;
        }

        // Spawn new room item

        var go = UiHelper.AddChild(ContentPanel, ChatPanelTemplate);
        var chatPanel = go.GetComponent<ChatPanel>();
        var occupant = new OccupantRef(new SlimActorRef { Id = t1.Result.Item1 }, new SlimRequestWaiter { Communicator = G.Comm }, null);
        chatPanel.SetOccupant(occupant);
        chatPanel.SetRoomInfo(t1.Result.Item2);
        chatPanel.ExitButtonClicked = () => OnRoomExitClick(roomName);

        var item = new RoomItem
        {
            ChatPanel = chatPanel,
            Occupant = occupant,
            ObserverId = observerId
        };
        _roomItemMap.Add(roomName, item);
        ControlPanel.AddRoomItem(roomName);
        G.Comm.AddObserver(observerId, chatPanel);

        // Select

        OnRoomItemClick(roomName);
    }

    private void OnRoomItemClick(string roomName)
    {
        foreach (var item in _roomItemMap)
        {
            item.Value.ChatPanel.gameObject.SetActive(item.Key == roomName);
        }

        ControlPanel.SelectRoomItem(roomName);
        _currentRoomName = roomName;
    }

    private void OnRoomExitClick(string roomName)
    {
        if (roomName == "#general")
            return;

        _isBusy = true;
        StartCoroutine(ProcessExitFromRoom(G.User, roomName));
    }

    private IEnumerator ProcessExitFromRoom(UserRef user, string roomName)
    {
        try
        {
            var t1 = user.ExitFromRoom(roomName);
            yield return t1.WaitHandle;
            if (t1.Status != TaskStatus.RanToCompletion)
            {
                Debug.LogError("ProcessExitFromRoom failed: " + t1.Exception);
                yield break;
            }
                
            RoomItem item;
            if (_roomItemMap.TryGetValue(roomName, out item) == false)
                yield break;

            _roomItemMap.Remove(roomName);
            Destroy(item.ChatPanel.gameObject);
            G.Comm.RemoveObserver(item.ObserverId);
            ControlPanel.DeleteRoomItem(roomName);

            OnRoomItemClick("#general");
        }
        finally
        {
            _isBusy = false;
        }
    }

    void IUserEventObserver.Whisper(ChatItem chatItem)
    {
        if (string.IsNullOrEmpty(_currentRoomName))
            return;

        _roomItemMap[_currentRoomName].ChatPanel.AppendChatMessage(string.Format(
            "@ <color=#800080ff><b>{0}</b></color>: {1}",
            chatItem.UserId, chatItem.Message));
    }

    void IUserEventObserver.Invite(string invitorUserId, string roomName)
    {
        if (string.IsNullOrEmpty(_currentRoomName))
            return;

        _roomItemMap[_currentRoomName].ChatPanel.AppendChatMessage(string.Format(
            "! <color=#800080ff><b>{0}</b></color> invites you from {1}",
            invitorUserId, roomName));
    }
}
