using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace SharableSpreadSheet
{

    internal class SharableSpreadSheet
    {
        ReaderWriterLockSlim[] lockList;
        String[,] spreadSheet;
        Mutex mutex;
        volatile int nUsersCount;
        int nUsers;
        int nRows;
        int nCols;
        double precent;
        int lockSize;
        public SharableSpreadSheet(int nRows, int nCols, int nUsers = -1)
        {
            // nUsers used for setConcurrentSearchLimit, -1 mean no limit.
            // construct a nRows*nCols spreadsheet
            this.nRows = nRows;
            this.nCols = nCols;
            nUsersCount = 0;
            precent = 0;
            mutex = new Mutex();
            setConcurrentSearchLimit(nUsers);
            spreadSheet = new String[nRows, nCols];
            setLocks();

        }

        public String getCell(int row, int col)
        {
            // return the string at [row,col]
            ReaderWriterLockSlim readerLock = getLockByIndex(row, col, false);
            readerLock.EnterReadLock();
            String cell = spreadSheet[row, col];
            releaseLock(readerLock, false);
            return cell;
        }
        public void setCell(int row, int col, String str)
        {
            // set the string at [row,col]
            ReaderWriterLockSlim writerLock = getLockByIndex(row, col, true);
            writerLock.EnterWriteLock();
            spreadSheet[row, col] = str;
            releaseLock(writerLock, true);
        }
        public Tuple<int, int> searchString(String str)
        {
            // return first cell indexes that contains the string (search from first row to the last row)  
            return searchInRange(0, nCols - 1, 0, nRows - 1, str);
        }
        public void exchangeRows(int row1, int row2)
        {
            if (row1 == row2) throw new Exception("Rows need to be different to exchange.");
            // exchange the content of row1 and row2
            List<ReaderWriterLockSlim> writerLocks1 = getLocksByRowOrCol(row1, false, true);
            List<ReaderWriterLockSlim> writerLocks2 = getLocksByRowOrCol(row2, false, true);

            String[] tempR1 = new string[nCols];
            String[] tempR2 = new string[nCols];

            // enter all locks for the wanted rows
            for (int i = 0; i < writerLocks1.Count; i++) writerLocks1[i].EnterWriteLock();
            for (int i = 0; i < writerLocks2.Count; i++)
            {
                if (writerLocks1.Contains(writerLocks2[i])) continue;
                writerLocks2[i].EnterWriteLock();
                writerLocks1.Add(writerLocks2[i]);
            }
            tempR1 = Enumerable.Range(0, spreadSheet.GetLength(1)).Select(x => spreadSheet[row1, x]).ToArray();
            tempR2 = Enumerable.Range(0, spreadSheet.GetLength(1)).Select(x => spreadSheet[row2, x]).ToArray();

            // exchange rows
            for (int i = 0; i < nCols; i++)
            {
                spreadSheet[row1, i] = tempR2[i];
                spreadSheet[row2, i] = tempR1[i];
            }
            //  release all locks
            for (int i = 0; i < writerLocks1.Count; i++) releaseLock(writerLocks1[i], true);

        }
        public void exchangeCols(int col1, int col2)
        {
            if (col1 == col2) throw new Exception("Columns need to be different to exchange.");
            // exchange the content of col1 and col2
            List<ReaderWriterLockSlim> writerLocks1 = getLocksByRowOrCol(col1, true, true);
            List<ReaderWriterLockSlim> writerLocks2 = getLocksByRowOrCol(col2, true, true);
            String[] tempC1 = new string[nRows];
            String[] tempC2 = new string[nRows];

            // enter all locks for the wanted rows
            for (int i = 0; i < writerLocks1.Count; i++) writerLocks1[i].EnterWriteLock();
            for (int i = 0; i < writerLocks2.Count; i++)
            {
                if (writerLocks1.Contains(writerLocks2[i])) continue;
                writerLocks2[i].EnterWriteLock();
                writerLocks1.Add(writerLocks2[i]);
            }

            tempC1 = Enumerable.Range(0, spreadSheet.GetLength(0)).Select(x => spreadSheet[x, col1]).ToArray();
            tempC2 = Enumerable.Range(0, spreadSheet.GetLength(0)).Select(x => spreadSheet[x, col2]).ToArray();

            // exchange cols
            for (int i = 0; i < nRows; i++)
            {
                spreadSheet[i, col1] = tempC2[i];
                spreadSheet[i, col2] = tempC1[i];
            }
            //  release all locks
            for (int i = 0; i < writerLocks1.Count; i++) releaseLock(writerLocks1[i], true);

        }
        public int searchInRow(int row, String str)
        {
            // perform search in specific row

            Tuple<int, int> result = searchInRange(0, nCols - 1, row, row, str);
            if (result == null) return -1;
            else return result.Item2;

        }
        public int searchInCol(int col, String str)
        {
            Tuple<int, int> result = searchInRange(col, col, 0, nRows - 1, str);
            if (result == null) return -1;
            else return result.Item1;
        }
        public Tuple<int, int> searchInRange(int col1, int col2, int row1, int row2, String str)
        {
            if (col2 < col1 || row2 < row1)
                throw new Exception("Colmun and row 2 must be greater than Colmun and row 1.");
            // perform search within spesific range: [row1:row2,col1:col2] 
            //includes col1,col2,row1,row2

            ReaderWriterLockSlim readerLock = getLockByIndex(row1, col1, false);
            readerLock.EnterReadLock();
            int counter = 0;
            // return first cell indexes that contains the string (search from first row to the last row)
            for (int i = row1; i <= row2; i++)
            {
                for (int j = col1; j <= col2; j++)
                {
                    if (counter % lockSize == 0 && counter != 0)
                    {
                        releaseLock(readerLock, false);
                        readerLock = getLockByIndex(i, j, false);
                        readerLock.EnterReadLock();
                    }

                    if (spreadSheet[i, j].Equals(str))
                    {
                        releaseLock(readerLock, false);
                        return Tuple.Create(i, j);
                    }
                    counter++;
                }
                counter += nCols - 1;
            }
            releaseLock(readerLock, false);
            return null;
        }
        public void addRow(int row1)
        {
            if (row1 >= nRows || row1 < 0) throw new Exception("Invalid row index.");
            //add a row after row1
            copySpreadAndADD(row1, false);

        }
        public void addCol(int col1)
        {
            if (col1 >= nRows || col1 < 0) throw new Exception("Invalid colmun index.");

            //add a column after col1
            copySpreadAndADD(col1, true);

        }
        public Tuple<int, int>[] findAll(String str, bool caseSensitive)
        {
            // perform search and return all relevant cells according to caseSensitive param
            List<Tuple<int, int>> equals = new List<Tuple<int, int>>();
            ReaderWriterLockSlim readerLock = getLockByIndex(0, 0, false);
            readerLock.EnterReadLock();
            int counter = 0;
            for (int i = 0; i < nRows; i++)
            {
                for (int j = 0; j < nCols; j++)
                {
                    if (counter % lockSize == 0 && counter != 0)
                    {
                        releaseLock(readerLock, false);
                        readerLock = getLockByIndex(i, j, false);
                        readerLock.EnterReadLock();
                    }

                    if (!caseSensitive && str.ToLower().Equals(spreadSheet[i, j].ToLower()))
                        equals.Add(Tuple.Create(i, j));

                    else if (caseSensitive && str.Equals(spreadSheet[i, j]))
                        equals.Add(Tuple.Create(i, j));

                    counter++;
                }
            }
            releaseLock(readerLock, false);

            return equals.ToArray();
        }
        public void setAll(String oldStr, String newStr, bool caseSensitive)
        {
            // replace all oldStr cells with the newStr str according to caseSensitive param
            ReaderWriterLockSlim writerLock = getLockByIndex(0, 0, true);
            writerLock.EnterWriteLock();
            int counter = 0;
            for (int i = 0; i < nRows; i++)
            {
                for (int j = 0; j < nCols; j++)
                {
                    if (counter % lockSize == 0 && counter != 0)
                    {
                        releaseLock(writerLock, true);
                        writerLock = getLockByIndex(i, j, true);
                        writerLock.EnterWriteLock();
                    }

                    if (!caseSensitive && oldStr.ToLower().Equals(spreadSheet[i, j].ToLower()))
                        spreadSheet[i, j] = newStr;

                    else if (caseSensitive && oldStr.Equals(spreadSheet[i, j]))
                        spreadSheet[i, j] = newStr;

                    counter++;
                }
            }
            releaseLock(writerLock, true);
        }
        public Tuple<int, int> getSize()
        {
            // return the size of the spreadsheet in nRows, nCols
            return new Tuple<int, int>(nRows, nCols);
        }
        public void setConcurrentSearchLimit(int nUsers)
        {
            // this function aims to limit the number of users that can perform the search operations concurrently.
            // The default is no limit. When the function is called, the max number of concurrent search operations is set to nUsers. 
            // In this case additional search operations will wait for existing search to finish.
            // This function is used just in the creation
            this.nUsers = nUsers;

        }

        public void save(String fileName)
        {
            ReaderWriterLockSlim readerLock = getLockByIndex(0, 0, false);
            readerLock.EnterReadLock();
            int counter = 0;
            // save the spreadsheet to a file fileName.
            // you can decide the format you save the data. There are several options.
            System.IO.StreamWriter streamWriter = new System.IO.StreamWriter(fileName, true);
            for (int i = 0; i < nRows; i++)
            {
                for (int j = 0; j < nCols; j++)
                {
                    if (counter % lockSize == 0 && counter != 0)
                    {
                        releaseLock(readerLock, false);
                        readerLock = getLockByIndex(i, j, false);
                        readerLock.EnterReadLock();
                    }
                    streamWriter.WriteLine(spreadSheet[i, j]);
                }
                streamWriter.WriteLine();

            }
            releaseLock(readerLock, false);
            streamWriter.Close();
        }
        public void load(String fileName)
        {
            // load the spreadsheet from fileName
            // replace the data and size of the current spreadsheet with the loaded data
            List<String[]> list = new List<String[]>();
            List<String> temp = new List<String>();

            // Create an instance of StreamReader to read from a file.
            using (StreamReader sr = new StreamReader(fileName))
            {
                string line;

                while ((line = sr.ReadLine()) != null)
                {
                    if (line.Equals(""))
                    {
                        list.Add(temp.ToArray());
                        temp.Clear();
                        continue;
                    }
                    temp.Add(line);
                }
            }

            String[,] loadSpread = To2D(list.ToArray());

            // lock all the spread sheet
            for (int i = 0; i < lockList.Length; i++) lockList[i].EnterWriteLock();

            nRows = loadSpread.GetLength(0);
            nCols = loadSpread.GetLength(1);
            spreadSheet = loadSpread;

            if (!setLocks()) for (int i = 0; i < lockList.Length; i++) lockList[i].ExitWriteLock();




        }

        private void copySpreadAndADD(int index, bool colFlag)
        {
            // get all locks to read and change the spread sheet.
            for (int i = 0; i < lockList.Length; i++) lockList[i].EnterWriteLock();

            String[,] newSpreadSheet;
            bool indexFlag = false;
            if (colFlag)
            {
                nCols++;
                newSpreadSheet = new string[nRows, nCols];
                for (int i = 0; i < nRows; i++)
                {
                    indexFlag = false;
                    for (int j = 0; j < nCols; j++)
                    {
                        if (j == index + 1)
                        {
                            newSpreadSheet[i, j] = String.Format("TestCell{0}{1}", i, j);
                            indexFlag = true;
                            continue;
                        }
                        if (indexFlag) newSpreadSheet[i, j] = spreadSheet[i, j - 1];
                        else newSpreadSheet[i, j] = spreadSheet[i, j];
                    }
                }
            }
            else
            {
                nRows++;
                newSpreadSheet = new string[nRows, nCols];
                for (int i = 0; i < nRows; i++)
                {
                    if (i == index + 1)
                    {
                        for (int j = 0; j < nCols; j++)
                            newSpreadSheet[i, j] = String.Format("TestCell{0}{1}", i, j);
                        indexFlag = true;
                        continue;
                    }
                    for (int j = 0; j < nCols; j++)
                    {
                        if (indexFlag) newSpreadSheet[i, j] = spreadSheet[i - 1, j];
                        else newSpreadSheet[i, j] = spreadSheet[i, j];
                    }
                }
            }
            spreadSheet = newSpreadSheet;
            if (!setLocks())
                for (int i = 0; i < lockList.Length; i++) lockList[i].ExitWriteLock();

        }

        private bool setLocks()
        {
            bool precentChanged = false;
            double newPrecent;
            if (nRows * nCols >= 100000) newPrecent = 0.01;
            else if (nRows * nCols >= 10000) newPrecent = 0.05;
            else if (nRows * nCols >= 1000) newPrecent = 0.12;
            else newPrecent = 0.15;

            if (precent != newPrecent)
            {
                precentChanged = true;
                precent = newPrecent;
                lockList = new ReaderWriterLockSlim[(int)Math.Ceiling(1 / precent)];
                for (int i = 0; i < lockList.Length; i++) lockList[i] = new ReaderWriterLockSlim();
            }
            lockSize = (int)Math.Ceiling(nRows * nCols * precent);
            return precentChanged;

        }
        private List<ReaderWriterLockSlim> getLocksByRowOrCol(int index, bool colFlag, bool writerFlag)
        {
            if (index < 0 || (index >= nRows && !colFlag) || (index >= nCols && colFlag))
                throw new Exception("Index not valid.");

            List<ReaderWriterLockSlim> RCLocksList = new List<ReaderWriterLockSlim>();
            if (colFlag)
            {
                for (int i = 0; i < nRows; i++)
                {
                    ReaderWriterLockSlim tempLock = getLockByIndex(i, index, writerFlag);
                    if (!RCLocksList.Contains(tempLock)) RCLocksList.Add(tempLock);

                }
            }
            else
            {
                for (int i = 0; i < nCols; i++)
                {
                    ReaderWriterLockSlim tempLock = getLockByIndex(index, i, writerFlag);
                    if (!RCLocksList.Contains(tempLock)) RCLocksList.Add(tempLock);
                }
            }
            return RCLocksList;

        }

        private ReaderWriterLockSlim getLockByIndex(int rIndex, int cIndex, bool writerFlag)
        {
            if (rIndex < 0 || rIndex >= nRows || cIndex < 0 || cIndex >= nCols)
                throw new Exception("Index not valid.");

            if (nUsers != -1 && !writerFlag)
            {
                if (nUsers == Thread.VolatileRead(ref nUsersCount)) mutex.WaitOne();
                Interlocked.Increment(ref nUsersCount);
            }
            // rIndex and cIndex starts from 0 ( not 1).
            int lIndex = (int)((rIndex * nCols) + cIndex) / (int)lockSize;
            if (lIndex == lockList.Count()) lIndex -= 1;
            return lockList[lIndex];

        }

        private void releaseLock(ReaderWriterLockSlim myLock, bool writerFlag)
        {

            if (nUsers != -1 && !writerFlag)
            {
                if (nUsers == Thread.VolatileRead(ref nUsersCount)) mutex.ReleaseMutex();
                Interlocked.Decrement(ref nUsersCount);
            }

            if (writerFlag) myLock.ExitWriteLock();
            else myLock.ExitReadLock();
        }

        private String[,] To2D(String[][] source)
        {
            try
            {
                int FirstDim = source.Length;
                int SecondDim = source.GroupBy(row => row.Length).Single().Key; // throws InvalidOperationException if source is not rectangular

                String[,] result = new String[FirstDim, SecondDim];
                for (int i = 0; i < FirstDim; ++i)
                    for (int j = 0; j < SecondDim; ++j)
                        result[i, j] = source[i][j];

                return result;
            }
            catch (InvalidOperationException)
            {
                throw new InvalidOperationException("The given jagged array is not rectangular.");
            }
        }


    }


}
