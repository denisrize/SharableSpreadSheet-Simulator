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
| 1000>              | 0.12                |
| else               | 0.15                |

### Demonstration
Example of Spreed-sheet size 10*10 and the compatible locks table: 
![Spread-Sheet](https://user-images.githubusercontent.com/55393990/190423753-5d9a8b4d-79aa-49a1-8898-f43b14197813.png)



