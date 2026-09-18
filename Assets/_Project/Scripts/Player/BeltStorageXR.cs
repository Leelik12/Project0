using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Пояс игрока: магазины (тег Ammo), отпущенные внутри триггера, примагничиваются к свободному слоту.
/// Требует BoxCollider с isTrigger на том же объекте.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class BeltStorageXR : MonoBehaviour
{
    [SerializeField] private List<Transform> slotPoints;
    [SerializeField] private bool showSlotHints = true;
    [SerializeField] private float magnetSpeed = 10f;

    private BoxCollider triggerBox;
    private GameObject[] storedObjects;
    private readonly List<GameObject> slotHints = new();

    private void Start()
    {
        triggerBox = GetComponent<BoxCollider>();
        storedObjects = new GameObject[slotPoints.Count];

        if (showSlotHints)
        {
            CreateSlotHints();
        }
    }

    private void CreateSlotHints()
    {
        Material hintMaterial = new Material(Shader.Find("Unlit/Color")) { color = new Color(0, 1f, 1f, 0.6f) };

        foreach (Transform slot in slotPoints)
        {
            GameObject hint = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            hint.name = "SlotHint";
            hint.transform.SetParent(slot, false);
            hint.transform.localScale = Vector3.one * 0.05f;
            Destroy(hint.GetComponent<Collider>());
            hint.GetComponent<Renderer>().sharedMaterial = hintMaterial;
            slotHints.Add(hint);
        }
    }

    private void Update()
    {
        // Освобождаем слот, если предмет пропал (например, магазин вставили в оружие)
        for (int i = 0; i < storedObjects.Length; i++)
        {
            GameObject item = storedObjects[i];
            if (item == null || !item.activeInHierarchy)
            {
                storedObjects[i] = null;
                SetHintVisible(i, true);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        XRGrabInteractable grab = other.GetComponent<XRGrabInteractable>();
        if (grab == null || System.Array.IndexOf(storedObjects, other.gameObject) >= 0) return;

        grab.selectExited.AddListener(OnItemReleasedInTrigger);
    }

    private void OnTriggerExit(Collider other)
    {
        XRGrabInteractable grab = other.GetComponent<XRGrabInteractable>();
        if (grab == null) return;

        grab.selectExited.RemoveListener(OnItemReleasedInTrigger);
    }

    private void OnItemReleasedInTrigger(SelectExitEventArgs args)
    {
        GameObject item = args.interactableObject.transform.gameObject;

        if (!item.CompareTag("Ammo")) return;
        if (!IsInsideTrigger(item)) return;
        if (System.Array.IndexOf(storedObjects, item) >= 0) return;

        int slotIndex = GetFirstFreeSlot();
        if (slotIndex < 0) return;

        storedObjects[slotIndex] = item;
        StartCoroutine(MagnetToSlot(item, slotPoints[slotIndex], slotIndex));
    }

    private bool IsInsideTrigger(GameObject obj)
    {
        Vector3 center = transform.TransformPoint(triggerBox.center);
        Vector3 halfExtents = Vector3.Scale(triggerBox.size, transform.lossyScale) * 0.5f;

        foreach (Collider col in Physics.OverlapBox(center, halfExtents, transform.rotation))
        {
            if (col.gameObject == obj) return true;
        }
        return false;
    }

    private int GetFirstFreeSlot()
    {
        for (int i = 0; i < slotPoints.Count; i++)
        {
            if (storedObjects[i] == null) return i;
        }
        return -1;
    }

    private IEnumerator MagnetToSlot(GameObject item, Transform targetSlot, int slotIndex)
    {
        XRGrabInteractable grab = item.GetComponent<XRGrabInteractable>();
        if (grab != null) grab.selectExited.RemoveListener(OnItemReleasedInTrigger);

        Rigidbody rb = item.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        while (item != null && Vector3.Distance(item.transform.position, targetSlot.position) > 0.01f)
        {
            item.transform.position = Vector3.MoveTowards(item.transform.position, targetSlot.position, Time.deltaTime * magnetSpeed);
            item.transform.rotation = Quaternion.Lerp(item.transform.rotation, targetSlot.rotation, Time.deltaTime * magnetSpeed);
            yield return null;
        }
        if (item == null) yield break;

        item.transform.SetParent(targetSlot);
        item.transform.localPosition = Vector3.zero;
        item.transform.localRotation = Quaternion.identity;

        if (grab != null) grab.selectExited.AddListener(OnItemTaken);
        SetHintVisible(slotIndex, false);
    }

    private void OnItemTaken(SelectExitEventArgs args)
    {
        GameObject item = args.interactableObject.transform.gameObject;
        int index = System.Array.IndexOf(storedObjects, item);

        if (index >= 0)
        {
            storedObjects[index] = null;
            SetHintVisible(index, true);
        }

        item.transform.SetParent(null);

        Rigidbody rb = item.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = false;

        XRGrabInteractable grab = item.GetComponent<XRGrabInteractable>();
        if (grab != null) grab.selectExited.RemoveListener(OnItemTaken);
    }

    private void SetHintVisible(int index, bool visible)
    {
        if (showSlotHints && index >= 0 && index < slotHints.Count && slotHints[index] != null)
        {
            slotHints[index].SetActive(visible);
        }
    }
}
