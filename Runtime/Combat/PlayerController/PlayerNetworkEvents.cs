using System;
using FishNet.Object;
using UnityEngine;

public class PlayerNetworkEvents : NetworkBehaviour
{
    [SerializeField] private GameObject messagePrefab;
    [SerializeField] private Transform messageSpawnPoint;

    public event Action OnAttack;
    public event Action OnDash;

    [ServerRpc(RequireOwnership = false)]
    public void ProcessAttack()
    {
        OnAttack?.Invoke();
    }
    [ServerRpc(RequireOwnership = false)]
    public void ProcessDash()
    {
        OnDash?.Invoke();
    }
    

}