using UnityEngine;
using UnityEngine.UI;
using SlimUnityChat.Interface;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using Akka.Interfaced;

public class ChatPanel : MonoBehaviour, IRoomObserver
{
    public Text RoomText;
    public Text UserText;
    public InputField ChatInput;
    public Text ChatText;

    public Action ExitButtonClicked;

    private OccupantRef _occupant;
    private RoomInfo _roomInfo;

    void Start()
    {
    }

    void Update()
    {
    }

    public void SetOccupant(OccupantRef occupant)
    {
        _occupant = occupant;
    }

    public void SetRoomInfo(RoomInfo info)
    {
        _roomInfo = info;
        if (_roomInfo.Users == null)
            _roomInfo.Users = new List<string>();
        DisplayRoomInfo();
    }

    private void DisplayRoomInfo()
    {
        RoomText.text = _roomInfo.Name;
        UserText.text = string.Format("#user: {0}", _roomInfo.Users.Count);
        if (_roomInfo.History != null)
        {
            foreach (var item in _roomInfo.History)
                ((IRoomObserver)this).Say(item);

            _roomInfo.History = null;
        }
    }

    public void OnInputFieldEndInput()
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            OnInputButtonClick();
            ChatInput.Select();
            ChatInput.ActivateInputField();
        }
    }

    public void OnInputButtonClick()
    {
        var msg = ChatInput.text.Trim();
        if (msg.StartsWith("/"))
        {
            var words = msg.Split(' ');
            var command = words[0].ToLower();
            switch (command.Substring(1))
            {
                case "i":
                case "invite":
                    for (int i = 1; i < words.Length; i++)
                        StartCoroutine(ProcessInvite(words[i]));
                    break;

                case "w":
                case "whisper":
                    if (words.Length < 3)
                        break;
                    StartCoroutine(ProcessWhisper(words[1], string.Join(" ", words.Skip(2).ToArray())));
                        break;

                default:
                    AppendChatMessage(string.Format("***** Illegal command: {0}", command));
                    break;
            }
        }
        else
        {
            _occupant.Say(msg);
        }

        ChatInput.text = "";
    }

    private IEnumerator ProcessInvite(string targetUserId)
    {
        var t1 = _occupant.Invite(targetUserId);
        yield return t1.WaitHandle;

        if (t1.Status == TaskStatus.RanToCompletion)
            AppendChatMessage(string.Format("***** Succeeded to invite {0}", targetUserId));
        else
            AppendChatMessage(string.Format("***** Failed to invite {0} ({1})", targetUserId, t1.Exception));
    }

    private IEnumerator ProcessWhisper(string targetUserId, string message)
    {
        var t1 = G.User.Whisper(targetUserId, message);
        yield return t1.WaitHandle;

        if (t1.Status == TaskStatus.RanToCompletion)
            AppendChatMessage(string.Format("***** Succeeded to whisper to {0}", targetUserId));
        else
            AppendChatMessage(string.Format("***** Failed to whisper {0} ({1})", targetUserId, t1.Exception));
    }

    public void AppendChatMessage(string text)
    {
        ChatText.text = ChatText.text + "\n" + text;
    }

    public void OnExitButtonClick()
    {
        if (ExitButtonClicked != null)
            ExitButtonClicked();
    }

    void IRoomObserver.Enter(string userId)
    {
        AppendChatMessage(string.Format("***** {0} entered", userId));
        _roomInfo.Users.Add(userId);
        DisplayRoomInfo();
    }

    void IRoomObserver.Exit(string userId)
    {
        AppendChatMessage(string.Format("***** {0} exited", userId));
        _roomInfo.Users.Remove(userId);
        DisplayRoomInfo();
    }

    void IRoomObserver.Say(ChatItem chatItem)
    {
        AppendChatMessage(string.Format(
            "<color=#000080ff><b>{0}</b></color>: {1}",
            chatItem.UserId, chatItem.Message));
    }
}
