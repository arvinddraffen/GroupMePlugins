using GroupMeClientApi.Models;
using GroupMeClientApi.Models.Attachments;
using GroupMeClientPlugin;
using GroupMeClientPlugin.GroupChat;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.Wpf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.AccessControl;
using System.Web.UI.WebControls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using Microsoft.Toolkit.Mvvm.ComponentModel;

namespace GroupMeStatsPlugin
{
    class StatisticData
    {
        public int messagesSent { get; set; }
        public int likesGiven { get; set; }
        public int likesReceived { get; set; }
        public int selfLikes { get; set; }
        public int imagesSent { get; set; }
        public int percentMessagesLiked { get; set; }
        public int wordsSent { get; set; }
        public int totalLikes { get; set; }     // only for total

        public StatisticData()
        {
            messagesSent = 0;
            likesGiven = 0;
            likesReceived = 0;
            selfLikes = 0;
            imagesSent = 0;
            percentMessagesLiked = 0;
            wordsSent = 0;
            totalLikes = 0;
        }
    }

    class MainWindowViewModel : ObservableObject
    {
        GlobalUser total = new GlobalUser { Name = "Total", Id = "total" };
        private Dictionary<GlobalUser, StatisticData> userStatistics;
        private Dictionary<GlobalUser, Dictionary<string, int>> popularWords;
        private Dictionary<GlobalUser, Dictionary<string, int>> messageTimes;
        private int totalMessages;
        private GlobalUser selectedPerson;
        private PlotModel userStatisticsModel;
        private PlotModel popularWordsModel;
        private PlotModel messageTimesModel;

        public ICommand RegenerateOutput { get; }

        public MainWindowViewModel(IMessageContainer groupChat, CacheSession cacheSession, IPluginUIIntegration uIIntegration)
        {
            this.GroupChat = groupChat;
            this.CacheSession = cacheSession;
            this.UIIntegration = UIIntegration;
            this.userStatisticsModel = new PlotModel();
            this.popularWordsModel = new PlotModel();
            this.messageTimesModel = new PlotModel();

            //this.RegenerateOutput = new RelayCommand(this.CalculateStatistics);
            this.RegenerateOutput = new Microsoft.Toolkit.Mvvm.Input.RelayCommand(()
               =>
           {
               this.CalculateStatistics();
               this.SetupGraphModels();
           });
        }

        /// <summary>
        /// Represents a particular member of a <see cref="Group"/> or <see cref="Chat"/>.
        /// </summary>
        public class GlobalUser : IEquatable<GlobalUser>, IComparable<GlobalUser>
        {
            /// <summary>
            /// Gets or sets the <see cref="Name"/> of the <see cref="GlobalUser"/>.
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// Gets or sets the <see cref="Id"/> of the <see cref="GlobalUser"/>.
            /// </summary>
            public string Id { get; set; }

            public int CompareTo(GlobalUser other)
            {
                return this.Id.CompareTo(other.Id);
            }

            public bool Equals(GlobalUser other)
            {
                return (other.Id == this.Id);
            }

            public override bool Equals(object obj)
            {
                if (obj is GlobalUser gu)
                {
                    return this.Equals(gu);
                }
                else
                {
                    return base.Equals(obj);
                }
            }

            public override int GetHashCode()
            {
                int hashCode = 1460282102;
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(this.Name);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(this.Id);
                return hashCode;
            }
        }

        public IEnumerable<GlobalUser> AllMembers
        {
            get
            {
                var allMembers = new List<GlobalUser>();
                if (GroupChat is Group group)
                {
                    foreach (var member in group.Members)
                    {
                        if (allMembers.Where(mem => mem.Id == member.UserId).Any() == false)
                        {
                            allMembers.Add(new GlobalUser { Name = member.Name, Id = member.UserId });
                        }
                    }
                }
                else if (GroupChat is Chat chat)
                {
                    allMembers.Add(new GlobalUser { Name = chat.OtherUser.Name, Id = chat.OtherUser.Id });
                    allMembers.Add(new GlobalUser { Name = GroupChat.Client.WhoAmI().Name, Id = GroupChat.Client.WhoAmI().Id });
                }

                allMembers.Add(total);
                var allMembersSorted = allMembers.OrderBy(s => s.Name).ToList();
                return allMembersSorted;
            }
        }

