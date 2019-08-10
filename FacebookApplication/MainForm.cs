using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using System.Text;
using Facebook;
using FacebookWrapper;
using FacebookWrapper.ObjectModel;

using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsForms;

namespace FacebookApplication
{
    public partial class MainForm : Form
    {
        private User m_FacebookUser;
        private Settings m_Settings;

        public MainForm()
        {
            InitializeComponent();
        }

        private void mainForm_Shown(object sender, EventArgs e)
        {
            m_Settings = Settings.LoadFromFile();

            if (m_Settings.RememberMe && !string.IsNullOrEmpty(m_Settings.AccessToken))
            {
                this.StartPosition = FormStartPosition.Manual;
                this.Size = m_Settings.WindowSize;
                this.Location = m_Settings.WindowLocation;
                LoginResult result = FacebookService.Connect(m_Settings.AccessToken);
                m_FacebookUser = result.LoggedInUser;
                checkBoxRememberMe.Checked = true;
                reloadUI();

            }
        }

        private void mainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (m_FacebookUser != null)
            {
                m_Settings.SaveToFile();
                FacebookService.Logout(logout_Operations);
            }
        }

        private void buttonLogin_Click(object sender, EventArgs e)
        {
            LoginResult result = FacebookService.Login(
            "1450160541956417",
            "email",
            "user_posts",
            "user_friends",
            "user_likes",
            "user_photos",
            "user_events",
            "user_birthday",
            "user_location",
            "user_gender",
            "user_tagged_places");

            m_Settings.AccessToken = result.AccessToken;

            if (!string.IsNullOrEmpty(result.AccessToken))
            {
                m_FacebookUser = result.LoggedInUser;
                reloadUI();
            }
            else
            {
                MessageBox.Show(result.ErrorMessage);
            }
        }

        private void reloadUI()
        {
            pictureBoxProfilePic.ImageLocation = m_FacebookUser.PictureNormalURL;
            pictureBoxProfilePic.SizeMode = PictureBoxSizeMode.StretchImage;
            this.Text = string.Format("Facebook - {0}", m_FacebookUser.Name);
            buttonLoginLogout.Click -= buttonLogin_Click;
            buttonLoginLogout.Click += buttonLogout_Click;
            buttonLoginLogout.Text = "Logout";
            makeButtonsEnabled(true);
            loadAbout();
            loadMyPosts();
            loadAlbums();
        }

        private void buttonLogout_Click(object sender, EventArgs e)
        {
            FacebookService.Logout(logout_Operations);
        }

        private void logout_Operations()
        {
            this.Text = "Facebook";
            pictureBoxProfilePic.ImageLocation = null;
            buttonLoginLogout.Click += buttonLogin_Click;
            buttonLoginLogout.Text = "Login";
            buttonLoginLogout.Click -= buttonLogout_Click;
            clearAbout();
            clearMyPosts();
            clearPhotos();
            listBoxFriends.Items.Clear();
            listBoxPages.Items.Clear();
            listBoxMyEvents.Items.Clear();
            makeButtonsEnabled(false);
            reInitializeMap();
        }

        private void makeButtonsEnabled(bool i_IsButtonEnabled)
        {
            buttonRefreshAlbums.Enabled = i_IsButtonEnabled;
            buttonShareMyPost.Enabled = i_IsButtonEnabled;
            buttonRefreshPosts.Enabled = i_IsButtonEnabled;
            buttonLoadEvents.Enabled = i_IsButtonEnabled;
            buttonRefreshFriends.Enabled = i_IsButtonEnabled;
            buttonRefreshPages.Enabled = i_IsButtonEnabled;
            buttonCreateAlbum.Enabled = i_IsButtonEnabled;
            buttonDeletePost.Enabled = i_IsButtonEnabled;
            buttonUploadPhoto.Enabled = i_IsButtonEnabled;
            buttonShowMyPlaces.Enabled = i_IsButtonEnabled;
            buttonClearMyPlaces.Enabled = i_IsButtonEnabled;
            buttonShowMyPlaces.Enabled = i_IsButtonEnabled;
            checkBoxcheckins.Enabled = i_IsButtonEnabled;
            checkBoxTagedPlaces.Enabled = i_IsButtonEnabled;
            checkBoxCurrentLocation.Enabled = i_IsButtonEnabled;


        }

        private void checkBoxRememberMe_CheckedChanged(object sender, EventArgs e)
        {
            m_Settings.RememberMe = checkBoxRememberMe.Checked;
            m_Settings.WindowSize = this.Size;
            m_Settings.WindowLocation = this.Location;
        }

        private void loadAbout()
        {
            this.labelFirstNameInfo.Text = m_FacebookUser.FirstName;
            this.labelLastNameInfo.Text = m_FacebookUser.LastName;
            this.labelEmailInfo.Text = m_FacebookUser.Email == null ? "Not available" : m_FacebookUser.Email;
            this.labelLocationInfo.Text = m_FacebookUser.Location == null ? "Not available" : m_FacebookUser.Location.Name;
            this.labelGenderInfo.Text = m_FacebookUser.Gender == User.eGender.female ? "Female" : "Male";
            this.labelBirthdayInfo.Text = m_FacebookUser.Birthday == null ? "Not available" : m_FacebookUser.Birthday;
        }

