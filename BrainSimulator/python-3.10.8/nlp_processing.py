import spacy
import sys


#to load a model:
#install spacy and go to the folder where it's installed and give the command:
#python -m spacy download en_core_web_trf
#nlp = spacy.load("en_core_web_sm")
#nlp = spacy.load("en_core_web_lg")
nlp = spacy.load("en_core_web_trf")
while True:
    phrase = input();
    if phrase == "exit": break
    doc = nlp(phrase)

    
    for token in doc:
        print(token.norm_, token.lemma_, token.dep_, token.pos_, token.tag_, token.head.i, token.morph, sep = '\t')
    print("Process Complete")
