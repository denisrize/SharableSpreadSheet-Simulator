using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharableSpreadSheet.Simulator
{
    internal class Simulator
    {

        public static void Main(String[] args)
        {
            int nRows = Int32.Parse(args[0]);
            int nCols = Int32.Parse(args[1]);
            int nThreads = Int32.Parse(args[2]);
            int nOperations = Int32.Parse(args[3]);
            int mssleep = Int32.Parse(args[4]);

            SharableSpreadSheet spreadSheet = new SharableSpreadSheet(nRows, nCols, nThreads);
            var waitHandles = new ManualResetEvent[nThreads];
            for (int i = 0; i < waitHandles.Length; i++) waitHandles[i] = new ManualResetEvent(false);

            for (int i = 0; i < nRows; i++)
            {
                for (int j = 0; j < nCols; j++)
                    spreadSheet.setCell(i, j, String.Format("Grade99Cell{0}{1}", i, j));
            }

            for (int i = 0; i < nThreads; i++)
            {
                new Thread((waitHandle) =>
                {
                    for (int k = 0; k < nOperations; k++)
                    {
                        doRandomOperation(spreadSheet, nRows, nCols);
                        Thread.Sleep(mssleep);
                    }
                   (waitHandle as ManualResetEvent).Set();
                }).Start(waitHandles[i]);
            }
            if (!WaitHandle.WaitAll(waitHandles, TimeSpan.FromSeconds(30)))
            {
                // timeout
            }


        }
        
        
        static private void doRandomOperation(SharableSpreadSheet spreadSheet, int nRows, int nCols)
        {
            Random rnd = new Random();
            int randomNum = rnd.Next(1, 12);
            int row = rnd.Next(nRows);
            int col = rnd.Next(nCols);
            int threadId = Thread.CurrentThread.ManagedThreadId;
            if (randomNum == 1)
            {
                String cell = spreadSheet.getCell(row, col);
                Console.WriteLine(String.Format("User [{0}]: string '{1}' found in cell[{2},{3}]", threadId, cell, row, col));
            }
            else if (randomNum == 2)
            {
                spreadSheet.setCell(row, col, "Grade 100");
                Console.WriteLine(String.Format("User [{0}]: string 'Grade 100' inserted to cell[{1},{2}].", threadId, row, col));
            }
            else if (randomNum == 3)
            {
                Tuple<int, int> result = spreadSheet.searchString("Grade 100");
                if (result == null) Console.WriteLine(String.Format("User [{0}]: String 'Grade 100' not in spread sheet.", threadId));
                else Console.WriteLine(String.Format("User[{0}]: String 'Grade 100' found in cell[{1},{2}].", threadId, result.Item1, result.Item2));

            }
            else if (randomNum == 4)
            {
                int row1 = rnd.Next(nRows);
                if (row1 != row) spreadSheet.exchangeRows(row, row1);
                else if (row1 > 0) spreadSheet.exchangeRows(row, row1 - 1);
                else spreadSheet.exchangeRows(row, row1 + 1);

                Console.WriteLine(String.Format("User [{0}]: rows {1} and {2} exchange successfully.", threadId, row, row1));

            }
            else if (randomNum == 5)
            {
                int col1 = rnd.Next(nCols);
                if (col != col1) spreadSheet.exchangeCols(col, col1);
                else if (0 < col1) spreadSheet.exchangeCols(col, col1 - 1);
                else spreadSheet.exchangeCols(col, col1 + 1);
                Console.WriteLine(String.Format("User [{0}]: rows {1} and {2} exchange successfully.", threadId, col, col1));
            }
            else if (randomNum == 6)
            {
                int result = spreadSheet.searchInRow(row, "Grade 100");
                if (result != -1) Console.WriteLine(String.Format("User[{0}]: String 'Grade 100' found in cell[{1},{2}].", threadId, row, result));
                else Console.WriteLine(String.Format("User[{0}]: String 'Grade 100' wasn't found in row {1}.", threadId, row));
            }
            else if (randomNum == 7)
            {
                int result = spreadSheet.searchInCol(col, "Grade 100");
                if (result != -1) Console.WriteLine(String.Format("User[{0}]: String 'Grade 100' found in col[{1},{2}].", threadId, result, col));
                else Console.WriteLine(String.Format("User[{0}]: String 'Grade 100' wasn't found in col {1}.", threadId, col));
            }
            else if (randomNum == 8)
            {
                int row2 = rnd.Next(row, nRows);
                int col2 = rnd.Next(col, nCols);
                Tuple<int, int> result = spreadSheet.searchInRange(col, col2, row, row2, "Grade 100");
                if (result != null) Console.WriteLine(String.Format("User[{0}]: String 'Grade 100' found in col[{1},{2}].", threadId, result.Item1, result.Item2));
            }
            else if (randomNum == 9)
            {
                spreadSheet.addRow(row);
                Console.WriteLine(String.Format("User[{0}]: a new row added after row {1}.", threadId, row));
            }
         
            else if (randomNum == 10)
            {
                bool caseSen = rnd.Next(2) == 1 ? true : false;
                Tuple<int, int>[] result = spreadSheet.findAll("Grade 100", caseSen);
                Console.WriteLine(String.Format("User[{0}]:The string 'Grade 100' appear {1} times at the spread sheet", threadId, result.Length));
            }
            else if (randomNum == 11)
            {
                bool caseSen = rnd.Next(2) == 1 ? true : false;
                spreadSheet.setAll("Grade 100", "Grade 110", caseSen);
                Console.WriteLine(String.Format("User[{0}]:The string 'Grade 100' changed successfully to 'Grade 110'.", threadId));
            }
            else
            {
                Tuple<int, int> size = spreadSheet.getSize();
                Console.WriteLine(String.Format("User[{0}]: Size of the spread sheet is {1} rows and {2} colmuns.", threadId, size.Item1, size.Item2));

            }
        }

    }
}
