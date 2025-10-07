# DirSync
Console utility that keeps two directories synchronized.

## Build
.NET9 standard build with default output paths
```
dotnet build
```

the executable is built in `/DirSync.ConsoleApp/bin/`

## Usage
```
./DirSync.ConsoleApp <sourceDirectoryPath> <replicaDirectoryPath> [<intervalInSeconds>] [<logFilePath>]
```

`<sourceDirectoryPath>` required, directory has to exist or program won't run, full path is recommended but relative path shouldn't cause problems
`<replicaDirectoryPath>` required, directory has to exist or program won't run, full path is recommended but relative path shouldn't cause problems
`<intervalInSeconds>` optional, first run is done immediately, interval starts after the synchronization process ends, defaults to `60`, values which aren't an integer or integer with values <= 0 are invalid, in this case the program won't run
`<logFilePath>` optional, defaults to `./{timestamp}.log`, full path is recommended but relative path shouldn't cause problems, if provided non existing path or not a path to a file the program won't run

Use with caution: providing wrong `<replicaDirectoryPath>` may cause unwanted data loss in the scope of the provided directory if it's not empty and not containing only source directory files.
The program can be stopped at any point in time even while synchronization process is running without data loss.

**Limitations:**
- The app supports only positional arguments; named arguments aren't implemented
- Dry run is not possible by user

## Scope and Limitations
This project was built within constraints of a "toy project" that simulates a real world scenario.
Such constraints put a limitation on the actual use case of this tool which makes certain features unreasonable to implement but still worth at least considering to represent the actual real world workflow that would occur while developing a simillar solution.

### Requirements
These are considered as must-haves based on the actual content of the assignment.

**Technical Requirements**
1. The codebase has to be written specifically in C#
2. The logging has to include at least file creation/copying/removal operations
3. The log output has to be at least console and file
4. The "source", "replica" directories paths; log file path and synchronization interval has to be configurable via providing the command line arguments
5. The core business logic has to have no external dependencies; external libraries are permitted for other technical details; AI/LLM usage is prohibited

**Bussiness Logic Requirements**
1. The program has to synchronize directory considered as a "source" to a directory considered a "replica" in such a way that the directories are identical
2. The synchronization has to work in one direction only - modifications to "replica" directory have to be overwritten and not passed to the "source" directory
3. The synchronization has to execute periodically

### Details in Scope
These are considered as should-bes based on my previous work experience. The basic details that are not specified in the requirements but are expected by default by users.
So far as I know the users of any project define the scope of it so to better understand the scope I need to define who the potential users of this project are:
- The reviewer of this project
- The imaginary user group that fits this case: tech-savvy people that need to have their home PC problems solved
- The classic 3 users that always show up: me, myself and I

With this definition I can extrapolate the additional details that are considered in scope:
1. The codebase should be written in such a way that it will be reliable and easy to test
2. The codebase should be tested enough according to level of criticality of the app (including automated tests)
3. The codebase should be reasonably extensible
4. The usage should be user friendly - providing correct usage when called incorrectly; have useful defaults
5. The synchronization should take reasonable time compared to number of operations needed to perform it and file sizes
6. The logs should provide useful information like timestamps / info level logs about synchronization runs
7. The failure of synchronization should be communicated to the user and logged with cause
8. The application should not crash on synchronization failure - the behavior expected by the user in this case would be to wait for next synchronization interval for a retry
9. The application should run on most common systems/filesystems
10. The application should be able to run in multiple instances synchronizing multiple directories
11. The application should have usage documentation
12. The project should provide build steps necessary to compile especially if no executable is provided
13. The synchronization process should have a timeout per action based on the file sizes etc. to avoid app hanging completely while ensuring it synchronizes large files also

### Details Out of Scope
These are considered as would-bes based on different aproaches that this project could theoretically go for. This is based on changing/extending the requirements to fit the needs typical to other user groups.

Let's consider some other groups of users and what could be the most important features to them in comparison with the scope targeted above:

**Nontechnical users**
To be useful to that kind of users the scope would need to change in such a way that ideally zero clicks are required by most of the users - the application would need to be so seemless that users would forget it is not their system native feature.
Considerable research would be needed to provide the default synchronization to just the directories they need. To suit the needs of users that want the customization there could be desktop UI matching their OS of choice design where they can further customize based on their individual needs.
The application would need an auto update feature for further no action required support.

