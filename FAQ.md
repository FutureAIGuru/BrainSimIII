# FAQ

## Block 1: Project Status & Access

### What is the state of the project?
Brain Simulator III should be considered as in a proof-of-concept phase. Although the UKS is fully functional, it has not been formally tested, and its API is still subject to change as new modules are created which require additional or modified features.

### Can I use the Brain Simulator fully functionally already?
Yes. You can use the Brain Simulator UI to develop apps which utilize the UKS API. However, be aware of the caveats above.


## Block 2: Knowledge Representation & Learning (UKS)

### How does the BS fill its knowledge?
Everything is broken down to **“Things”**, “**Relationship**”, and “**Clauses**”. 

**Things** might represent anything, a physical object (Fido), an action (plays), a place, a color, a relationship type (is-a). 

A **relationship** relates two things which a relationship type: [Fido, is-a, dog]. Each relationship consists of three Things, the source (Fido), the type (is-a), and the target (dog) and any number of Clauses. 

A **clause** consists of a relationship type (IF) and a pointer to another relationship: [Fido, play, outside] IF [weather, is, sunny]. 

With these capabilities, any thought should be expressible in the UKS.

IMPORTANT: Things must represent unambiguous concepts. Things have labels which are important for reference but should not be used to represent the content or words to describe the content. Instead words are represented by separate Things which are linked to by Relationships such as “called” “means”, etc.

### How does the BS fill its knowledge up in practice?
There are API calls to add/modify/delete Things, Relationships, and Clauses. 

More abstract learning processes exist in modules have not been migrated into the UKS as yet.

### Can the BS (UKS) be filled via an LLM?
Yes, indirectly. An LLM can serve as a “Natural Language Understanding” frontend. The LLM translates unstructured text (e.g., from Wikipedia) into the precise, structured node-and-edge format of the UKS. It acts as a “translator” for the knowledge architecture, not as a direct source of knowledge itself.

### Is the version of the UKS a stable designed architecture for the future versions?
Not yet. Although the architecture of Things, Relationships, and Clauses (conditional) is stable, its implementation has not been sufficiently tested.

#### So the filled in knowledge wont be lost later?
Yes. The core architecture (graph structure) is stable. The schema (the types of nodes and edges) can be furhter developed. Robust migration scripts ensure that all existing data is automatically adapted to any new schema. The continuity of knowledge is guaranteed.

In the UKS dialog, there is an “Export” function which would output UKS content to a neutral text file which can be imported in future versions. It is unlikely that this text file format will change.

### What database software is used for the UKS?
A proprietary, in-memory graph database. Standard solutions could not meet the extreme requirements for latency, throughput, and seamless integration with the neural simulation. The solution is optimized for massive parallel processing.

### A 'node' and 'edge' are datastructures in the background?
Yes. (See: file.xyz)

**Node** represents an entity or a concept (called a “Thing” in the source code) (e.g., “Apple”, “the color Red”, a specific memory).

**Edge** represents a directed and 'typed' relationship between two nodes (called a “Relationship” in the source code) (e.g., Node(Apple) --[has_color]--> Node(Red)).

A detailed description of the data structures can be found here: [Link to the technical documentation of the UKS]


## Block 3: Capabilities & Architecture

### How is Brain Simulator III different from traditional AI and neural networks?
Unlike deep learning systems that require massive datasets and compute power, Brain Simulator III:

- Uses a graph (the UKS) which can be searched based on its content.

- Extends the traditional Knowledge Graph paradigm by adding:
    - Bi-directional edges
    - Attribute inheritance in a hierarchy of is-a relationship
    - Multiple inheritance
    - Exceptions
    - Conditional relationships (which are relationships between relationships).

- Allows modules which can form agents which perform actions like:
    - Bubbling common attributes to ancestor nodes
    - Removing expired information from the graph
    - Creating subclasses when class membership becomes too large.

     New modules can be created easily.

- Is designed with a user interface which allows for experimentation when adding new capabilities such as vision or interfaces to LLMs.

This makes it better suited for problems requiring understanding, reasoning, and adaptability rather than pure pattern recognition.

### Can the BS be used to think (deduction)?
It inherently solves: Socrates is a man, all men are mortal, therefore Socrates is mortal. It does this through attribute inheritance rather than logic.

#### Can the BS understand my natural language queries?
There is an LLM  interface which is currently under development to perform this function.

### What is the difference from Brain Simulator 2 to 3?
**Brain Simulator II** is a NEURON simulator which you can use to understand the capabilities and limitations of biological neurons and how various neural circuits can perform higher-level functions.

**Brain Simulator III** includes a graph-based engine which will form the basis of future applications. 

### How is episodic memory realized? 
It is represented in the UKS using relationship types such as “after”, “before”, “next”, etc.  This aspect has been only lightly developed.

Link for further informations: (video 1, video 2, article, ..)

#### How does an experience and its subparts connect to time and space in practice in the UKS?
This is still under development and subject to change.

### How much storage will the UKS need?
A 1TB system should exceed the information content of the human brain.

### How much computational power will the BS need?
Data can be added to the UKS tested at 100,000 relationships/second. Searches have not been evaluated but are independent of the size of the graph.

### Does the BS run on one instance or can it run distributed?
The UKS should be capable of running as a server.

### Can the BS serve inference of multiple users at the same time?
Yes. Although this has not been tried.

## Block 4: Future & Acceleration

### In what ways can the project be accelerated?
- Partnership (Research-Institutions, Software-Institutions, Universities, ..)
- Open source contributions
- Financial support

Link for further informations: (https://futureaisociety.org/participate/volunteer/)
