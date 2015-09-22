using UnityEngine;
using SlimUnityChat.Interface;

class TestRoomObserver : IRoomObserver
{
    private string _name;

    public TestRoomObserver(string name)
    {
        _name = name;
    }

    void IRoomObserver.Enter(string userId)
    {
        Debug.LogFormat("IRoomObserver({0}).Enter({1})", _name, userId);
    }

    void IRoomObserver.Exit(string userId)
    {
        Debug.LogFormat("IRoomObserver({0}).Exit({1})", _name, userId);
    }

    void IRoomObserver.Say(ChatItem chatItem)
    {
        Debug.LogFormat("IRoomObserver({0}).Say({1}, {2}, {3})", _name, chatItem.UserId, chatItem.Time, chatItem.Message);
    }
}
