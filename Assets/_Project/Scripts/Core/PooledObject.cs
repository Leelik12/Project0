using System.Collections.Generic;
using UnityEngine;

/// <summary>Экземпляр из <see cref="ObjectPool"/>: по истечении времени выключается и возвращается в пул.</summary>
public class PooledObject : MonoBehaviour
{
    private Stack<PooledObject> pool;
    private ParticleSystem[] particles;
    private float returnTime;

    public void Init(Stack<PooledObject> owner)
    {
        pool = owner;
        particles = GetComponentsInChildren<ParticleSystem>(true);
    }

    public void Activate(float lifetime)
    {
        returnTime = Time.time + lifetime;
        gameObject.SetActive(true);

        // Частицы при повторном использовании перезапускаем с нуля
        foreach (ParticleSystem ps in particles)
        {
            if (ps == null) continue;
            ps.Clear(false);
            ps.Play(false);
        }
    }

    private void Update()
    {
        if (Time.time >= returnTime) Release();
    }

    private void Release()
    {
        gameObject.SetActive(false);
        pool.Push(this);
    }
}
