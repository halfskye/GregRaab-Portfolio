using UnityEngine;
using Normal.Realtime;
using Emerge.Home.Cloud;

namespace Emerge.Connect.Avatar
{

    public class UserStateSync : RealtimeComponent<UserStateModel>
    {
        [SerializeField] RealtimeAvatarBase realtimeAvatar;
        [SerializeField] AvatarInactiveStateManager avatarStateManager;
        [SerializeField] HiResScreenshot hiResScreenshot;
        public HiResScreenshot screenShot => hiResScreenshot;

        public string AvatarUserID => model.avatarUserID;
        public string AvatarImageURL => model.avatarImageURL;
        public string AvatarName => model.avatarName;
        public bool IsUserActive => model.isPlayerActive;
     
        private void Start()
        {
            avatarStateManager.gameObject.GetComponent<RealtimeView>().RequestOwnership();
            avatarStateManager.gameObject.GetComponent<RealtimeTransform>().RequestOwnership();
            InitUserValues();   
        }

        private void InitUserValues()
        {
            if (AvatarManager.Instance.LocalAvatar == null)
                return;

            if(this.GetComponent<RealtimeView>().isOwnedLocallyInHierarchy)
            {
                if (!string.IsNullOrEmpty(EmergeCloud.CurrentUser.FirstName))
                {
                    SetAvatarName(EmergeCloud.CurrentUser.FirstName);
                }
                if (!string.IsNullOrEmpty(EmergeCloud.CurrentUser.ImageUrl))
                {
                    SetImageURL(EmergeCloud.CurrentUser.ImageUrl);
                }
                if (!string.IsNullOrEmpty(EmergeCloud.CurrentUser.Id))
                {
                    SetAvatarUserID(EmergeCloud.CurrentUser.Id);
                }
            }    
        }
        protected override void OnRealtimeModelReplaced(UserStateModel previousModel, UserStateModel currentModel)
        {
            if (previousModel != null)
            {
                previousModel.isPlayerActiveDidChange -= UserStateDidChange;
                previousModel.avatarImageURLDidChange -= UserImageULRChange;
            }
            if (currentModel != null)
            {
                if (currentModel.isFreshModel)
                {
                    currentModel.avatarImageURL = EmergeCloud.CurrentUser.ImageUrl;
                    currentModel.isPlayerActive = true;
                    currentModel.avatarUserID = EmergeCloud.CurrentUser.Id;
                    currentModel.avatarName = EmergeCloud.CurrentUser.FirstName;
                }
                UpdateUserState(currentModel.isPlayerActive);
                UpdateImageURL(currentModel.avatarImageURL);
                currentModel.isPlayerActiveDidChange += UserStateDidChange;
                currentModel.avatarImageURLDidChange += UserImageULRChange;
            }
        }

        private void UserImageULRChange(UserStateModel model, string value)
        {
            UpdateImageURL(value);
        }
        private void UpdateImageURL(string imageURL)
        {
            avatarStateManager.SetAvatarImage(imageURL);
        }
        public void SetImageURL(string url)
        {
            model.avatarImageURL = url;
        }

        public void SetAvatarName(string name)
        {
            model.avatarName = name;
        }

        public void SetAvatarUserID(string _ID)
        {
            model.avatarUserID = _ID;
        }

        private void UserStateDidChange(UserStateModel model, bool value)
        {
            UpdateUserState(value);
        }
        private void UpdateUserState(bool userState)
        {
            realtimeAvatar.SetAvatarActiveWithPauseTimer(userState);
        }

        public void SetIsPlayerActive(bool active)
        {
            model.isPlayerActive = active;
        }
    }
}
