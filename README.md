# What is SherLock

SherLock is a synchronization inference tool described in the paper "SherLock: Unsupervised Synchronization-Operation Inference" in ASPLOS 2021.

## Synchronization definition
We define the synchronization operation as the program points that introducing happens-before relation, for example, the lock read/write, thread start/join and while-loop on flag. The format of synchronization could be very flexible while they are very important to analyze the correctness of program. That’s why we design SherLock to infer them with an unsupervised approach. In general, SherLock classifies the synchronization as two types: release and acquire. The release always happens before the corresponding acquire.

## System Requirement
Python3, numpy in Python, flipy in Python and Visual Studio 2019 or newer. 

## SherLock Approach
SherLock is a dynamic analyze tool that instruments the program to collect runtime trace and analyzes the trace to infer synchronizations.

### Instrumentation
‘instrument-tool’ is the directory contain the instrumentation tool. It is modified from ‘https://github.com/microsoft/TSVD’. By compiling the project in this directory generates a ‘TorchLite.exe’ 

        .\TorchList.exe [path of binaries]; instrumenting the binaries in the path.

### Log-Analysis
‘log-analysis’ has two parts: a preprocessing and linear solver. After running the instrumented binaries, a ‘Runtime.log’ file is generated in the running directory. Compiling the project in preprocessing generates ‘Analyzer.exe’

       .\Analyze.exe [path contains Runtime.log]; preprocessing the log.
‘Analyze.exe’ produces the result in the same directory with ‘Runtime.log’. Eventually, in the linear-solver directory:

        Python log_analyzer.log  --batch [path to preprocessed result] -refine ; inferring the synchronizations.

## Example
  

