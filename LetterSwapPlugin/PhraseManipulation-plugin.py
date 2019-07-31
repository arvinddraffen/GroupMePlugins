import argparse
import json

def main():
    parser = argparse.ArgumentParser(description='Select GroupMe message type(s) for program to use.')
    input_string = setup_argparser(parser)
    results = {}
    results['Letter Swap'] = swetter_lap(input_string)
    results['GS Swap'] = g_s_swap(input_string)
    results['BlahsBlahforBlah'] = blahsblahforblah(input_string)
    print(json.dumps(results))

def setup_argparser(parser):
    parser.add_argument('string', help='String to analyze', nargs='+')
    return ' '.join(parser.parse_args().string)

def g_s_swap(string):
    new_string = ""
    for letter in string:
        if letter == 'S':
            new_letter = 'G'
        elif letter == 's':
            new_letter = 'g'
        elif letter == 'G':
            new_letter = 'S'
        elif letter == 'g':
            new_letter = 's'
        else:
            new_letter = letter
        new_string += new_letter
    return new_string

def swetter_lap(string):
    initial_characters = []
    vowels = ['a', 'e', 'i', 'o', 'u', 'y']
    words = string.split()
    for word in words:
        for index in range(len(word)):
            if word[index].lower() in vowels:
                if word[index].lower() == 'y':      
                    if index == len(word)-1:                         # if 'y' is the last letter in the word
                        initial_characters.append(word[:index])
                    else:
                        if word[index+1].lower() in vowels:
                            initial_characters.append(word[index])
                else:
                    initial_characters.append(word[:index])
                break
            elif word[0] in vowels:
                initial_characters.append(word[0])
                break
            elif index == len(word)-1:
                initial_characters.append(word[0])
    for index in range(len(words)):
        words[index] = words[index][len(initial_characters[index]):]
        words[index] = initial_characters[index-1] + words[index]
    return " ".join(words)

def reverse(string):
    return string[::-1]

def blahsblahforblah(string):
    return string + "\'s " + string + " for " + string

if __name__ == '__main__':
    main()