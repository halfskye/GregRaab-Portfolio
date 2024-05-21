using UnityEngine;
using Normal.Realtime;
using Cloud;

namespace Avatar
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
                if (!string.IsNullOrEmpty(Cloud.CurrentUser.FirstName))
                {
                    SetAvatarName(Cloud.CurrentUser.FirstName);
                }
                if (!string.IsNullOrEmpty(Cloud.CurrentUser.ImageUrl))
                {
                    SetImageURL(Cloud.CurrentUser.ImageUrl);
                }
                if (!string.IsNullOrEmpty(Cloud.CurrentUser.Id))
                {
                    SetAvatarUserID(Cloud.CurrentUser.Id);
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
                    currentModel.avatarImageURL = Cloud.CurrentUser.ImageUrl;
                    currentModel.isPlayerActive = true;
                    currentModel.avatarUserID = Cloud.CurrentUser.Id;
                    currentModel.avatarName = Cloud.CurrentUser.FirstName;
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