        private void InitializeDictionary()
        {
            userStatistics = new Dictionary<GlobalUser, StatisticData>();
            popularWords = new Dictionary<GlobalUser, Dictionary<string, int>>();
            messageTimes = new Dictionary<GlobalUser, Dictionary<string, int>>();
            foreach (var member in this.AllMembers)
            {
                userStatistics[member] = new StatisticData();
                popularWords[member] = new Dictionary<string, int>();
                messageTimes[member] = new Dictionary<string, int>();
            }
            userStatistics[total] = new StatisticData();
            popularWords[total] = new Dictionary<string, int>();
            messageTimes[total] = new Dictionary<string, int>();
        }

        private void CalculateStatistics()
        {
            this.InitializeDictionary();

            var membersToAnalyze = this.AllMembers
                .Where(m => m.Id != total.Id)
                .ToList();

            foreach (var member in membersToAnalyze)
            {
                var messagesSent = this.CacheSession.CacheForGroupOrChat
                    .Where(m => m.UserId == member.Id)
                    .ToList();
                var messagesWithText = messagesSent
                    .Where(t => !string.IsNullOrEmpty(t.Text))
                    .ToList();
                var messagesWithImage = messagesSent
                    .Where(t => t.Attachments.Count > 0)
                    .ToList();
                var numMessagesLiked = messagesSent
                    .Where(l => l.FavoritedBy.Count > 0)
                    .Count();
                // add function for calculating up-to-date likes

                var messagesNotSentByUser = this.CacheSession.CacheForGroupOrChat
                    .Where(m => m.UserId != member.Id)
                    .ToList();

                var times = messagesSent
                    .Select(t => t.CreatedAtTime)
                    .ToList();

                if (!userStatistics.ContainsKey(member))
                {
                    Debug.WriteLine($"Member: {member.Id}");
                    List<string> list = userStatistics
                        .Select(t => t.Key.Id)
                        .ToList();
                    list.ForEach(t => Debug.WriteLine(t));
                }

                var test = userStatistics[member];
                //new Exception("Exception");

                userStatistics[member].messagesSent = messagesSent.Count;
                foreach (var message in messagesWithText)
                {
                    userStatistics[member].likesReceived += message.FavoritedBy.Count;
                    if (message.FavoritedBy.Contains(GroupChat.Client.WhoAmI().Id))
                    {
                        userStatistics[member].selfLikes++;
                        userStatistics[member].likesGiven++;
                    }
                    string[] words = message.Text.Split(' ');
                    userStatistics[member].wordsSent += words.Length;
                    foreach (var word in words)
                    {
                        if (popularWords[member].ContainsKey(word))
                        {
                            popularWords[member][word]++;
                        }
                        else
                        {
                            popularWords[member][word] = 1;     // first instance
                        }

                        if (popularWords[total].ContainsKey(word))
                        {
                            popularWords[total][word]++;
                        }
                        else
                        {
                            popularWords[total][word] = 1;
                        }
                    }
                }
                foreach (var message in messagesWithImage)
                {
                    if (message.Attachments.OfType<ImageAttachment>().Any())
                    {
                        userStatistics[member].imagesSent++;
                    }
                }
                foreach (var message in messagesNotSentByUser)
                {
                    if (message.FavoritedBy.Contains(GroupChat.Client.WhoAmI().Id))
                    {
                        userStatistics[member].likesGiven++;
                    }
                }
                foreach (var time in times)
                {
                    if (messageTimes[member].ContainsKey(time.ToString("HH")))
                    {
                        messageTimes[member][time.ToString("HH")]++;
                    }
                    else
                    {
                        messageTimes[member][time.ToString("HH")] = 1;
                    }

                    if (messageTimes[total].ContainsKey(time.ToString("HH")))
                    {
                        messageTimes[total][time.ToString("HH")]++;
                    }
                    else
                    {
                        messageTimes[total][time.ToString("HH")] = 1;
                    }
                }

                userStatistics[member].percentMessagesLiked = (int)Math.Round((double)numMessagesLiked / userStatistics[member].messagesSent * 100, 2);

                userStatistics[total].totalLikes += userStatistics[member].likesGiven;
                userStatistics[total].selfLikes += userStatistics[member].selfLikes;
                userStatistics[total].wordsSent += userStatistics[member].wordsSent;
                userStatistics[total].imagesSent += userStatistics[member].imagesSent;
            }
            userStatistics[total].messagesSent = this.CacheSession.CacheForGroupOrChat.Count();
            var numLikedMessages = this.CacheSession.CacheForGroupOrChat
                .AsEnumerable()
                .Where(l => l.FavoritedBy.Count > 0)
                .Count();

            userStatistics[total].percentMessagesLiked = (int)Math.Round((double)numLikedMessages / userStatistics[total].messagesSent * 100, 2);
        }

