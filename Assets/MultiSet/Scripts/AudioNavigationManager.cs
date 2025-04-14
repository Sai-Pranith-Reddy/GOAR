using UnityEngine;
using System.Collections.Generic;
using System.Security.Cryptography;

public class AudioNavigationManager : MonoBehaviour
{
    public static AudioNavigationManager Instance;

    [Header("Audio Clips")]
    public AudioClip TurnLeft;
    public AudioClip TurnRight;
    public AudioClip DistanceAlert;
    public AudioClip DestinationReached;

    [Header("Settings")]
    [SerializeField] private float turnThreshold = 30f;
    [SerializeField]
    [Tooltip("Distance to trigger turn announcement")]
    private float turnAnnounceDistance = 3f;
    [SerializeField]
    [Tooltip("Distance for destination proximity alert")]
    private float destinationAnnounceDistance = 5f;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private float announcementCooldown = 0.5f;

    private Queue<AudioClip> audioQueue = new Queue<AudioClip>();
    private bool isSpeaking;
    private Transform userTransform;
    private Vector3 lastAnnouncedTurn;
    void Awake()
    {
        Instance = this;
        userTransform = Camera.main.transform;
        InitializeAudioSource();
    }
    void InitializeAudioSource()
    {
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 0;
            audioSource.playOnAwake = false;
            Debug.Log("Created new AudioSource component");
        }
    }
    void Start()
    {
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 0;
            audioSource.playOnAwake = false;
        }

        if (userTransform == null)
            userTransform = Camera.main.transform;
    }
    public void Initialize(Transform userTransform) => this.userTransform = userTransform;

    public void UpdateNavigation(Vector3[] pathCorners, Vector3 destination)
    {
        if (pathCorners == null || pathCorners.Length < 2)
        {
            Debug.LogWarning("Invalid path corners array");
            return;
        }

        HandleDestinationProximity(destination);
        HandleTurnDetection(pathCorners);
    }

    void HandleDestinationProximity(Vector3 destination)
    {
        float remainingDistance = Vector3.Distance(userTransform.position, destination);
        if (remainingDistance <= destinationAnnounceDistance)
        {
            Debug.Log($"Near destination ({remainingDistance}m)");
            if (!audioQueue.Contains(DistanceAlert)) PlayClip(DistanceAlert);
        }
    }

    void HandleTurnDetection(Vector3[] pathCorners)
    {
        bool turnFound = false;

        // Find first valid turn within announcement range
        for (int i = 0; i < pathCorners.Length - 2; i++)
        {
            Vector3 turnPoint = pathCorners[i + 1];

            if (Vector3.Distance(userTransform.position, turnPoint) > turnAnnounceDistance)
                continue;

            if (turnPoint == lastAnnouncedTurn)
                continue;

            Vector3 incoming = (turnPoint - pathCorners[i]).normalized;
            Vector3 outgoing = (pathCorners[i + 2] - turnPoint).normalized;
            float angle = Vector3.SignedAngle(incoming, outgoing, Vector3.up);

            Debug.Log($"Turn check at point {i}: {angle}°");

            if (Mathf.Abs(angle) > turnThreshold)
            {
                AudioClip clip = angle > 0 ? TurnRight : TurnLeft;
                Debug.Log($"Queuing turn: {(angle > 0 ? "Right" : "Left")} at {turnPoint}");
                PlayClip(clip);
                lastAnnouncedTurn = turnPoint;
                turnFound = true;
                break;
            }
        }

        if (!turnFound) Debug.Log("No valid turns detected in path");
    }

    private float CalculatePathDistance(Vector3[] path)
    {
        float distance = 0f;
        for (int i = 0; i < path.Length - 1; i++)
        {
            distance += Vector3.Distance(path[i], path[i + 1]);
        }
        return distance;
    }
    public void PlayPriorityClip(AudioClip clip)
    {
        if (clip == null) return;

        Debug.Log($"Playing priority clip: {clip.name}");
        audioSource.Stop();
        audioQueue.Clear();
        audioSource.PlayOneShot(clip);
        isSpeaking = true;
        Invoke(nameof(ResetSpeech), clip.length + announcementCooldown);
    }



    public void PlayClip(AudioClip clip)
    {
        if (clip == null)
        {
            Debug.LogWarning("Tried to play null audio clip");
            return;
        }

        if (!audioQueue.Contains(clip))
        {
            Debug.Log($"Queuing audio: {clip.name}");
            audioQueue.Enqueue(clip);
        }
    }

    void Update()
    {
        if (!isSpeaking && audioQueue.Count > 0)
        {
            AudioClip nextClip = audioQueue.Dequeue();
            Debug.Log($"Now playing: {nextClip.name}");
            audioSource.PlayOneShot(nextClip);
            isSpeaking = true;
            Invoke(nameof(ResetSpeech), nextClip.length + announcementCooldown);
        }
    }
    void ResetSpeech() => isSpeaking = false;
}