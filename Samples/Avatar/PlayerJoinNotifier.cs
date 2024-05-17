using Emerge.Connect.Normcore;
using UnityEngine;

namespace Emerge.Connect.Avatar
{
    [RequireComponent(typeof(AudioSource))]
    public class PlayerJoinNotifier : MonoBehaviour
    {
        [SerializeField]
        private AudioClip playerJoinClip;
        
        [SerializeField]
        private AudioClip playerLeaveClip;
        
        private AudioSource audioSource;
        
        private void Start()
        {
            audioSource = GetComponent<AudioSource>();

            ConnectionManager.OnAddClient += OnClientAdd;
            ConnectionManager.OnRemoveClient += OnClientRemove;
        }

        private void OnClientAdd(int clientId, bool isLocal)
        {
            if (isLocal) return;
            
            audioSource.PlayOneShot(playerJoinClip);
        }
        
        private void OnClientRemove(int clientId, bool isLocal)
        {
            if (isLocal) return;
            
            audioSource.PlayOneShot(playerLeaveClip);
        }
    }
}