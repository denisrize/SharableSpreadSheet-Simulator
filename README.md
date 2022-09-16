## Sharable Spreadsheet
In this project i designed spreadsheet object supports several elementary operations, this object used by many threads (each thread is different client) concurrently and hence needs to be designed to be thread-safe.

**Structure** - The spreadsheet represent a 2D array of rows*colmuns cells,
each cell contains a String.

**Locks** - I used "ReaderWriterLockSlim" for this project,this lock allows multiple threads to be in read mode and only one thread to be in write mode with exclusive ownership of the lock. Whenever writer require the lock, readers have to wait until it will be acquire and release by the writer for them to take it.

The main idea of this project is to bulid Spreadsheet efficient as possible in term of Time-Space complexity.The trade off for this case is that the more locks contained in the object the less time each client can get his desired resource(for example each cell could contain a lock), but each lock take another space in memory.
The way i choose to split the locks across the spreed-sheet is based on Experiments and diagnosis. My inspiration for this come from the way Machine learning algorithm works.

My design of the spread-sheet included also an array of locks, when each lock responsible for (Size of spread-sheet)/(number of locks) cells. The number of locks is dynmic and determined in relation to the spread-sheet size. All my experiments was tested with 10 clients, started on 10*10 spread-sheet when the lock array in 0.12% of it size ( 12 locks). My goal was to stay approximately with the same run time while the spread-sheet get larger,but the locks number growth ratio is small as possible ( smaller than linear).  
In addition there is another lock for the all spreed-sheet for some operation that i explain later on. 


**Result**

| Spread-Sheet size  | locks table precent |
| -------------      | -------------       |
| 100000>            | 0.01                |
| 10000>             | 0.05                | 
| 1000>              | 0.1                 |
| else               | 0.12                |

### Demonstration
Example of Spreed-sheet size 10*10 and the compatible locks table(0.12%): 
![Spread-Sheet](https://user-images.githubusercontent.com/55393990/190423753-5d9a8b4d-79aa-49a1-8898-f43b14197813.png)

### Operations

The operations on the spread-sheet are the following:
- get/set cell info.
- exchange rows/colmuns.
- search in row/colmun.
- search in range.
- add row/colmun.
- find all (perform search and return all relevant cells according to caseSensitive param).
- set all (replace all old string cells with the new string according to caseSensitive param).
- get size.
- save/load spread-sheet (you can decide the format you save the data).

All of this operations request different number of locks( read/write) to accomplish their goal. For example to exchange rows in spread-sheet you need to acquire  writer locks on each row, while adding a row requires to lock the entire spreed-sheet starting from the lower row to accomplish Thread safe operation.

## Simulator
I wanted to test and debug my spread-sheet under 'stress' of multiple clients, so i implement a console application simulator for multiple threads to use the spread sheet.
My program work as follow (input arguments):
Simulator < rows > < cols > < nThreads > < nOperations > < mssleep >

1. Simulator start with a creation of new spreadsheet in a size of rows*cols.
2. After the creation of empty table,i filled the empty spreadsheet with random strings.
3. Start nThreads number of threads (users) concurrently works on the object. nOperations is the number of random operations each thread performs with a sleep of <sleep> millisecond between each operation it perform.
  
 Output example for input of 20 20 10 5 500 -  

![newConsoleResult](https://user-images.githubusercontent.com/55393990/190705415-0fbc8c40-6ade-46d4-acb9-4489fe20c6fc.png)

