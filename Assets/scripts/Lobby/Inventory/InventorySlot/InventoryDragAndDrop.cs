using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventoryDragAndDrop : MonoBehaviour, IPointerClickHandler, IDropHandler, IBeginDragHandler
{
    [Header("Drag Settings")]
    [SerializeField] private Canvas dragCanvas;
    [SerializeField] private float dragAlpha = 0.8f;
    [SerializeField] private float dragThreshold = 10f; // Minimum distance to consider it a drag

    private InventorySlot originalSlot;
    private Image dragImage;
    private RectTransform dragTransform;
    private CanvasGroup canvasGroup;
    private bool isDragging = false;
    private bool isSplitting = false;
    private Vector2 dragStartPosition;

    // Static reference to track the currently dragged item
    private static InventoryDragAndDrop currentlyDraggedItem;

    private void Awake()
    {
        originalSlot = GetComponent<InventorySlot>();
        InitializeDragComponents();
    }

    private void InitializeDragComponents()
    {
        if (dragCanvas == null)
            dragCanvas = GetComponentInParent<Canvas>().rootCanvas;

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Only handle clicks if we're not dragging
        if (isDragging) return;

        if (eventData.button == PointerEventData.InputButton.Left)
        {
            // Handle shift+click for splitting stacks
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                HandleShiftClick();
            }
            else
            {
                // If we're already dragging something, drop it
                if (currentlyDraggedItem != null && currentlyDraggedItem != this)
                {
                    currentlyDraggedItem.HandleDropOnSlot(originalSlot);
                    return;
                }

                // If nothing is being dragged and this slot has an item, start dragging
                if (currentlyDraggedItem == null && !originalSlot.IsEmpty)
                {
                    StartDrag();
                }
            }
        }
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            // Right click to cancel drag
            if (currentlyDraggedItem == this)
            {
                CancelDrag();
            }
        }
    }

    // Implement IBeginDragHandler to properly handle drag initiation
    public void OnBeginDrag(PointerEventData eventData)
    {
        // This helps Unity's EventSystem recognize this as a drag operation
        if (!originalSlot.IsEmpty && !isDragging)
        {
            StartDrag();
        }
    }

    // Implement IDropHandler to detect drops on this GameObject
    public void OnDrop(PointerEventData eventData)
    {
        // If this is being dropped on a non-inventory slot area and we have a dragged item
        if (currentlyDraggedItem != null && currentlyDraggedItem != this)
        {
            // Check if this GameObject has an InventorySlot component
            InventorySlot slot = GetComponent<InventorySlot>();
            if (slot == null)
            {
                Debug.Log("Not inventory slot - drop cancelled");
                currentlyDraggedItem.CancelDrag();
            }
            else
            {
                // If it is an inventory slot, handle the drop normally
                currentlyDraggedItem.HandleDropOnSlot(slot);
            }
        }
    }

    private void Update()
    {
        // Update drag position to follow mouse cursor
        if (isDragging && dragTransform != null)
        {
            Vector2 mousePosition = Input.mousePosition;
            RectTransformUtility.ScreenPointToWorldPointInRectangle(
                dragTransform, mousePosition, null, out Vector3 worldPoint);
            dragTransform.position = worldPoint;

            // Check if we've moved enough to be considered dragging (not just clicking)
            if (Vector2.Distance(dragStartPosition, mousePosition) < dragThreshold)
            {
                return; // Don't process drops until we've actually dragged
            }
        }

        // Cancel drag on Escape key
        if (isDragging && Input.GetKeyDown(KeyCode.Escape))
        {
            CancelDrag();
        }

        // Handle drop when mouse button is released
        if (isDragging && Input.GetMouseButtonUp(0))
        {
            // Only process as a drop if we've actually dragged the item
            if (dragTransform != null && Vector2.Distance(dragStartPosition, Input.mousePosition) >= dragThreshold)
            {
                // Check if we're over a UI element
                if (EventSystem.current.IsPointerOverGameObject())
                {
                    // If we're still dragging here and no OnDrop was called, it means we didn't drop on a valid IDropHandler
                    if (isDragging)
                    {
                        Debug.Log("Dropped on UI element without IDropHandler - not an inventory slot");
                        CancelDrag();
                    }
                }
                else
                {
                    // Dropped outside of UI entirely
                    Debug.Log("Dropped outside of UI - not an inventory slot");
                    CancelDrag();
                }
            }
            else
            {
                // If we didn't move enough, it was just a click - cancel the drag
                CancelDrag();
            }
        }
    }

    private void HandleShiftClick()
    {
        // Only split if slot has a stackable item with more than 1 item
        if (originalSlot.IsEmpty ||
            !originalSlot.CurrentItem.IsStackable ||
            originalSlot.StackCount <= 1)
        {
            return;
        }

        // Find an empty slot in the inventory
        InventorySlot emptySlot = FindEmptySlotInInventory();
        if (emptySlot != null)
        {
            SplitStack(emptySlot);
        }
    }

    private InventorySlot FindEmptySlotInInventory()
    {
        InventorySlot[] allSlots = originalSlot.transform.parent.GetComponentsInChildren<InventorySlot>();
        foreach (InventorySlot slot in allSlots)
        {
            if (slot != originalSlot && slot.IsEmpty)
            {
                return slot;
            }
        }
        return null;
    }

    private void SplitStack(InventorySlot targetSlot)
    {
        int currentStackSize = originalSlot.StackCount;
        int splitAmount = currentStackSize / 2;

        if (splitAmount < 1) return;

        Item itemToSplit = originalSlot.CurrentItem;
        originalSlot.DecreaseStack(splitAmount);
        targetSlot.TryAddItem(itemToSplit, splitAmount);
    }

    private void StartDrag()
    {
        isDragging = true;
        currentlyDraggedItem = this;
        dragStartPosition = Input.mousePosition;

        // Create drag visual
        CreateDragVisual();

        // Make original slot semi-transparent
        canvasGroup.alpha = 0.5f;
        canvasGroup.blocksRaycasts = false;
    }

    private void CreateDragVisual()
    {
        GameObject dragObject = new GameObject("DragVisual");
        dragObject.transform.SetParent(dragCanvas.transform, false);
        dragObject.transform.SetAsLastSibling();

        dragTransform = dragObject.AddComponent<RectTransform>();
        dragTransform.sizeDelta = GetComponent<RectTransform>().sizeDelta;

        dragImage = dragObject.AddComponent<Image>();
        dragImage.sprite = originalSlot.CurrentItem.Icon;
        dragImage.preserveAspect = true;

        CanvasGroup dragCanvasGroup = dragObject.AddComponent<CanvasGroup>();
        dragCanvasGroup.alpha = dragAlpha;
        dragCanvasGroup.blocksRaycasts = false;

        dragTransform.position = transform.position;
    }

    public void HandleDropOnSlot(InventorySlot targetSlot)
    {
        if (!isDragging || targetSlot == null)
        {
            CancelDrag();
            return;
        }

        // Check if target slot is an equipment slot
        EquipmentSlot equipmentSlot = targetSlot as EquipmentSlot;
        if (equipmentSlot != null)
        {
            HandleEquipmentDrop(equipmentSlot);
        }
        else if (isSplitting)
        {
            HandleSplitDrop(targetSlot);
        }
        else
        {
            HandleSlotDrop(targetSlot);
        }
    }

    private void HandleSplitDrop(InventorySlot targetSlot)
    {
        if (targetSlot.IsEmpty && originalSlot.CurrentItem.IsStackable && originalSlot.StackCount > 1)
        {
            int splitAmount = originalSlot.StackCount / 2;

            if (splitAmount < 1)
            {
                CancelDrag();
                return;
            }

            Item itemToSplit = originalSlot.CurrentItem;
            originalSlot.DecreaseStack(splitAmount);
            targetSlot.TryAddItem(itemToSplit, splitAmount);
        }

        EndDrag();
        isSplitting = false;
    }

    private void HandleSlotDrop(InventorySlot targetSlot)
    {
        Item draggedItem = originalSlot.CurrentItem;
        int draggedCount = originalSlot.StackCount;

        if (targetSlot.IsEmpty)
        {
            MoveItemToEmptySlot(targetSlot);
        }
        else if (CanMergeStacks(originalSlot, targetSlot))
        {
            MergeStacks(targetSlot);
        }
        else if (CanSwapItems(originalSlot, targetSlot))
        {
            SwapItems(targetSlot);
        }
        else
        {
            CancelDrag();
        }
    }

    private bool CanMergeStacks(InventorySlot fromSlot, InventorySlot toSlot)
    {
        Item fromItem = fromSlot.CurrentItem;
        Item toItem = toSlot.CurrentItem;

        if (fromItem == null || toItem == null ||
            fromItem.PlayFabId != toItem.PlayFabId ||
            !fromItem.IsStackable ||
            fromSlot == toSlot)
        {
            return false;
        }

        return toSlot.StackCount < toItem.MaxStackSize;
    }

    private bool CanSwapItems(InventorySlot fromSlot, InventorySlot toSlot)
    {
        return !fromSlot.IsEmpty && !toSlot.IsEmpty;
    }

    private void MoveItemToEmptySlot(InventorySlot targetSlot)
    {
        Item item = originalSlot.CurrentItem;
        int count = originalSlot.StackCount;

        originalSlot.RemoveItem();
        targetSlot.TryAddItem(item, count);

        EndDrag();
    }

    private void MergeStacks(InventorySlot targetSlot)
    {
        Item draggedItem = originalSlot.CurrentItem;
        Item targetItem = targetSlot.CurrentItem;

        int currentTargetStack = targetSlot.StackCount;
        int currentDraggedStack = originalSlot.StackCount;
        int maxStackSize = draggedItem.MaxStackSize;

        int availableSpace = maxStackSize - currentTargetStack;
        int amountToTransfer = Mathf.Min(currentDraggedStack, availableSpace);

        if (amountToTransfer > 0)
        {
            targetSlot.TryIncreaseStack(amountToTransfer);

            if (amountToTransfer == currentDraggedStack)
            {
                originalSlot.RemoveItem();
            }
            else
            {
                originalSlot.DecreaseStack(amountToTransfer);
            }
        }

        EndDrag();
    }

    private void SwapItems(InventorySlot targetSlot)
    {
        Item tempItem = targetSlot.CurrentItem;
        int tempCount = targetSlot.StackCount;

        targetSlot.RemoveItem();
        Item originalItem = originalSlot.CurrentItem;
        int originalCount = originalSlot.StackCount;
        originalSlot.RemoveItem();

        if (tempItem != null)
            originalSlot.TryAddItem(tempItem, tempCount);

        if (originalItem != null)
            targetSlot.TryAddItem(originalItem, originalCount);

        EndDrag();
    }

    private void CancelDrag()
    {
        // Just cleanup the drag visual, item stays in original slot
        CleanupDragVisual();

        // Restore original slot appearance
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        isDragging = false;
        isSplitting = false;
        currentlyDraggedItem = null;
    }

    private void EndDrag()
    {
        isDragging = false;
        currentlyDraggedItem = null;

        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        CleanupDragVisual();

        // Notify inventory manager to save layout
        PlayFabInventoryManager inventoryManager = FindObjectOfType<PlayFabInventoryManager>();
        if (inventoryManager != null)
        {
            inventoryManager.OnInventoryStateChanged();
        }
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
    // Add this method to your InventoryDragAndDrop.cs
    private void HandleEquipmentDrop(EquipmentSlot equipmentSlot)
    {
        if (!isDragging || equipmentSlot == null)
        {
            CancelDrag();
            return;
        }

        // Check if the equipment slot can accept this item
        if (equipmentSlot.CanAcceptItem(originalSlot.CurrentItem, originalSlot.StackCount))
        {
            // Store original item data
            Item draggedItem = originalSlot.CurrentItem;
            int draggedCount = originalSlot.StackCount;

            // Remove from original slot
            originalSlot.RemoveItem();

            // Add to equipment slot (this will automatically call Equip)
            if (equipmentSlot.TryAddItem(draggedItem, draggedCount))
            {
                EndDrag();
            }
            else
            {
                // If equipment slot rejected the item, return it to original slot
                originalSlot.TryAddItem(draggedItem, draggedCount);
                CancelDrag();
            }
        }
        else
        {
            Debug.Log($"Equipment slot cannot accept this item type. Required: {equipmentSlot.GetAcceptedItemClass()}");
            CancelDrag();
        }
    }
    private void OnDestroy()
    {
        if (currentlyDraggedItem == this)
        {
            currentlyDraggedItem = null;
        }
        CleanupDragVisual();
    }
}
