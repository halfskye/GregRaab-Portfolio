using System;
using Oculus.Avatar2;

namespace Emerge.Connect.Avatar.Meta
{
    /**
 * 
 * NetworkedAvatarEntity is in charge to configure Meta avatar entity
 * The configuration methods are called by the MetaAvatarSync class when an user prefab is spawned
 *     
 **/

    public class NetworkedAvatarEntity : SampleAvatarEntity
    {
        bool isRemote = true;

        protected override CAPI.ovrAvatar2EntityCreateInfo? ConfigureCreationInfo()
        {
            if (isRemote)
            {
                return new CAPI.ovrAvatar2EntityCreateInfo
                {
                    features = CAPI.ovrAvatar2EntityFeatures.Preset_Remote,
                    renderFilters = new CAPI.ovrAvatar2EntityFilters

                    {
                        lodFlags = CAPI.ovrAvatar2EntityLODFlags.All,
                        manifestationFlags = CAPI.ovrAvatar2EntityManifestationFlags.Half,
                        viewFlags = CAPI.ovrAvatar2EntityViewFlags.All,
                        subMeshInclusionFlags = CAPI.ovrAvatar2EntitySubMeshInclusionFlags.All
                    }
                };

            }
            else
            {
                return new CAPI.ovrAvatar2EntityCreateInfo
                {
                    features = CAPI.ovrAvatar2EntityFeatures.Preset_Default
                               | CAPI.ovrAvatar2EntityFeatures.Rendering_ObjectSpaceTransforms,
                    renderFilters = new CAPI.ovrAvatar2EntityFilters

                    {
                        lodFlags = CAPI.ovrAvatar2EntityLODFlags.All,
                        manifestationFlags = CAPI.ovrAvatar2EntityManifestationFlags.Half,
                        viewFlags = CAPI.ovrAvatar2EntityViewFlags.All,
                        subMeshInclusionFlags = CAPI.ovrAvatar2EntitySubMeshInclusionFlags.All
                    }
                };

            }
        }

        public void ConfigureAsLocal()
        {
            isRemote = false;
            SetAvatarAsFirstPerson();
            SetIsLocal(true);
            Teardown();
            CreateEntity();
            
            LoadUser();
        }
        public void ConfigureAsRemote()
        {
            isRemote = true;
            SetActiveView(CAPI.ovrAvatar2EntityViewFlags.ThirdPerson);
            SetIsLocal(false);
            Teardown();
            CreateEntity();
            
            LoadUser();
        }

        public bool CanStreamJointData()
        {
            return IsCreated
                   && HasJoints;
        }
        
        public void DisplayHead()
        {
            SetActiveView(CAPI.ovrAvatar2EntityViewFlags.ThirdPerson);
        }

        public void SetMetaUserId(string metaUserId)
        {
            _userId = Convert.ToUInt64(metaUserId);
        }

        public void SetAvatarAsFirstPerson()
        {
            SetActiveView(CAPI.ovrAvatar2EntityViewFlags.FirstPerson);
        }
        public void SetAvatarIndex(int avatarIndex)
        {
            if (_assets != null && _assets.Count > 0)
            {
                var asset = _assets[0];
                asset.path = avatarIndex.ToString();
                _assets[0] = asset;
            }
        }

    }
}
