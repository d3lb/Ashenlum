using System.Collections;
using TMPro;
using UnityEngine;

// onFinished fires after the box closes - that is the boss's and the shop's cue.
public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    // Static so gameplay scripts can gate on it with no reference and no null check.
    public static bool IsDialogueActive { get; private set; }

    [Header("UI")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TMP_Text   dialogueText;
    [SerializeField] private TMP_Text   nameText;

    [Header("Typewriter")]
    [SerializeField] private float typeSpeed = 0.03f;

    private string[]  sentences;
    private int       index;
    private bool      isTyping;
    private Coroutine typingRoutine;
    private Coroutine closeRoutine;
    private int       lastInputFrame = -1;
    private System.Action finished;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        IsDialogueActive = false;

        dialoguePanel.SetActive(false);
    }

    // A static "frozen" flag must never outlive this object, or the player is locked out.
    private void OnDisable()
    {
        if (Instance != this) return;

        closeRoutine  = null;
        typingRoutine = null;
        isTyping      = false;
        IsDialogueActive = false;
        finished = null;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        if (!IsDialogueActive) return;

        // Ignore the press that opened the box, forwarded by the NPC earlier this frame.
        if (lastInputFrame == Time.frameCount) return;
        if (closeRoutine != null) return;
        if (PauseManager.Instance != null && PauseManager.Instance.IsPaused) return;

        if (UIInput.AdvancePressed)
            HandleAdvance();
    }

    public void StartDialogue(Conversation conversation, System.Action onFinished = null)
    {
        if (conversation == null || conversation.sentences == null || conversation.sentences.Length == 0)
        {
            Debug.LogWarning("[DialogueManager] Empty Conversation asset.", this);
            onFinished?.Invoke();
            return;
        }
        if (IsDialogueActive || closeRoutine != null) return;

        finished = onFinished;

        sentences        = conversation.sentences;
        index            = 0;
        IsDialogueActive = true;
        lastInputFrame   = Time.frameCount;

        dialoguePanel.SetActive(true);

        bool hasName = !string.IsNullOrEmpty(conversation.speakerName);
        nameText.gameObject.SetActive(hasName);
        nameText.text = hasName ? conversation.speakerName : string.Empty;

        ShowSentence(sentences[index]);
    }

    private void HandleAdvance()
    {
        lastInputFrame = Time.frameCount;

        if (isTyping)                      CompleteSentence();
        else if (++index < sentences.Length) ShowSentence(sentences[index]);
        else                                 EndDialogue();
    }

    private void ShowSentence(string sentence)
    {
        if (typingRoutine != null) StopCoroutine(typingRoutine);
        typingRoutine = StartCoroutine(TypeRoutine(sentence));
    }

    private IEnumerator TypeRoutine(string sentence)
    {
        isTyping = true;

        // Set once and revealed by character: no per-character allocations, tags stay intact.
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
        dialogueText.maxVisibleCharacters = int.MaxValue;
        isTyping = false;
    }

    private void EndDialogue()
    {
        if (typingRoutine != null) StopCoroutine(typingRoutine);
        typingRoutine = null;
        isTyping      = false;
        sentences     = null;
        index         = 0;

        dialoguePanel.SetActive(false);

        closeRoutine = StartCoroutine(ReleaseActiveFlag());
    }

    // Hold the flag one extra frame so the closing keypress cannot reopen a conversation.
    private IEnumerator ReleaseActiveFlag()
    {
        yield return null;

        IsDialogueActive = false;
        closeRoutine     = null;

        System.Action callback = finished;
        finished = null;
        callback?.Invoke();
    }
}
