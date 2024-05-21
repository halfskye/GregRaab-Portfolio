using System.Collections;
using Oculus.Platform;
using UnityEngine;

namespace Avatar.Meta
{
    public class MetaAvatarInit : MonoBehaviour
    {
        private string _metaUserId = string.Empty;
        
        private void Awake() {
            StartCoroutine(SetupOvrPlatform());
        }

        private IEnumerator SetupOvrPlatform() {
            // Ensure OvrPlatform is Initialized
            if(OvrPlatformInit.status == OvrPlatformInitStatus.NotStarted) {
                OvrPlatformInit.InitializeOvrPlatform();
            }
            while(OvrPlatformInit.status != OvrPlatformInitStatus.Succeeded) {
                if(OvrPlatformInit.status == OvrPlatformInitStatus.Failed) {
                    Debug.LogError("Error initializing OvrPlatform");
                    yield break;
                }
                yield return null;
            }
            Users.GetLoggedInUser().OnComplete(message => {
                if(!message.IsError) {
                    _metaUserId = message.Data.ID.ToString();
                } else {
                    var e = message.GetError();
                }
            });
        }
    }
}