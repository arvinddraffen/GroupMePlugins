using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using GroupMeClientPlugin.MessageCompose;

namespace LetterSwapPlugin
{
    public class LetterSwapPlugin : GroupMeClientPlugin.PluginBase, IMessageComposePlugin
    {
        public string EffectPluginName => this.PluginDisplayName;

        public override string PluginDisplayName => "Letter Swap";

        public override string PluginVersion => Assembly.GetExecutingAssembly().GetName().Version.ToString();

        public override Version ApiVersion => new Version(2, 0, 0);

        public Task<MessageSuggestions> ProvideOptions(string typedMessage)
        {
            var results = new MessageSuggestions();
            results.TextOptions.Add(this.DoLetterSwap(typedMessage));
            results.TextOptions.Add(this.DoLetterSwapReverse(typedMessage));
            results.TextOptions.Add(this.DoGSSwap(typedMessage));
            results.TextOptions.Add(this.DoBlahsBlahforBlah(typedMessage));
            results.TextOptions.Add(this.DoBlaherFile(typedMessage));
            return Task.FromResult(results);
        }

        private string DoLetterSwap(string text)
        {
            List<string> leadingCharacters = new List<string>();
            List<char> vowels = new List<char>()
            {
                'a', 'e', 'i', 'o', 'u', 'y'
            };
            string[] originalWords = text.Split(' ');
            List<string> words = new List<string>();
            foreach (string word in originalWords)
            {
                if (!string.IsNullOrEmpty(word))
                {
                    words.Add(word);
                }
            }

            foreach (string word in words)
            {
                for (int i = 0; i < word.Length; i++)
                {
                    if (vowels.Contains(char.ToLower(word[i])))
                    {
                        if (char.ToLower(word[i]) == 'y')
                        {
                            if (i == word.Length - 1)
                            {
                                leadingCharacters.Add(word.Substring(0, i));
                            }
                            else
                            {
                                if (vowels.Contains(char.ToLower(word[i + 1])))
                                {
                                    leadingCharacters.Add(word[i].ToString());
                                    break;
                                }
                                else
                                {
                                    leadingCharacters.Add(word.Substring(0, i));
                                    break;
                                }
                            }
                        }
                        else
                        {
                            leadingCharacters.Add(word.Substring(0, i));
                            break;
                        }
                    }
                    else if (vowels.Contains(char.ToLower(word[0])))
                    {
                        leadingCharacters.Add(word[0].ToString());
                        break;
                    }
                    else if (i == word.Length - 1)
                    {
                        leadingCharacters.Add(word);
                        break;
                    }
                }
            }

            int startIndex, endIndex;
            for (int i = 0; i < words.Count; i++)
            {
                startIndex = leadingCharacters[i].Length;
                endIndex = words[i].Length - startIndex;
                words[i] = words[i].Substring(startIndex, endIndex);
                if (i == 0)
                {
                    words[i] = leadingCharacters[leadingCharacters.Count - 1] + words[i];
                }
                else
                {
                    words[i] = leadingCharacters[i - 1] + words[i];
                }
            }
            return string.Join(" ", words);
        }

        private string DoLetterSwapReverse(string text)
        {
            List<string> leadingCharacters = new List<string>();
            List<char> vowels = new List<char>()
            {
                'a', 'e', 'i', 'o', 'u'
            };

            string[] originalWords = text.Split(' ');
            List<string> words = new List<string>();
            foreach (string word in originalWords)
            {
                if (!string.IsNullOrEmpty(word))
                {
                    words.Add(word);
                }
            }

            foreach (string word in words)
            {
                for (int i = 0; i < word.Length; i++)
                {
                    if (vowels.Contains(char.ToLower(word[i])))
                    {
                        if (char.ToLower(word[i]) == 'y')
                        {
                            if (i == word.Length - 1)
                            {
                                leadingCharacters.Add(word.Substring(0, i));
                            }
                            else
                            {
                                if (vowels.Contains(char.ToLower(word[i + 1])))
                                {
                                    leadingCharacters.Add(word[i].ToString());
                                    break;
                                }
                                else
                                {
                                    leadingCharacters.Add(word.Substring(0, i));
                                    break;
                                }
                            }
                        }
                        else
                        {
                            leadingCharacters.Add(word.Substring(0, i));
                            break;
                        }
                    }
                    else if (vowels.Contains(char.ToLower(word[0])))
                    {
                        leadingCharacters.Add(word[0].ToString());
                        break;
                    }

                    else if (i == word.Length - 1)
                    {
                        leadingCharacters.Add(word);
                        break;
                    }
                }
            }

            int startIndex, endIndex;
            for (int i = 0; i < words.Count; i++)
            {
                startIndex = leadingCharacters[i].Length;
                endIndex = words[i].Length - startIndex;
                words[i] = words[i].Substring(startIndex, endIndex);
                words[i] = leadingCharacters[(i + 1) % leadingCharacters.Count] + words[i];
            }
            return string.Join(" ", words);
        }

        private string DoGSSwap(string text)
        {
            StringBuilder builder = new StringBuilder();
            foreach(char letter in text)
            {
                if (letter == 'S')
                {
                    builder.Append('G');
                }
                else if (letter == 's')
                {
                    builder.Append('g');
                }
                else if (letter == 'G')
                {
                    builder.Append('S');
                }
                else if (letter == 'g')
                {
                    builder.Append('s');
                }
                else
                {
                    builder.Append(letter);
                }
            }
            return builder.ToString();
        }

        private string DoBlahsBlahforBlah(string text)
        {
            return text + "'s " + text + " for " + text;
        }

        private string DoBlaherFile(string text)
        {
            if (text.EndsWith("e"))
            {
                return text + "r file";
            }
            else
            {
                return text + "er file";
            }
        }
    }
}
