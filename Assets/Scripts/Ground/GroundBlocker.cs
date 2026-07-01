using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(EntityId))]
public class GroundBlocker : MonoBehaviour
{
    [SerializeField] private Collider2D footprintCollider;

    private Coroutine registrationRoutine;
    private bool isRegistered;
    private EntityId entityId;

    private string BlockerId
    {
        get
        {
            if (entityId == null)
                entityId = GetComponent<EntityId>();

            if (entityId == null)
                return "";

            return entityId.Id;
        }
    }

    private void Awake()
    {
        entityId = GetComponent<EntityId>();

        if (entityId != null)
            entityId.EnsureId();

        if (footprintCollider == null)
            footprintCollider = GetComponent<Collider2D>();
    }

    private void Reset()
    {
        footprintCollider = GetComponent<Collider2D>();

        EntityId id = GetComponent<EntityId>();

        if (id != null)
            id.EnsureId();
    }

    private void OnEnable()
    {
        registrationRoutine =
            StartCoroutine(RegisterWhenGroundIsReady());
    }

    private void OnDisable()
    {
        if (registrationRoutine != null)
        {
            StopCoroutine(registrationRoutine);
            registrationRoutine = null;
        }

        Unregister();
    }

    private IEnumerator RegisterWhenGroundIsReady()
    {
        while (GroundManager.Instance == null ||
               !GroundManager.Instance.IsInitialized)
        {
            yield return null;
        }

        RegisterNow();
        registrationRoutine = null;
    }

    [ContextMenu("Refresh Blocked Ground Cells")]
    public void RefreshBlockedCells()
    {
        if (!isActiveAndEnabled)
            return;

        if (GroundManager.Instance == null ||
            !GroundManager.Instance.IsInitialized)
        {
            return;
        }

        RegisterNow();
    }

    private void RegisterNow()
    {
        if (footprintCollider == null)
        {
            Debug.LogError(
                "GroundBlocker needs a Collider2D footprint.",
                this);

            return;
        }

        if (string.IsNullOrWhiteSpace(BlockerId))
        {
            Debug.LogError(
                "GroundBlocker needs a valid EntityId.",
                this);

            return;
        }

        GroundManager groundManager = GroundManager.Instance;

        List<Vector3Int> occupiedCells =
            groundManager.GetCellsOverlappingBounds(
                footprintCollider.bounds);

        groundManager.RegisterBlocker(
            BlockerId,
            occupiedCells);

        isRegistered = true;
    }

    private void Unregister()
    {
        if (!isRegistered)
            return;

        if (GroundManager.Instance != null &&
            !string.IsNullOrWhiteSpace(BlockerId))
        {
            GroundManager.Instance.UnregisterBlocker(BlockerId);
        }

        isRegistered = false;
    }
}