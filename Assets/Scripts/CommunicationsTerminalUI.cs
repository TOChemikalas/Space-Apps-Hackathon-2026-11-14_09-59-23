using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using static GreenhouseCanvas;

// Presentation only. The existing Operations menu retains modal/input ownership.
internal sealed class CommunicationsTerminalUI : IDisposable
{
    readonly CommunicationsSession session;
    readonly Sprite onlineArtwork, offlineArtwork;
    readonly RectTransform root, transmission, inbox, listContent, bodyContent;
    readonly List<GameObject> rows = new List<GameObject>();
    readonly Image inboxBackdrop;
    readonly Text transmissionLink, inboxLink, linkReading, latency, lastMessage, story, signal, channel;
    readonly Text body, feedback;
    readonly InputField input;
    string selectedId;
    public bool IsOpen => root.gameObject.activeSelf;

    internal CommunicationsTerminalUI(Transform parent, CommunicationsSession session, Sprite transmissionArtwork,
        Sprite onlineArtwork, Sprite offlineArtwork, Action back)
    {
        this.session = session;
        this.onlineArtwork = onlineArtwork;
        this.offlineArtwork = offlineArtwork;
        root = NativeLayout(parent, "Communications terminals");
        transmission = Create(root, "Transmission channel", new Rect(0, 0, 1672, 941));
        Image(transmission, "Supplied transmission artwork", new Rect(0, 0, 1672, 941), Color.white).sprite = transmissionArtwork;
        transmissionLink = Reading(transmission, "Physical link", new Rect(1385, 47, 240, 40), 27);
        signal = Reading(transmission, "Measured signal", new Rect(90, 525, 425, 39), 25);
        Set(Reading(transmission, "Data rate unavailable", new Rect(625, 523, 235, 40), 26), "—");
        Set(Reading(transmission, "Latency unavailable", new Rect(955, 523, 280, 40), 26), "—");
        channel = Reading(transmission, "Narrative channel status", new Rect(1360, 523, 250, 42), 23);
        var inputSurface = Image(transmission, "Message entry", new Rect(64, 634, 1544, 165), Background);
        inputSurface.raycastTarget = true;
        input = inputSurface.gameObject.AddComponent<InputField>();
        input.targetGraphic = inputSurface;
        input.textComponent = Label(inputSurface.transform, "Player message", new Rect(10, 8, 1524, 149), "", 26, TextAnchor.UpperLeft);
        input.textComponent.supportRichText = false;
        input.textComponent.gameObject.AddComponent<RectMask2D>();
        input.placeholder = Label(inputSurface.transform, "Placeholder", new Rect(10, 8, 1524, 149), "Enter message…", 26, TextAnchor.UpperLeft);
        input.lineType = InputField.LineType.MultiLineSubmit;
        input.characterLimit = 4000;
        input.onEndEdit.AddListener(_ =>
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame))
                Submit();
        });
        feedback = Label(transmission, "Transmission feedback", new Rect(260, 861, 1090, 63), "", 24);
        LabeledButton(transmission, "Back", new Rect(25, 864, 190, 56), () => back());

        inbox = Create(root, "Incoming messages", new Rect(0, 0, 1672, 941));
        inboxBackdrop = Image(inbox, "Supplied empty inbox artwork", new Rect(0, 0, 1672, 941), Color.white);
        inboxLink = Reading(inbox, "Physical link", new Rect(1385, 48, 240, 40), 27);
        linkReading = Reading(inbox, "Dish link status", new Rect(205, 444, 86, 41), 23);
        latency = Reading(inbox, "Latency unavailable", new Rect(155, 503, 135, 36), 22);
        lastMessage = Reading(inbox, "Last received Sol", new Rect(180, 556, 110, 36), 20);
        listContent = Scroll(inbox, "Received message list", new Rect(340, 214, 266, 603));
        bodyContent = Scroll(inbox, "Read-only message body", new Rect(670, 216, 950, 600));
        body = Label(bodyContent, "Selected transmission", new Rect(10, 8, 925, 580), "", 26, TextAnchor.UpperLeft);
        body.supportRichText = false;
        story = Reading(inbox, "Connection and story status", new Rect(470, 886, 1000, 38), 21);
        LabeledButton(inbox, "Back", new Rect(25, 873, 190, 51), () => back());
        session.Changed += Refresh;
        Hide();
    }

    internal static RectTransform NativeLayout(Transform parent, string name)
    {
        var layout = Create(parent, name, new Rect(0, 0, 1672, 941));
        layout.anchorMin = layout.anchorMax = layout.pivot = new Vector2(.5f, .5f);
        layout.anchoredPosition = Vector2.zero;
        float scale = Mathf.Min(2752f / 1672, 1536f / 941);
        layout.localScale = new Vector3(scale, scale, 1);
        return layout;
    }

    public void Show(bool incoming)
    {
        root.gameObject.SetActive(true);
        transmission.gameObject.SetActive(!incoming);
        inbox.gameObject.SetActive(incoming);
        Refresh();
    }
    public void Hide() => root.gameObject.SetActive(false);

    void Submit()
    {
        if (!IsOpen || !transmission.gameObject.activeSelf || string.IsNullOrWhiteSpace(input.text)) return;
        if (!session.Transmit(input.text)) { Set(feedback, "Transmission unavailable."); return; }
        input.text = "";
        Set(feedback, "Transmission sent.");
        input.ActivateInputField();
    }

    void Refresh()
    {
        if (!IsOpen) return;
        var model = session.Model;
        string physical = model.Online ? "LINK ACTIVE" : "LINK OFFLINE";
        Set(transmissionLink, physical);
        Set(inboxLink, physical);
        transmissionLink.color = inboxLink.color = model.Online ? Green : Orange;
        Set(linkReading, model.Online ? "Online" : "Offline");
        linkReading.color = model.Online ? Green : Orange;
        Set(latency, "—"); // No measured latency or data-rate source exists in this project.
        bool terminated = model.State.narrative == CommunicationNarrative.CommunicationTerminated;
        Set(channel, terminated ? "CONTACT ENDED" : model.Online ? "Stable" : "Offline");
        Set(story, terminated ? "EARTH CONTROL HAS ENDED CONTACT — dish hardware status is unchanged" :
            model.Online ? "ONLINE — DISH ALIGNED AND CONNECTED TO EARTH RELAY" : "OFFLINE — pending messages await the satellite link");
        Set(signal, session.SignalPercent.ToString("0.0") + "% signal strength");
        inboxBackdrop.sprite = !model.Online && offlineArtwork ? offlineArtwork : onlineArtwork;
        if (!inbox.gameObject.activeSelf) return;
        foreach (var row in rows) { row.SetActive(false); UnityEngine.Object.Destroy(row); }
        rows.Clear();
        TransmissionRecord selected = null, latest = null;
        float y = 0;
        foreach (var record in model.State.messages)
        {
            if (record.status != DeliveryStatus.Received) continue;
            if (latest == null || record.receivedAt > latest.receivedAt) latest = record;
            if (record.id == selectedId) selected = record;
            string id = record.id;
            var row = Button(listContent, id, new Rect(0, y, 266, 150), record.read ? Panel : Background, () => Select(id));
            var label = Label(row.transform, "Message summary", new Rect(10, 6, 246, 138),
                (record.read ? "READ" : "NEW") + " · " + Timestamp(record.receivedAt) + "\n" + record.sender + "\n" + record.subject, 20, TextAnchor.UpperLeft);
            label.supportRichText = false;
            rows.Add(row.gameObject);
            y += 158;
        }
        listContent.sizeDelta = new Vector2(266, Mathf.Max(603, y));
        Set(lastMessage, latest == null ? "—" : "Sol " + (1 + (int)(latest.receivedAt / MarsClock.SecondsPerSol)));
        string text = selected == null ? "" : selected.sender + " — " + selected.subject + "\n" + Timestamp(selected.receivedAt) + "\n\n" + selected.body;
        Set(body, text);
        float height = Mathf.Max(580, body.preferredHeight + 20);
        body.rectTransform.sizeDelta = new Vector2(925, height);
        bodyContent.sizeDelta = new Vector2(950, Mathf.Max(600, height + 16));
    }

    void Select(string id)
    {
        selectedId = id;
        bodyContent.anchoredPosition = Vector2.zero;
        session.Model.MarkRead(id);
        Refresh();
    }

    static string Timestamp(double seconds)
    {
        double hour = seconds / MarsClock.SecondsPerSol % 1 * 24;
        return $"Sol {1 + (int)(seconds / MarsClock.SecondsPerSol)} {Math.Floor(hour):00}:{Math.Floor(hour % 1 * 60):00}";
    }

    static RectTransform Scroll(Transform parent, string name, Rect rect)
    {
        var viewport = Image(parent, name, rect, new Color(.02f, .06f, .075f, .94f));
        viewport.raycastTarget = true;
        viewport.gameObject.AddComponent<RectMask2D>();
        var content = Create(viewport.transform, name + " content", new Rect(0, 0, rect.width, rect.height));
        var scroll = viewport.gameObject.AddComponent<ScrollRect>();
        scroll.viewport = viewport.rectTransform;
        scroll.content = content;
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 45;
        return content;
    }

    internal static Text Reading(Transform parent, string name, Rect rect, int size)
    {
        var mask = Image(parent, name, rect, Background);
        return Label(mask.transform, name, new Rect(3, 0, rect.width - 6, rect.height), "", size, TextAnchor.MiddleRight);
    }

    static Button LabeledButton(Transform parent, string name, Rect rect, UnityEngine.Events.UnityAction action)
    {
        var button = Button(parent, name, rect, Panel, action);
        Label(button.transform, name, new Rect(0, 0, rect.width, rect.height), name, 27, TextAnchor.MiddleCenter);
        return button;
    }
    static void Set(Text label, string value) { if (label.text != value) label.text = value; }
    public void Dispose() => session.Changed -= Refresh;
}
