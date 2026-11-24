using UnityEngine;

/// <summary>Keeps the light rain emitter above the player as they explore.</summary>
public class WeatherRainFollow : MonoBehaviour
{
    [SerializeField] private float heightAbovePlayer = 8f;
    private Transform player;

    private void Start()
    {
        GameObject playerObject = GameObject.Find("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
            FollowPlayer();
        }

        ParticleSystem rain = GetComponent<ParticleSystem>();
        if (rain != null) rain.Play();
    }

    private void LateUpdate()
    {
        if (player == null)
        {
            GameObject playerObject = GameObject.Find("Player");
            if (playerObject != null) player = playerObject.transform;
        }
        FollowPlayer();
    }

    private void FollowPlayer()
    {
        if (player != null)
        {
            Vector3 position = player.position;
            position.y += heightAbovePlayer;
            transform.position = position;
        }
    }
}