        private void clearAbout()
        {
            this.labelFirstNameInfo.Text = string.Empty;
            this.labelLastNameInfo.Text = string.Empty;
            this.labelEmailInfo.Text = string.Empty;
            this.labelLocationInfo.Text = string.Empty;
            this.labelGenderInfo.Text = string.Empty;
            this.labelBirthdayInfo.Text = string.Empty;
        }

        private void loadMyPosts()
        {
            m_FacebookUser.ReFetch();
            listBoxMyPosts.Items.Clear();

            foreach (Post post in m_FacebookUser.Posts)
            {
                if (post.Message != null)
                {
                    listBoxMyPosts.Items.Add(string.Format(
                        "Date: {0}        By: {1}        Post: {2}",
                        post.UpdateTime,
                        post.From == null ? m_FacebookUser.Name : post.From.Name,
                        post.Message));
                }
                else
                {
                    listBoxMyPosts.Items.Add(string.Format("[{0}]", post.Type));
                }
            }

            if (m_FacebookUser.Posts.Count == 0)
            {
                MessageBox.Show("No Posts to retrieve.");
            }
        }

        private void sharePost()
        {
            try
            {
                m_FacebookUser.PostStatus(textBoxPost.Text);
            }
            catch (FacebookOAuthException)
            {
                MessageBox.Show("Sorry, there is no permission to share a post from this application.");
            }
            finally
            {
                textBoxPost.Text = string.Empty;
            }
        }

        private void deletePost()
        {
            if (listBoxMyPosts.SelectedItem != null)
            {
                try
                {
                    m_FacebookUser.Posts[listBoxMyPosts.SelectedIndex].Delete();
                }
                catch (FacebookOAuthException)
                {
                    MessageBox.Show("Authentication error. Cannot delete a post for current user to Facebook.");
                }
            }
            else
            {
                MessageBox.Show("Post doesn't exist.");
            }

            loadMyPosts();
        }

        private void clearMyPosts()
        {
            listBoxMyPosts.Items.Clear();
            textBoxPost.Text = string.Empty;
        }

        private void loadAlbums()
        {
            listBoxAlbums.Items.Clear();
            listBoxAlbums.DisplayMember = "Name";

            foreach (Album albums in m_FacebookUser.Albums)
            {
                listBoxAlbums.Items.Add(albums);
            }
        }

        private void createAlbum()
        {
            string inputAlbumName = Microsoft.VisualBasic.Interaction.InputBox("Please enter an album name:");
            try
            {
                m_FacebookUser.CreateAlbum(inputAlbumName);
            }
            catch (FacebookOAuthException)
            {
                MessageBox.Show("Authentication error. Cannot create an album for current user on Facebook.");
            }
        }

        private void uploadPhoto()
        {
            try
            {
                if (listBoxAlbums.SelectedItem != null)
                {
                    var FD = new OpenFileDialog();
                    FD.Filter = "Image Files(*.JPG)| *.JPG";
                    if (FD.ShowDialog() == DialogResult.OK)
                    {
                        (listBoxAlbums.SelectedItem as Album).UploadPhoto(FD.FileName);
                    }
                }
            }
            catch (FacebookOAuthException)
            {
                MessageBox.Show("Authentication error. Cannot upload a photo for current user to Facebook.");
            }
        }

        private void clearPhotos()
        {
            listBoxAlbums.Items.Clear();
            listBoxPhoto.Items.Clear();
            pictureBoxPhoto.ImageLocation = null;
        }

        private void loadFriends()
        {
            listBoxFriends.Items.Clear();
            listBoxFriends.DisplayMember = "Name";

            foreach (User friend in m_FacebookUser.Friends)
            {
                listBoxFriends.Items.Add(friend);
                friend.ReFetch(DynamicWrapper.eLoadOptions.Full);
            }

            if (m_FacebookUser.Friends.Count == 0)
            {
                MessageBox.Show("No Friends to retrieve.");
            }
        }

        private void loadPages()
        {
            listBoxPages.Items.Clear();
            listBoxPages.DisplayMember = "Name";

            try
            {
                foreach (Page userLikedPage in m_FacebookUser.LikedPages)
                {
                    listBoxPages.Items.Add(userLikedPage);
                }

                if (m_FacebookUser.LikedPages.Count == 0)
                {
                    MessageBox.Show("No liked pages to retrieve.");
                }
            }
            catch (FacebookOAuthException)
            {
                MessageBox.Show("Authentication error. Cannot fetch liked pages for current user from Facebook.");
            }
        }

        private void loadEvents()
        {
            listBoxMyEvents.Items.Clear();
            listBoxMyEvents.DisplayMember = "Name";

            try
            {
                foreach (Event userEvent in m_FacebookUser.Events)
                {
                    listBoxMyEvents.Items.Add(userEvent);
                }

                if (m_FacebookUser.Events.Count == 0)
                {
                    MessageBox.Show("No Events to retrieve.");
                }
            }
            catch (FacebookOAuthException)
            {
                MessageBox.Show("Authentication error. Cannot fetch events for current user from Facebook.");
            }
        }