**Enterprise users**
These kind of users would probably be in a need of an approach that would suit their needs to manage many different servers or end user machines.
The application would need to operate as an installable agent with centralized managment as most commonly either browser based UI or without UI as CLI.
There would be a need of some kind of auth (possibly including LDAP etc.) with granular access and privileges.
This would also possibly spawn a need for some kind of an API that would make it possible to integrate with other systems or alerting.
The no internet access internal networks would need to be considered in design of things such as update rollout.
In this scenario the core feature would need to handle large scale file sizes and volumes possibly over network - copy approach would not be scalable; there would be a need to minimize the network operations possibly via sending over the network only file diff etc.
Use of networking also implies that there would be some security concerns that would need to be additionally addressed.
There would be a need for more miscellaneous features like log collection etc.

## Pre Implementation Notes
First of all it needs to be acknowledged that this case could be solved with most of the requirements met with some throwaway 5 minutes of work PowerShell script that would just basically work like this:
Every interval remove everything inside directory "replica" and then copy everything inside directory "source" to directory "replica".
This solution while not the best idea would be technically correct but that is not the real goal of this project.

The goal in mind when developing this app is to make it as simple as possible while meeting the requirements without cutting corners on quality.
At first I'm going to use the approach that I first thought of when reading the requirements.
That might prove to be the naive approach and further research will be needed; the possible resources to deal with this problem are existing solutions that are open source projects that provide simillar features - an example of an app with simillar features would be OpenCloud, GIT etc. If the writes would need to be instantly synchronized any open source database like MariaDB specifically replication feature could be considered in the research but that is not the case.

As for the core feature implementation the first draft of the plan is very simple:

1. Acquire "source" and "replica" directory structure including nested directories and files; generate checksum for each file
2. Compare both of these directories generating actions to be executed based on the state of both directories:
ADD_DIRECTORY which internally creates directory in "replica"
REMOVE_DIRECTORY which internally removes directory from "replica"
the directory actions are created based on these rules:
a) a directory that exists in directory "source" and doesn't exist in directory "replica" generates action ADD_DIRECTORY
b) a directory that does not exist in directory "source" and exists in directory "replica" generates action REMOVE_DIRECTORY
c) a directory that exists in directory "source" and exists in directory "replica" generates no actions

ADD_FILE which internally copies the file from "source" to "replica" overwriting if needed,
REMOVE_FILE which internally removes the file from "replica"
the file actions are created based on these rules:
a) a file that exists in directory "source" and doesn't exist in directory "replica" generates action ADD_FILE
b) a file that does not exist in directory "source" and exists in directory "replica" generates action REMOVE_FILE
c) a file that exists in directory "source" and exists in directory "replica" with different checksums generates action REMOVE_FILE and ADD_FILE
d) a file that exists in directory "source" and exists in directory "replica" with matching checksums generates no actions
3. Execute the actions in order, file actions in the same order can be parallelized, directory actions with different roots can be parallelized:
first ADD_DIRECTORY
second REMOVE_FILE
third ADD_FILE
fourth REMOVE_DIRECTORY

This makes it possible to perform all of the actions that are needed to synchronize these directories and does not generate any metadata that needs to be kept.
This also ensures that there is no quirky behavior for the user like with for example GIT where empty directories wouldn't be copied or untracked files would stay.
Of course this solution is not perfect and doesn't do any work that would make it at least semi useful in real world case scenario f.e. file diffing.

## Post Implementation Notes
Additional research wasn't necessary for this simple slice of the problem. The support of outdated systems wasn't a concern in this project so I defaulted to using .NET9.
The result of my work is very much indeed PoC level of work but it covers the core functionality needed. The project could easily be now improved upon. I left many comments with additional concerns that got raised while developing this solution; the work at any point could be resumed by anyone else without losing the context that I achieved while working on this.
The result isn't perfect and cleanest in any way but there had to be some quality tradeoff for the faster delivery time. This version could be used to gather additional feedback no matter if this was from the reviewers or users. The biggest problem while developing was under vs over engineering balance needed to finish the project in reasonable time while still providing some realworld-like scenario codebase when also documenting the process to give some of the additional context of the work that would occur during live coding session style interview.

The codebase has just ok amount of tests compared to the reliability of the project needed (no critical functions with possible data loss, synchronization interval suggests that there is a time window where directories not synchronized is acceptable; if this wouldn't be acceptable the writes to the source directory would need to be queued and cosidered written when both source and replica directory were changed etc. - the source is always existing and replica can be always overwritten to achieve desired output; if the replica would be considered a backup rather than synchronization source which was unclear this assumption wouldn't be necessary true - source wouldn't be considered always existing).
The project was also manually tested multiple times although the testing was mostly limited to the happy path style tests (including some real world directories laying around) so more testing with the unhappy path in mind would need to be considered (f.e. file locks on files were manually tested, should consider adding it to automated testing etc.).