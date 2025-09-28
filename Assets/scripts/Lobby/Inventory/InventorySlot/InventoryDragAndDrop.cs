using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventoryDragAndDrop : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Drag Settings")]
    [SerializeField] private Canvas dragCanvas;
    [SerializeField] private float dragAlpha = 0.8f;

    private InventorySlot originalSlot;
    private Image dragImage;
    private RectTransform dragTransform;
    private CanvasGroup canvasGroup;
    private bool isDragging = false;

    private void Awake()
    {
        originalSlot = GetComponent<InventorySlot>();
        InitializeDragComponents();
    }

    private void InitializeDragComponents()
    {
        // Get or add required components
        if (dragCanvas == null)
            dragCanvas = GetComponentInParent<Canvas>().rootCanvas;

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Only allow dragging if slot has an item
        if (originalSlot.IsEmpty)
        {
            eventData.pointerDrag = null;
            return;
        }

        StartDrag();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;

        // Update drag position
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
            dragTransform, eventData.position, eventData.pressEventCamera, out Vector3 worldPoint))
        {
            dragTransform.position = worldPoint;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging) return;

        EndDrag();

        // Find the slot we're dropping on
        GameObject dropTarget = eventData.pointerCurrentRaycast.gameObject;
        if (dropTarget != null)
        {
            HandleDrop(dropTarget);
        }
        else
        {
            // Dropped outside any slot - return to original position
            ResetDrag();
        }
    }

    private void StartDrag()
    {
        isDragging = true;

        // Create drag visual
        CreateDragVisual();

        // Make original slot semi-transparent
        canvasGroup.alpha = 0.5f;
        canvasGroup.blocksRaycasts = false;
    }

    private void CreateDragVisual()
    {
        // Create new GameObject for drag visual
        GameObject dragObject = new GameObject("DragVisual");
        dragObject.transform.SetParent(dragCanvas.transform, false);
        dragObject.transform.SetAsLastSibling();

        // Setup RectTransform
        dragTransform = dragObject.AddComponent<RectTransform>();
        dragTransform.sizeDelta = GetComponent<RectTransform>().sizeDelta;

        // Setup Image
        dragImage = dragObject.AddComponent<Image>();
        dragImage.sprite = originalSlot.CurrentItem.Icon;
        dragImage.preserveAspect = true;

        // Setup CanvasGroup for transparency
        CanvasGroup dragCanvasGroup = dragObject.AddComponent<CanvasGroup>();
        dragCanvasGroup.alpha = dragAlpha;
        dragCanvasGroup.blocksRaycasts = false;

        // Set initial position
        dragTransform.position = transform.position;
    }

    private void EndDrag()
    {
        isDragging = false;

        // Restore original slot appearance
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
    }

    private void HandleDrop(GameObject dropTarget)
    {
        InventorySlot targetSlot = FindSlotFromGameObject(dropTarget);

        if (targetSlot != null && targetSlot != originalSlot)
        {
            HandleSlotDrop(targetSlot);
        }
        else
        {
            ResetDrag();
        }
    }

    private InventorySlot FindSlotFromGameObject(GameObject target)
    {
        // Check if the target is a slot or a child of a slot
        InventorySlot slot = target.GetComponent<InventorySlot>();
        if (slot != null) return slot;

        // Check parent if target is a child of slot
        return target.GetComponentInParent<InventorySlot>();
    }

    private void HandleSlotDrop(InventorySlot targetSlot)
    {
        Item draggedItem = originalSlot.CurrentItem;
        int draggedCount = originalSlot.StackCount;

        // Case 1: Target slot is empty - move entire stack
        if (targetSlot.IsEmpty)
        {
            MoveItemToEmptySlot(targetSlot);
        }
        // Case 2: Same stackable item - try to merge
        else if (CanMergeStacks(originalSlot, targetSlot))
        {
            MergeStacks(targetSlot);
        }
        // Case 3: Different items or non-stackable - swap
        else if (CanSwapItems(originalSlot, targetSlot))
        {
            SwapItems(targetSlot);
        }
        else
        {
            ResetDrag();
        }
    }

    private bool CanMergeStacks(InventorySlot fromSlot, InventorySlot toSlot)
    {
        return fromSlot.CurrentItem == toSlot.CurrentItem &&
               fromSlot.CurrentItem.IsStackable &&
               !toSlot.IsFull;
    }

    private bool CanSwapItems(InventorySlot fromSlot, InventorySlot toSlot)
    {
        // Allow swapping if both slots have items
        return !fromSlot.IsEmpty && !toSlot.IsEmpty;
    }

    private void MoveItemToEmptySlot(InventorySlot targetSlot)
    {
        Item item = originalSlot.CurrentItem;
        int count = originalSlot.StackCount;

        originalSlot.RemoveItem();
        targetSlot.TryAddItem(item, count);

        CleanupDragVisual();
    }

    private void MergeStacks(InventorySlot targetSlot)
    {
        int amountToTransfer = Mathf.Min(
            originalSlot.StackCount,
            targetSlot.AvailableSpace
        );

        if (amountToTransfer > 0)
        {
            // Add to target slot
            targetSlot.TryIncreaseStack(amountToTransfer);

            // Remove from original slot
            if (amountToTransfer == originalSlot.StackCount)
            {
                originalSlot.RemoveItem();
            }
            else
            {
                originalSlot.DecreaseStack(amountToTransfer);
            }
        }

        CleanupDragVisual();
    }

    private void SwapItems(InventorySlot targetSlot)
    {
        Item tempItem = targetSlot.CurrentItem;
        int tempCount = targetSlot.StackCount;

        // Remove from both slots first
        targetSlot.RemoveItem();
        Item originalItem = originalSlot.CurrentItem;
        int originalCount = originalSlot.StackCount;
        originalSlot.RemoveItem();

        // Add to new positions
        if (tempItem != null)
            originalSlot.TryAddItem(tempItem, tempCount);

        if (originalItem != null)
            targetSlot.TryAddItem(originalItem, originalCount);

        CleanupDragVisual();
    }

    private void ResetDrag()
    {
        // Just cleanup the drag visual, item stays in original slot
        CleanupDragVisual();
    }

    private void CleanupDragVisual()
    {
        if (dragImage != null && dragImage.gameObject != null)
        {
            Destroy(dragImage.gameObject);
        }
        dragImage = null;
        dragTransform = null;
    }

    // Cleanup when destroyed
    private void OnDestroy()
    {
        CleanupDragVisual();
    }
}