        private void buildMap()
        {
            
            GMapOverlay markersOverlay = new GMapOverlay("markers");
            GMap.NET.WindowsForms.Markers.GMarkerGoogle marker;

            if(checkBoxcheckins.Checked==true)
            {
                foreach (Checkin checkin in m_FacebookUser.Checkins)
                {
                    marker = new GMap.NET.WindowsForms.Markers.GMarkerGoogle(new PointLatLng(
                        checkin.Place.Location.Latitude.Value, checkin.Place.Location.Longitude.Value),
                        GMap.NET.WindowsForms.Markers.GMarkerGoogleType.green_pushpin);
                    marker.ToolTipText = checkin.Name;
                    markersOverlay.Markers.Add(marker);
                }
            }

            if (checkBoxTagedPlaces.Checked == true)
            {
                foreach (Photo photo in m_FacebookUser.PhotosTaggedIn)
                {
                    if (photo.Place != null)
                    {
                        marker = new GMap.NET.WindowsForms.Markers.GMarkerGoogle(new PointLatLng(
                        photo.Place.Location.Latitude.Value, photo.Place.Location.Longitude.Value),
                        GMap.NET.WindowsForms.Markers.GMarkerGoogleType.blue_pushpin);
                        marker.ToolTipText = photo.Name;
                        markersOverlay.Markers.Add(marker);
                    }

                }
            }
                
            if(checkBoxCurrentLocation.Checked==true)
            {
                if (m_FacebookUser.Location.Location!= null)
                {
                    marker = new GMap.NET.WindowsForms.Markers.GMarkerGoogle(new PointLatLng(
                    m_FacebookUser.Location.Location.Latitude.Value, m_FacebookUser.Location.Location.Longitude.Value),
                    GMap.NET.WindowsForms.Markers.GMarkerGoogleType.pink_pushpin);
                    marker.ToolTipText = "My current location";
                    markersOverlay.Markers.Add(marker);
                }
                    
            }

            map.Overlays.Add(markersOverlay);
        }

        private void reInitializeMap()
        {
            foreach(GMapOverlay overlay in map.Overlays)
            {
                overlay.Clear();
            }
        }
        
        private void buttonRefreshPosts_Click(object sender, EventArgs e)
        {
            loadMyPosts();
        }

        private void buttonShareMyPost_Click(object sender, EventArgs e)
        {
            sharePost();
        }

        private void buttonDeletePost_Click(object sender, EventArgs e)
        {
            deletePost();
        }

        private void buttonRefreshAlbums_Click(object sender, EventArgs e)
        {
            loadAlbums();
        }

        private void listBoxPhoto_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBoxPhoto.SelectedItem != null)
            {
                pictureBoxPhoto.ImageLocation = (listBoxPhoto.SelectedItem as Photo).PictureNormalURL;
                pictureBoxPhoto.SizeMode = PictureBoxSizeMode.StretchImage;
                labelPhotoDescription.Text = (listBoxPhoto.SelectedItem as Photo).Message;
            }
        }

        private void listBoxAlbums_SelectedIndexChanged(object sender, EventArgs e)
        {
            listBoxPhoto.Items.Clear();
            listBoxPhoto.DisplayMember = "Name";

            foreach (Photo photo in (listBoxAlbums.SelectedItem as Album).Photos)
            {
                listBoxPhoto.Items.Add(photo);
            }
        }

        private void buttonCreateAlbum_Click(object sender, EventArgs e)
        {
            createAlbum();
        }

        private void buttonUploadPhoto_Click(object sender, EventArgs e)
        {
            uploadPhoto();
        }

        private void buttonRefreshFriends_Click(object sender, EventArgs e)
        {
            loadFriends();
        }

        private void listBoxFriends_SelectedValueChanged(object sender, EventArgs e)
        {
            displaySelectedFriend();
        }

        private void displaySelectedFriend()
        {
            User friend = listBoxFriends.SelectedItem as User;
            pictureBoxFriendPicture.ImageLocation = friend.PictureNormalURL;
            pictureBoxFriendPicture.SizeMode = PictureBoxSizeMode.StretchImage;
            this.labelFriendFirstNameInfo.Text = friend.FirstName;
            this.labelFriendLastNameInfo.Text = friend.LastName;
            this.labelFriendGenderInfo.Text = friend.Gender == User.eGender.female ? "Female" : "Male";
        }

        private void buttonLoadPages_Click(object sender, EventArgs e)
        {
            loadPages();
        }

        private void buttonLoadEvents_Click(object sender, EventArgs e)
        {
            loadEvents();
        }

        private void buttonTaggedPlaces_Click(object sender, EventArgs e)
        {
            buildMap();
        }

        private void Map_Load(object sender, EventArgs e)
        {
            map.MapProvider = GMapProviders.GoogleMap;
            map.Position = new PointLatLng(32.046440, 34.759790);
            map.Zoom = 10;
        }

        private void ButtonClearMyPlaces_Click(object sender, EventArgs e)
        {
            reInitializeMap();
        }
    }
}
