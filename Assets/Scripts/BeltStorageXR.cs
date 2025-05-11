using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class BeltStorageXR : MonoBehaviour
{
    [SerializeField] private List<Transform> slotPoints;
    [SerializeField] private bool showSlotHints = true;
    [SerializeField] private float magnetSpeed = 10f;

    private GameObject[] storedObjects;
    private List<GameObject> slotHints = new();

    private void Start()
    {
        storedObjects = new GameObject[slotPoints.Count];

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

        int slotIndex;
        Transform freeSlot = GetFirstFreeSlot(out slotIndex);
        if (freeSlot == null) return;

        storedObjects[slotIndex] = item;
        StartCoroutine(MagnetToSlot(item, freeSlot, slotIndex));
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

    private Transform GetFirstFreeSlot(out int index)
    {
        for (int i = 0; i < slotPoints.Count; i++)
        {
            if (storedObjects[i] == null)
            {
                index = i;
                return slotPoints[i];
            }
        }
        index = -1;
        return null;
    }

    private IEnumerator MagnetToSlot(GameObject item, Transform targetSlot, int slotIndex)
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

        if (showSlotHints && slotIndex >= 0 && slotIndex < slotHints.Count)
            slotHints[slotIndex].SetActive(false);
    }

    private void OnItemTaken(SelectExitEventArgs args)
    {
        GameObject item = args.interactableObject.transform.gameObject;
        int index = System.Array.IndexOf(storedObjects, item);

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
    private void Update()
    {
        for (int i = 0; i < storedObjects.Length; i++)
        {
            GameObject item = storedObjects[i];

            // Если предмет пропал или был уничтожен
            if (item == null || !item.activeInHierarchy)
            {
                storedObjects[i] = null;

                if (showSlotHints && i < slotHints.Count)
                {
                    slotHints[i].SetActive(true);
                }
            }
        }
    }

}
