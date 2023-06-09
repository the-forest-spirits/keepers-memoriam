using System;
using UnityEngine;
using UnityEngine.Events;

/**
 * Defines an object that can be collected.
 * It should be given a name in the Unity editor.
 */
public class Collectable : AutoMonoBehaviour
{
    /** true if this Collectable is attached to an item */
    public bool CollectsItem => itemId != "";

    /** the item that this Collectable is attached to */
    public InventoryItem Item => InventoryItem.GetItemById(itemId);

    [Tooltip("True if this Collectable can currently be collected.")]
    public bool canBeCollected = true;

    [Tooltip("Optional; if specified, the item that collecting this unlocks.")]
    public string itemId;

    public AudioClip onCollectSound;

    [Tooltip("Triggered when this Collectable is collected")]
    public UnityEvent onCollect;

    /** Call this when the collectable is touched by lil' guy */
    public void Touch() {
        if (!canBeCollected) return;
        Collect();
    }

    /** Always collects this item, even if it's set to not be collectable */
    public void Collect() {
        var defaultCollectSound = SceneInfo.Instance.defaultOnCollectCollectable;
        if (onCollectSound != null) {
            Lil.Music.PlaySFX(onCollectSound);
        } else if (defaultCollectSound != null) {
            Lil.Music.PlaySFX(defaultCollectSound);
        }
        
        onCollect.Invoke();
        Lil.Guy.onCollect.Invoke(this);
        if (CollectsItem) {
            Item.Unlock();
        }
        Destroy(gameObject);
    }

    public void SetCollectable(bool flag) {
        canBeCollected = flag;
    }
}