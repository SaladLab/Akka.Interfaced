using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ControlPanel : MonoBehaviour
{
    public Text UserNameText;
    public GameObject Rooms;
    public GameObject RoomItemTemplate;

    public Action LogoutButtonClicked;
    public Action RoomButtonClicked;
    public Action<string> RoomItemSelected;

    private List<Tuple<string, GameObject>> _roomItems = new List<Tuple<string, GameObject>>();
    private readonly int RoomItemHeight = 25;

    void Start()
    {
        UserNameText.text = "";
        RoomItemTemplate.SetActive(false);

        /*
        AddRoomItem("#general");
        AddRoomItem("#chat");
        AddRoomItem("#test");
        AddRoomItem("#test2");
        DeleteRoomItem("#chat");
        SelectRoomItem("#test");
        */
    }

    public void SetUserName(string name)
    {
        UserNameText.text = name;
    }

    public void AddRoomItem(string roomName)
    {
        Debug.Log(roomName);

        // create game object

        var go = UiHelper.AddChild(Rooms, RoomItemTemplate);
        var image = go.GetComponent<Image>();
        var nameText = go.transform.FindChild("NameText").GetComponent<Text>();
        image.enabled = false;
        nameText.text = roomName;

        // position

        var rt = go.GetComponent<RectTransform>();
        var posy = rt.localPosition.y - _roomItems.Count * RoomItemHeight;
        rt.localPosition = new Vector3(rt.localPosition.x, posy, rt.localPosition.z);

        // bind click event

        var trigger = new EventTrigger.TriggerEvent();
        trigger.AddListener((PointerEventData) => OnRoomItemClick(roomName));
        var entry = new EventTrigger.Entry { callback = trigger, eventID = EventTriggerType.PointerClick };
        go.GetComponent<EventTrigger>().triggers.Add(entry);

        go.SetActive(true);
        _roomItems.Add(Tuple.Create(roomName, go));
    }

    public void DeleteRoomItem(string roomName)
    {
        var idx = _roomItems.FindIndex(x => x.Item1 == roomName);
        if (idx == -1)
            return;

        // Remove specified room item

        GameObject.Destroy(_roomItems[idx].Item2);
        _roomItems.RemoveAt(idx);

        // Life up all room slots after removed one

        for (int i = idx; i<_roomItems.Count; i++)
        {
            var go = _roomItems[i].Item2;
            var rt = go.GetComponent<RectTransform>();
            var posy = rt.localPosition.y + RoomItemHeight;
            rt.localPosition = new Vector3(rt.localPosition.x, posy, rt.localPosition.z);
        }
    }

    public void SelectRoomItem(string roomName)
    {
        for (int i = 0; i < _roomItems.Count; i++)
        {
            var go = _roomItems[i].Item2;
            var image = go.GetComponent<Image>();
            image.enabled = (_roomItems[i].Item1 == roomName);
        }
    }

    public void OnLogoutButtonClick()
    {
        if (LogoutButtonClicked != null)
            LogoutButtonClicked();
    }

    public void OnRoomButtonClick()
    {
        if (RoomButtonClicked != null)
            RoomButtonClicked();
    }

    public void OnRoomItemClick(string roomName)
    {
        if (RoomItemSelected != null)
            RoomItemSelected(roomName);
    }
}
