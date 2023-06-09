using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

/**
 * Represents a GameObject that can have dialogue.
 * References conversations and branches written in the editor.
 */
public class Talker : AutoMonoBehaviour, IMouseEventReceiver
{
    #region Unity fields

    [Tooltip("Where the text will show up")]
    [AutoDefaultInChildren, Required]
    public TextMeshPro textRef;

    [Tooltip("Where new conversations will start")]
    public Branch startBranch;

    [Header("Events")]
    public UnityEvent onStart;

    public UnityEvent onEnd;
    public UnityEvent onNext;
    private bool fading;

    #endregion

    #region Private variables

    [SerializeField, ReadOnly]
    private int _index = 0;

    [SerializeField, ReadOnly]
    private bool _talking = false;

    [SerializeField, ReadOnly]
    private bool _stopped = false;

    [SerializeField, ReadOnly]
    private Conversation _currentConversation;

    #endregion

    #region Public methods

    /** Call this method when the player interacts with the talking object */
    public void OnInteract() {
        if (_stopped) return;
        Next();
    }

    /**
     * Starts a new conversation, or moves to the next line of dialogue.
     * Ignores being "stopped" by a ConversationPart marked as "stop until forced"
     */
    public void Next() {
        _stopped = false;
        if (!_talking) {
            StartConversation();
        }
        else {
            //Only run this if the conversation is not CURRENTLY fading out!
            if (!fading) {
                if (_index >= 0) {
                    //if there's an onEnd method, invoke it now -- this will usually be the call to fadeout
                    _currentConversation.dialogue[_index].onEnd.Invoke();
                }

                fading = true;
                //wait for the amount of time specified in waitTime, then call next -- waittime in this case
                //is the time you need for the text to fade OUT
                this.WaitThen(_currentConversation.dialogue[_index].waitTime, () => {
                    NextInternal();
                    fading = false;
                });
            }
        }
    }

    public void StopConversation() {
        if (_talking) {
            _currentConversation.dialogue[_index].onEnd.Invoke();
        }
        Teardown();
    }

    public void GoTo(Branch branch) {
        if (_talking) {
            NextInternal(branch);
        }
        else {
            StartConversationInternal(branch);
        }
    }

    /** Starts a conversation from scratch */
    public void StartConversation() {
        StartConversationInternal(startBranch);
    }

    #endregion

    #region Private helper functions

    private void StartConversationInternal(Branch toBranch) {
        if (_talking) Teardown();
        if (toBranch == null) return;

        _currentConversation = toBranch.GetConversation();
        if (_currentConversation == null) return;
        _currentConversation.onStart.Invoke();

        _talking = true;
        _index = -1;
        Setup();
        NextInternal();
    }

    private void Setup() {
        onStart.Invoke();
    }

    private void Teardown() {
        onEnd.Invoke();
        _talking = false;
        _index = 0;
        _currentConversation = null;
        textRef.text = "";
    }

    private void NextInternal(Branch overrideBranch = null) {
        _index++;

        // If there's still text to go...
        if ((_index < _currentConversation.dialogue.Length) && overrideBranch == null) {
            var next = _currentConversation.dialogue[_index];
            onNext.Invoke();

            next.onStart.Invoke();

            textRef.text = next.text;
            _stopped = next.stopUntilForced;
            foreach (var linkResponder in next.linkResponders.Where(lr => lr.thenContinue)) {
                UnityAction<string> action = null;
                action = _ => {
                    Next();
                    linkResponder.onLink.RemoveListener(action);
                };

                linkResponder.onLink.AddListener(action);
            }

            return;
        }

        // Otherwise, try to move to the next Branch (or finish)
        _currentConversation.onEnd.Invoke();

        Branch nextBranch = overrideBranch;

        if (nextBranch == null) {
            nextBranch = _currentConversation.andThen;
        }

        if (nextBranch == null) {
            // If there's nowhere to go, stop.
            Teardown();
            return;
        }

        _currentConversation = nextBranch.GetConversation();

        // If the next branch leads nowhere, stop.
        if (_currentConversation == null) {
            Teardown();
            return;
        }

        // Otherwise continue to the beginning of the next conversation.
        _index = -1;
        _currentConversation.onStart.Invoke();
        NextInternal();
    }

    #endregion

    #region Mouse Events

    /** Triggers links when clicked */
    public void OnPointerUp(Vector2 screenPos, Camera cam) {
        if (!_talking || _currentConversation == null) {
            return ;
        }

        int linkIndex = TMP_TextUtilities.FindIntersectingLink(textRef, screenPos, cam);
        if (linkIndex == -1) {
            return ;
        }

        var info = textRef.textInfo.linkInfo[linkIndex];
        string linkId = info.GetLinkID();

        _currentConversation.dialogue[_index].TriggerLink(linkId);

    }

    public float GetScreenOrdering() {
        return transform.position.z;
    }

    /** Shows the "clickable" cursor only if we're hovering over a link */
    public bool IsMouseInteractableAt(Vector2 screenPos, Camera cam, IMouseAttachable receiver) {
        if (!_talking || _currentConversation == null || receiver != null) {
            return false;
        }

        int linkIndex = TMP_TextUtilities.FindIntersectingLink(textRef, screenPos, cam);
        
        return linkIndex != -1;
    }

    #endregion
}