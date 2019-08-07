using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using System.Text;
using Facebook;
using FacebookWrapper;
using FacebookWrapper.ObjectModel;

namespace FacebookApplication
{
    public partial class MainForm : Form
    {
        private User m_FacebookUser;
        private Settings m_Settings;
        private bool m_chartDisplayed;

        public MainForm()
        {
            InitializeComponent();
            m_chartDisplayed = false;
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
            "1201096130063323",
            "email",
            "user_posts",
            "user_friends",
            "user_likes",
            "user_photos",
            "user_events",
            "user_birthday",
            "user_location",
            "user_gender");

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
            reInitializeChart();
            clearKnowYourFriends();
            makeButtonsEnabled(false);
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
            buttonCountPosts.Enabled = i_IsButtonEnabled;
            buttonClearChart.Enabled = i_IsButtonEnabled;
            buttonKnowYourFriends.Enabled = i_IsButtonEnabled;
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

        private void buildChart()
        {
            foreach (Post post in m_FacebookUser.Posts)
            {
                int hour = getHour(post.UpdateTime);
                double[] yValuesArray = new double[4];
                yValuesArray = chartYourActivity.Series["Series1"].Points[hour].YValues;
                yValuesArray[0]++;
                chartYourActivity.Series["Series1"].Points[hour].YValues = yValuesArray;
            }
        }

        private void countPosts()
        {
            if (m_chartDisplayed)
            {
                MessageBox.Show("Please clean the chart first.");
            }
            else
            {
                buildChart();
                m_chartDisplayed = true;
            }
        }

        private void reInitializeChart()
        {
            foreach (DataPoint arr in chartYourActivity.Series["Series1"].Points)
            {
                arr.YValues = new double[4];
            }

            m_chartDisplayed = false;
        }

        private int getHour(DateTime? i_UpdateTime)
        {
            int hour = 0;
            string strHour = i_UpdateTime.ToString();
            StringBuilder stringBuilder = new StringBuilder();
            for (int i = 0; i < strHour.Length; i++)
            {
                if (strHour[i] == ':')
                {
                    if (!strHour[i - 2].Equals(" "))
                    {
                        stringBuilder.Append(strHour[i - 2]);
                    }

                    stringBuilder.Append(strHour[i - 1]);
                    hour = Convert.ToInt32(stringBuilder.ToString());
                    if (strHour[strHour.Length - 2].Equals('P'))
                    {
                        if (hour != 12)
                        {
                            hour += 12;
                        }
                    }
                    else
                    {
                        if (hour == 12)
                        {
                            hour = 0;
                        }
                    }

                    break;
                }
            }

            return hour;
        }

        private void knowYourFriends()
        {
            Dictionary<string, int> userFriendsNumberOfFriends = new Dictionary<string, int>(m_FacebookUser.Friends.Count);
            Dictionary<string, string> userFriendsIDS = new Dictionary<string, string>();
            int maxMutualFriends = 0;
            List<string> listOfConnectedFriends;

            foreach (User friend in m_FacebookUser.Friends)
            {
                userFriendsNumberOfFriends.Add(friend.Id, maxMutualFriends);
                userFriendsIDS.Add(friend.Id, friend.Name);
            }

            foreach (User friend in m_FacebookUser.Friends)
            {
                foreach (User friendOfFriend in friend.Friends)
                {
                    if (userFriendsNumberOfFriends.ContainsKey(friendOfFriend.Id))
                    {
                        userFriendsNumberOfFriends[friendOfFriend.Id]++;
                        if (maxMutualFriends <= userFriendsNumberOfFriends[friendOfFriend.Id])
                        {
                            maxMutualFriends = userFriendsNumberOfFriends[friendOfFriend.Id];
                        }
                    }
                }
            }

            listOfConnectedFriends = getlistOfConnectedFriends(userFriendsNumberOfFriends, userFriendsIDS, maxMutualFriends);
            addItemsToListBoxKnowYourFriends(listOfConnectedFriends);
            labelNumberOfConnectedFriendsInfo.Text = maxMutualFriends.ToString();
        }

        private List<string> getlistOfConnectedFriends(
            Dictionary<string, int> i_UserFriendsNumberOfFriends,
            Dictionary<string, string> i_UserFriendsIDS,
            int i_MaxMutualFriends)
        {
            List<string> listOfConnectedFriends = new List<string>();

            foreach (string id in i_UserFriendsNumberOfFriends.Keys)
            {
                if (i_UserFriendsNumberOfFriends[id] == i_MaxMutualFriends)
                {
                    listOfConnectedFriends.Add(i_UserFriendsIDS[id]);
                }
            }

            return listOfConnectedFriends;
        }

        private void addItemsToListBoxKnowYourFriends(List<string> i_ListOfConnectedFriends)
        {
            foreach (string friendName in i_ListOfConnectedFriends)
            {
                listBoxKnowYourFriends.Items.Add(friendName);
            }
        }

        private void clearKnowYourFriends()
        {
            listBoxKnowYourFriends.Items.Clear();
            labelNumberOfConnectedFriendsInfo.Text = string.Empty;
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

        private void buttonCountPosts_Click(object sender, EventArgs e)
        {
            countPosts();
        }

        private void buttonClearChart_Click(object sender, EventArgs e)
        {
            reInitializeChart();
        }

        private void buttonKnowYourFriends_Click(object sender, EventArgs e)
        {
            knowYourFriends();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {

        }

        private void labelCredit_Click(object sender, EventArgs e)
        {

        }

        private void labelYourActivityTitle_Click(object sender, EventArgs e)
        {

        }
    }
}
