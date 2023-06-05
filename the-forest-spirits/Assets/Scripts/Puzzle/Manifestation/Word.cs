using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Word : AutoMonoBehaviour, IMouseEventReceiver
{
    public Bounds Bounds => _collider.bounds;

    public string word;


    //[SerializeField, ReadOnly, Required]
    public Collider2D _collider;

    private readonly List<Word> _overlapping = new();

    private void Awake() {
        _overlapping.Add(this);
    }


    private void OnCollisionEnter2D(Collision2D other) {
        Debug.Log("ow we collide");
        Debug.Log(other.gameObject.name);
        if (!other.collider.IsUnityNull() && other.collider.GetComponent<Word>() is var w && w != null) {
            _overlapping.Add(w);
            _overlapping.Sort((w1, w2) => {
                var diff = w1.Bounds.min.x - w2.Bounds.min.x;
                return diff switch {
                    < 0 => -1,
                    > 0 => 1,
                    _ => 0
                };
            });
            foreach (Word word in (_overlapping)) {
                Debug.Log(word.word);
            }
        }
    }
    private void OnTriggerEnter2D(Collider2D other) {
        Debug.Log("ow we trigger");
        Debug.Log(other.gameObject.name);
        if (!other.IsUnityNull() && other.gameObject.GetComponent<Word>() is var w && w != null) {
            _overlapping.Add(w);
            _overlapping.Sort((w1, w2) => {
                var diff = w1.Bounds.min.x - w2.Bounds.min.x;
                return diff switch {
                    < 0 => -1,
                    > 0 => 1,
                    _ => 0
                };
            });
            foreach (Word word in (_overlapping)) {
                Debug.Log(word.word);
            }
        }
    }

    private void OnCollisionExit2D(Collision2D other) {
        if (!other.collider.IsUnityNull() && other.collider.GetComponent<Word>() is var w && w != null) {
            _overlapping.Remove(w);
            Debug.Log(_overlapping);
        }
    }

    private void OnTriggerExit2D(Collider2D other) {
        if (!other.IsUnityNull() && other.gameObject.GetComponent<Word>() is var w && w != null) {
            _overlapping.Remove(w);
            Debug.Log(_overlapping);
        }
    }


    public string CurrentWord =>
        _overlapping.Aggregate("", (current, w) => current + w.word);

    public bool IsMouseInteractableAt(Vector2 screenPos, Camera cam, IMouseAttachable receiver) {
        return receiver is Stencil stencil && stencil.targetWord.ToLower() == CurrentWord.ToLower();
    }
}