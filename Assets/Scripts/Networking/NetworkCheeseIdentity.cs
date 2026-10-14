using UnityEngine;

public class NetworkCheeseIdentity : MonoBehaviour
{
    [Header("Identidad de red")]
    [SerializeField] private int cheeseId;

    [SerializeField] private string cheeseType;

    public int CheeseId => cheeseId;
    public string CheeseType => cheeseType;

    public void SetIdentity(
        int id,
        string type)
    {
        cheeseId = id;
        cheeseType = type;
    }
}