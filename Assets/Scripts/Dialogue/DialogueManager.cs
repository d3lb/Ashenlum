using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Central, scene-level owner of the dialogue box. NPCs never touch the UI
// directly — they just hand it a Dialogue asset via StartDialogue(). One key
// (E) drives everything: the first press finishes the current line instantly,
// the next press advances; when the last line is dismissed the box closes.
public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    // Static so gameplay scripts can gate themselves with a bare
    // `if (!DialogueManager.IsDialogueActive)` — no reference, no null check.
    // Stays true for one extra frame after closing (see ReleaseActiveFlag).
    public static bool IsDialogueActive { get; private set; }

    [Header("UI References (leave empty to auto-build a basic box at runtime)")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TMP_Text   dialogueText;
    [SerializeField] private TMP_Text   nameText;

    [Header("Typewriter")]
    [Tooltip("Seconds between each character.")]
    [SerializeField] private float typeSpeed = 0.03f;

    [Header("Setup")]
    [Tooltip("If the panel/text references are missing, build a simple dialogue box at runtime so nothing breaks.")]
    [SerializeField] private bool autoBuildUIIfMissing = true;

    // Fire-and-forget hooks so other systems can react without coupling.
    public System.Action onDialogueStart;
    public System.Action onDialogueEnd;

    private string[]  sentences;
    private int       index;
    private bool      isTyping;
    private Coroutine typingRoutine;
    private Coroutine closeRoutine;
    private int       lastInputFrame = -1;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        IsDialogueActive = false;   // static — never let a stale value leak across scenes

        if ((dialoguePanel == null || dialogueText == null) && autoBuildUIIfMissing)
            BuildRuntimeUI();

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
    }

    // Safety net: a static "frozen" flag must never survive this object going
    // away mid-conversation, or the player would be locked out permanently.
    private void OnDisable()
    {
        if (Instance != this) return;

        closeRoutine  = null;
        typingRoutine = null;
        isTyping      = false;

        if (IsDialogueActive)
        {
            IsDialogueActive = false;
            onDialogueEnd?.Invoke();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        if (!IsDialogueActive) return;

        // Ignore a key press already consumed this frame (e.g. the very press
        // that opened the box, forwarded by the NPC earlier in the same frame).
        if (lastInputFrame == Time.frameCount) return;

        // Also ignore input while closing, and while the game is paused.
        if (closeRoutine != null) return;
        if (PauseManager.Instance != null && PauseManager.Instance.IsPaused) return;

        if (Input.GetKeyDown(KeyCode.E))
            HandleAdvance();
    }

    // ── Public API ────────────────────────────────────────────────────────────
    public void StartDialogue(Dialogue dialogue)
    {
        if (dialogue == null || dialogue.sentences == null || dialogue.sentences.Length == 0)
        {
            Debug.LogWarning("[DialogueManager] StartDialogue called with an empty Dialogue asset.", this);
            return;
        }
        if (dialogueText == null)
        {
            Debug.LogError("[DialogueManager] No dialogue Text assigned and auto-build is off.", this);
            return;
        }
        if (IsDialogueActive || closeRoutine != null) return;   // busy or still closing

        sentences        = dialogue.sentences;
        index            = 0;
        IsDialogueActive = true;
        lastInputFrame   = Time.frameCount;

        if (dialoguePanel != null) dialoguePanel.SetActive(true);

        if (nameText != null)
        {
            bool hasName = !string.IsNullOrEmpty(dialogue.speakerName);
            nameText.gameObject.SetActive(hasName);
            nameText.text = hasName ? dialogue.speakerName : string.Empty;
        }

        onDialogueStart?.Invoke();
        ShowSentence(sentences[index]);
    }

    // Lets other systems bail out of a conversation early (menus, scene changes…).
    public void CloseDialogue()
    {
        if (IsDialogueActive && closeRoutine == null) EndDialogue();
    }

    // ── Flow ──────────────────────────────────────────────────────────────────
    private void HandleAdvance()
    {
        lastInputFrame = Time.frameCount;

        if (isTyping)
        {
            CompleteSentence();               // first press → snap the line to full
        }
        else if (++index < sentences.Length)
        {
            ShowSentence(sentences[index]);   // next press → next line
        }
        else
        {
            EndDialogue();                    // past the last line → close
        }
    }

    private void ShowSentence(string sentence)
    {
        if (typingRoutine != null) StopCoroutine(typingRoutine);
        typingRoutine = StartCoroutine(TypeRoutine(sentence));
    }

    private IEnumerator TypeRoutine(string sentence)
    {
        isTyping = true;

        // Set the whole string once, then reveal it character-by-character. This
        // avoids per-character string allocations and plays nicely with rich text.
        dialogueText.text = sentence;
        dialogueText.ForceMeshUpdate();
        int total = dialogueText.textInfo.characterCount;
        dialogueText.maxVisibleCharacters = 0;

        var wait = new WaitForSeconds(typeSpeed);
        for (int shown = 1; shown <= total; shown++)
        {
            dialogueText.maxVisibleCharacters = shown;
            yield return wait;
        }

        isTyping      = false;
        typingRoutine = null;
    }

    private void CompleteSentence()
    {
        if (typingRoutine != null) StopCoroutine(typingRoutine);
        typingRoutine = null;
        dialogueText.maxVisibleCharacters = int.MaxValue;   // text already set → reveal all
        isTyping = false;
    }

    private void EndDialogue()
    {
        if (typingRoutine != null) StopCoroutine(typingRoutine);
        typingRoutine = null;
        isTyping      = false;
        sentences     = null;
        index         = 0;

        if (dialoguePanel != null) dialoguePanel.SetActive(false);   // hide the UI immediately

        // …but hold IsDialogueActive true for one more frame. PlayerInput latches
        // InteractPressed once per Update, so depending on script execution order
        // an NPC can still read the *same* press next frame — this outlasts it, so
        // only a genuinely fresh E press can start a new conversation.
        closeRoutine = StartCoroutine(ReleaseActiveFlag());
    }

    private IEnumerator ReleaseActiveFlag()
    {
        yield return null;

        IsDialogueActive = false;
        closeRoutine     = null;
        onDialogueEnd?.Invoke();
    }

    // ── Runtime UI fallback ─────────────────────────────────────────────────────
    // Builds a minimal, unobtrusive bottom bar if the references were left empty.
    // The recommended path is still to wire your own styled panel in the Inspector.
    private void BuildRuntimeUI()
    {
        var canvasGO = new GameObject("DialogueCanvas");
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;                       // above gameplay HUD
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        var panelGO = new GameObject("DialoguePanel");
        panelGO.transform.SetParent(canvasGO.transform, false);
        var panelImg = panelGO.AddComponent<Image>();
        panelImg.color = new Color(0.03f, 0.04f, 0.06f, 0.88f);
        var panelRT = panelGO.GetComponent<RectTransform>();
        panelRT.anchorMin        = new Vector2(0.5f, 0f);
        panelRT.anchorMax        = new Vector2(0.5f, 0f);
        panelRT.pivot            = new Vector2(0.5f, 0f);
        panelRT.sizeDelta        = new Vector2(1400, 220);
        panelRT.anchoredPosition = new Vector2(0, 60);

        var nameGO = new GameObject("NameText");
        nameGO.transform.SetParent(panelGO.transform, false);
        var speaker = nameGO.AddComponent<TextMeshProUGUI>();
        speaker.fontSize  = 34;
        speaker.fontStyle = FontStyles.Bold;
        speaker.color     = new Color(0.85f, 0.82f, 0.70f);
        var nameRT = nameGO.GetComponent<RectTransform>();
        nameRT.anchorMin        = new Vector2(0, 1);
        nameRT.anchorMax        = new Vector2(1, 1);
        nameRT.pivot            = new Vector2(0, 1);
        nameRT.sizeDelta        = new Vector2(-80, 44);
        nameRT.anchoredPosition = new Vector2(40, -16);
        nameText = speaker;

        var textGO = new GameObject("DialogueText");
        textGO.transform.SetParent(panelGO.transform, false);
        var body = textGO.AddComponent<TextMeshProUGUI>();
        body.fontSize  = 30;
        body.color     = new Color(0.92f, 0.92f, 0.95f);
        body.alignment = TextAlignmentOptions.TopLeft;
        var textRT = textGO.GetComponent<RectTransform>();
        textRT.anchorMin = new Vector2(0, 0);
        textRT.anchorMax = new Vector2(1, 1);
        textRT.pivot     = new Vector2(0.5f, 0.5f);
        textRT.offsetMin = new Vector2(40, 30);      // left / bottom padding
        textRT.offsetMax = new Vector2(-40, -70);    // right / top padding (room for the name)
        dialogueText = body;

        dialoguePanel = panelGO;
    }
}
