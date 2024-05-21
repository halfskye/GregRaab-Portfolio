using UnityEngine;

namespace Avatar.AvatarAudioProcessor
{
    public abstract class AvatarAudioProcessor : MonoBehaviour
    {
        public virtual float[] Process(float[] buf)
        {
            return buf;
        }
    }
}
