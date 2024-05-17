using UnityEngine;
using Emerge.Connect.UI;

namespace Emerge.Connect.Avatar.Meta
{
    public class PhotoAvatarManager : MonoBehaviour
    {
        [SerializeField] private HiResScreenshot hiResScreenShots;
        [SerializeField] private AvatarPhotoUIController avatarPhotoUIController;
        [SerializeField] private SampleAvatarEntity mirrorEntity;
        private OVRCameraRig _hardwareRig;

        private void OnEnable()
        {
            _hardwareRig = FindObjectOfType<OVRCameraRig>();
            mirrorEntity.gameObject.transform.position = new Vector3(0f, 100f, 0f);
            hiResScreenShots = AvatarManager.Instance.LocalAvatar.UserStateSync.screenShot;
            ConfigureMirrorAvatar();
            if(mirrorEntity.IsCreated)
            {
                InitAvatarPhoto();
                SetMirrorAvatarPosition();
            }     
        }

        private void ConfigureMirrorAvatar()
        {
            mirrorEntity.OnUserAvatarLoadedEvent.AddListener(_ =>
            {
                InitMirrorAvatar();
            });
        }

        private void InitAvatarPhoto()
        {
            avatarPhotoUIController.Init();
        }

        private void SetMirrorAvatarPosition()
        {
            Vector3 direction = Camera.main.transform.forward;
            var avatarPosition = AvatarManager.Instance.LocalAvatar.transform.position;
            mirrorEntity.gameObject.transform.position = avatarPosition + direction * 2f;
            Vector3 temp = mirrorEntity.transform.position;
            temp.y = avatarPosition.y;
            mirrorEntity.transform.position = temp;
            mirrorEntity.transform.rotation = _hardwareRig.transform.rotation * Quaternion.Euler(0, 180, 0);
        }

        private void InitMirrorAvatar()
        {
            avatarPhotoUIController.Init();
            var inputManager = _hardwareRig.GetComponentInChildren<SampleInputManager>();
            mirrorEntity.SetBodyTracking(inputManager);
            SetMirrorAvatarPosition();
        }
    }
}
