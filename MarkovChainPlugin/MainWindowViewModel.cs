using GroupMeClientApi.Models;
using GroupMeClientPlugin;
using GroupMeClientPlugin.GroupChat;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Toolkit.Mvvm.ComponentModel;

namespace GroupPluginDemoWPF_MVVM
{
    class MainWindowViewModel : ObservableObject
    {
        private string inputText;
        private string outputText;
        private bool currentGroupChatOnly;
        private Dictionary<GlobalUser, Dictionary<string, List<string>>> phraseDictionary;
        private Dictionary<GlobalUser, Dictionary<string, List<string>>> phraseDictionaryLocal;
        private GlobalUser selectedPerson;

        public ICommand RegenerateOutput { get; }
        public ICommand CopyOutput { get; }

        public MainWindowViewModel(IMessageContainer groupChat, CacheSession cacheSession, IPluginUIIntegration uIIntegration)
        {
            this.GroupChat = groupChat;
            this.CacheSession = cacheSession;
            this.UIIntegration = UIIntegration;

            var mostRecentGroupMessage = this.CacheSession.CacheForGroupOrChat.OrderByDescending(m => m.CreatedAtUnixTime).First();
            var mostRecentGlobalMessage = this.CacheSession.GlobalCache.OrderByDescending(m => m.CreatedAtUnixTime).First();

            this.MaxNumWords = 100;
            this.outputText = "";
            this.phraseDictionary = new Dictionary<GlobalUser, Dictionary<string, List<string>>>();
            this.phraseDictionaryLocal = new Dictionary<GlobalUser, Dictionary<string, List<string>>>();
            this.RegenerateOutput = new Microsoft.Toolkit.Mvvm.Input.RelayCommand(this.RegenerateChain);
            this.CopyOutput = new Microsoft.Toolkit.Mvvm.Input.RelayCommand(this.CopyOutputChain);
        }

        /// <summary>
        /// Represents a particular member of a <see cref="Group"/> or <see cref="Chat"/>.
        /// </summary>
        public class GlobalUser
        {
            /// <summary>
            /// Gets or sets the <see cref="Name"/> of the <see cref="GlobalUser"/>.
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// Gets or sets the <see cref="Id"/> of the <see cref="GlobalUser"/>.
            /// </summary>
            public string Id { get; set; }
        }

        /// <summary>
        /// Gets or sets the boolean value for using messages from all <see cref="Group"/>s/<see cref="Chat"/>s or only the current Group/Chat.
        /// A true value represents using the current <see cref="Group"/>/<see cref="Chat"/> only.
        /// </summary>
        public bool CurrentGroupChatOnly
        {
            get => this.currentGroupChatOnly;
            set => this.SetProperty(ref this.currentGroupChatOnly, value);
        }