        public GlobalUser SelectedPerson
        {
            get => this.selectedPerson;

            set
            {
                this.SetProperty(ref this.selectedPerson, value);
            }
        }

        public PlotModel UserStatisticsModel
        {
            get => this.userStatisticsModel;
            set => this.SetProperty(ref this.userStatisticsModel, value);
        }

        public PlotModel PopularWordsModel
        {
            get => this.popularWordsModel;
            set => this.SetProperty(ref this.popularWordsModel, value);
        }

        public PlotModel MessageTimesModel
        {
            get => this.messageTimesModel;
            set => this.SetProperty(ref this.messageTimesModel, value);
        }

        private void SetupGraphModels()
        {
            // setup general statistics
            if (userStatisticsModel.Series.Count > 0)
            {
                userStatisticsModel.Series.Clear();
            }

            var barSeries = new OxyPlot.Series.BarSeries
            {
                ItemsSource = new List<BarItem>(new[]
                {
                    new BarItem{ Value = userStatistics[selectedPerson].messagesSent },
                    new BarItem{ Value = userStatistics[selectedPerson].likesReceived},
                    new BarItem{ Value = userStatistics[selectedPerson].selfLikes},
                    new BarItem{ Value = userStatistics[selectedPerson].likesGiven},
                    new BarItem{ Value = userStatistics[selectedPerson].totalLikes},
                    new BarItem{ Value = userStatistics[selectedPerson].percentMessagesLiked},
                    new BarItem{ Value = userStatistics[selectedPerson].imagesSent},
                    new BarItem{ Value = userStatistics[selectedPerson].wordsSent}
                }),
                LabelPlacement = LabelPlacement.Base
            };
            
            UserStatisticsModel.Series.Add(barSeries);

            UserStatisticsModel.Axes.Add(new OxyPlot.Axes.CategoryAxis
            {
                Position = AxisPosition.Bottom,
                Key = "GeneralStatsAxis",
                ItemsSource = new[]
                {
                    "Messages Sent",
                    "Likes Received",
                    "Self Likes",
                    "Likes Given",
                    "Total Likes",
                    "Percent Messages Liked",
                    "Images Sent",
                    "Words Sent"
                }
            });
            UserStatisticsModel.Axes.Add(new OxyPlot.Axes.LinearAxis
            {
                Position = AxisPosition.Left,
                MinimumPadding = 0,
                MaximumPadding = 0.06,
                AbsoluteMinimum = 0
            });
        }

        private IMessageContainer GroupChat { get; }

        private CacheSession CacheSession { get; }

        private IPluginUIIntegration UIIntegration { get; }
    }
}