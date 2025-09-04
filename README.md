# BrainSim3
Adding Common Sense to Artificial Intelligence.

Brain Simulator III is a knowledge system capable of representing and relating information needed to implement Common Sense. Centered on the Universal Knowledge Store (UKS), the system creates a web of nodes and edges and has a growing library if modular software agents which can preform any desired function.

Agents are independent modules and can be written in C# or Python. The Brain Simulator system runs on Windows or MAC. A growing library of modules is being adapted from previous research and development.

The Brain Simulator is supported by the non-profit “Future AI Society” which has additional information and holds regular online development meetings.  You can join free at: https://futureaisociety.org Or support continuing development with a paid membership.
With the UKS, this project is leapfrogging other AI technologies which are unable to represent the information needed for the understanding which underpins Common Sense. The Brain Simulator system can  
•	Represent multi-sensory information so that sounds, words, and images can be related.  
•	Represent a real-time mental model of immediate surroundings akin to the mind’s similar ability.  
•	Handle nebulous and/or conflicting information.  
•	Store action information so it learns which actions lead to positive outcomes for a current situation.  
•	Update content in real time to handle real-world robotic applications. 
•	Incorporate agent software Modules  to perform any desired functionality. 

# About the UKS
The UKS includes a graph of nodes connected by edges. Within the UKS, nodes are called “Things”. Things can be related by edges called “Relationships” which consist of a source Thing, a target Thing, and a relationship Type (which is also a Thing).  For example: Fido is-a dog would be represented by a single is-a relationship relating Things representing “Fido” and “dog” with the “is-a” Relationship type. 

The UKS implements inheritance so that Relationships which add attributes to the dog Thing will also be expressed as attributes of Fido and any other dog. Given that “dogs have 4 legs”; querying Fido will automatically include the fact that Fido has 4 legs even though that information is never explicitly represented. The inheritance process supports exceptions so that adding the information that Tripper, a dog, has 3 legs will override the inheritance process. This combination of inheritance and exceptions is a huge step forward in efficiency similar to the human mind…you don’t ever need to store all the attributes of a Thing, only those attributes which make a given Thing unique.

In the same way that Relationships relate multiple Things, “Clauses” relate multiple Relationships. This is important because not all “facts” are either true or false but are dependent on other information. Consider “Fido can play fetch IF the weather is sunny.” 

How to Install, Run and Develop documentation coming soon.

Thanks for your interest! 

