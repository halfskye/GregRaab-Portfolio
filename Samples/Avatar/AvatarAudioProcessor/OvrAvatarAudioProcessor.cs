using Oculus.Avatar2;

namespace Emerge.Connect.Avatar.AvatarAudioProcessor
{
    public class OvrAvatarAudioProcessor : AvatarAudioProcessor
    {
        public OvrAvatarLipSyncContext lipSyncContext;
        public int channels;
        
        public override float[] Process(float[] buf)
        {
            if (lipSyncContext != null)
            {
                lipSyncContext.ProcessAudioSamples(buf, channels);
            }
            return buf;
        }
    }
}
