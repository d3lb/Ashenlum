using UnityEngine;

public enum PersistenceType
{
    None,
    Temporary,
    Permanent
}

public class PersistentObject : MonoBehaviour
{
    [SerializeField] private string id;
    [SerializeField] private PersistenceType persistenceType = PersistenceType.None;

    public string Id => id;
    public PersistenceType PersistenceType => persistenceType;
}