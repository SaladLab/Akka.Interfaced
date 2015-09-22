using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class RoomDialog : UiDialog
{
    public class Argument
    {
        public string CurrentRoomName;
        public List<string> RoomList;
    }

    public InputField NameInput;
    public ScrollRect RoomList;
    public GameObject RoomListTemplate;

    private List<GameObject> _roomItems = new List<GameObject>();

    public override void OnShow(object param)
    {
        var arg = (Argument)param;

        // Show Name of Room

        NameInput.text = arg.CurrentRoomName ?? "";

        // Clear Room List

        foreach (var room in _roomItems)
            GameObject.Destroy(room);
        _roomItems.Clear();

        // Show Room List

        var y = 0f;
        foreach (var roomName in arg.RoomList)
        {
            var goItem = UiHelper.AddChild(RoomList.content.gameObject, RoomListTemplate);
            goItem.GetComponent<Text>().text = roomName;
            var rt = goItem.GetComponent<RectTransform>();

            var posy = rt.localPosition.y - y;
            rt.localPosition = new Vector3(rt.localPosition.x, posy, rt.localPosition.z);
            y += rt.rect.height;

            var localRoomName = roomName;
            var trigger = new EventTrigger.TriggerEvent();
            trigger.AddListener((PointerEventData) => OnRoomItemClick(localRoomName));
            var entry = new EventTrigger.Entry { callback = trigger, eventID = EventTriggerType.PointerClick };
            goItem.GetComponent<EventTrigger>().triggers.Add(entry);

            goItem.SetActive(true);
            _roomItems.Add(goItem);
        }

        RoomList.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, y);
        RoomListTemplate.SetActive(false);
    }

    public void OnEnterButtonClick()
    {
        Hide(NameInput.text);
    }

    private void OnRoomItemClick(string name)
    {
        NameInput.text = name;
    }
}
