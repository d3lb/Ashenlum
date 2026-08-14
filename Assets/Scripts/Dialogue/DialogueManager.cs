using System.Collections;
using TMPro;
using UnityEngine;

// Owns the dialogue box. NPCs hand it a Dialogue asset and never touch the UI.
// E finishes the current line, then advances, then closes.
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

        if (Input.GetKeyDown(KeyCode.E))
            HandleAdvance();
    }

    public void StartDialogue(Dialogue dialogue)
    {
        if (dialogue == null || dialogue.sentences == null || dialogue.sentences.Length == 0)
        {
            Debug.LogWarning("[DialogueManager] Empty Dialogue asset.", this);
            return;
        }
        if (IsDialogueActive || closeRoutine != null) return;

        sentences        = dialogue.sentences;
        index            = 0;
        IsDialogueActive = true;
        lastInputFrame   = Time.frameCount;

        dialoguePanel.SetActive(true);

        bool hasName = !string.IsNullOrEmpty(dialogue.speakerName);
        nameText.gameObject.SetActive(hasName);
        nameText.text = hasName ? dialogue.speakerName : string.Empty;

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

        // Set the whole string once and reveal it character by character - no per-character
        // allocations, and rich text tags stay intact.
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
    }
}
