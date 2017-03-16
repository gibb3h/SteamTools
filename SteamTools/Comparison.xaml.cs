using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using SteamTools.Annotations;
using SteamTools.Classes;
using Xceed.Wpf.Toolkit.Primitives;

namespace SteamTools
{
    /// <summary>
    /// Interaction logic for Comparison.xaml
    /// </summary>
    public partial class Comparison
    {
        private List<Game> Games { get; set; }
        public List<User> AllUsers { get; set; }
        public List<CompGame> OrigGames { get; set; }

        private ObservableCollection<CompGame> _allGames { get; set; }
        private ObservableCollection<CompTag> _allTags { get; set; }
        private ObservableCollection<CompTag> _allUserNames { get; set; }
        private CompGame _selectedGame { get; set; }
        public ObservableCollection<CompGame> AllGames
        {
            get { return _allGames ?? (_allGames = new ObservableCollection<CompGame>()); }
        }

        public ObservableCollection<CompTag> AllTags
        {
            get { return _allTags ?? (_allTags = new ObservableCollection<CompTag>()); }
        }

        public ObservableCollection<CompTag> AllUserNames
        {
            get { return _allUserNames ?? (_allUserNames = new ObservableCollection<CompTag>()); }
        }

        public CompGame SelectedGame
        {
            get { return _selectedGame ?? new CompGame(); }
            set
            {
                if (_selectedGame == null || value == null || value.AppId == _selectedGame.AppId)
                {
                    _selectedGame = new CompGame();
                    return;                
                }
                   
                _selectedGame = value;
                ShowScreenShots(_selectedGame);
            }
        }

        public Comparison(List<Game> allGames, List<User> allUsers)
        {
            Games = allGames;

            _allGames = new ObservableCollection<CompGame>();
            var allIds = allUsers.SelectMany(u => u.Games).GroupBy(g => g).Select(g => g.First()).ToList();
            var userGames = allGames.Where(g => allIds.Contains(g.AppId)).ToList();

            userGames.Sort(delegate(Game g1, Game g2)
                {
                    var g1Count = allUsers.Count(u => u.Games.Contains(g1.AppId));
                    var g2Count = allUsers.Count(u => u.Games.Contains(g2.AppId));

                    return g2Count.CompareTo(g1Count);
                });

            foreach (var id in userGames)
            {
                var gameUsers = allUsers.Where(u => u.Games.Any(g => g.Equals(id.AppId))).ToList();
                _allGames.Add(new CompGame
                    {
                        AppId = id.AppId,
                        Logo = id.Logo,
                        Name = id.Name,
                        Tags = id.Tags,
                        Users = gameUsers
                    });
            }

            OrigGames = new List<CompGame>(_allGames);
            AllUsers = allUsers;
            DataContext = this;
            var gameTags = GetUserGameIds();

            _allTags = new ObservableCollection<CompTag>();
            var tmp = _allGames.Where(g => gameTags.Contains(g.AppId)).SelectMany(g => g.Tags).Distinct();
            foreach (var a in tmp)
            {
                _allTags.Add(new CompTag
                    {
                        Count = _allGames.Count(g => g.Tags.Contains(a)),
                        Name = a
                    });
            }

            _allUserNames = new ObservableCollection<CompTag>();
            var tmp2 = allUsers.Select(u => u.Name);
            foreach (var a in tmp2)
            {
                _allUserNames.Add(new CompTag
                    {
                        Count = _allGames.SelectMany(g => g.Users.Where(u => u.Name.Equals(a))).Count(),
                        Name = a
                    });
            }

            InitializeComponent();
        }

        private List<int> GetUserGameIds()
        {
            var allGameIds = _allGames.Select(f => f.AppId).Distinct().ToList();
            var userGameIds = AllUsers.SelectMany(u => u.Games).Distinct().ToList();
            allGameIds = allGameIds.Where(userGameIds.Contains).ToList();
            allGameIds.Sort();
            return allGameIds;
        }

        private void TagsDropDown_OnItemSelectionChanged(object sender, ItemSelectionChangedEventArgs e)
        {
            var k = (from CompTag a in UsersDropDown.SelectedItems select a.Name).ToList();
            var j = (from CompTag a in TagsDropDown.SelectedItems select a.Name).ToList();
            _allGames.Clear();

            foreach (var g in OrigGames.Where(compGame =>
                                              j.All(t => compGame.Tags.Contains(t)) &&
                                              k.All(s => compGame.Users.Select(u => u.Name).ToList().Contains(s))))
            {
                _allGames.Add(g);
            }

            foreach (var tag in _allTags)
            {
                tag.Count = _allGames.Count(g => g.Tags.Contains(tag.Name));
                tag.IsZero = tag.Count.Equals(0);
            }

            foreach (var user in _allUserNames)
            {
                user.Count = _allGames.SelectMany(g => g.Users.Where(u => u.Name.Equals(user.Name))).Count();
                user.IsZero = user.Count.Equals(0);
            }
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            Renderer.Render(AllUsers.ToList(), Games);
        }

        private void ShowScreenShots(CompGame game)
        {
            var ssg = new ScreenShotGallery(game.AppId, game.Users);
            if (ssg.ScreenShots.Any())
                ssg.ShowDialog();
            else
                MessageBox.Show("No screenshots for this game!");
        }
    }

#region internalClasses
    public class CompUser
    {
        public string Name { get; set; }
        public int Count { get; set; }
    }

    public class CompTag : INotifyPropertyChanged
    {
        private int _count;
        private bool _isZero;
        public string Name { get; set; }

        public bool IsZero
        {
            get { return _isZero; }
            set
            {
                if (_isZero != value)
                {
                    _isZero = value;
                    OnPropertyChanged();
                }
            }
        }

        public int Count
        {
            get { return _count; }
            set
            {
                if (_count != value)
                {
                    _count = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        public override string ToString()
        {
            return Name;
        }
    }

    public class CompGame
    {
        public int AppId { get; set; }
        public string Logo { get; set; }
        public string Name { get; set; }
        public List<string> Tags { get; set; }
        public List<User> Users { get; set; }
    }

#endregion
}