        /// <summary>
        /// Gets the list of all members in the <see cref="GlobalUser"/>'s <see cref="Group"/>s and <see cref="Chat"/>s.
        /// </summary>
        public IEnumerable<GlobalUser> AllMembers
        {
            get
            {
                var allMembers = new List<GlobalUser>();

                foreach (Chat chat in GroupChat.Client.Chats())
                {
                    if (allMembers.Where(m => m.Id == chat.Id).Any() == false)
                    {
                        allMembers.Add(new GlobalUser { Name = chat.OtherUser.Name, Id = chat.Id });
                    }
                }

                foreach (Group group in GroupChat.Client.Groups())
                {
                    foreach (var member in group.Members)
                    {
                        if (allMembers.Where(mem => mem.Id == member.UserId).Any() == false)
                        {
                            allMembers.Add(new GlobalUser { Name = member.Name, Id = member.UserId });
                        }
                    }
                }

                var allMembersSorted = allMembers.OrderBy(s => s.Name).ToList();
                return allMembersSorted;
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="GlobalUser"/> selected for whom to generate a Markov Chain.
        /// Selected by the Combobox in the Plugin UI.
        /// </summary>
        public GlobalUser SelectedPerson
        {
            get => this.selectedPerson;

            set
            {
                this.SetProperty(ref this.selectedPerson, value);
                this.UpdatePhraseDictionary(value);
            }
        }

        /// <summary>
        /// Gets or sets the text entered from which to begin the Markov Chain.
        /// </summary>
        public string InputText
        {
            get => this.inputText;

            set
            {
                this.SetProperty(ref this.inputText, value);
                this.OutputText = this.MakeMarkovChain();
            }
        }

        /// <summary>
        /// Gets or sets the generated Markov Chain.
        /// </summary>
        public string OutputText
        {
            get => this.outputText;
            set => this.SetProperty(ref this.outputText, value);
        }

        /// <summary>
        /// Gets or sets the maximum number of words in the Markov Chain.
        /// </summary>
        public int MaxNumWords { get; set; }

        /// <summary>
        /// If the <see cref="SelectedPerson"/> is not present in <see cref="phraseDictionary"/>, iterate through all <see cref="Group"/>s and <see cref="Chat"/>s
        /// and retrieve text from all all messages sent by the <see cref="SelectedPerson"/>. Updates both <see cref="phraseDictionary"/> and <see cref="phraseDictionaryLocal"/>.
        /// </summary>
        /// <param name="person"></param>
        private void UpdatePhraseDictionary(GlobalUser person)
        {
            if (!phraseDictionary.ContainsKey(person))
            {
                List<string> messagesGlobal = new List<string>();
                List<string> messagesLocal = new List<string>();

                // Retrieve all messages containing text from all groups shared between GMDC user and SelectedPerson and from a chat thread, if it exists
                messagesGlobal = this.CacheSession.GlobalCache
                .Where(m => m.UserId == person.Id)
                .Select(m => m.Text)
                .Where(t => !string.IsNullOrEmpty(t))
                .ToList();
                phraseDictionary[person] = new Dictionary<string, List<string>>();

                // Retrieve all messages containing text from the current group shared with SelectedPerson or the current chat with SelectedPerson
                messagesLocal = this.CacheSession.CacheForGroupOrChat
                .Where(m => m.UserId == person.Id)
                .Select(m => m.Text)
                .Where(t => !string.IsNullOrEmpty(t))
                .ToList();
                phraseDictionaryLocal[person] = new Dictionary<string, List<string>>();

                this.MakePairs(person, messagesGlobal, messagesLocal);
            }
        }

        /// <summary>
        /// Iterates word-by-word through all messages sent by the <see cref="SelectedPerson"/> and updates dictionary of key/value pairs for Markov Chain generation.
        /// For a pair, the key is the word in the message immediately preceding the word that is the value.
        /// For accuracy, phrases were incorporated in the pairing process. Here, the key is all contents of the message from the beginning and ending at the word before the current word that is the value.
        /// </summary>
        /// <param name="person">The person for which to generate the Markov Chain.
        /// <param name="messagesGlobal">Messages of the person retrieved from all <see cref="Group"/>s and <see cref="Chat"/>s.</param>
        /// <param name="messagesLocal">Messages of the person retrieved from only the current <see cref="Group"/> or <see cref="Chat"/>.</param>
        private void MakePairs(GlobalUser person, List<string> messagesGlobal, List<string> messagesLocal)
        {
            foreach (var message in messagesGlobal)
            {
                var words = message.Split(' ');
                // add each word and word immediately following it as a key/value pair
                for (int i = 0; i <= words.Length - 2; i++)
                {
                    if (this.phraseDictionary[person].ContainsKey(words[i]))
                    {
                        this.phraseDictionary[person][words[i]].Add(words[i + 1]);
                    }
                    else
                    {
                        this.phraseDictionary[person][words[i]] = new List<string>();
                        this.phraseDictionary[person][words[i]].Add(words[i + 1]);
                    }

                    // add the part of the message immediately preceding the word and the single following word as a key/value pair
                    if (i > 0)
                    {
                        string phrase = string.Join(" ", words, 0, i+1);
                        if (this.phraseDictionary[person].ContainsKey(phrase))
                        {
                            this.phraseDictionary[person][phrase].Add(words[i + 1]);
                        }
                        else
                        {
                            this.phraseDictionary[person][phrase] = new List<string>();
                            this.phraseDictionary[person][phrase].Add(words[i + 1]);
                        }
                    }
                }
            }

            foreach (var message in messagesLocal)
            {
                var words = message.Split(' ');
                // add each word and word immediately following it as a key/value pair
                for (int i = 0; i <= words.Length - 2; i++)
                {
                    if (this.phraseDictionaryLocal[person].ContainsKey(words[i]))
                    {
                        this.phraseDictionaryLocal[person][words[i]].Add(words[i + 1]);
                    }
                    else
                    {
                        this.phraseDictionaryLocal[person][words[i]] = new List<string>();
                        this.phraseDictionaryLocal[person][words[i]].Add(words[i + 1]);
                    }

                    // add the part of the message immediately preceding the word and the single following word as a key/value pair
                    if (i > 0)
                    {
                        string phrase = string.Join(" ", words, 0, i+1);
                        if (this.phraseDictionaryLocal[person].ContainsKey(phrase))
                        {
                            this.phraseDictionaryLocal[person][phrase].Add(words[i + 1]);
                        }
                        else
                        {
                            this.phraseDictionaryLocal[person][phrase] = new List<string>();
                            this.phraseDictionaryLocal[person][phrase].Add(words[i + 1]);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Generates the Markov Chain from either <see cref="phraseDictionary"/> or <see cref="phraseDictionaryLocal"/>.
        /// </summary>
        /// <returns></returns>
        private string MakeMarkovChain()
        {
            // ignore empty input
            if (string.IsNullOrEmpty(this.inputText))
            {
                return "";
            }
            Random r = new Random();
            List<string> markovChain = new List<string>()
            {
                this.inputText
            };

            if (this.CurrentGroupChatOnly)
            {
                for (int i = 0; i < this.MaxNumWords; i++)
                {
                    string currentChain = string.Join(" ", markovChain);

                    // see if current Markov Chain phrase is in the phrase dictionary
                    try
                    {
                        markovChain.Add(this.phraseDictionaryLocal[this.SelectedPerson][currentChain][r.Next(0, this.phraseDictionaryLocal[this.SelectedPerson][currentChain].Count)]);
                    }

                    // if not, only consider the last word
                    // for performance reasons, only the leading phrase was considered, where once the chain was broken the phrase technique will likely not find any key in the dictionary
                    // there could be potential for more "realistic" outputs if generation of the chain from this point was considered as the start of a new phrase to query in the dictionary
                    catch (KeyNotFoundException exceptionKeyOuter)
                    {
                        try
                        {
                            string[] chain = currentChain.Split(' ');       // necessary for input text containing multiple words (so cannot just use this.markovChain)
                            markovChain.Add(this.phraseDictionaryLocal[this.SelectedPerson][chain.Last()][r.Next(0, this.phraseDictionaryLocal[this.SelectedPerson][chain.Last()].Count)]);
                        }
                        // if there are no words that follow the last word in the phrase, end chain generation
                        catch (KeyNotFoundException exceptionKeyInner)
                        {
                            break;
                        }
                    }
                    catch (IndexOutOfRangeException indexException)
                    {
                        break;
                    }
                }
                return string.Join(" ", markovChain);
            }
            else
            {
                for (int i = 0; i < this.MaxNumWords; i++)
                {
                    string currentChain = string.Join(" ", markovChain);
                    // see if current Markov Chain phrase is in the phrase dictionary
                    try
                    {
                        markovChain.Add(this.phraseDictionary[this.SelectedPerson][currentChain][r.Next(0, this.phraseDictionary[this.SelectedPerson][currentChain].Count)]);
                    }
                    // if not, only consider the last word
                    // for performance reasons, only the leading phrase was considered, where once the chain was broken the phrase technique will likely not find any key in the dictionary
                    // there could be potential for more "realistic" outputs if generation of the chain from this point was considered as the start of a new phrase to query in the dictionary
                    catch (KeyNotFoundException exceptionKeyOuter)
                    {
                        try
                        {
                            string[] chain = currentChain.Split(' ');       // necessary for input text containing multiple words (so cannot just use this.markovChain)
                            markovChain.Add(this.phraseDictionary[this.SelectedPerson][chain.Last()][r.Next(0, this.phraseDictionary[this.SelectedPerson][chain.Last()].Count)]);
                        }
                        // if there are no words that follow the last word in the phrase, end chain generation
                        catch (KeyNotFoundException exceptionKeyInner)
                        {
                            break;
                        }
                    }
                    catch (IndexOutOfRangeException indexException)
                    {
                        break;
                    }
                }
                return string.Join(" ", markovChain);
            }
        }

        /// <summary>
        /// Binding for the "Regenerate" button in the UI, initiates generation of a new Markov Chain.
        /// </summary>
        private void RegenerateChain()
        {
            this.OutputText = MakeMarkovChain();
        }

        /// <summary>
        /// Binding for the "Copy" button in the UI, copies <see cref="outputText"/> to the clipboard.
        /// </summary>
        private void CopyOutputChain()
        {
            System.Windows.Clipboard.SetText(this.OutputText);
        }

        /// <summary>
        /// Gets the <see cref="IMessageContainer"/> used to hold a <see cref="Group"/> or <see cref="Chat"/>.
        /// </summary>
        private IMessageContainer GroupChat { get; }

        /// <summary>
        /// Gets the <see cref="CacheSession"/> to use for message retrieval.
        /// </summary>
        private CacheSession CacheSession { get; }

        /// <summary>
        /// Gets the <see cref="IPluginUIIntegration"/> for Plugin UI support.
        /// </summary>
        private IPluginUIIntegration UIIntegration { get; }
    }
}