using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace TAP_test
{
    class Program
    {
        static void Main(string[] args)
        {
            // https://docs.microsoft.com/en-us/dotnet/standard/parallel-programming/task-based-asynchronous-programming

            //EXAMPLE 001
            //ParallelInvoke();

            //EXAMPLE 002
            //CreatingAndRunningTasksExplicitly();

            //EXAMPLE 003
            //BasicTaskWithResult();

            //EXAMPLE 004
            //CreatingTaskContinuations();

            //EXAMPLE 005
            //CreatingChildTasks();

            //EXAMPLE 006
            //WaitingForTasksToFinish();

            //EXAMPLE 007
            //TaskFromResult();

            // https://docs.microsoft.com/en-us/dotnet/standard/parallel-programming/task-cancellation
            // https://docs.microsoft.com/en-us/dotnet/standard/parallel-programming/how-to-cancel-a-task-and-its-children [better example]

            //EXAMPLE 008 - approach to cancel single and/or child tasks monitoring the parent state
            TaskCancellation();
            //LD execution example.:
            /*
                Task 1 executing [SINGLE]
                Task 2 executing [EXTERNAL]
                - - - DoSomeWorkCancellation BEGIN for Task 1
                Task 3 executing [CHILD]
                - - - DoSomeWorkCancellation BEGIN for Task 2
                - - - DoSomeWorkCancellation BEGIN for Task 3
                - - - DoSomeWorkCancellation END for Task 1
                - - - DoSomeWorkCancellation END for Task 3
                - - - DoSomeWorkCancellation END for Task 2
                Task 4 executing [CHILD]
                - - - DoSomeWorkCancellation BEGIN for Task 2
                - - - DoSomeWorkCancellation BEGIN for Task 4
                - - - DoSomeWorkCancellation END for Task 2
                - - - DoSomeWorkCancellation END for Task 4
                Task 5 executing [CHILD]
                - - - DoSomeWorkCancellation BEGIN for Task 2
                - - - DoSomeWorkCancellation BEGIN for Task 5
                c
                Task cancellation requested.
                Task 5 status is now Running
                Task 4 status is now RanToCompletion
                Task 3 status is now RanToCompletion
                Task 2 status is now Running
                Task 1 status is now RanToCompletion
             */

            // https://docs.microsoft.com/en-us/dotnet/api/system.threading.tasks.task.fromresult?view=netcore-3.1
            //EXAMPLE 009
            TaskFromResult();
        }

        public static void ParallelInvoke()
        {
            //The Parallel.Invoke method provides a convenient way to run any number of arbitrary statements concurrently
            //we pass in an "action" delegate. Action -> Encapsulates a method that has no parameters and does not return a value.
            Parallel.Invoke(() =>
            {
                DoSomeWork();
            }, () => DoSomeOtherWork(3));
        }

        public static void CreatingAndRunningTasksExplicitly()
        {

            Thread.CurrentThread.Name = "LdThreadName";

            // Create a task and supply a user delegate by using a lambda expression.
            Task taskA = new Task(() => DoSomeWork());
            // Start the task.
            taskA.Start();

            // Task.Run methods to create and start a task in one operation. 
            Task taskB = Task.Run(() => DoSomeOtherWork(4));

            //You can also use the TaskFactory.StartNew method to create and start a task in one operation
            Task taskC = Task.Factory.StartNew(() => DoSomeOtherWork(5));

            // Output a message from the calling thread.
            Console.WriteLine("Hello from thread '{0}'.", Thread.CurrentThread.Name);
            taskA.Wait();

        }

        public static void BasicTaskWithResult()
        {
            //if instead of "Task<double>" I put "Task" it does not compile because the reference to the result is missing
            Task<double> taskD = Task<double>.Factory.StartNew(() => DoComputation(1000));
            double result = taskD.Result;

            Console.WriteLine("METHOD -> BasicTaskWithResult -> " + result.ToString());
        }

        /// <summary>
        /// The Task.ContinueWith and Task<TResult>.ContinueWith methods let you specify a task to start when the antecedent task finishes. 
        /// The delegate of the continuation task is passed a reference to the antecedent task so that it can examine the antecedent task's status and, 
        /// by retrieving the value of the Task<TResult>.Result property, can use the output of the antecedent as input for the continuation.
        /// </summary>
        public static void CreatingTaskContinuations()
        {

            Task<double> taskD = Task<double>.Factory.StartNew(() => DoComputation(1000));
            Console.WriteLine("METHOD -> CreatingTaskContinuations -> " + taskD.Result.ToString());

            Task<double> taskE = taskD.ContinueWith((x) => DoSomeOtherComputation(x.Result));
            Console.WriteLine("METHOD -> CreatingTaskContinuations - result continued task -> " + taskE.Result);

            //need to pass "x" even if not used because is a continuation
            //the result type is inferred from the return type of the lambda expression passed to the Task
            Task<string> taskF = taskE.ContinueWith((x) => DoSomeOtherWork(3));

        }

        /// <summary>
        ///  You can use the AttachedToParent option to express structured task parallelism, 
        ///  because the parent task implicitly waits for all attached child tasks to finish
        /// </summary>
        public static void CreatingChildTasks()
        {
            Func<String, int, bool> predicate = (str, index) => str.Length == index;
            var resultCallungAFunct = predicate("a string", 9);

            var parent = Task.Factory.StartNew(() => {
                Console.WriteLine("Parent task beginning.");
                for (int ctr = 0; ctr < 10; ctr++)
                {
                    int taskNo = ctr;

                    Task.Factory.StartNew((x) => { DoSomeWorkObject(x); },
                    taskNo,
                    TaskCreationOptions.AttachedToParent);
                }
            });

            parent.Wait();
            Console.WriteLine("Parent task completed.");

            /*
                Parent task beginning.
                Attached child #6 completed.
                Attached child #2 completed.
                Attached child #8 completed.
                Attached child #4 completed.
                Attached child #0 completed.
                Attached child #9 completed.
                Attached child #5 completed.
                Attached child #3 completed.
                Attached child #1 completed.
                Attached child #7 completed.
                Parent task completed.
             */
        }

        /// <summary>
        /// Typically, you would wait for a task for one of these reasons:
        /// The main thread depends on the final result computed by a task.
        /// You have to handle exceptions that might be thrown from the task.
        /// The application may terminate before all tasks have completed execution. For example, console applications will terminate as soon as all synchronous code in Main (the application entry point) has executed.
        /// </summary>
        public static void WaitingForTasksToFinish()
        {
            Task[] tasks = new Task[2]
                {
                    Task.Factory.StartNew(() => DoSomeWork()),
                    Task.Factory.StartNew(() => DoSomeOtherWork(5))
                };

            //Block until all tasks complete. IF I COMMENT THE LINE BELOW THE MAIN THREAD JUST DO NOT WAIT
            Task.WaitAll(tasks);

            // Continue on this thread...
            Console.WriteLine("Continue on this thread... ");
        }

        /// <summary>
        /// By using the Task.FromResult method, you can create a Task<TResult> object that holds a pre-computed result. 
        ///
        /// This method creates a Task<TResult> object whose Task<TResult>.Result property is result and whose Status property is RanToCompletion. 
        /// The method is commonly used when the return value of a task is immediately known without executing a longer code path. 
        /// The example provides an illustration.    
        /// </summary>
        public static void TaskFromResult()
        {

        }

        public static void TaskCancellation()
        {
            var tokenSource = new CancellationTokenSource();
            var token = tokenSource.Token;

            // Store references to the tasks so that we can wait on them and
            // observe their status after cancellation.
            Task t;
            var tasks = new ConcurrentBag<Task>();

            Console.WriteLine("Press any key to begin tasks...");
            Console.ReadKey(true);
            Console.WriteLine("To terminate the example, press 'c' to cancel and exit...");
            Console.WriteLine();

            // Request CANCELLATION OF A SINGLE TASK when the token source is canceled.
            // Pass the token to the user delegate, and also to the task so it can
            // handle the exception correctly.
            //  - - NOTE - - the "ID" is picked by the system in sequence. In all my tests this "ID" was 1
            t = Task.Run(() => DoSomeWorkCancellation(1, token), token);
            Console.WriteLine("Task {0} executing [SINGLE]", t.Id);
            tasks.Add(t);

            // Request CANCELLATION OF A TASKS AND ITS CHILDREN. Note the token is passed
            // to (1) the user delegate and (2) as the second argument(T001) to Task.Run, so
            // that the task instance can correctly handle the OperationCanceledException.
            t = Task.Run(() =>
            {
                // Create some cancelable child tasks.
                Task tc;
                for (int i = 3; i <= 6; i++)
                {
                    // For each child task, pass the same token
                    // to each user delegate and to Task.Run.
                    tc = Task.Run(() => DoSomeWorkCancellation(i, token), token);
                    Console.WriteLine("Task {0} executing [CHILD]", tc.Id);
                    tasks.Add(tc);

                    // Pass the same token again to do work on the parent task.
                    // All will be signaled by the call to tokenSource.Cancel below.
                    // LD WE DO THIS to monitor the state of the parent [EXTERNAL]
                    DoSomeWorkCancellation(2, token);
                }
            }, token); //(T001)

            Console.WriteLine("Task {0} executing [EXTERNAL]", t.Id);
            tasks.Add(t);

            // Request cancellation from the UI thread.
            char ch = Console.ReadKey().KeyChar;
            if (ch == 'c' || ch == 'C')
            {
                tokenSource.Cancel();
                Console.WriteLine("\nTask cancellation requested.");

                // Optional: Observe the change in the Status property on the task.
                // It is not necessary to wait on tasks that have canceled. However,
                // if you do wait, you must enclose the call in a try-catch block to
                // catch the TaskCanceledExceptions that are thrown. If you do
                // not wait, no exception is thrown if the token that was passed to the
                // Task.Run method is the same token that requested the cancellation.
            }

            try
            {
                Task.WhenAll(tasks.ToArray());
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine($"INDIDE CATCH \n{nameof(OperationCanceledException)} thrown\n");
            }
            finally
            {
                tokenSource.Dispose();
            }

            // Display status of all tasks.
            foreach (var task in tasks)
                Console.WriteLine("Task {0} status is now {1}", task.Id, task.Status);
        }



        #region Support Methods

        public static void DoSomeWork()
        {
            Console.WriteLine("METHOD -> DoSomeWork");
        }

        public static string DoSomeOtherWork(int aNumber)
        {
            Thread.SpinWait(100000000);
            Console.WriteLine("METHOD -> DoSomeOtherWork -> " + aNumber.ToString());
            return aNumber.ToString();
        }

        public static void DoSomeWorkObject(object o)
        {
            Thread.SpinWait(100000000);
            Console.WriteLine("Attached child #{0} completed.", o);
        }

        private static Double DoComputation(Double start)
        {

            return start + start;
        }

        private static Double DoSomeOtherComputation(Double start)
        {
            return start * 2;
        }

        static void DoSomeWorkCancellation(int taskNumToCancel, CancellationToken ct)
        {
            Console.WriteLine("- - - DoSomeWorkCancellation BEGIN for Task {0}", taskNumToCancel);
            // Was cancellation already requested?
            if (ct.IsCancellationRequested)
            {
                Console.WriteLine("Task {0} was cancelled before it got started.", taskNumToCancel);
                ct.ThrowIfCancellationRequested();
            }

            int maxIterations = 100;

            // NOTE!!! A "TaskCanceledException was unhandled
            // by user code" error will be raised here if "Just My Code"
            // is enabled on your computer. On Express editions JMC is
            // enabled and cannot be disabled. The exception is benign.
            // Just press F5 to continue executing your code.
            for (int i = 0; i <= maxIterations; i++)
            {
                // Do a bit of work. Not too much.
                var sw = new SpinWait();
                for (int j = 0; j <= 100; j++)
                    sw.SpinOnce();

                if (ct.IsCancellationRequested)
                {
                    //LD this notify with exception "ThrowIfCancellationRequested" that a Cancellation was requested 
                    //NOTE -> after disabling in debug setup "Just My Code", SOMETIME THE COMPILER DOES NOT GET IN HERE, SO WILL NOT SEE THIS MESSAGE AT CONSOLE, 
                    //for the rest it's working. I GUESS IT'S A BUG
                    Console.WriteLine("Task {0} cancelled [DO SOME WORK CANCELLATION]", taskNumToCancel);
                    ct.ThrowIfCancellationRequested();
                }
            }
            Console.WriteLine("- - - DoSomeWorkCancellation END for Task {0}", taskNumToCancel);
        }
        #endregion

    }
}
