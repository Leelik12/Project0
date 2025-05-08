using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class BeltStorageXR : MonoBehaviour
{
    [SerializeField] private List<Transform> slotPoints;
    [SerializeField] private bool showSlotHints = true;
    [SerializeField] private float magnetSpeed = 10f;

    private List<GameObject> storedObjects = new();
    private List<GameObject> slotHints = new();

    private void Start()
    {
        if (showSlotHints)
        {
            foreach (var slot in slotPoints)
            {
                GameObject hint = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                hint.transform.SetParent(slot);
                hint.transform.localPosition = Vector3.zero;
                hint.transform.localScale = Vector3.one * 0.05f;
                hint.GetComponent<Collider>().enabled = false;

                var renderer = hint.GetComponent<Renderer>();
                renderer.material = new Material(Shader.Find("Unlit/Color"));
                renderer.material.color = new Color(0, 1f, 1f, 0.6f);

                slotHints.Add(hint);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        XRGrabInteractable grab = other.GetComponent<XRGrabInteractable>();
        if (grab == null || storedObjects.Contains(other.gameObject)) return;

        // Подписка на событие отпускания
        grab.selectExited.AddListener(OnItemReleasedInTrigger);
    }

    private void OnTriggerExit(Collider other)
    {
        XRGrabInteractable grab = other.GetComponent<XRGrabInteractable>();
        if (grab == null) return;

        // Отписка от события при выходе
        grab.selectExited.RemoveListener(OnItemReleasedInTrigger);
    }

    private void OnItemReleasedInTrigger(SelectExitEventArgs args)
    {
        GameObject item = args.interactableObject.transform.gameObject;

        // Проверка: предмет всё ещё в зоне пояса
        if (!IsInsideTrigger(item)) return;

        if (storedObjects.Contains(item)) return;

        Transform freeSlot = GetFirstFreeSlot();
        if (freeSlot == null) return;
        if (!item.transform.CompareTag("Ammo")) return;

        storedObjects.Add(item);
        StartCoroutine(MagnetToSlot(item, freeSlot));

        int slotIndex = slotPoints.IndexOf(freeSlot);
        if (showSlotHints && slotIndex >= 0 && slotIndex < slotHints.Count)
            slotHints[slotIndex].SetActive(false);
    }

    private bool IsInsideTrigger(GameObject obj)
    {
        Collider[] hits = Physics.OverlapBox(transform.position, transform.localScale * 0.5f, transform.rotation);
        foreach (var col in hits)
        {
            if (col.gameObject == obj)
                return true;
        }
        return false;
    }

    private Transform GetFirstFreeSlot()
    {
        for (int i = 0; i < slotPoints.Count; i++)
        {
            if (i >= storedObjects.Count || storedObjects[i] == null)
            {
                return slotPoints[i];
            }
        }
        return null;
    }

    private System.Collections.IEnumerator MagnetToSlot(GameObject item, Transform targetSlot)
    {
        XRGrabInteractable grab = item.GetComponent<XRGrabInteractable>();
        if (grab != null)
            grab.selectExited.RemoveListener(OnItemReleasedInTrigger);

        Rigidbody rb = item.GetComponent<Rigidbody>();
        if (rb) rb.isKinematic = true;

        while (Vector3.Distance(item.transform.position, targetSlot.position) > 0.01f)
        {
            item.transform.position = Vector3.MoveTowards(item.transform.position, targetSlot.position, Time.deltaTime * magnetSpeed);
            item.transform.rotation = Quaternion.Lerp(item.transform.rotation, targetSlot.rotation, Time.deltaTime * magnetSpeed);
            yield return null;
        }

        item.transform.SetParent(targetSlot);
        item.transform.localPosition = Vector3.zero;
        item.transform.localRotation = Quaternion.identity;

        grab?.selectExited.AddListener(OnItemTaken);
    }

    private void OnItemTaken(SelectExitEventArgs args)
    {
        GameObject item = args.interactableObject.transform.gameObject;
        int index = storedObjects.IndexOf(item);

        if (index >= 0)
        {
            storedObjects[index] = null;

            if (showSlotHints && index < slotHints.Count)
                slotHints[index].SetActive(true);
        }

        item.transform.SetParent(null);

        Rigidbody rb = item.GetComponent<Rigidbody>();
        if (rb) rb.isKinematic = false;

        XRGrabInteractable grab = item.GetComponent<XRGrabInteractable>();
        if (grab != null)
        {
            grab.selectExited.RemoveListener(OnItemTaken);
        }
    }
}
